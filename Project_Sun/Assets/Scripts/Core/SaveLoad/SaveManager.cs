using System;
using System.IO;
using ProjectSun.V2.Data;
using UnityEngine;

namespace ProjectSun.V2.Core
{
    public static class SaveManager
    {
        private const string SaveFileName = "save_slot_0.json";

        private static string SaveFilePath =>
            Path.Combine(Application.persistentDataPath, SaveFileName);

        public static bool Save(GameState state)
        {
            if (state.currentPhase != PhaseType.Day)
            {
                Debug.LogWarning("[SaveManager] Save is only allowed during Day phase.");
                return false;
            }

            try
            {
                var saveData = SaveData.Create(state);
                var json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveManager] Game saved to {SaveFilePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e.Message}");
                return false;
            }
        }

        public static GameState Load()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.LogWarning("[SaveManager] No save file found.");
                return null;
            }

            try
            {
                var json = File.ReadAllText(SaveFilePath);
                var saveData = JsonUtility.FromJson<SaveData>(json);

                if (saveData == null || saveData.gameState == null)
                {
                    Debug.LogError("[SaveManager] Failed to deserialize save data.");
                    return null;
                }

                Debug.Log($"[SaveManager] Game loaded from {SaveFilePath} (v{saveData.saveVersion}, saved at {saveData.timestamp})");
                return saveData.gameState;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e.Message}");
                return null;
            }
        }

        public static bool HasSave()
        {
            return File.Exists(SaveFilePath);
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                    Debug.Log("[SaveManager] Save file deleted.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Delete failed: {e.Message}");
            }
        }
    }
}
