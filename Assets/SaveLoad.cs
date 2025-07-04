using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

[System.Serializable]
public struct GameData
{
    public bool canUndo;
    public SwipeData previousSwipe;
    public TileData[] activeTileData;
    public TileData[] removedTileData;
    public uint score;
    public uint previousScore;
    public int size;
}

namespace My2048
{
    public class SaveLoad : MonoBehaviour
    {
        string savePath;
        BinaryFormatter bf = new BinaryFormatter();
        GameData data;

        void Awake()
        {
            savePath = Application.persistentDataPath + "/save.dat";
        }

        public GameData Load(GameBoard board, bool isNewGame)
        {
            if (!TryLoad())
            {
                Debug.LogWarning("Couldn't Load a saved game, perhaps there wasn't one.");
                return new GameData();
            }
            else
            {
                InitGame(board, isNewGame);
            }
            return data;
        }

        public void InitGame(GameBoard gb, in bool isNewGame)
        {
            if (!isNewGame)
            {
                LoadBoard(gb, isNewGame);
            }
            else
            {
                GameBoard.Create(gb, gameObject, isNewGame);
                data.activeTileData = new TileData[gb.length];
                data.removedTileData = new TileData[gb.length];
                gb.SpawnRandomTile();
                gb.SpawnRandomTile();
            }
        }

        void LoadBoard(GameBoard gb, in bool isNewGame)
        {
            GameBoard.Load(gb, gameObject, data, isNewGame);
        }
        
        bool TryLoad()
        {
            if (File.Exists(savePath)) {
                FileStream file = File.Open(savePath, FileMode.Open);
                data = (GameData)bf.Deserialize(file);
                file.Close();
                return true;
            }
            return false;
        }

        public void Save(TwentyFortyEight game)
        {
            var gameData = new GameData {
                activeTileData  = new TileData[game.board.length],
                removedTileData = new TileData[game.board.length]
            };

            for (int x = 0; x < game.board.size; x++)
            {
                for (int y = 0; y < game.board.size; y++)
                {
                    Tile t = game.board[x, y];
                    Tile r = game.board.removedTiles[x, y];
                    TileData d;
                    if (t)
                    {
                        d = new TileData(t.value, t.index, t.currentMove.index, t.otherTileIndex, t.currentMove.merged, t.currentMove.spawnedFromMove);
                    }
                    else
                    {
                        d = new TileData(0, Index.Invalid, Index.Invalid, Index.Invalid, false, false);
                    }

                    gameData.activeTileData[x + y * game.board.size] = d;

                    if (r)
                    {
                        d = new TileData(r.value, r.index, r.currentMove.index, r.otherTileIndex, false, false);
                    }
                    else
                    {
                        d = new TileData(0, Index.Invalid, Index.Invalid, Index.Invalid, false, false);
                    }

                    gameData.removedTileData[x + y * game.board.size] = d;
                }
            }
            gameData.canUndo        = game.gameData.canUndo;
            gameData.score          = game.gameData.score;
            gameData.size           = game.board.size;
            gameData.previousScore  = game.gameData.previousScore;
            gameData.previousSwipe  = game.gameData.previousSwipe;
            
            FileStream file = File.Create(savePath);
            bf.Serialize(file, gameData);
            file.Close();
        }
    }
}