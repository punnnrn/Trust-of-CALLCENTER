using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BillCategory
{
    Utility, // ค่าไฟ ค่าน้ำ ฯลฯ (คอลัมน์ "Bills" ในหน้าจอ)
    Rent     // ค่าเช่า (คอลัมน์ "Rents")
}

/// <summary>ชนิดของบิล 1 ชนิด ตั้งค่าใน Inspector ของ BillSystem</summary>
[Serializable]
public class BillType
{
    public string displayName = "ค่าไฟ";
    public BillCategory category = BillCategory.Utility;
    public Sprite icon; // ไม่ใส่ก็ได้
    [Min(0)] public int amountMin = 40;
    [Min(0)] public int amountMax = 70;
    [Tooltip("บิลมาแล้วถึงวันครบกำหนดอีกกี่วัน  0 = ต้องจ่ายภายในวันที่บิลมา (จ่ายช้าไม่ได้)  เลขมากกว่า 0 = จ่ายช้าได้ ถึงวันครบกำหนดแล้วหักเงินอัตโนมัติ")]
    [Min(0)] public int dueInDays = 0;
}

/// <summary>บิล 1 ใบที่ออกมาแล้วและยังไม่จ่าย (เก็บลงเซฟได้)</summary>
[Serializable]
public class Bill
{
    public int typeIndex;
    public int amount;
    public int billDay;
    public int dueDay;
}

/// <summary>
/// ระบบบิล: ตั้งแต่ First Bill Day เป็นต้นไป ทุกวันจะมีบิลใหม่ออกมา 1 ใบต่อชนิด (ค่าไฟ ค่าน้ำ ค่าเช่า ฯลฯ)
/// ผู้เล่นกดจ่ายเองได้ตลอดจากหน้าบิลในคอม ถ้าไม่จ่าย พอจบวันที่ถึงวันครบกำหนด จะหักเงินสดให้อัตโนมัติ
/// ถ้าเงินสดไม่พอหัก = แพ้เกม (CallCenterManager เรียก ProcessDayEnd ตอนจบวัน)
/// ยอดเงินของแต่ละวันสุ่มแบบคงที่ตามเลขวัน (เล่นวันเดิมซ้ำได้ยอดเดิม)
/// </summary>
public class BillSystem : MonoBehaviour
{
    public static BillSystem Instance { get; private set; }

    [Header("Bills")]
    [Tooltip("บิลใบแรกออกวันที่เท่าไหร่ (2 = เริ่มมีบิลวันที่ 2 วันที่ 1 ยังไม่มี)")]
    [SerializeField, Min(1)] private int firstBillDay = 2;

    [SerializeField] private List<BillType> types = new List<BillType>
    {
        new BillType { displayName = "ค่าไฟ",     category = BillCategory.Utility, amountMin = 40,  amountMax = 70,  dueInDays = 3 },
        new BillType { displayName = "ค่าน้ำ",     category = BillCategory.Utility, amountMin = 20,  amountMax = 35,  dueInDays = 0 },
        new BillType { displayName = "ค่าเช่าห้อง", category = BillCategory.Rent,    amountMin = 100, amountMax = 100, dueInDays = 0 },
    };

    /// บิลที่ยังไม่จ่ายเปลี่ยน (ออกใหม่/จ่ายแล้ว/ถูกหักอัตโนมัติ)
    public event Action OnBillsChanged;

    private readonly List<Bill> bills = new List<Bill>();
    private int lastBillDay;
    private int loadedLastBillDay = int.MinValue;

