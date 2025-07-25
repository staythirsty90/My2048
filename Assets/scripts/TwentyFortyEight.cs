﻿using My2048;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SaveLoad), typeof(MyInput))]
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
    public bool IsUndoing { get; private set; }
    public bool IsMoving { get; private set; }

    uint bestScore;
    uint deltaScore;

    Stack<Tile> tileStack;
    GamePhase phase = GamePhase.GETTING_INPUT;
    SaveLoad saveLoad;
    MyInput myInput;

    void Awake() {
        saveLoad = GetComponent<SaveLoad>();
        myInput  = GetComponent<MyInput>();
    }

    void Start() {
        saveData                = saveLoad.Load(board);
        tileStack               = new Stack<Tile>(board.size);
        bestScore               = (uint)PlayerPrefs.GetInt("best");
        bestText.text           = bestScore.ToString();
        scoreText.text          = saveData.score.ToString();
        undoButton.interactable = saveData.canUndo;

        MoveData.Init(board.size);

        if(saveData.activeTileData == null) {
            // There was no save file.
            GameBoard.Create(board, false);
            board.SpawnRandomTile();
            board.SpawnRandomTile();
        }
    }

    void Update() {
        switch(phase) {
            case GamePhase.GETTING_INPUT:
                myInput.HandleInput();
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
            if(t == null) continue;
            t.InitLerp(tileLerpDuration);
        }

        foreach(var t in board.removedTiles) {
            if(t == null) continue;
            t.InitLerp(tileLerpDuration);
        }
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
    
    public void OnSpawnedTileShrunk() {
        board[board.spawnedTile.index] = null;
        board.AddTileToPool(board.spawnedTile);
        board.spawnedTile = null;
        Undo();
    }

    void Undo() {
        if(IsUndoing) return;
        if(!saveData.canUndo) return;
        IsUndoing = true;
        saveData.canUndo = false;
        var move = new MoveData();
        switch(saveData.previousSwipe.direction) {
            case Direction.TOP_TO_BOTTOM:
                move = saveData.previousSwipe.invert ? MoveData.Up : MoveData.Down;
                break;
            case Direction.LEFT_TO_RIGHT:
                move = saveData.previousSwipe.invert ? MoveData.Right : MoveData.Left;
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
                t.SetSprite();
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
        t.index                     = t.currentMove.index;
        board[t.currentMove.index]  = t;
        t.lerpData.end              = board.GetWorldPos(t.currentMove.index);
    }

    void OnTilesFinishedLerp() {
        if(IsUndoing) {
            IsUndoing           = false;
            saveData.score      = saveData.previousScore;
            scoreText.text      = saveData.score.ToString();
        }
        else {
            board.spawnedTile   = board.SpawnRandomTile(spawnedFromMove: true);
            saveData.score      += deltaScore;
            deltaScore          = 0;

            if(saveData.score > bestScore) {
                bestScore       = saveData.score;
                bestText.text   = bestScore.ToString();
            }

            scoreText.text      = saveData.score.ToString();
            
            PlayerPrefs.SetInt("best", (int)bestScore);
            CheckForWinOrLose();
        }

        IsMoving                = false;
        undoButton.interactable = saveData.canUndo;
        phase                   = GamePhase.GETTING_INPUT;
        
        saveLoad.Save(game: this);
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

    public void MoveTiles(in MoveData move) {
        if(IsMoving) {
            return;
        }
        
        if(!CanMove(move.swipe)) {
            return;
        }
        
        saveData.previousSwipe  = move.swipe;
        saveData.previousScore  = saveData.score;
        IsMoving                = true;
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

        if(tile.currentMove.removed) {
            return false;
        }

        Tile query = board.GetNextTile(tile, swipeData);
        if(query && tile.value == query.value) {
            return true;
        }
        
        Index i = board.GetNextEmptyIndex(tile, swipeData);
        return i.x != -1 && i.y != -1;
    }

    public void NewGamePressed() {
        saveData.score          = 0;
        saveData.previousScore  = 0;
        saveData.canUndo        = false;
        IsMoving                = false;
        IsUndoing               = false;

        for(int i = 0; i < board.Length; i++) {
            DeactivateTile(i, board.tiles[i]);
            DeactivateTile(i, board.removedTiles[i]);
        }

        saveLoad.InitGame(board, isNewGame: true);
    }

    void DeactivateTile(in int i, in Tile t) {
        if(!t) return;
        t.Deactivate();
        board.tiles[i] = null;
    }

    public void UndoPressed() {
        if(IsUndoing) return;
        if(board.spawnedTile)
            board.spawnedTile.Shrink();
    }
}