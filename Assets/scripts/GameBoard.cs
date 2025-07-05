using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RemovedTiles {
    public List<Tile> list;
    int size;

    public Tile this[Index index] {
        get => list[index.x + index.y * size];
        set => list[index.x + index.y * size] = value;
    }

    public RemovedTiles(int size) {
        this.size = size;
        list = new List<Tile>(this.size * this.size);
    }
}

[System.Serializable]
public class GameBoard {
    public Tile tilePrefab;
    public int seed = 101;
    public Vector2[] positions;
    public List<Tile> tiles;
    public RemovedTiles removedTiles;
    public int size;
    public int length;
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
        if (index < 0 || index >= length) {
            return null;
        }
        return tiles[index];
    }

    void SetTileFromIndex(in int x, in int y, Tile tile) {
        int index = x + y * size;
        if (index < 0 || index >= length) {
            return;
        }
        tiles[index] = tile;
    }

    public static void InitializePool(GameBoard g) {
        g.tilePool = new List<Tile>(g.length);
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

    public static void Create(GameBoard g, GameObject go, bool isNewGame) {
        g.length        = g.size * g.size;
        g.positions     = new Vector2[g.length];
        g.tiles         = new List<Tile>(g.length);
        g.removedTiles  = new RemovedTiles(g.size);

        if(!isNewGame) {
            InitializePool(g);
        }

        for (int x = 0; x < g.size; x++) {
            for (int y = 0; y < g.size; y++) {
                int i = x + y * g.size;
                g.positions[i] = new Vector2(x, y);
                g.tiles.Add(null);
                g.removedTiles.list.Add(null);
                
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

    public static void Load(GameBoard gb, GameObject go, GameData gd, bool isNewGame) {
        gb.size         = gd.size;
        gb.length       = gd.activeTileData.Length;
        gb.positions    = new Vector2[gb.length];
        gb.tiles        = new List<Tile>(gb.length);
        gb.removedTiles = new RemovedTiles(gb.size);
        
        if(!isNewGame) {
            InitializePool(gb);
        }

        for (int i = 0; i < gb.length; i++) {
            gb.tiles.Add(null);
            gb.removedTiles.list.Add(null);
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
                    gb.removedTiles[r.index]    = r;
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
                        Tile r = gb.removedTiles[t.otherTileIndex];
                        if (r) {
                            t.otherTileIndex        = r.index;
                            r.otherTileIndex        = t.index;
                            r.transform.position    = gb.GetWorldPos(r.otherTileIndex);
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
        int index = Random.Range(0, length);
        float t = 0;
        while (tiles[index]) {
            index = Random.Range(0, length);
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
        t.Reset();
    }

    public void ClearRemovedTiles() {
        for (int i = 0; i < removedTiles.list.Count; i++) {
            Tile r = removedTiles.list[i];
            if(!r) {
                continue;
            }
            if(!tilePool.Contains(r)) {
                tilePool.Add(r);
            }
            removedTiles.list[i] = null;
        }
    }
}