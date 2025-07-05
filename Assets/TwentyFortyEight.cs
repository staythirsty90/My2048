using My2048;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum Direction {
    TOP_TO_BOTTOM, LEFT_TO_RIGHT
}

public enum Process {
    GETTING_INPUT,
    LERPING_TILES,
    UNDOING
}

[System.Serializable]
public struct SwipeData {
    public Direction direction;
    public bool invert;
}

[System.Serializable]
public struct TileData {
    public uint value;
    public Index index;
    public Index oldIndex;
    public Index otherTileIndex;
    public bool merged;
    public bool spawned;

    public TileData(uint Value, Index index, Index oldIndex, Index otherIndex, bool Merged, bool Spawned) {
        value = Value;
        this.index = index;
        this.oldIndex = oldIndex;
        otherTileIndex = otherIndex;
        merged = Merged;
        spawned = Spawned;
    }

    public static void FillTileData(ref TileData[] tileDatas, in List<Tile> tiles) {
        tileDatas = new TileData[tiles.Capacity];
        for(int i = 0; i < tileDatas.Length; i++) {
            tileDatas[i] = MakeTileData(tiles[i]);
        }
    }

    public static void FillTileDataRemoved(ref TileData[] removedTileDatas, in List<Tile> removedTiles) {
        removedTileDatas = new TileData[removedTiles.Capacity];
        for(int i = 0; i < removedTileDatas.Length; i++) {
            removedTileDatas[i] = MakeTileDataRemoved(removedTiles[i]);
        }
    }

    public static TileData MakeTileData(in Tile t) {
        if(t) {
            return new TileData(t.value, t.index, t.currentMove.index, t.otherTileIndex, t.currentMove.merged, t.currentMove.spawnedFromMove);
        }
        return Empty;
    }

    public static TileData MakeTileDataRemoved(in Tile rt) {
        // TODO: Do we need to make sure that the Tile is actually 'removed'?
        if(rt) {
            return new TileData(rt.value, rt.index, rt.currentMove.index, rt.otherTileIndex, false, false);
        }
        return Empty;
    }

    public static TileData Empty => new TileData(0, Index.Invalid, Index.Invalid, Index.Invalid, false, false);
}

[RequireComponent(typeof(SaveLoad))]
public class TwentyFortyEight : MonoBehaviour {
    public int TouchDeadZone = 60;
    public float TouchMaxHeightPercent = 90;
    public int winningNumber = 2048;
    public Sprite[] spriteTiles;
    public Text scoreText;
    public Text bestText;
    public Button undoButton;
    public GameData gameData;
    uint bestScore;
    uint deltaScore;
    Vector2 startTouch;
    Vector2 swipeDelta;
    bool undoing;
    bool moving;
    public GameBoard board;
    MoveData move;
    Stack<Tile> stack;
    Process state = Process.GETTING_INPUT;
    SwipeData currentSwipe;
    SaveLoad saveLoad;

    void Awake() {
        saveLoad = GetComponent<SaveLoad>();
    }

    void Start() {
        gameData                = saveLoad.Load(board, false);
        stack                   = new Stack<Tile>(board.size);
        bestScore               = (uint)PlayerPrefs.GetInt("best");
        bestText.text           = bestScore.ToString();
        scoreText.text          = gameData.score.ToString();
        undoButton.interactable = gameData.canUndo;

        if(gameData.activeTileData == null) {
            // There was no save file.
            GameBoard.Create(board, null, false);
            board.SpawnRandomTile(false);
            board.SpawnRandomTile(false);
        }
    }

    void Update() {
        switch(state) {
            case Process.GETTING_INPUT:
                HandleInput();
                break;
            case Process.LERPING_TILES:
                LerpTiles();
                break;
            case Process.UNDOING:
                break;
            default:
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
            if(!t) {
                continue;
            }
            t.lerpData = new LerpData<Vector2>() {
                timeStarted     = Time.time,
                start           = t.transform.position,
                end             = t.nextPosition,
                lerpDuration    = 0.25f,
                t               = 0f,
            };
        }

        foreach(var t in board.removedTiles.list) {
            if(!t) {
                continue;
            }
            t.lerpData = new LerpData<Vector2>() {
                timeStarted     = Time.time,
                start           = t.transform.position,
                end             = t.nextPosition,
                lerpDuration    = 0.25f,
                t               = 0f,
            };
        }
    }

