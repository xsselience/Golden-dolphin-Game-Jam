using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveManager : MonoBehaviour
{
    public const int MAX_SLOTS = 4;
    private static string saveFolder = Application.persistentDataPath + "/Saves";

    public static string GetSlotPath(int slot) => $"{saveFolder}/slot_{slot}.json";

    public static bool SlotExists(int slot) => File.Exists(GetSlotPath(slot));

    public static void Save(int slot, SaveData data)
    {
        Directory.CreateDirectory(saveFolder);
        data.isEmpty = false;
        data.saveTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSlotPath(slot), json);
    }

    public static SaveData Load(int slot)
    {
        if (!SlotExists(slot)) return null;
        string json = File.ReadAllText(GetSlotPath(slot));
        return JsonUtility.FromJson<SaveData>(json);
    }

    public static void Delete(int slot)
    {
        if (SlotExists(slot))
            File.Delete(GetSlotPath(slot));
    }

    public static List<SaveData> GetAllSaves()
    {
        List<SaveData> list = new List<SaveData>();
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            SaveData d = Load(i);
            list.Add(d ?? new SaveData());  // null 时给空存档占位
        }
        return list;
    }

    /// <summary>返回最近一次存档的槽位，没有则 -1</summary>
    public static int GetLatestSlot()
    {
        int latest = -1;
        DateTime latestTime = DateTime.MinValue;
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            SaveData d = Load(i);
            if (d != null && !d.isEmpty)
            {
                DateTime t = DateTime.Parse(d.saveTime);
                if (t > latestTime)
                {
                    latestTime = t;
                    latest = i;
                }
            }
        }
        return latest;
    }
}
