using My2048;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TwentyFortyEight : MonoBehaviour {
    public int      TouchDeadZone           = 60;
    public float    TouchMaxHeightPercent   = 90;
    public int      winningNumber           = 2048;
    public float    tileLerpDuration        = 0.25f;
    
    public Sprite[] spriteTiles;
    public Text scoreText;
    public Text bestText;
    public Button undoButton;
    public GameState gameState;
    public GameBoard board;
    public bool IsUndoing { get; private set; }
    public bool IsMoving { get; /*private*/ set; }

    uint bestScore;
    uint deltaScore;

    public Stack<Tile> tileStack;
    GamePhase phase;

    public MyRingBuffer<TileData> RingBuffer = new MyRingBuffer<TileData>();

    [SerializeField] int targetFrameRate = 31;
    [SerializeField] Vector2Int targetResolution = new Vector2Int(720, 720);

    void Awake() {
        Application.targetFrameRate = targetFrameRate;
        Screen.SetResolution(targetResolution.x, targetResolution.y, true);
    }

    void Start() {
        MoveData.Init(board.size); // TODO: Assuming board exists, since its public and Serialized.

        if(SaveLoad.TryLoad(out gameState)) {
            board.Load(gameState);
        }
        else {
            // Couldn't load the save file.
            CreateBoardAndStartingTiles();
        }

        tileStack               = new Stack<Tile>(board.size);
        bestScore               = (uint)PlayerPrefs.GetInt("best");
        bestText.text           = bestScore.ToString();
        scoreText.text          = gameState.score.ToString();
        //undoButton.interactable = gameState.canUndo;
        undoButton.interactable = CanUndo();
    }

    void Update() {
        switch(phase) {
            case GamePhase.GETTING_INPUT:

                var didMove = false;
                var moveDir = MyInput.GetInputDirection(game: this);

                if(moveDir == Vector2.up && MoveTiles(MoveData.Up)) {
                    didMove = true;
                }
                else if(moveDir == Vector2.down && MoveTiles(MoveData.Down)) {
                    didMove = true;
                }
                else if(moveDir == Vector2.right && MoveTiles(MoveData.Right)) {
                    didMove = true;
                }
                else if(moveDir == Vector2.left && MoveTiles(MoveData.Left)) {
                    didMove = true;
                }

                if(didMove) {
                    BeginLerpPhase();
                }

                break;

            case GamePhase.LERPING_TILES:
                LerpTiles();
                break;
        }
    }

    void CreateBoardAndStartingTiles() {
        board.CreateBoardAndStartingTiles();

        PushTileStates();
    }

    List<TileData> tileDatasTempBuffer = new List<TileData>(16);

    void PushTileStates() {
        // Push the state before moving the Tiles. Why exactly is this required?

        // NOTE:
        // We cannot push the board.tiles because they are of different Types (Tile & TileData) ...
        tileDatasTempBuffer.Clear();
        var count = board.tiles.Count;
        for(int i = 0; i < count; i++) {
            var t = board.tiles[i];
            if(t) {
                tileDatasTempBuffer.Add(t.CurrentMove);
            }
        }
        if(tileDatasTempBuffer.Count > 0) {
            RingBuffer.Push(tileDatasTempBuffer);
        }
    }

    void ResetTileFlags() {
        for(int i = 0; i < board.tiles.Count; i++) {
            var t = board.tiles[i];
            if(t) {
                t.CurrentMove.removed = false;
                t.CurrentMove.merged = false;
                t.CurrentMove.spawnedFromMove = false;
            }
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

    void BeginLerpPhase() {

        foreach(var t in board.TilePool) {
            Tile.InitLerp(t, tileLerpDuration);
        }

        phase = GamePhase.LERPING_TILES;
    }

    void LerpTiles() {
        var isLerping = false;
        
        foreach(var t in board.TilePool) {
            CheckLerp(ref isLerping, t);
        }

        if(!isLerping) {
            OnTilesFinishedLerp();
        }
    }

    void CheckLerp(ref bool lerping, in Tile t) {
        if(t && t.gameObject.activeInHierarchy && t.lerpData.t < 1) {
            t.Lerp();
            lerping = true;
        }
    }
    
    public void OnSpawnedTileShrunk() {
        Tile.DebugSetGameObjectName(board.spawnedTile);
        board[board.spawnedTile.CurrentMove.index]  = null;
        board.spawnedTile                           = null;
        Undo();
    }

    bool CanUndo() {
        return RingBuffer.head > 2; // TODO: Magic number 2
    }

    void Undo() {
        if(IsUndoing) {
            return;
        }

        if(!CanUndo()) {
            Debug.Log("Cannot Undo!");
            return;
        }

        IsUndoing               = true;
        undoButton.interactable = false;
        //gameState.canUndo   = false;

        // Deactivate and null all Tiles currently in play.
        for(int i = 0; i < board.tiles.Count; i++) {
            if(board.tiles[i]) {
                board.tiles[i].gameObject.SetActive(false);
                board.tiles[i] = null;
            }
        }

        DeactiveAllTiles(); // TODO: Do we still need this now that I've fixed
                            // overwriting board Tiles during Undo?

        var bufferLengthIdx = RingBuffer.bufferLengths.Count-2;
        var bufferLength    = RingBuffer.bufferLengths[bufferLengthIdx];
        
        for(int i = RingBuffer.head - bufferLength; i < RingBuffer.head; i++) {
            
            var td   = RingBuffer.buffer[i];
            var tile = board.GetTileFromPool(isUndoing: true); // TODO: assert we found a Tile GameObject. 

            if(td.merged) {
                td.value /= 2;
            }

            var temp    = td.index;
            td.index    = td.indexEnd;
            td.indexEnd = temp;
            
            tile.CurrentMove = td;

            tile.transform.position = new Vector3(td.index.x, td.index.y, tile.transform.position.z);
            tile.lerpData.end = new Vector2(td.indexEnd.x, td.indexEnd.y);

            tile.SetSprite();

            if(td.spawnedFromMove) {
                tile.gameObject.SetActive(false);
            }

            Tile.DebugSetGameObjectName(tile);
        }

        // Remove latest Undo state.
        RingBuffer.bufferLengths.RemoveAt(bufferLengthIdx);
        RingBuffer.buffer.RemoveRange(RingBuffer.head - bufferLength, bufferLength);
        RingBuffer.head = RingBuffer.buffer.Count;

        BeginLerpPhase();
    }

    void OnTilesFinishedLerp() {
        foreach(var t in board.TilePool) {
            Tile.TileEndLerp(t, IsUndoing);
        }

        if(IsUndoing) {

            DeactiveAllTiles();

            Tile hasSpawned     = null;
            var bufferLengthIdx = RingBuffer.bufferLengths.Count - 2;
            var bufferLength    = RingBuffer.bufferLengths[bufferLengthIdx];

            for(int i = RingBuffer.head - bufferLength; i < RingBuffer.head; i++) {

                var td   = RingBuffer.buffer[i];
                var tile = board.GetTileFromPool(IsUndoing); // TODO: assert we found a Tile GameObject. 

                if(td.spawnedFromMove) {
                    hasSpawned = tile;
                }

                if(td.removed) {
                    tile.gameObject.SetActive(false);
                }
                else {
                    // Write non-removed Tiles to the board again.
                    var idx = td.indexEnd;
                    if(board[idx]) {
                        throw new System.Exception ($"Rewriting an existing Tile at Index ({idx.x},{idx.y})! This should NOT happen!");
                    }
                    board[idx] = tile;
                }

                tile.CurrentMove        = td;
                tile.transform.position = new Vector3(td.indexEnd.x, td.indexEnd.y);
                tile.SetSprite();
                Tile.DebugSetGameObjectName(tile);
            }

            board.spawnedTile       = hasSpawned;
            IsUndoing               = false;
            gameState.score         = gameState.previousScore;
            scoreText.text          = gameState.score.ToString();
        }
        else {
            board.spawnedTile   = board.SpawnRandomTile(spawnedFromMove: IsMoving);
            
            PushTileStates();

            gameState.score     += deltaScore;
            deltaScore          = 0;

            if(gameState.score > bestScore) {
                bestScore       = gameState.score;
                bestText.text   = bestScore.ToString();
            }

            scoreText.text      = gameState.score.ToString();
            PlayerPrefs.SetInt("best", (int)bestScore);
            CheckForWinOrLose();
        }

        IsMoving                = false;
        //undoButton.interactable = gameState.canUndo;
        undoButton.interactable = CanUndo();
        phase                   = GamePhase.GETTING_INPUT;
        
        SaveLoad.Save(game: this);
    }

    private void DeactiveAllTiles() {
        // Using TilePool because board.tiles could be all null.
        foreach(var tile in board.TilePool) {
            tile.gameObject.SetActive(false);
            // NOTE HACK-ish:
            // Restoring scale and rotation due to Tile's shrink animation.
            tile.transform.localScale   = Vector3.one;
            tile.transform.rotation     = Quaternion.identity; 
        }
    }

    void CheckForWinOrLose() {
        foreach(var tile in board.tiles) {
            if(tile && tile.CurrentMove.value >= winningNumber) {
                GameWon();
                return;
            }
        }

        if(!CanMoveAtAll()) {
            GameOver();
        }
    }

    void GameWon() {
    }

    void GameOver() {
        Debug.Log("GameOver!");
    }

    /// <summary>
    /// Attempt to move the Tiles in the given MoveData direction. If the Tiles cannot move, this method returns false, otherwise it returns true.
    /// </summary>
    /// <param name="move">The direction for the tiles to move to.</param>
    /// <returns>Whether or not the tiles were moved.</returns>
    public bool MoveTiles(in MoveData move) {
        if(IsMoving) {
            return false;
        }

        if(!CanMove(move.swipe)) {
            return false;
        }

        ResetTileFlags();

        gameState.previousSwipe  = move.swipe;
        gameState.previousScore  = gameState.score;
        IsMoving                 = true;
        //gameState.canUndo        = true;

        for(int i = 0; i < board.size; i++) {
            int x = move.startX + i * move.xRowShift;
            int y = move.startY + i * move.yRowShift;
            for(int j = 0; j < board.size; j++) {
                Tile t = board[x, y];
                if(t) {
                    tileStack.Push(t);
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

                    // Merge prevTile and flag Tile to be Removed.
                    prevTile.CurrentMove.value += prevTile.CurrentMove.value;

                    prevTile.CurrentMove = new TileData {
                        index           = prevTile.CurrentMove.index,
                        indexEnd        = prevTile.CurrentMove.indexEnd,
                        merged          = true,
                        removed         = false,
                        spawnedFromMove = false,
                        value           = prevTile.CurrentMove.value,
                    };

                    tile.CurrentMove = new TileData {
                        index           = tile.CurrentMove.indexEnd,
                        indexEnd        = prevTile.CurrentMove.indexEnd,
                        merged          = false,
                        removed         = true,
                        spawnedFromMove = tile.CurrentMove.spawnedFromMove,
                        value           = tile.CurrentMove.value,
                    };

                    // NOTE: We have push the Removed tile, because PushTileState uses tiles that are on the board, only.
                    // Removed tiles won't be on the board, and will be missed....
                    RingBuffer.Push(tile.CurrentMove);

                    Tile.DebugSetGameObjectName(tile);

                    deltaScore          += prevTile.CurrentMove.value;
                    tile.lerpData.end   = prevTile.lerpData.end;

                    Tile.DebugSetGameObjectName(prevTile);
                    continue;
                }

                var newIndex = new Index(x, y);
                tile.CurrentMove = new TileData {
                    index           = tile.CurrentMove.indexEnd,
                    indexEnd        = newIndex,
                    merged          = false,
                    removed         = false,
                    spawnedFromMove = false,
                    value           = tile.CurrentMove.value,
                };

                board[tile.CurrentMove.indexEnd]    = tile;
                tile.lerpData.end                   = board.GetWorldPos(x, y);
                prevTile                            = tile;
                x                                   -= move.xDir;
                y                                   -= move.yDir;

                Tile.DebugSetGameObjectName(tile);
            }
        }
        return true;
    }

    static bool CanMerge(in Tile prevTile, in Tile tile) {
        return prevTile && prevTile.CurrentMove.value == tile.CurrentMove.value && !prevTile.CurrentMove.merged && !prevTile.CurrentMove.removed && !tile.CurrentMove.removed && !tile.CurrentMove.merged;
    }

    bool CanMove(in SwipeData swipeData) {
        int count = board.tiles.Count;
        for(int i = 0; i < count; i++) {
            if(CanTileMoveOrMerge(board.tiles[i], swipeData)) {
                return true;
            }
        }
        return false;
    }
    
    bool CanMoveAtAll() {
        return
            CanMove(MoveData.GetSwipeUp)   ||
            CanMove(MoveData.GetSwipeDown) ||
            CanMove(MoveData.GetSwipeLeft) ||
            CanMove(MoveData.GetSwipeRight);
    }

    bool CanTileMoveOrMerge(in Tile tile, in SwipeData swipeData) {
        if(!tile) {
            return false;
        }

        Tile query = board.GetNextTile(tile, swipeData);
        if(query && tile.CurrentMove.value == query.CurrentMove.value) {
            return true;
        }
        
        Index i = board.GetNextEmptyIndex(tile, swipeData);
        return i.x != -1 && i.y != -1;
    }

    public void NewGamePressed() {
        gameState.score              = 0;
        gameState.previousScore      = 0;
        //gameState.canUndo            = false;
        IsMoving                    = false;
        IsUndoing                   = false;
        gameState.activeTileData     = new TileData[board.Length];
        gameState.removedTileData    = new TileData[board.Length];

        for(int i = 0; i < board.Length; i++) {
            DeactivateTile(i, board.tiles[i]);
        }

        CreateBoardAndStartingTiles();
    }

    void DeactivateTile(in int i, in Tile t) {
        if(!t) return;
        t.Deactivate();
        board.tiles[i] = null;
    }

    public void UndoPressed() {
        if(IsUndoing) return;
        if(board.spawnedTile) {
            if(!board.spawnedTile.gameObject.activeInHierarchy) {
                Debug.LogWarning("Spawned tile is inactive...");
            }
            board.spawnedTile.AnimateShrink();
        }
        else {
            Debug.LogWarning("Spawned tile was null. Trying Undo() directly");
            Undo();
        }
    }
}