    void LerpTiles() {
        var lerping = false;
        foreach(var t in board.tiles) {
            if(!t) {
                continue;
            }
            if(t.lerpData.t < 1) {
                t.Lerp();
                lerping = true;
            }
        }
        foreach(var t in board.removedTiles.list) {
            if(!t) {
                continue;
            }
            if(t.lerpData.t < 1) {
                t.Lerp();
                lerping = true;
            }
        }
        if(!lerping) {
            foreach(var t in board.tiles) {
                if(!t) {
                    continue;
                }
                t.transform.position = t.nextPosition;
                if(t.currentMove.merged) {
                    t.animator.SetTrigger("merge");
                }
                t.SetSprite();
            }
            foreach(var t in board.removedTiles.list) { // i need this because i remove the tiles and put them into another list...
                if(!t) {
                    continue;
                }
                t.transform.position = t.nextPosition;
                t.gameObject.SetActive(false);
                t.SetSprite();
            }
            OnTilesFinishedMoving();
            state = Process.GETTING_INPUT;
        }
    }

    void HandleInput() {
        if(moving || undoing) {
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
            OnSwipeUp();
        }

        if(Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow)) {
            OnSwipeDown();
        }

        if(Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow)) {
            OnSwipeRight();
        }

        if(Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow)) {
            OnSwipeLeft();
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
                    OnSwipeLeft();
                }
                else {
                    OnSwipeRight();
                }
            }
            else if(y > 0) {
                // up or down
                OnSwipeUp();
            }
            else {
                OnSwipeDown();
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
        if(undoing) return;
        if(!gameData.canUndo) return;
        undoing = true;
        gameData.canUndo = false;
        var move = new MoveData();
        switch(gameData.previousSwipe.direction) {
            case Direction.TOP_TO_BOTTOM:
                move = gameData.previousSwipe.invert ? board.upData : board.downData;
                break;
            case Direction.LEFT_TO_RIGHT:
                move = gameData.previousSwipe.invert ? board.leftData : board.rightData;
                break;
        }
        for(int i = 0; i < board.size; i++) {
            int x = move.startX + i * move.xRowShift;
            int y = move.startY + i * move.yRowShift;
            for(int j = 0; j < board.size; j++) {
                Tile t = board[x, y];
                if(t) {
                    t.Undo();
                    board[x, y]                 = null;
                    t.index                     = t.currentMove.index;
                    board[t.currentMove.index]  = t;
                    t.nextPosition              = board.GetWorldPos(t.currentMove.index);
                    if(!t.currentMove.merged) {
                        x += move.xDir;
                        y += move.yDir;
                        continue;
                    }
                    Tile r = board.removedTiles[t.otherTileIndex];
                    board[r.currentMove.index] = r;
                    r.nextPosition = board.GetWorldPos(r.currentMove.index);
                    board.removedTiles[r.currentMove.index] = null;
                    r.Undo();
                    t.Clean();
                    r.Clean();
                    r.index = r.currentMove.index;
                }
                x += move.xDir;
                y += move.yDir;
            }
        }
        InitializeLerp();
        state = Process.LERPING_TILES;
    }

    public void OnTilesFinishedMoving() {
        if(undoing) {
            undoing = false;
            gameData.score = gameData.previousScore;
        }
        else if(!undoing) {
            board.spawnedTile = board.SpawnRandomTile(true);
            gameData.score += deltaScore;
            deltaScore = 0;
            if(gameData.score > bestScore) {
                bestScore = gameData.score;
            }
            CheckGameStatus();
        }
        moving = false;
        if(!undoing) {
            scoreText.text = gameData.score.ToString();
            bestText.text = bestScore.ToString();
        }
        else {
            scoreText.text = gameData.previousScore.ToString();
        }
        undoButton.interactable = gameData.canUndo;
        saveLoad.Save(this);
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
        if(moving) {
            return;
        }
        if(!CanAMoveBeMade()) {
            return;
        }
        gameData.previousSwipe  = currentSwipe;
        gameData.previousScore  = gameData.score;
        moving                  = true;
        gameData.canUndo        = true;
        board.ClearRemovedTiles();
        for(int i = 0; i < board.size; i++) {
            int x = move.startX + i * move.xRowShift;
            int y = move.startY + i * move.yRowShift;
            for(int j = 0; j < board.size; j++) {
                Tile t = board[x, y];
                if(t) {
                    stack.Push(t);
                    t.Clean();
                    board[x, y] = null;
                }
                x += move.xDir;
                y += move.yDir;
            }

            int tileCount = stack.Count;
            x = move.endX + i * move.xRowShift;
            y = move.endY + i * move.yRowShift;
            Tile prevTile = null;
            for(int j = 0; j < tileCount; j++) {
                Tile tile = stack.Pop();
                
                if(prevTile && prevTile.value == tile.value && !prevTile.currentMove.merged && !prevTile.currentMove.removed && !tile.currentMove.removed && !tile.currentMove.merged) {
                    prevTile    += tile;
                    deltaScore  += prevTile.value;
                    tile.currentMove.index = tile.index;
                    board[tile.currentMove.index] = null;
                    board.removedTiles[tile.currentMove.index] = tile;
                    Tile.DebugSetGameObjectName(prevTile);
                    tile.nextPosition = prevTile.nextPosition;
                    continue;
                }
                
                tile.currentMove.index  = tile.index;
                tile.index              = new Index(x, y);
                board[tile.index]       = tile;
                tile.nextPosition       = board.GetWorldPos(x, y);
                prevTile                = tile;
                x                       -= move.xDir;
                y                       -= move.yDir;
                Tile.DebugSetGameObjectName(tile);
            }
        }
        InitializeLerp();
        state = Process.LERPING_TILES;
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

    bool CanMoveOrMergeTile(Tile tile) {
        if(!tile) {
            return false;
        }

        if(tile.currentMove.removed) {
            return false;
        }

        Tile query = board.GetNextTile(tile, currentSwipe);
        if(query) {
            if(tile.value == query.value) return true;
        }
        Index i = board.GetNextEmptyIndex(tile, currentSwipe);
        if(i.x != -1 && i.y != -1) {
            return true;
        }
        return false;
    }

    public void OnSwipeUp() {
        currentSwipe.direction = Direction.TOP_TO_BOTTOM;
        currentSwipe.invert = true;
        move = board.upData;
        MoveTiles();
    }

    public void OnSwipeDown() {
        currentSwipe.direction = Direction.TOP_TO_BOTTOM;
        currentSwipe.invert = false;
        move = board.downData;
        MoveTiles();
    }

    public void OnSwipeLeft() {
        currentSwipe.direction = Direction.LEFT_TO_RIGHT;
        currentSwipe.invert = false;
        move = board.rightData;
        MoveTiles();
    }

    public void OnSwipeRight() {
        currentSwipe.direction = Direction.LEFT_TO_RIGHT;
        currentSwipe.invert = true;
        move = board.leftData;
        MoveTiles();
    }

    public void NewGamePressed() {
        gameData.score          = 0;
        gameData.previousScore  = 0;
        gameData.canUndo        = false;
        moving                  = false;
        undoing                 = false;

        for(int i = 0; i < board.length; i++) {
            Tile t = board.tiles[i];
            if(!t) {
                continue;
            }
            t.Reset();
            t.gameObject.SetActive(false);
            board.tiles[i] = null;

            t = board.removedTiles.list[i];
            if(!t) {
                continue;
            }
            t.Reset();
            t.gameObject.SetActive(false);
            board.removedTiles.list[i] = null;
        }
        saveLoad.InitGame(board, true);
    }

    public void UndoPressed() {
        if(undoing) return;
        if(board.spawnedTile)
            board.spawnedTile.Shrink();
    }
}
