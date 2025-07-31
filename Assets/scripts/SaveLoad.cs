using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace My2048 {
    public class SaveLoad : MonoBehaviour {
        string savePath;
        BinaryFormatter bf = new BinaryFormatter();
        SaveData data;

        void Awake() {
            savePath = Application.persistentDataPath + "/save.dat";
        }

        public SaveData Load(GameBoard gb, bool isNewGame = false) {
            if (!TryLoad()) {
                Debug.LogWarning("Couldn't Load a saved game, perhaps there wasn't one.");
                return new SaveData();
            }
            else {
                InitGame(gb, isNewGame);
            }
            return data;
        }

        public void InitGame(GameBoard gb, in bool isNewGame) {
            if (!isNewGame) {
                LoadBoard(gb, isNewGame);
            }
            else {
                gb.Create(isNewGame);
                data.activeTileData = new TileData[gb.Length];
                data.removedTileData = new TileData[gb.Length];
                gb.SpawnRandomTile();
                gb.SpawnRandomTile();
            }
        }

        void LoadBoard(GameBoard gb, in bool isNewGame) {
            gb.Load(data, isNewGame);
        }
        
        bool TryLoad() {
            if (File.Exists(savePath)) {
                FileStream file = File.Open(savePath, FileMode.Open);
                data = (SaveData)bf.Deserialize(file);
                file.Close();
                return true;
            }
            return false;
        }

        public void Save(in TwentyFortyEight game) {

            var gameData = new SaveData() {
                canUndo         = game.saveData.canUndo,
                score           = game.saveData.score,
                size            = game.board.size,
                previousScore   = game.saveData.previousScore,
                previousSwipe   = game.saveData.previousSwipe,
            };

            TileData.FillTileData(ref gameData.activeTileData, game.board.tiles);
            TileData.FillTileDataRemoved(ref gameData.removedTileData, game.board.removedTiles);
            
            var file = File.Create(savePath);
            bf.Serialize(file, gameData);
            file.Close();
        }
    }
}