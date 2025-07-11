using My2048;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(SaveLoad))]
public class TwentyFortyEight : MonoBehaviour {
    public int      TouchDeadZone           = 60;
    public float    TouchMaxHeightPercent   = 90;
    public int      winningNumber           = 2048;
    public float    tileLerpDuration        = 0.25f;
    
    public Sprite[] spriteTiles;
    public Text scoreText;
    public Text bestText;
    public Button undoButton;
    public SaveData saveData;
    public GameBoard board;

    uint bestScore;
    uint deltaScore;
    Vector2 startTouch;
    Vector2 swipeDelta;
    bool isUndoing;
    bool isMoving;
    MoveData move;
    Stack<Tile> tileStack;
    GamePhase phase = GamePhase.GETTING_INPUT;
    SwipeData currentSwipe;
    SaveLoad saveLoad;

    void Awake() {
        saveLoad = GetComponent<SaveLoad>();
    }

    void Start() {
        saveData                = saveLoad.Load(board, false);
        tileStack               = new Stack<Tile>(board.size);
        bestScore               = (uint)PlayerPrefs.GetInt("best");
        bestText.text           = bestScore.ToString();
        scoreText.text          = saveData.score.ToString();
        undoButton.interactable = saveData.canUndo;

        MoveData.Init(board.size);

        if(saveData.activeTileData == null) {
            // There was no save file.
            GameBoard.Create(board, null, false);
            board.SpawnRandomTile(false);
            board.SpawnRandomTile(false);
        }
    }

    void Update() {
        switch(phase) {
            case GamePhase.GETTING_INPUT:
                HandleInput();
                break;
            case GamePhase.LERPING_TILES:
                LerpTiles();
                break;
        }
    }

    public Sprite ValueToTile(uint value) {
        if(spriteTiles == null)
            return null;
        if(spriteTiles.Length < 1)
            return null;
        int i = 0;
        while(value > 1) {
            value /= 2;
            i++;
        }
        return spriteTiles[i];
    }

    void InitializeLerp() {
        foreach(var t in board.tiles) {
            InitLerpForTile(t);
        }

        foreach(var t in board.removedTiles) {
            InitLerpForTile(t);
        }
    }

    void InitLerpForTile(Tile t) {
        if(t == null) return;
        
        t.lerpData.timeStarted     = Time.time;
        t.lerpData.start           = t.transform.position;
        t.lerpData.lerpDuration    = tileLerpDuration;
        t.lerpData.t               = 0f;
    }

    void LerpTiles() {
        var isLerping = false;
        
        foreach(var t in board.tiles) {
            CheckLerp(ref isLerping, t);
        }
        
        foreach(var rt in board.removedTiles) {
            CheckLerp(ref isLerping, rt);
        }

        if(!isLerping) {
            // All tiles have completing their Lerping.
            foreach(var t in board.tiles) {
                if(!t) {
                    continue;
                }
                t.transform.position = t.lerpData.end;
                if(t.currentMove.merged) {
                    t.animator.SetTrigger("merge");
                }
                t.SetSprite();
            }

            foreach(var t in board.removedTiles) {
                if(!t) {
                    continue;
                }
                t.transform.position = t.lerpData.end;
                t.gameObject.SetActive(false);
                t.SetSprite();
            }

            OnTilesFinishedLerp();
        }
    }

    void CheckLerp(ref bool lerping, Tile t) {
        if(t && t.lerpData.t < 1) {
            t.Lerp();
            lerping = true;
        }
    }

    void HandleInput() {
        if(isMoving || isUndoing) {
            return;
        }
        #region Swipe Input
        if(Input.GetMouseButtonDown(0)) {
            if(IsTouchOverUIButton()) {
                return;
            }
            startTouch = Input.mousePosition;
        }
        else if(Input.GetMouseButtonUp(0)) {
            swipeDelta = (Vector2)Input.mousePosition - startTouch;
            TrySwipe();
        }

        if(Input.touchCount != 0) {
            Touch touch = Input.GetTouch(0);
            if(touch.phase == TouchPhase.Began) {
                if(IsTouchOverUIButton()) {
                    return;
                }
                startTouch = touch.position;
            }
            else if(touch.phase == TouchPhase.Ended) {
                swipeDelta = Vector2.zero;
                if(startTouch != Vector2.zero) {
                    swipeDelta = touch.position - startTouch;
                }
                TrySwipe();
                startTouch = swipeDelta = Vector2.zero;
            }
        }
        #endregion

        #if UNITY_EDITOR
        if(Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow)) {
            SwipeUp();
        }

