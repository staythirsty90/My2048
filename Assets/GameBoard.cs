using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct MoveData {
    public int xDir;
    public int yDir;
    public int startX;
    public int startY;
    public int endX;
    public int endY;
    public int xRowShift;
    public int yRowShift;
    public MoveData(int xDirection, int yDirection, int startX, int startY, int endX, int endY, int xrowshift, int yrowshift) {
        xDir = xDirection;
        yDir = yDirection;
        this.startX = startX;
        this.startY = startY;
        this.endX = endX;
        this.endY = endY;
        xRowShift = xrowshift;
        yRowShift = yrowshift;
    }

    public override string ToString() {
        return string.Format("xDir: {0}, yDir: {1}, startX: {2}, startY: {3}, endX: {4}, endY: {5}, xRowShift:{6}, yRowShift:{7}",
            xDir, yDir, startX, startY, endX, endY, xRowShift, yRowShift);
    }
}

[System.Serializable]
public class RemovedTiles {
    public List<Tile> list;
    int size;

    public Tile this[Index index] {
        get { return list[index.x + index.y * size]; }
        set { list[index.x + index.y * size] = value; }
    }

    public RemovedTiles(int size) {
        this.size = size;
        list = new List<Tile>(this.size * this.size);
    }

    public void Add(Tile t) {
        list.Add(t);
    }
}

[System.Serializable]
public class GameBoard {
    public Tile tilePrefab;
    public int seed = 101;
    public Index[] indices;
    public Vector2[] positions;
    public List<Tile> tiles;
    public RemovedTiles removedTiles;
    public int size;
    public int length;
    public MoveData upData;
    public MoveData downData;
    public MoveData rightData;
    public MoveData leftData;
    List<Tile> tilePool;
    SpriteRenderer sr;
    GameObject gameObject;
    public Tile spawnedTile;

    public Tile this[int i] {
        get { return tiles[i]; }
        set { tiles[i] = value; }
    }

    public Tile this[Index Index] {
        get {
            int index = Index.x + Index.y * size;
            if (index < 0 || index >= length) {
                return null;
            }
            return tiles[index];
        }
        set {
            int index = Index.x + Index.y * size;

            if (index < 0 || index >= length) {
                return;
            }
            tiles[Index.x + Index.y * size] = value;
        }
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
        g.indices       = new Index[g.length];
        g.positions     = new Vector2[g.length];
        g.tiles         = new List<Tile>(g.length);
        g.removedTiles  = new RemovedTiles(g.size);
        g.gameObject    = go;

        if(!isNewGame) {
            InitializePool(g);
        }

        for (int x = 0; x < g.size; x++) {
            for (int y = 0; y < g.size; y++) {
                int i = x + y * g.size;
                g.indices[i] = new Index(x, y);
                g.positions[i] = new Vector2(x, y);
                g.tiles.Add(null);
                g.removedTiles.Add(null);
                
            }
        }

        g.upData = new MoveData {
            xDir = 0,
            yDir = 1,
            startX = 0,
            startY = 0,
            endX = 0,
            endY = g.size - 1,
            xRowShift = 1,
            yRowShift = 0
        };

        g.downData = new MoveData {
            xDir = 0,
            yDir = -1,
            startX = 0,
            startY = g.size - 1,
            endX = 0,
            endY = 0,
            xRowShift = 1,
            yRowShift = 0
        };

        g.leftData = new MoveData {
            xDir = 1,
            yDir = 0,
            startX = 0,
            startY = 0,
            endX = g.size - 1,
            endY = 0,
            xRowShift = 0,
            yRowShift = 1
        };

        g.rightData = new MoveData {
            xDir = -1,
            yDir = 0,
            startX = g.size - 1,
            startY = 0,
            endX = 0,
            endY = 0,
            xRowShift = 0,
            yRowShift = 1
        };
    }