    public IReadOnlyList<Bill> Bills => bills;
    public int LastBillDay => lastBillDay;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        SaveData save = SaveSystem.Loaded;
        if (save != null)
        {
            if (save.bills != null) bills.AddRange(save.bills);
            loadedLastBillDay = save.lastBillDay;
        }
    }

    private void Start()
    {
        int day = GameClock.Instance != null ? GameClock.Instance.Day : 1;

        // เซฟเก่าที่ไม่มีข้อมูลบิล (ค่า -1): ให้ถือว่าบิลก่อนวันนี้ไม่มี ไม่ต้องออกย้อนหลังรวดเดียว
        if (loadedLastBillDay == -1) lastBillDay = day - 1;
        else if (loadedLastBillDay != int.MinValue) lastBillDay = loadedLastBillDay;

        EnsureBillsForDay(day);

        if (GameClock.Instance != null)
            GameClock.Instance.OnDayStarted += EnsureBillsForDay;
    }

    private void OnDestroy()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnDayStarted -= EnsureBillsForDay;
        if (Instance == this) Instance = null;
    }

    // ---------- Public API ----------

    public BillType TypeOf(Bill bill) =>
        bill != null && bill.typeIndex >= 0 && bill.typeIndex < types.Count ? types[bill.typeIndex] : null;

    public int UnpaidCount => bills.Count;

    /// จำนวนบิลที่ถึงกำหนดภายในวันนี้ (จะถูกหักตอนจบวัน ถ้ายังไม่จ่าย)
    public int DueTodayCount(int today) => bills.Count(b => b.dueDay <= today);

    /// ผู้เล่นกดจ่ายบิล: หักเงินสดแล้วเอาบิลออก คืน false ถ้าเงินไม่พอ
    public bool TryPay(Bill bill)
    {
        if (bill == null || !bills.Contains(bill)) return false;
        if (PlayerStats.Instance == null || !PlayerStats.Instance.TrySpend(bill.amount)) return false;

        bills.Remove(bill);
        OnBillsChanged?.Invoke();
        return true;
    }

    /// เรียกตอนจบวัน: หักเงินอัตโนมัติสำหรับบิลที่ถึงกำหนด (dueDay <= วันนี้) เรียงตามวันครบกำหนด
    /// คืน true ถ้าหักครบ  คืน false ถ้าเงินสดไม่พอ (failedBillName = ชื่อบิลที่จ่ายไม่ไหว)
    public bool ProcessDayEnd(int today, out int autoPaidTotal, out string failedBillName)
    {
        autoPaidTotal = 0;
        failedBillName = null;

        if (PlayerStats.Instance == null) return true;

        List<Bill> due = bills.Where(b => b.dueDay <= today)
                              .OrderBy(b => b.dueDay).ThenBy(b => b.amount).ToList();

        foreach (Bill bill in due)
        {
            if (!PlayerStats.Instance.TrySpend(bill.amount))
            {
                BillType t = TypeOf(bill);
                failedBillName = t != null ? t.displayName : "บิล";
                OnBillsChanged?.Invoke();
                return false;
            }

            autoPaidTotal += bill.amount;
            bills.Remove(bill);
        }

        OnBillsChanged?.Invoke();
        return true;
    }

    // ---------- Internal ----------

    /// ออกบิลของทุกวันที่ยังไม่ได้ออกจนถึง day (ถ้าเรียกซ้ำวันเดิมจะไม่ออกซ้ำ)
    private void EnsureBillsForDay(int day)
    {
        bool changed = false;

        while (lastBillDay < day)
        {
            lastBillDay++;
            if (lastBillDay >= firstBillDay)
            {
                GenerateBills(lastBillDay);
                changed = true;
            }
        }

        if (changed) OnBillsChanged?.Invoke();
    }

    private void GenerateBills(int day)
    {
        for (int i = 0; i < types.Count; i++)
        {
            BillType t = types[i];
            if (t == null) continue;

            int min = Mathf.Max(0, t.amountMin);
            int max = Mathf.Max(min, t.amountMax);
            System.Random rng = new System.Random(day * 100 + i); // ยอดคงที่ตามวัน กัน Continue เพื่อสุ่มยอดใหม่

            bills.Add(new Bill
            {
                typeIndex = i,
                amount = rng.Next(min, max + 1),
                billDay = day,
                dueDay = day + Mathf.Max(0, t.dueInDays)
            });
        }
    }
}