        if(Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow)) {
            SwipeDown();
        }

        if(Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow)) {
            SwipeRight();
        }

        if(Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow)) {
            SwipeLeft();
        }

        if(Input.GetKeyUp(KeyCode.R)) {
            SceneManager.LoadScene(0);
        }

        if(Input.GetKeyDown(KeyCode.T)) {
            board.SpawnRandomTile(false);
        }

        if(Input.GetKeyUp(KeyCode.Z)) {
            if(board.spawnedTile)
                board.spawnedTile.Shrink();
        }
        #endif
    }

    public void TrySwipe() {
        if(startTouch == Vector2.zero) {
            return;
        }

        if(swipeDelta.magnitude > TouchDeadZone && (startTouch.y / Screen.height) < TouchMaxHeightPercent) {
            if(IsTouchOverUIButton()) {
                return;
            }
            float x = swipeDelta.x;
            float y = swipeDelta.y;
            if(Mathf.Abs(x) > Mathf.Abs(y)) {
                // left or right
                if(x < 0) {
                    SwipeLeft();
                }
                else {
                    SwipeRight();
                }
            }
            else if(y > 0) {
                // up or down
                SwipeUp();
            }
            else {
                SwipeDown();
            }
            startTouch = swipeDelta = Vector2.zero;
        }
    }

    List<RaycastResult> results = new List<RaycastResult>(2);
    public bool IsTouchOverUIButton() {
        results.Clear();
        var p = new PointerEventData(EventSystem.current) {
            position = Input.mousePosition
        };
        EventSystem.current.RaycastAll(p, results);
        return results.Count > 0;
        #if !UNITY_EDITOR && !UNITY_STANDALONE
        return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
        #else
        return EventSystem.current.IsPointerOverGameObject();
        #endif
    }

    public void OnSpawnedTileShrunk() {
        board[board.spawnedTile.index] = null;
        board.AddTileToPool(board.spawnedTile);
        board.spawnedTile = null;
        Undo();
    }

    void Undo() {
        if(isUndoing) return;
        if(!saveData.canUndo) return;
        isUndoing = true;
        saveData.canUndo = false;
        var move = new MoveData();
        switch(saveData.previousSwipe.direction) {
            case Direction.TOP_TO_BOTTOM:
                move = saveData.previousSwipe.invert ? MoveData.UpData : MoveData.DownData;
                break;
            case Direction.LEFT_TO_RIGHT:
                move = saveData.previousSwipe.invert ? MoveData.LeftData : MoveData.RightData;
                break;
        }
        for(int i = 0; i < board.size; i++) {
            int x = move.startX + i * move.xRowShift;
            int y = move.startY + i * move.yRowShift;
            for(int j = 0; j < board.size; j++) {
                Tile t = board[x, y];
                if(!t) {
                    x += move.xDir;
                    y += move.yDir;
                    continue;
                }

                board[x, y] = null;
                
                RestoreTile(t);

                if(!t.currentMove.merged) {
                    x += move.xDir;
                    y += move.yDir;
                    continue;
                }

                Tile r                                  = board.removedTiles[board.GetOtherIFromIndex(t)];
                board[r.currentMove.index]              = r;
                r.lerpData.end                          = board.GetWorldPos(r.currentMove.index);
                board.removedTiles[board.GetIFromIndex(r)] = null;
                r.Undo();
                t.ResetFlagsAndIndex();
                r.ResetFlagsAndIndex();
                r.index                                 = r.currentMove.index;
            }
        }
        InitializeLerp();
        phase = GamePhase.LERPING_TILES;
    }

    void RestoreTile(Tile t) {
        t.Undo();
        t.index = t.currentMove.index;
        board[t.currentMove.index] = t;
        t.lerpData.end = board.GetWorldPos(t.currentMove.index);
    }

    public void OnTilesFinishedLerp() {
        if(isUndoing) {
            isUndoing             = false;
            saveData.score      = saveData.previousScore;
            scoreText.text      = saveData.score.ToString();
        }
        else {
            board.spawnedTile   = board.SpawnRandomTile(true);
            saveData.score      += deltaScore;
            deltaScore          = 0;

            if(saveData.score > bestScore) {
                bestScore       = saveData.score;
                bestText.text   = bestScore.ToString();
            }

            scoreText.text      = saveData.score.ToString();
            CheckGameStatus();
        }

        isMoving                  = false;
        undoButton.interactable = saveData.canUndo;
        saveLoad.Save(this);
        
        phase                   = GamePhase.GETTING_INPUT;
    }

    void CheckGameStatus() {
        SaveScores();
        var isBoardFull = true;
        foreach(var tile in board.tiles) {
            if(!tile) {
                isBoardFull = false;
                continue;
            }
            if(tile.value >= winningNumber) {
                OnGameWon();
                return;
            }
        }

        if(isBoardFull) {
            if(!CanAMoveBeMade())
                OnGameOver();
        }
    }

    void SaveScores() {
        PlayerPrefs.SetInt("best", (int)bestScore);
    }

    void OnGameWon() {
    }

    void OnGameOver() {
    }

    void MoveTiles() {
        if(isMoving) {
            return;
        }
        if(!CanAMoveBeMade()) {
            return;
        }
        saveData.previousSwipe  = currentSwipe;
        saveData.previousScore  = saveData.score;
        isMoving                  = true;
        saveData.canUndo        = true;
        board.ClearRemovedTiles();
        for(int i = 0; i < board.size; i++) {
            int x = move.startX + i * move.xRowShift;
            int y = move.startY + i * move.yRowShift;
            for(int j = 0; j < board.size; j++) {
                Tile t = board[x, y];
                if(t) {
                    tileStack.Push(t);
                    t.ResetFlagsAndIndex();
                    board[x, y] = null;
                }
                x += move.xDir;
                y += move.yDir;
            }

            x = move.endX + i * move.xRowShift;
            y = move.endY + i * move.yRowShift;
            Tile prevTile = null;
            int tileCount = tileStack.Count;
            for(int j = 0; j < tileCount; j++) {
                Tile tile = tileStack.Pop();

                if(CanMerge(prevTile, tile)) {
                    prevTile.MergeWith(tile);
                    deltaScore                                      += prevTile.value;
                    tile.currentMove.index                          = tile.index;
                    board.removedTiles[board.GetIFromIndex(tile)]   = tile;
                    tile.lerpData.end                               = prevTile.lerpData.end;

                    Tile.DebugSetGameObjectName(prevTile);
                    continue;
                }

                tile.currentMove.index  = tile.index;
                tile.index              = new Index(x, y);
                board[tile.index]       = tile;
                tile.lerpData.end       = board.GetWorldPos(x, y);
                prevTile                = tile;
                x                       -= move.xDir;
                y                       -= move.yDir;

                Tile.DebugSetGameObjectName(tile);
            }
        }
        InitializeLerp();
        phase = GamePhase.LERPING_TILES;
    }

    static bool CanMerge(in Tile prevTile, in Tile tile) {
        return prevTile && prevTile.value == tile.value && !prevTile.currentMove.merged && !prevTile.currentMove.removed && !tile.currentMove.removed && !tile.currentMove.merged;
    }

    bool CanAMoveBeMade() {
        int count = board.tiles.Count;
        for(int i = 0; i < count; i++) {
            Tile t = board.tiles[i];
            if(CanMoveOrMergeTile(t)) {
                return true;
            }
        }
        return false;
    }

    bool CanMoveOrMergeTile(in Tile tile) {
        if(!tile) {
            return false;
        }

        if(tile.currentMove.removed) {
            return false;
        }

        Tile query = board.GetNextTile(tile, currentSwipe);
        if(query && tile.value == query.value) {
            return true;
        }
        
        Index i = board.GetNextEmptyIndex(tile, currentSwipe);
        return i.x != -1 && i.y != -1;
    }

    public void SwipeUp() {
        currentSwipe.direction  = Direction.TOP_TO_BOTTOM;
        currentSwipe.invert     = true;
        move                    = MoveData.UpData;
        MoveTiles();
    }

    public void SwipeDown() {
        currentSwipe.direction  = Direction.TOP_TO_BOTTOM;
        currentSwipe.invert     = false;
        move                    = MoveData.DownData;
        MoveTiles();
    }

    public void SwipeLeft() {
        currentSwipe.direction  = Direction.LEFT_TO_RIGHT;
        currentSwipe.invert     = false;
        move                    = MoveData.RightData;
        MoveTiles();
    }

    public void SwipeRight() {
        currentSwipe.direction  = Direction.LEFT_TO_RIGHT;
        currentSwipe.invert     = true;
        move                    = MoveData.LeftData;
        MoveTiles();
    }

    public void NewGamePressed() {
        saveData.score          = 0;
        saveData.previousScore  = 0;
        saveData.canUndo        = false;
        isMoving                = false;
        isUndoing               = false;

        for(int i = 0; i < board.length; i++) {
            Tile t = board.tiles[i];
            if(!t) {
                continue;
            }
            t.Reset();
            board.tiles[i] = null;

            t = board.removedTiles[i];
            if(!t) {
                continue;
            }
            t.Reset();
            board.removedTiles[i] = null;
        }
        saveLoad.InitGame(board, true);
    }

    public void UndoPressed() {
        if(isUndoing) return;
        if(board.spawnedTile)
            board.spawnedTile.Shrink();
    }
}