    static Tile LoadTile(GameBoard gb, TileData td)
    {
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

    public static void Load(GameBoard gb, GameObject go, GameData gd, bool isNewGame)
    {
        gb.size         = gd.size;
        gb.length       = gd.activeTileData.Length;
        gb.indices      = new Index[gb.length];
        gb.positions    = new Vector2[gb.length];
        gb.tiles        = new List<Tile>(gb.length);
        gb.removedTiles = new RemovedTiles(gb.size);
        gb.gameObject   = go;
        gb.sr           = gb.gameObject.GetComponent<SpriteRenderer>();
        
        if(!isNewGame) {
            InitializePool(gb);
        }

        for (int i = 0; i < gb.length; i++)
        {
            gb.tiles.Add(null);
            gb.removedTiles.Add(null);
        }

        for (int x = 0; x < gb.size; x++)
        {
            for (int y = 0; y < gb.size; y++)
            {
                int i           = x + y * gb.size;
                gb.indices[i]   = new Index(x, y);
                gb.positions[i] = new Vector2(x, y);

                TileData td = gd.removedTileData[i];
                Tile r      = LoadTile(gb, td);
                if (r)
                {
                    r.gameObject.SetActive(false);
                    r.SetSprite();
                    r.currentMove.removed                   = true;
                    r.nextPosition                          = gb.GetWorldPos(r.currentMove.index.x, r.currentMove.index.y);
                    gb.removedTiles[r.index]   = r;
                }
            }
        }

        for (int x = 0; x < gb.size; x++)
        {
            for (int y = 0; y < gb.size; y++)
            {
                int i = x + y * gb.size;
                TileData td = gd.activeTileData[i];
                Tile t = LoadTile(gb, td);
                if (t)
                {
                    if (t.currentMove.spawnedFromMove)
                    {
                        gb.spawnedTile = t;
                    }
                    if (t.currentMove.merged)
                    {
                        Tile r = gb.removedTiles[t.otherTileIndex];
                        if (r)
                        {
                            t.otherTileIndex        = r.index;
                            r.otherTileIndex        = t.index;
                            r.transform.position    = gb.GetWorldPos(r.otherTileIndex);
                        }
                    }
                    t.SetSprite();
                    t.transform.position = gb.positions[i];
                    t.nextPosition       = gb.GetWorldPos(t.index.x, t.index.y);
                    gb[new Index(x, y)]  = t;
                }
            }
        }

        gb.upData = new MoveData
        {
            xDir = 0,
            yDir = 1,
            startX = 0,
            startY = 0,
            endX = 0,
            endY = gb.size - 1,
            xRowShift = 1,
            yRowShift = 0
        };

        gb.downData = new MoveData
        {
            xDir = 0,
            yDir = -1,
            startX = 0,
            startY = gb.size - 1,
            endX = 0,
            endY = 0,
            xRowShift = 1,
            yRowShift = 0
        };

        gb.leftData = new MoveData
        {
            xDir = 1,
            yDir = 0,
            startX = 0,
            startY = 0,
            endX = gb.size - 1,
            endY = 0,
            xRowShift = 0,
            yRowShift = 1
        };

        gb.rightData = new MoveData
        {
            xDir = -1,
            yDir = 0,
            startX = gb.size - 1,
            startY = 0,
            endX = 0,
            endY = 0,
            xRowShift = 0,
            yRowShift = 1
        };
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
            Tile query = this[new Index(x + xDir, y + yDir)];
            if (!query) continue;
            next = query;
        }
        return next;
    }

    public Index GetNextEmptyIndex(Tile t, SwipeData swipeData) {
        var x       = t.index.x;
        var y       = t.index.y;
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
            var index = new Index(x + xDir, y + yDir);
            if (index.x >= size || index.y >= size) {
                return Index.Invalid;
            }
            Tile query = this[index];
            if(!query) {
                return index;
            }
        }
        return Index.Invalid;
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