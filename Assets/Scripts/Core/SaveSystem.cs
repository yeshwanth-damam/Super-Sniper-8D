using System;
using System.IO;
using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// The persistent player profile: credits and upgrade levels that carry
    /// across sessions, plus a couple of records. Plain serializable data so
    /// <see cref="JsonUtility"/> can round-trip it.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int credits;
        public int[] upgradeLevels = new int[4];
        public int bestScore;
        public int highestLevel;

        public int GetLevel(UpgradeTrack track)
        {
            EnsureArray();
            return upgradeLevels[(int)track];
        }

        public void SetLevel(UpgradeTrack track, int value)
        {
            EnsureArray();
            upgradeLevels[(int)track] = value;
        }

        public void EnsureArray()
        {
            if (upgradeLevels == null || upgradeLevels.Length < 4)
            {
                var fresh = new int[4];
                if (upgradeLevels != null)
                    Array.Copy(upgradeLevels, fresh, Mathf.Min(upgradeLevels.Length, 4));
                upgradeLevels = fresh;
            }
        }
    }

    /// <summary>
    /// Loads and saves the <see cref="SaveData"/> profile as JSON in the
    /// platform's persistent data path. All failures degrade gracefully to a
    /// fresh profile so a corrupt or missing file never breaks the game.
    /// </summary>
    public static class SaveSystem
    {
        static string FilePath =>
            Path.Combine(Application.persistentDataPath, "supersniper8d.save.json");

        public static SaveData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var data = JsonUtility.FromJson<SaveData>(json);
                    if (data != null)
                    {
                        data.EnsureArray();
                        return data;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SuperSniper8D] Save load failed, starting fresh: {e.Message}");
            }
            return new SaveData();
        }

        public static void Save(SaveData data)
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SuperSniper8D] Save write failed: {e.Message}");
            }
        }
    }
}
