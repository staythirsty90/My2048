using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameBoard {
    public Tile tilePrefab;
    public int seed = 101;
    public Vector2[] positions;
    public List<Tile> tiles;
    public List<Tile> removedTiles;
    public int size;
    public int Length { get; private set; }
    public Tile spawnedTile;
    
    List<Tile> tilePool;

    public Tile this[int x, int y] {
        get => GetTileFromIndex(x, y);
        set => SetTileFromIndex(x, y, value);
    }

    public Tile this[Index Index] {
        get => GetTileFromIndex(Index.x, Index.y);
        set => SetTileFromIndex(Index.x, Index.y, value);
    }

    Tile GetTileFromIndex(in int x, in int y) {
        int index = x + y * size;
        if (index < 0 || index >= Length) {
            return null;
        }
        return tiles[index];
    }

    void SetTileFromIndex(in int x, in int y, Tile tile) {
        int index = x + y * size;
        if (index < 0 || index >= Length) {
            return;
        }
        tiles[index] = tile;
    }

    public static void InitializePool(GameBoard g) {
        g.tilePool = new List<Tile>(g.Length);
        for (int x = 0; x < g.size; x++) {
            for (int y = 0; y < g.size; y++) {
                Tile t = Object.Instantiate(g.tilePrefab);
                t.gameObject.SetActive(false);
                g.tilePool.Add(t);
                t = Object.Instantiate(g.tilePrefab);
                t.gameObject.SetActive(false);
                g.tilePool.Add(t);
            }
        }
    }

    public static void Create(GameBoard gb, bool isNewGame) {
        gb.Length        = gb.size * gb.size;
        gb.positions     = new Vector2[gb.Length];
        gb.tiles         = new List<Tile>(gb.Length);
        gb.removedTiles  = new List<Tile>(gb.Length);;

        if(!isNewGame) {
            InitializePool(gb);
        }

        for (int x = 0; x < gb.size; x++) {
            for (int y = 0; y < gb.size; y++) {
                int i = x + y * gb.size;
                gb.positions[i] = new Vector2(x, y);
                gb.tiles.Add(null);
                gb.removedTiles.Add(null);
                
            }
        }

    }

    static Tile LoadTile(GameBoard gb, TileData td) {
        if (td.value == 0) {
            return null;
        }
        
        Tile t                          = gb.GetTileFromPool();
        t.index                         = td.index;
        t.currentMove.index             = td.oldIndex;
        t.otherTileIndex                = td.otherTileIndex;
        t.currentMove.merged            = td.merged;
        t.value                         = td.value;
        t.currentMove.spawnedFromMove   = td.spawned;
        return t;
    }

    public static void Load(GameBoard gb, SaveData gd, bool isNewGame) {
        gb.size         = gd.size;
        gb.Length       = gd.activeTileData.Length;
        gb.positions    = new Vector2[gb.Length];
        gb.tiles        = new List<Tile>(gb.Length);
        gb.removedTiles = new List<Tile>(gb.Length);
        
        if(!isNewGame) {
            InitializePool(gb);
        }

        for (int i = 0; i < gb.Length; i++) {
            gb.tiles.Add(null);
            gb.removedTiles.Add(null);
        }

        for (int x = 0; x < gb.size; x++) {
            for (int y = 0; y < gb.size; y++) {
                int i           = x + y * gb.size;
                gb.positions[i] = new Vector2(x, y);

                TileData td = gd.removedTileData[i];
                Tile r      = LoadTile(gb, td);
                if (r) {
                    r.gameObject.SetActive(false);
                    r.SetSprite();
                    r.currentMove.removed       = true;
                    r.lerpData.end              = gb.GetWorldPos(r.currentMove.index.x, r.currentMove.index.y);
                    gb.removedTiles[i]          = r;
                }
            }
        }

        for (int x = 0; x < gb.size; x++) {
            for (int y = 0; y < gb.size; y++) {
                int i       = x + y * gb.size;
                TileData td = gd.activeTileData[i];
                Tile t      = LoadTile(gb, td);
                if (t) {
                    if (t.currentMove.spawnedFromMove) {
                        gb.spawnedTile = t;
                    }
                    if (t.currentMove.merged) {
                        Tile r = gb.removedTiles[gb.GetOtherIFromIndex(t)];
                        if(r) {
                            t.otherTileIndex = r.index;
                            r.otherTileIndex = t.index;
                            r.transform.position = gb.GetWorldPos(r.otherTileIndex);
                        }
                    }
                    t.SetSprite();
                    t.transform.position = gb.positions[i];
                    t.lerpData.end       = gb.GetWorldPos(t.index.x, t.index.y);
                    gb[x, y]             = t;
                }
            }
        }
    }

    public int GetOtherIFromIndex(in Tile t) {
        return t.otherTileIndex.x + t.otherTileIndex.y * size;
    }

    public int GetIFromIndex(in Tile t) {
        return t.currentMove.index.x + t.currentMove.index.y * size;
    }

    public Tile SpawnTile(int index, uint value, bool spawnedFromMove) {
        Tile t = GetTileFromPool();
        if (!t) {
            Debug.LogWarning("There are no more tiles in the pool! Instantiating one.");
            t = Object.Instantiate(tilePrefab);
        }
        tiles[index]        = t;
        t.value             = value;
        t.index             = new Index(index % size, index / size);
        t.currentMove.index = t.index;
        t.MoveToPos(positions[index]);
        if (spawnedFromMove) {
            t.Spawn();
            t.currentMove.spawnedFromMove = true;
        }
        t.SetSprite();
        return t;
    }

    public Tile SpawnRandomTile(bool spawnedFromMove = false) {
        int index = Random.Range(0, Length);
        float t = 0;
        while (tiles[index]) {
            index = Random.Range(0, Length);
            t += Time.deltaTime;
            if (t > 100) {
                Debug.LogWarning("couldn't spawn a random tile because grid is full");
                return null;
            }
        }
        uint value = (uint)(Random.Range(0, 100) < 90 ? 2 : 4);
        return SpawnTile(index, value, spawnedFromMove);
    }

    public Vector2 GetWorldPos(in int x, in int y) {
        return positions[x + y * size];
    }

    public Vector2 GetWorldPos(in Index index) {
        return positions[index.x + index.y * size];
    }

    public Tile GetNextTile(Tile t, SwipeData swipeData) {
        int x = t.index.x;
        int y = t.index.y;
        Tile next = null;
        int count = 0;
        int xDir = 0;
        int yDir = 0;
        switch (swipeData.direction) {
            case Direction.TOP_TO_BOTTOM:
                xDir = 0;
                yDir = swipeData.invert ? 1 : -1;
                count = swipeData.invert ? size - y - 1 : y;
                break;
            case Direction.LEFT_TO_RIGHT:
                xDir = swipeData.invert ? 1 : -1;
                yDir = 0;
                count = swipeData.invert ? size - x - 1 : x;
                break;
        }
        for (int i = 0; i < count; i++) {
            Tile query = this[x + xDir, y + yDir];
            if (!query) continue;
            next = query;
        }
        return next;
    }

    public Index GetNextEmptyIndex(Tile t, SwipeData swipeData) {
        var x       = t.index.x;
        var y       = t.index.y;
        var next    = Index.Invalid;
        var count   = 0;
        var xDir    = 0;
        var yDir    = 0;
        
        switch (swipeData.direction) {
            case Direction.TOP_TO_BOTTOM:
                xDir    = 0;
                yDir    =  swipeData.invert ? 1 : -1;
                count   = swipeData.invert ? size - y - 1 : y;
                break;
            case Direction.LEFT_TO_RIGHT:
                xDir    = swipeData.invert ? 1 : -1;
                yDir    = 0;
                count   = swipeData.invert ? size - x - 1 : x;
                break;
        }

        for (int i = 0; i < count; i++) {
            if (x + xDir >= size || y + yDir >= size) {
                return next;
            }
            Tile query = this[x + xDir, y + yDir];
            if(!query) {
                next = new Index(x + xDir, y + yDir);
            }
        }
        return next;
    }

    Tile GetTileFromPool() {
        for (int i = 0; i < tilePool.Count; i++) {
            Tile t = tilePool[i];
            if (t) {
                t.OnRemovedFromPool();
                tilePool[i] = null;
                return t;
            }
        }
        return null;
    }

    public void AddTileToPool(Tile t) {
        if(!tilePool.Contains(t)) {
            tilePool.Add(t);
        }
        t.Deactivate();
    }

    public void ClearRemovedTiles() {
        for (int i = 0; i < removedTiles.Count; i++) {
            Tile r = removedTiles[i];
            if(!r) {
                continue;
            }
            if(!tilePool.Contains(r)) {
                tilePool.Add(r);
            }
            removedTiles[i] = null;
        }
    }
}