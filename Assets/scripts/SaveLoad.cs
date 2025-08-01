using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace My2048 {
    public static class SaveLoad {
        static string SavePath => $"{Application.persistentDataPath}/save.dat";
        static readonly BinaryFormatter bf = new BinaryFormatter();

        public static bool TryLoad(out SaveData saveData) {

            if (File.Exists(SavePath)) {
                var file = File.Open(SavePath, FileMode.Open);
                saveData = (SaveData)bf.Deserialize(file);
                file.Close();
                return true;
            }
            else {
                Debug.LogWarning("Couldn't load a saved game, perhaps there wasn't one.");
            }

            saveData = default;
            return false;
        }
        
        public static void Save(in TwentyFortyEight game) {

            var gameData = new SaveData() {
                canUndo         = game.saveData.canUndo,
                score           = game.saveData.score,
                size            = game.board.size,
                previousScore   = game.saveData.previousScore,
                previousSwipe   = game.saveData.previousSwipe,
            };

            TileData.FillTileData(ref gameData.activeTileData, game.board.tiles);
            TileData.FillTileDataRemoved(ref gameData.removedTileData, game.board.removedTiles);
            
            var file = File.Create(SavePath);
            bf.Serialize(file, gameData);
            file.Close();
        }
    }
}