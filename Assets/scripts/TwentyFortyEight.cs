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
    public bool IsMoving { get; private set; }

    uint bestScore;
    uint deltaScore;

    Stack<Tile> tileStack;
    GamePhase phase;

    public MyRingBuffer<TileData> RingBuffer = new MyRingBuffer<TileData>();


    void Start() {
        MoveData.Init(board.size);

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

    void PushTileStates() {
        // Push the state before moving the Tiles. Why exactly is this required?
        for(int i = 0; i < board.tiles.Count; i++) {
            var t = board.tiles[i];
            if(t) {
                t.CurrentMove.merged    = false;
                t.CurrentMove.removed   = false;
                RingBuffer.Push(t.CurrentMove);
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
        board[board.spawnedTile.CurrentMove.index] = null;
        // A spawnedTile might have an additonal state??????
        //board.spawnedTile.RingBuffer.Reset();
        board.spawnedTile = null;
        Undo();
    }

    void Undo() {
        if(IsUndoing) {
            return;
        }

        if(RingBuffer.head <= 0) {
            // Can't undo.
            return;
        }

        IsUndoing           = true;
        //gameState.canUndo   = false;

        // null all tiles currently in play.
        for(int i = 0; i < board.tiles.Count; i++) {
            var tile = board.tiles[i];
            if(tile) {
                board[tile.CurrentMove.index] = null;
            }
        }

        foreach(var tile in board.TilePool) {

            //if(tile.Undo() && tile.Undo()) { // Ghetto

            //    if(tile.CurrentMove.spawnedFromMove) {
            //        Debug.Assert(board.spawnedTile == null);
            //        board.spawnedTile = tile;
            //    }
            //}
            
            // Don't write deactivated tiles back to the board.
            if(!tile.gameObject.activeInHierarchy) {
                continue;
            }

            // Not happy having to check for removed again.
            var targetIndex = tile.CurrentMove.index;
            if(tile.CurrentMove.removed) {
                targetIndex = tile.CurrentMove.removedIndex;
            }

            Debug.Log($"Writing tile ({targetIndex}) to board");
            Debug.Assert(board[targetIndex] == null, $"Writing to non-null Tile at ({targetIndex})!");
            board[targetIndex] = tile;
        }
     
        BeginLerpPhase();
    }

    void OnTilesFinishedLerp() {
        foreach(var t in board.TilePool) {
            Tile.TileEndLerp(t, undo: IsUndoing);
        }

        if(IsUndoing) {
            IsUndoing           = false;
            gameState.score     = gameState.previousScore;
            scoreText.text      = gameState.score.ToString();
        }
        else {
            board.spawnedTile   = board.SpawnRandomTile(spawnedFromMove: true);
            gameState.score     += deltaScore;
            deltaScore          = 0;

            if(gameState.score > bestScore) {
                bestScore       = gameState.score;
                bestText.text   = bestScore.ToString();
            }

            scoreText.text      = gameState.score.ToString();
            
            PlayerPrefs.SetInt("best", (int)bestScore);
            
            // Push states? do we want to do this also when we Undo???
            PushTileStates();

            CheckForWinOrLose();
        }


        IsMoving                = false;
        ////undoButton.interactable = gameState.canUndo;
        phase                   = GamePhase.GETTING_INPUT;
        
        SaveLoad.Save(game: this);
    }

    void CheckForWinOrLose() {
        foreach(var tile in board.tiles) {
            if(tile && tile.value >= winningNumber) {
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
    bool MoveTiles(in MoveData move) {
        if(IsMoving) {
            return false;
        }

        if(!CanMove(move.swipe)) {
            return false;
        }

        PushTileStates();

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
                    prevTile.value += prevTile.value;

                    prevTile.CurrentMove = new TileData {
                        index           = prevTile.CurrentMove.index,
                        merged          = true,
                        removed         = false,
                        spawnedFromMove = false,
                        value           = prevTile.value,
                    };

                    tile.CurrentMove = new TileData {
                        removedIndex    = tile.CurrentMove.index,
                        index           = prevTile.CurrentMove.index,
                        merged          = false,
                        removed         = true,
                        spawnedFromMove = false,
                        value           = tile.value,
                    };

                    Tile.DebugSetGameObjectName(tile);

                    deltaScore          += prevTile.value;
                    tile.lerpData.end   = prevTile.lerpData.end;

                    Tile.DebugSetGameObjectName(prevTile);
                    continue;
                }

                tile.CurrentMove = new TileData {
                    index           = new Index(x, y),
                    merged          = false,
                    removed         = false,
                    spawnedFromMove = false,
                    value           = tile.value,
                };

                board[tile.CurrentMove.index]   = tile;
                tile.lerpData.end               = board.GetWorldPos(x, y);
                prevTile                        = tile;
                x                               -= move.xDir;
                y                               -= move.yDir;

                Tile.DebugSetGameObjectName(tile);
            }
        }
        return true;
    }

    static bool CanMerge(in Tile prevTile, in Tile tile) {
        return prevTile && prevTile.value == tile.value && !prevTile.CurrentMove.merged && !prevTile.CurrentMove.removed && !tile.CurrentMove.removed && !tile.CurrentMove.merged;
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

        //if(tile.CurrentMove.removed) { // Removed flag is okay now?
        //    return false;
        //}

        Tile query = board.GetNextTile(tile, swipeData);
        if(query && tile.value == query.value) {
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
            board.spawnedTile.AnimateShrink();
        }
        else {
            Undo();
        }
    }
}