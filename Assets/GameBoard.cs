using System.Collections;
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
    public Tile this[int x, int y] {
        get { return list[x + y * size]; }
        set { list[x + y * size] = value; }
    }

    public RemovedTiles(int size) {
        this.size = size;
        list = new List<Tile>(this.size * this.size);
    }

    public void Add(Tile t) {
        list.Add(t);
    }

    void Clear() {
        int Count = list.Count;
        for (int i = 0; i < Count; i++) {
            list[i] = null;
        }
    }

    public bool isCleared() {
        int Count = list.Count;
        for (int i = 0; i < Count; i++) {
            if (list[i] != null)
                return false;

        }
        return true;
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

    public Tile this[int x, int y] {
        get {
            int index = x + y * size;
            if (index < 0 || index >= length) {
                return null;
            }
            return tiles[index];
        }
        set {
            int index = x + y * size;
            if (index < 0 || index >= length) {
                return;
            }
            tiles[x + y * size] = value;
        }
    }

    public Tile this[Index index] {
        get {
            int i = index.x + index.y * size;
            if (i < 0 || i >= length) {
                return null;
            }
            return tiles[i];
        }
        set {
            int i = index.x + index.y * size;
            if (i < 0 || i >= length) {
                return;
            }
            tiles[index.x + index.y * size] = value;
        }
    }


    public static void initializePool(GameBoard g) {
        g.tilePool = new List<Tile>(g.length);
        for (int x = 0; x < g.size; x++) {
            for (int y = 0; y < g.size; y++) {
                Tile t = GameObject.Instantiate(g.tilePrefab);
                t.gameObject.SetActive(false);
                g.tilePool.Add(t);
                t = GameObject.Instantiate(g.tilePrefab);
                t.gameObject.SetActive(false);
                g.tilePool.Add(t);
            }
        }
    }

    public static void create(GameBoard g, GameObject go, bool newGame) {
        //  Random.InitState(g.seed);
        g.length = g.size * g.size;
        g.indices = new Index[g.length];
        g.positions = new Vector2[g.length];
        g.tiles = new List<Tile>(g.length);
        g.removedTiles = new RemovedTiles(g.size);
        g.gameObject = go;
        //g.sr = g.gameObject.GetComponent<SpriteRenderer>();
        if(!newGame)initializePool(g);
        //       g.sr.size = new Vector2(g.size, g.size);
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

    static Tile loadTile(GameBoard g, tileData d)
    {
        if (d.value == 0)
            return null;
        // Tile t = GameObject.Instantiate(g.tilePrefab);
        Tile t = g.getTileFromPool();
        t.index = d.index;
        t.currentMove.index = d.oldIndex;
        t.otherTileIndex = d.otherTileIndex;
        t.currentMove.merged = d.merged;
        t.value = d.value;
        t.currentMove.spawnedFromMove = d.spawned; // shouldnt happen if merged is true
        return t;
    }


    public static void load(GameBoard g, GameObject go, GameData d, bool newGame)
    {
        g.size = d.size;/// need to save the size of the loaded game....
        g.length = d.activeTileData.Length;
        g.indices = new Index[g.length];
        g.positions = new Vector2[g.length];
        g.tiles = new List<Tile>(g.length);
        g.removedTiles = new RemovedTiles(g.size);
        g.gameObject = go;
        g.sr = g.gameObject.GetComponent<SpriteRenderer>();
        if (!newGame)
            initializePool(g);

        for (int i = 0; i < g.length; i++)
        {
            g.tiles.Add(null);
            g.removedTiles.Add(null);
        }

        for (int x = 0; x < g.size; x++)
        {
            for (int y = 0; y < g.size; y++)
            {
                int i = x + y * g.size;
                g.indices[i] = new Index(x, y);
                g.positions[i] = new Vector2(x, y);
                tileData td;
                td = d.removedTileData[i];
                Tile r = loadTile(g, td);
                if (r)
                {
                    r.currentMove.removed = true;
                    r.gameObject.SetActive(false);
                    r.nextPosition = g.getWorldPos(r.currentMove.index.x, r.currentMove.index.y);
                    r.setSprite();
                    g.removedTiles[r.index.x, r.index.y] = r;
                }
            }
        }

        for (int x = 0; x < g.size; x++)
        {
            for (int y = 0; y < g.size; y++)
            {
                int i = x + y * g.size;
                tileData td = d.activeTileData[i];
                Tile t = loadTile(g, td);
                if (t)
                {
                    if (t.currentMove.spawnedFromMove)
                    {
                        g.spawnedTile = t;
                    }
                    if (t.currentMove.merged)
                    {
                        Tile r = g.removedTiles[t.otherTileIndex.x, t.otherTileIndex.y];
                        if (r)
                        {
                            t.otherTileIndex = r.index;
                            r.otherTileIndex = t.index;
                            r.transform.position = g.getWorldPos(r.otherTileIndex.x, r.otherTileIndex.y);
                        }
                    }
                    t.transform.position = g.positions[i];
                    t.nextPosition = g.getWorldPos(t.index.x, t.index.y);
                    t.setSprite();
                    g[x, y] = t;
                }
            }
        }

        g.upData = new MoveData
        {
            xDir = 0,
            yDir = 1,
            startX = 0,
            startY = 0,
            endX = 0,
            endY = g.size - 1,
            xRowShift = 1,
            yRowShift = 0
        };
        g.downData = new MoveData
        {
            xDir = 0,
            yDir = -1,
            startX = 0,
            startY = g.size - 1,
            endX = 0,
            endY = 0,
            xRowShift = 1,
            yRowShift = 0
        };
        g.leftData = new MoveData
        {
            xDir = 1,
            yDir = 0,
            startX = 0,
            startY = 0,
            endX = g.size - 1,
            endY = 0,
            xRowShift = 0,
            yRowShift = 1
        };
        g.rightData = new MoveData
        {
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

    public Tile spawnTile(int index, uint value, bool spawnedFromMove) {
        //Tile t = GameObject.Instantiate(tilePrefab);
        Tile t = getTileFromPool();
        if (!t) {
            Debug.LogError("no more tiles in the pool. need to instantiate more");
            t = GameObject.Instantiate(tilePrefab);
            //return null;
        }
        tiles[index] = t;
        t.value = value;
        t.index = new Index(index % size, index / size);
        t.currentMove.index = t.index;
        //t.transform.SetParent(canvas.transform);
        t.moveToPos(positions[index]);
        if (spawnedFromMove) {
            t.spawn();
            t.currentMove.spawnedFromMove = true;
        }
        t.setSprite();
        return t;
    }

    public Tile spawnTile(int x, int y, uint value, bool spawnedFromMove) {
        int index = x + y * size;
        return spawnTile(index, value, spawnedFromMove);
    }

    public Tile spawnRandomTile(bool spawnedFromMove) {
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
        return spawnTile(index, value, spawnedFromMove);
    }

    public Vector2 getWorldPos(int x, int y) {
        return positions[x + y * size];
    }

    public Vector2 getWorldPos(int i) {
        return positions[i];
    }

    public Tile getNextTile(Tile t, SwipeData swipeData) {
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

    public Index getNextEmptyIndex(Tile t, SwipeData swipeData) {
        int x = t.index.x;
        int y = t.index.y;
        Index next = new Index(-1, -1);
        int count = 0;
        int xDir = 0;
        int yDir = 0;
        switch (swipeData.direction) {
            case Direction.TOP_TO_BOTTOM:
                xDir = 0;
                yDir =  swipeData.invert ? 1 : -1;
                count = swipeData.invert ? size - y - 1 : y;
                break;
            case Direction.LEFT_TO_RIGHT:
                xDir = swipeData.invert ? 1 : -1;
                yDir = 0;
                count = swipeData.invert ? size - x - 1 : x;
                break;
        }
        for (int i = 0; i < count; i++) {
            if (x + xDir >= size || y + yDir >= size)
                return next;
            Tile query = this[x + xDir, y + yDir];
            if (!query) next = new Index(x + xDir, y + yDir);
        }
        return next;
    }

    Tile getTileFromPool() {
        for (int i = 0; i < tilePool.Count; i++) {
            Tile t = tilePool[i];
            if (t) {
                t.onRemovedFromPool();
                tilePool[i] = null;
                return t;
            }
        }
        return null;
    }

    public void AddTileToPool(Tile t) {
        if (!tilePool.Contains(t))
            tilePool.Add(t);
        t.reset();
        //t.gameObject.SetActive(false);
    }

    public void clearTilePool() {
        for (int i = 0; i < tilePool.Count; i++) {
            tilePool[i] = null;
        }
    }

    public void ClearRemovedTiles() {
        for (int i = 0; i < removedTiles.list.Count; i++) {
            Tile r = removedTiles.list[i];
            if (!r)
                continue;
            if (!tilePool.Contains(r))
                tilePool.Add(r);
            removedTiles.list[i] = null;
        }
    }
}
