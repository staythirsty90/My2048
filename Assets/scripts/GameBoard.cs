using System.Collections.Generic;
using UnityEngine;

namespace My2048 {
    [System.Serializable]
    public class GameBoard {
        public Tile tilePrefab;
        public int seed = 101;
        public List<Tile> tiles;
        public int size;
        public int Length { get; private set; }
        public Tile spawnedTile;

        public List<Tile> TilePool { get; private set; }

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
            if(index < 0 || index >= Length) {
                return null;
            }
            return tiles[index];
        }

        void SetTileFromIndex(in int x, in int y, Tile tile) {
            int index = x + y * size;
            if(index < 0 || index >= Length) {
                return;
            }
            tiles[index] = tile;
        }

        /// <summary>
        /// Creates the Tile GameObjects, deactivates them, and adds them into the GameBoard's tilePool.
        /// </summary>
        public void InitializePool() {

            if(TilePool != null && TilePool.Count > 0) {
                // We already have instantiated tiles. No need to continue.
                return;
            }

            TilePool = new List<Tile>(Length);
            for(int x = 0; x < size; x++) {
                for(int y = 0; y < size; y++) {
                    Tile t = Object.Instantiate(tilePrefab);
                    t.gameObject.SetActive(false);
                    TilePool.Add(t);
                }
            }
        }

        public void Create() {

            Random.InitState(seed);

            Length          = size * size;
            tiles           = new List<Tile>(Length);

            InitializePool();

            for(int x = 0; x < Length; x++) {
                tiles.Add(null);
            }
        }

        public void CreateBoardAndStartingTiles() {
            Create();
            SpawnRandomTile();
            SpawnRandomTile();
        }

        Tile LoadTile(in TileData td) {
            if(td.value == 0) {
                return null;
            }

            Tile t = GetTileFromPool();

            if(!t) {
                throw new System.NullReferenceException("Couldn't file a Tile GameObject from the Tile Pool.");
            }

            t.CurrentMove = new TileData {
                index           = td.index,
                merged          = td.merged,
                value           = td.value,
                spawnedFromMove = td.spawnedFromMove,
            };

            return t;
        }

        public void Load(in GameState gs) {
            size            = gs.size;
            Length          = gs.activeTileData.Length;
            tiles           = new List<Tile>(Length);

            InitializePool();

            for(int i = 0; i < Length; i++) {
                tiles.Add(null);
            }

            for(int x = 0; x < size; x++) {
                for(int y = 0; y < size; y++) {
                    int i = x + y * size;

                    TileData td = gs.removedTileData[i];
                    Tile r      = LoadTile(td);
                    if(!r) continue;
                    
                    r.gameObject.SetActive(false);
                    r.SetSprite();

                    var currentMove         = r.CurrentMove;
                    currentMove.removed     = true;
                    r.CurrentMove           = currentMove;
                    r.lerpData.end          = GetWorldPos(r.CurrentMove.index.x, r.CurrentMove.index.y);
                }
            }

            for(int x = 0; x < size; x++) {
                for(int y = 0; y < size; y++) {
                    int i = x + y * size;
                    TileData td = gs.activeTileData[i];
                    Tile t = LoadTile(td);
                    if(!t) {
                        continue;
                    }
                    if(t.CurrentMove.spawnedFromMove) {
                        spawnedTile = t;
                    }
                    
                    t.SetSprite();
                    t.transform.position = GetWorldPos(t.CurrentMove.index);
                    t.lerpData.end       = GetWorldPos(t.CurrentMove.index);
                    this[x, y] = t;
                }
            }
        }

        

        /// <summary>
        /// Return the i index from the Tile's Index.x and Index.y
        /// </summary>
        /// <param name="t">The tile.</param>
        /// <returns></returns>
        public int Get_i(in Tile t) {
            return t.CurrentMove.index.x + t.CurrentMove.index.y * size;
        }

        public Tile SpawnTile(int index, uint value, bool spawnedFromMove) {
            Tile t = GetTileFromPool();
            if(!t) {
                Debug.LogWarning("There are no more tiles in the pool! Instantiating one.");
                t = Object.Instantiate(tilePrefab);
            }

            tiles[index] = t;
            t.value = value;
            var currentMove = new TileData {
                index = new Index(index % size, index / size),
                value = value,
            };
            t.transform.position = GetWorldPos(currentMove.index);
            if(spawnedFromMove) {
                t.AnimateSpawn();
                currentMove.spawnedFromMove = true;
            }
            t.SetSprite();
            t.CurrentMove = currentMove;
            return t;
        }

        public Tile SpawnRandomTile(bool spawnedFromMove = false) {
            int index = Random.Range(0, Length);
            float t = 0;
            while(tiles[index]) {
                index = Random.Range(0, Length);
                t += Time.deltaTime;
                if(t > 100) {
                    Debug.LogWarning("couldn't spawn a random tile because grid is full");
                    return null;
                }
            }
            uint value = (uint)(Random.Range(0, 100) < 90 ? 2 : 4);
            return SpawnTile(index, value, spawnedFromMove);
        }

        public Vector2 GetWorldPos(in int x, in int y) {
            return new Vector2(x, y);
        }

        public Vector2 GetWorldPos(in Index index) {
            return new Vector2(index.x, index.y);
        }

        public Tile GetNextTile(Tile t, SwipeData swipeData) {
            // TODO: Don't like having to check for removed!
            int x = t.CurrentMove.removed ? t.CurrentMove.removedIndex.x : t.CurrentMove.index.x;
            int y = t.CurrentMove.removed ? t.CurrentMove.removedIndex.y : t.CurrentMove.index.y;
            Tile next = null;
            int count = 0;
            int xDir = 0;
            int yDir = 0;
            switch(swipeData.direction) {
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
            for(int i = 0; i < count; i++) {
                Tile query = this[x + xDir, y + yDir];
                if(!query) continue;
                next = query;
            }
            return next;
        }

        public Index GetNextEmptyIndex(Tile t, SwipeData swipeData) {
            // TODO: Don't like having to check for removed!
            int x = t.CurrentMove.removed ? t.CurrentMove.removedIndex.x : t.CurrentMove.index.x;
            int y = t.CurrentMove.removed ? t.CurrentMove.removedIndex.y : t.CurrentMove.index.y;
            var next = Index.Invalid;
            var count = 0;
            var xDir = 0;
            var yDir = 0;

            switch(swipeData.direction) {
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

            for(int i = 0; i < count; i++) {
                if(x + xDir >= size || y + yDir >= size) {
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
            for(int i = 0; i < TilePool.Count; i++) {
                Tile t = TilePool[i];
                if(t && !t.gameObject.activeInHierarchy) { // TODO: better checking for a tile ready to be used.
                    t.OnRemovedFromPool();
                    //TilePool[i] = null;
                    return t;
                }
            }
            return null;
        }
    }
}