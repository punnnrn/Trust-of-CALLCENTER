using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// สีหน้าของตัวละครหลัก ชื่อไฟล์รูปใน Assets/Resources/Expressions ต้องเป็นชื่อเหล่านี้แบบตัวพิมพ์เล็ก
/// (neutral, flat, smirk, grin, angry, sweat) ไฟล์ที่ลากเข้าโฟลเดอร์นั้นจะถูกตั้งชื่อให้เองอัตโนมัติ
/// </summary>
public enum FaceExpression
{
    Neutral = 0, // หน้าปกติ
    Flat = 1,    // หน้านิ่ง เฉย ๆ ไร้พลัง
    Smirk = 2,   // ยิ้มมุมปาก
    Grin = 3,    // ยิ้มกว้าง ดีใจ
    Angry = 4,   // โกรธ
    Sweat = 5    // เหงื่อตก กังวล
}

/// <summary>โหลดรูปสีหน้าจาก Resources/Expressions (ถ้าไฟล์ไหนยังไม่มี จะถอยไปใช้ Neutral)</summary>
public static class ExpressionLibrary
{
    private const string Folder = "Expressions/";
    private static readonly Dictionary<FaceExpression, Sprite> cache = new Dictionary<FaceExpression, Sprite>();
    private static bool loaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Reload();
    }

    public static void Reload()
    {
        loaded = false;
        cache.Clear();
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        foreach (FaceExpression e in Enum.GetValues(typeof(FaceExpression)))
            cache[e] = Resources.Load<Sprite>(Folder + e.ToString().ToLowerInvariant());
    }

    /// รูปของสีหน้านั้นตรง ๆ (ไม่มีไฟล์ = null)
    public static Sprite Get(FaceExpression e)
    {
        EnsureLoaded();
        Sprite s;
        return cache.TryGetValue(e, out s) ? s : null;
    }

    public static bool Has(FaceExpression e)
    {
        return Get(e) != null;
    }

    /// มีสีหน้ากี่แบบที่ใช้ได้จริง
    public static int Count
    {
        get
        {
            EnsureLoaded();
            int n = 0;
            foreach (KeyValuePair<FaceExpression, Sprite> kv in cache)
                if (kv.Value != null) n++;
            return n;
        }
    }

    /// รูปของสีหน้านั้น ถ้าไม่มีไฟล์ ถอยไป Neutral ถ้ายังไม่มีอีกใช้ fallback
    public static Sprite Resolve(FaceExpression e, Sprite fallback = null)
    {
        Sprite s = Get(e);
        if (s == null) s = Get(FaceExpression.Neutral);
        return s != null ? s : fallback;
    }
}
