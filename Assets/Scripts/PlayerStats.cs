using System;
using TMPro;
using UnityEngine;

/// <summary>
/// เก็บเงิน/หนี้กลาง ให้ CallCenterManager และ UI อื่นๆ แก้ค่าเดียวกันได้
/// เงินกับหนี้เป็นคนละก้อน: มีเงินเท่าไหร่ไม่ได้หักหนี้อัตโนมัติ ผู้เล่นต้องกด "จ่ายหนี้" เอง
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Start Values")]
    [SerializeField] private int startMoney = 0;
    [Tooltip("หนี้ตั้งต้นตอนเริ่มเกม (ค่าบวก เช่น 5000 = ติดหนี้ 5000)")]
    [SerializeField] private int startingDebt = 5000;

    [Header("UI (optional)")]
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private string statsFormat = "เงิน {0}    หนี้คงเหลือ {1}";

    public int Money { get; private set; }
    public int Debt { get; private set; }

    /// หนี้หมดแล้วหรือยัง (ใช้เช็คเงื่อนไขชนะเกม)
    public bool IsDebtCleared => Debt <= 0;

    public event Action<int> OnMoneyChanged;
    public event Action<int> OnDebtChanged;
    /// ยิงครั้งเดียวตอนหนี้เพิ่งลดจาก >0 มาเป็น 0 (ใช้เป็นจุดเริ่มหน้าชนะเกม)
    public event Action OnDebtCleared;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Continue: ใช้ค่าจากไฟล์เซฟ / New Game: ใช้ค่าตั้งต้นใน Inspector
        SaveData save = SaveSystem.Loaded;
        Money = save != null ? save.money : startMoney;
        Debt = Mathf.Max(0, save != null ? save.debt : startingDebt);
    }

    private void Start() => RefreshUI();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// เพิ่ม/ลดเงินสด (ใช้กับผลลัพธ์จากสายต่างๆ) ไม่กระทบหนี้โดยตรง
    public void AddMoney(int amount)
    {
        if (amount == 0) return;
        Money += amount;
        OnMoneyChanged?.Invoke(Money);
        RefreshUI();
    }

    /// เพิ่มหนี้ตรงๆ (เผื่อทำสายหลอกที่ทำให้หนี้งอกในอนาคต) ไม่ลดต่ำกว่า 0
    public void AddDebt(int amount)
    {
        if (amount == 0) return;
        int before = Debt;
        Debt = Mathf.Max(0, Debt + amount);
        OnDebtChanged?.Invoke(Debt);
        RefreshUI();
        if (before > 0 && Debt <= 0) OnDebtCleared?.Invoke();
    }

    /// จ่ายหนี้ด้วยเงินที่มี: จ่ายได้ไม่เกินเงินสดที่มี และไม่เกินยอดหนี้ที่เหลือ
    /// คืนค่าจำนวนเงินที่จ่ายไปจริง (เผื่อเอาไปโชว์ผลใน UI)
    public int PayDebt(int amount)
    {
        if (amount <= 0 || Debt <= 0 || Money <= 0) return 0;

        int actualPay = Mathf.Min(amount, Money, Debt);
        if (actualPay <= 0) return 0;

        Money -= actualPay;
        int before = Debt;
        Debt -= actualPay;

        OnMoneyChanged?.Invoke(Money);
        OnDebtChanged?.Invoke(Debt);
        RefreshUI();

        if (before > 0 && Debt <= 0) OnDebtCleared?.Invoke();

        return actualPay;
    }

    /// จ่ายหนี้ให้มากที่สุดเท่าที่เงินสดตอนนี้จะจ่ายได้ (ปุ่ม "จ่ายหนี้ทั้งหมดที่มี")
    public int PayDebtWithAllMoney() => PayDebt(Money);

    private void RefreshUI()
    {
        if (statsText != null)
            statsText.text = string.Format(statsFormat, Money, Debt);
    }
}
