using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using My2048;

namespace Tests
{
    public class MovementTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void MovementTestSimplePasses() {
            TwentyFortyEight game = new GameObject().AddComponent<TwentyFortyEight>();

            var gb = new GameBoard {
                size = 4
            };
            var maxIndex = gb.size - 1;
            var prefab = new GameObject();
            prefab.AddComponent<Tile>();
            gb.tilePrefab = prefab.GetComponent<Tile>();

            gb.Create();

            Assert.NotNull(gb.tiles);
            Assert.NotZero(gb.size);
            Assert.NotZero(gb.Length);

            var tile = gb.SpawnTile(4, 2, false);
            Assert.NotZero(gb.tiles.Count);

            // Game was expecting a GameBoard to be set on Start()
            game.board = gb;

            MoveData.Init(gb.size);
            game.tileStack = new Stack<Tile>(gb.size);

            Assert.AreNotEqual(MoveData.Up, default(MoveData));
            Assert.AreNotEqual(MoveData.Left, default(MoveData));
            Assert.AreNotEqual(MoveData.Right, default(MoveData));
            Assert.AreNotEqual(MoveData.Down, default(MoveData));

            Assert.NotNull(game.tileStack);
            Assert.Zero(game.tileStack.Count);

            MoveDown(game);
            Assert.IsNull(gb.tiles[4]); // Old index before moving.
            Assert.AreEqual(new Index(0, 0), tile.CurrentMove.indexEnd);

            MoveUp(game);
            Assert.AreEqual(new Index(0, maxIndex), tile.CurrentMove.indexEnd);

            MoveRight(game);
            Assert.AreEqual(new Index(maxIndex, maxIndex), tile.CurrentMove.indexEnd);

            var tile2 = gb.SpawnTile(4, 2, false);

            var tcount = 0;
            foreach(var t in gb.tiles) {
                if(t) {
                    tcount++;
                }
            }

            Assert.AreEqual(2, tcount);

            MoveRight(game); // again
            Assert.AreEqual(new Index(maxIndex, maxIndex), tile.CurrentMove.indexEnd);
            Assert.AreEqual(tile.CurrentMove.indexEnd, tile.CurrentMove.index);

            MoveLeft(game);
            Assert.AreEqual(new Index(0, maxIndex), tile.CurrentMove.indexEnd);
            Assert.AreEqual(new Index(maxIndex, maxIndex), tile.CurrentMove.index);
        }

        private static void MoveRight(TwentyFortyEight game) {
            game.MoveTiles(MoveData.Right);
            game.IsMoving = false;
        }

        private static void MoveLeft(TwentyFortyEight game) {
            game.MoveTiles(MoveData.Left);
            game.IsMoving = false;
        }

        private static void MoveUp(TwentyFortyEight game) {
            game.MoveTiles(MoveData.Up);
            game.IsMoving = false;
        }

        private static void MoveDown(TwentyFortyEight game) {
            game.MoveTiles(MoveData.Down);
            game.IsMoving = false;
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator MovementTestWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
