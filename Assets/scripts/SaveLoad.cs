using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace My2048 {
    public class SaveLoad : MonoBehaviour {
        string savePath;
        BinaryFormatter bf = new BinaryFormatter();
        GameData data;

        void Awake() {
            savePath = Application.persistentDataPath + "/save.dat";
        }

        public GameData Load(GameBoard board, bool isNewGame) {
            if (!TryLoad()) {
                Debug.LogWarning("Couldn't Load a saved game, perhaps there wasn't one.");
                return new GameData();
            }
            else {
                InitGame(board, isNewGame);
            }
            return data;
        }

        public void InitGame(GameBoard gb, in bool isNewGame) {
            if (!isNewGame) {
                LoadBoard(gb, isNewGame);
            }
            else {
                GameBoard.Create(gb, gameObject, isNewGame);
                data.activeTileData = new TileData[gb.length];
                data.removedTileData = new TileData[gb.length];
                gb.SpawnRandomTile();
                gb.SpawnRandomTile();
            }
        }

        void LoadBoard(GameBoard gb, in bool isNewGame) {
            GameBoard.Load(gb, gameObject, data, isNewGame);
        }
        
        bool TryLoad() {
            if (File.Exists(savePath)) {
                FileStream file = File.Open(savePath, FileMode.Open);
                data = (GameData)bf.Deserialize(file);
                file.Close();
                return true;
            }
            return false;
        }

        public void Save(in TwentyFortyEight game) {

            var gameData = new GameData() {
                canUndo         = game.gameData.canUndo,
                score           = game.gameData.score,
                size            = game.board.size,
                previousScore   = game.gameData.previousScore,
                previousSwipe   = game.gameData.previousSwipe,
            };

            TileData.FillTileData(ref gameData.activeTileData, game.board.tiles);
            TileData.FillTileDataRemoved(ref gameData.removedTileData, game.board.removedTiles);
            
            var file = File.Create(savePath);
            bf.Serialize(file, gameData);
            file.Close();
        }
    }
}