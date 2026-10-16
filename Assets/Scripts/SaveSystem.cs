using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class CallCooldownEntry
{
    public string callName;
    public int lastResolvedDay;
}

/// <summary>ข้อมูลที่เซฟลงไฟล์ (สถานะ "ต้นวัน" ที่จะกลับมาเล่นต่อ)</summary>
[Serializable]
public class SaveData
{
    public int version = 1;
    public int day = 1;
    public int money;
    public int debt;
    public float hunger = 100f;
    public List<CallCooldownEntry> callCooldowns = new List<CallCooldownEntry>();
}

/// <summary>
/// ระบบเซฟแบบ static ไม่ต้องวางใน Scene
/// - เซฟอัตโนมัติตอนจบวัน (CallCenterManager เรียก Save ให้)
/// - Main Menu เรียก StartNewGame() / TryLoadForContinue() ก่อนโหลดซีนเกม
/// - ระบบต่างๆ (PlayerStats, GameClock, HungerSystem, CallCenterManager) อ่านค่าจาก SaveSystem.Loaded ตอนเริ่มซีน
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";

    /// ข้อมูลที่โหลดมาจาก Continue (null = เริ่มเกมใหม่)
    public static SaveData Loaded;

    /// ตั้งเป็น true ตอนกด New Game ให้ซีนเกมเขียนเซฟเริ่มต้น (วันที่ 1) ทันทีที่เริ่ม
    public static bool NewGameRequested;

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave() => ReadFile() != null;

    /// กด New Game: ลบเซฟเก่าทิ้ง แล้วให้ซีนเกมเริ่มจากศูนย์
    public static void StartNewGame()
    {
        Delete();
        NewGameRequested = true;
    }

    /// กด Continue: โหลดเซฟเข้า Loaded คืนค่า false ถ้าไม่มีเซฟ/ไฟล์เสีย
    public static bool TryLoadForContinue()
    {
        SaveData data = ReadFile();
        if (data == null) return false;

        Loaded = data;
        NewGameRequested = false;
        return true;
    }

    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);              // เขียนไฟล์ชั่วคราวก่อน กันไฟล์เซฟพังถ้าเกมปิดกลางคัน
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: บันทึกไม่สำเร็จ ({e.Message})");
        }
    }

    /// ลบเซฟทิ้ง (ใช้ตอน New Game, ตอนชนะ และตอนแพ้ในอนาคต)
    public static void Delete()
    {
        Loaded = null;
        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: ลบเซฟไม่สำเร็จ ({e.Message})");
        }
    }

    private static SaveData ReadFile()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            string json = File.ReadAllText(FilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            return data != null && data.day >= 1 ? data : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SaveSystem: อ่านเซฟไม่ได้ ({e.Message})");
            return null;
        }
    }
}
