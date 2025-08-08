using System.Collections.Generic;
using System;
using UnityEngine;

namespace My2048 {

    [Serializable]
    public class MyRingBuffer<T> {

        public int head = -1;

        public T[] buffer;
        public int capacity;

        public MyRingBuffer(int Capacity = 4) {
            if(Capacity < 1) {
                throw new ArgumentException("Capacity must be greater than 0.", nameof(Capacity));
            }

            capacity = Capacity;
            buffer   = new T[capacity];
            head     = -1;
        }

        public void Push(T item) {

            head += 1;

            if(head >= capacity) {
                head = capacity - 1;
                for(int i = 0; i < head; i++) {
                    buffer[i] = buffer[i + 1];
                }
            }

            buffer[head] = item;
        }

        public void Set(T item) {
            buffer[head] = item;
        }

        public void Undo() {
            buffer[head] = default;
            head--;
            if(head < 0) {
                head = 0;
            }
        }

        public void Reset() {
            for(int i = 0; i < capacity; i++) {
                buffer[i] = default;
            }
        }

        public T Peek() {
            return buffer[head];
            //return buffer[(head -1) % capacity];
        }
    }

    public enum Direction {
        TOP_TO_BOTTOM,
        LEFT_TO_RIGHT
    }

    public enum GamePhase {
        GETTING_INPUT,
        LERPING_TILES,
    }

    [Serializable]
    public struct SwipeData {
        public Direction direction;
        public bool invert;
    }

    [Serializable]
    public struct MoveData {
        public int xDir;
        public int yDir;
        public int startX;
        public int startY;
        public int endX;
        public int endY;
        public int xRowShift;
        public int yRowShift;
        public SwipeData swipe;

        public MoveData(int xDirection, int yDirection, int startX, int startY, int endX, int endY, int xrowshift, int yrowshift, SwipeData swipe) {
            xDir = xDirection;
            yDir = yDirection;
            this.startX = startX;
            this.startY = startY;
            this.endX = endX;
            this.endY = endY;
            xRowShift = xrowshift;
            yRowShift = yrowshift;
            this.swipe = swipe;
        }

        public static void Init(in int size) {
            Up = new MoveData {
                xDir = 0,
                yDir = 1,
                startX = 0,
                startY = 0,
                endX = 0,
                endY = size - 1,
                xRowShift = 1,
                yRowShift = 0,

                swipe = GetSwipeUp,
            };

            Down = new MoveData {
                xDir = 0,
                yDir = -1,
                startX = 0,
                startY = size - 1,
                endX = 0,
                endY = 0,
                xRowShift = 1,
                yRowShift = 0,

                swipe = GetSwipeDown,
            };

            Left = new MoveData {
                xDir = -1,
                yDir = 0,
                startX = size - 1,
                startY = 0,
                endX = 0,
                endY = 0,
                xRowShift = 0,
                yRowShift = 1,

                swipe = GetSwipeLeft,
            };

            Right = new MoveData {
                xDir = 1,
                yDir = 0,
                startX = 0,
                startY = 0,
                endX = size - 1,
                endY = 0,
                xRowShift = 0,
                yRowShift = 1,

                swipe = GetSwipeRight,
            };

        }

        public static MoveData Up;
        public static MoveData Down;
        public static MoveData Left;
        public static MoveData Right;

        public static SwipeData GetSwipeUp => new SwipeData {
            direction = Direction.TOP_TO_BOTTOM,
            invert = true,
        };

        public static SwipeData GetSwipeDown =>
            new SwipeData {
                direction = Direction.TOP_TO_BOTTOM,
                invert = false,
            };

        public static SwipeData GetSwipeLeft =>
            new SwipeData {
                direction = Direction.LEFT_TO_RIGHT,
                invert = false,
            };

        public static SwipeData GetSwipeRight =>
            new SwipeData {
                direction = Direction.LEFT_TO_RIGHT,
                invert = true,
            };

        public override string ToString() {
            return string.Format("xDir: {0}, yDir: {1}, startX: {2}, startY: {3}, endX: {4}, endY: {5}, xRowShift:{6}, yRowShift:{7}",
                xDir, yDir, startX, startY, endX, endY, xRowShift, yRowShift);
        }
    }

    [Serializable]
    public struct GameState {
        public SwipeData previousSwipe;
        public TileData[] activeTileData;
        public TileData[] removedTileData;
        public uint score;
        public uint previousScore;
        public int size;
    }

    [Serializable]
    public struct Index {
        public int x;
        public int y;

        public Index(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public static Index Invalid => new Index(-1, -1);

        public override string ToString() {
            return x.ToString() + " : " + y.ToString();
        }
    }

    [Serializable]
    public struct LerpData<T> {
        public float lerpDuration;
        public float timeStarted;
        public T start;
        public T end;
        public float t;
    }

    [Serializable]
    public struct TileData {
        public uint value;
        public bool merged;
        public bool removed;
        public bool spawnedFromMove;
        public Index index;
        public Index removedIndex; // Need this to better keep track of Undoing. Don't like having to have two indices......

        public TileData(uint Value, Index Index, Index RemovedIndex, bool Merged, bool Spawned, bool Removed) {
            value           = Value;
            index           = Index;
            removedIndex    = RemovedIndex;
            merged          = Merged;
            spawnedFromMove = Spawned;
            removed         = Removed;
        }

        public static void FillTileData(ref TileData[] tileDatas, in List<Tile> tiles) {
            tileDatas = new TileData[tiles.Capacity];
            for(int i = 0; i < tileDatas.Length; i++) {
                tileDatas[i] = MakeTileData(tiles[i]);
            }
        }

        public static TileData MakeTileData(in Tile t) {
            if(t) {
                return new TileData(t.value, t.CurrentMove.index, t.CurrentMove.removedIndex, t.CurrentMove.merged, t.CurrentMove.spawnedFromMove, t.CurrentMove.removed);
            }
            return Empty;
        }

        public static TileData Empty => new TileData(0, Index.Invalid, Index.Invalid, false, false, false);
    }
}