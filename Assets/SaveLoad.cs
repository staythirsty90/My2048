using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

[System.Serializable]
public struct GameData
{
    public bool canUndo;
    public SwipeData previousSwipe;
    public tileData[] activeTileData;
    public tileData[] removedTileData;
    public uint score;
    public uint previousScore;
    public int size;
}


namespace My2048
{
    public class SaveLoad : MonoBehaviour
    {
        static string savePath;
        static BinaryFormatter bf = new BinaryFormatter();
        static GameData data;

        public static string SavePath {
            get { return savePath; }
        }

        private void Awake()
        {
            savePath = Application.persistentDataPath + "/save.dat";
        }

        public GameData Load(GameBoard board, bool newGame)
        {
            if (!TryLoad())
            {
                Debug.LogWarning("there was no saved game");
                return new GameData();
            }
            else
            {
                initGame(board, newGame);
            }
            return data;
        }

        public void initGame(GameBoard board, bool newGame)
        {
            if (!newGame)
            {
                loardBoard(board, newGame);
            }
            else
            {
                GameBoard.create(board, this.gameObject, newGame);
                data.activeTileData = new tileData[board.length];
                data.removedTileData = new tileData[board.length];
                board.spawnRandomTile(false);
                board.spawnRandomTile(false);
            }
            ////GameBoard.load(board, this.gameObject, data, newGame);
        }

        private void loardBoard(GameBoard board, bool newGame)
        {
            GameBoard.load(board, this.gameObject, data, newGame);
            //data.canUndo = data.canUndo;
            //data.score = data.score;
            //data.previousScore = data.previousScore;
            //data.previousSwipe = data.previousSwipe;
        }
        private static bool TryLoad()
        {
            if (File.Exists(savePath)) {
                FileStream file = File.Open(savePath, FileMode.Open);
                data = (GameData)bf.Deserialize(file);
                file.Close();
                return true;
            }
            return false;
        }

        public static void Save(TwentyFortyEight game)
        {
            GameData gameData = new GameData();
            gameData.activeTileData = new tileData[game.board.length];
            gameData.removedTileData = new tileData[game.board.length];

            for (int x = 0; x < game.board.size; x++)
            {
                for (int y = 0; y < game.board.size; y++)
                {
                    Tile t = game.board[x, y];
                    Tile r = game.board.removedTiles[x, y];
                    tileData d;
                    if (t)
                    {
                        d = new tileData(t.value, t.index, t.currentMove.index, t.otherTileIndex, t.currentMove.merged, t.currentMove.spawnedFromMove);
                    }
                    else
                    {
                        d = new tileData(0, new Index(-1, -1), new Index(-1, -1), new Index(-1, -1), false, false);
                    }

                    gameData.activeTileData[x + y * game.board.size] = d;

                    if (r)
                    {
                        d = new tileData(r.value, r.index, r.currentMove.index, r.otherTileIndex, false, false);
                    }
                    else
                    {
                        d = new tileData(0, new Index(-1, -1), new Index(-1, -1), new Index(-1, -1), false, false);
                    }

                    gameData.removedTileData[x + y * game.board.size] = d;
                }
            }
            gameData.canUndo = game.gameData.canUndo;
            gameData.score = game.gameData.score;
            gameData.size = game.board.size;
            gameData.previousScore = game.gameData.previousScore;
            gameData.previousSwipe = game.gameData.previousSwipe;
            FileStream file = File.Create(savePath);
            bf.Serialize(file, gameData);
            file.Close();
        }
    }
}

