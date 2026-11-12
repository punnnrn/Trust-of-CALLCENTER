using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// หลอดความหิว: ลดลงตามเวลาในเกม และลดลงทุกครั้งที่ "ทำงานเสร็จ" (รับสายแล้วคุยจนจบ 1 สาย)
/// เรียก OnWorkCompleted() ทุกครั้งที่งานเสร็จ 1 ชิ้น (CallCenterManager เป็นคนเรียกให้อัตโนมัติ)
/// 100 = อิ่ม, 0 = หิวจัด
/// </summary>
public class HungerSystem : MonoBehaviour
{
    public static HungerSystem Instance { get; private set; }

    [Header("Hunger Values")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float startHunger = 100f;

    [Header("Drain")]
    [Tooltip("ความหิวที่ลดลงทุกครั้งที่ทำงานเสร็จ 1 ชิ้น (1 สายที่คุยจนจบ)")]
    [SerializeField] private float drainPerWork = 8f;
    [Tooltip("ความหิวที่ลดลงต่อ 1 ชั่วโมงในเกม (นอกเหนือจากการทำงาน)")]
    [SerializeField] private float drainPerHour = 1.5f;

    [Header("Hunger Effects")]
    [Tooltip("ต่ำกว่าสัดส่วนนี้ของหลอด = หิว: ได้เงินจากงานลดลง + ขึ้นข้อความเตือน")]
    [SerializeField, Range(0f, 1f)] private float hungryThreshold = 0.25f;
    [Tooltip("ต่ำกว่าสัดส่วนนี้ = หิวจัด: ได้เงินลดลงมาก และถ้าจบวันแบบนี้จะนับเป็นวันอดอยาก (StarvationSystem)")]
    [SerializeField, Range(0f, 1f)] private float famishedThreshold = 0.10f;
    [Tooltip("ตัวคูณเงินจากงานตอนหิว (0.75 = ได้ 75%)")]
    [SerializeField, Range(0f, 1f)] private float hungryIncomeMultiplier = 0.75f;
    [Tooltip("ตัวคูณเงินจากงานตอนหิวจัด")]
    [SerializeField, Range(0f, 1f)] private float famishedIncomeMultiplier = 0.5f;

    [Header("UI (optional)")]
    [SerializeField] private Slider hungerBar;
    [SerializeField] private Image fillImage;
    [SerializeField] private Color fullColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color lowColor = new Color(0.9f, 0.25f, 0.2f);
    [SerializeField] private TMP_Text hungerText;

    // ---------- Events ----------
    /// ค่าความหิวเปลี่ยน (ค่าปัจจุบัน 0..max)
    public event Action<float> OnHungerChanged;
    /// เพิ่งหิวจนถึง 0 (ยิงครั้งเดียวตอนเปลี่ยนสถานะ)
    public event Action OnStarving;
    /// มีการกินอาหาร (เรียก Eat) ทุกครั้ง ไม่ว่าความหิวจะเต็มอยู่แล้วหรือไม่ ใช้นับว่า "วันนี้ได้กินแล้ว"
    public event Action OnAte;
    /// เพิ่งกินจนพ้นสถานะหิวจัด
    public event Action OnRecovered;

    public float Current { get; private set; }
    public float Max => maxHunger;
    public float Normalized => maxHunger <= 0f ? 0f : Current / maxHunger;
    public bool IsStarving => Current <= 0f;
    /// หิว: ต่ำกว่า hungryThreshold
    public bool IsHungry => Normalized <= hungryThreshold;
    /// หิวจัด: ต่ำกว่า famishedThreshold (ตอนจบวันแบบนี้ = นับเป็นวันอดอยาก)
    public bool IsFamished => Normalized <= famishedThreshold;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        float initial = SaveSystem.Loaded != null ? SaveSystem.Loaded.hunger : startHunger;
        Current = Mathf.Clamp(initial, 0f, maxHunger);
    }

    private void Start()
    {
        RefreshUI();
        if (GameClock.Instance != null) GameClock.Instance.OnHourChanged += HandleHourChanged;
    }

    // ทุกชั่วโมงในเกม ความหิวลดลงเอง
    private void HandleHourChanged(int hour) => Drain(drainPerHour);

    private void OnDestroy()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnHourChanged -= HandleHourChanged;
        if (Instance == this) Instance = null;
    }

    // ---------- Public API ----------

    /// เรียกทุกครั้งที่ "ทำงานเสร็จ" 1 ชิ้น (เช่น คุยสายจนจบแล้วตัดสินใจเสร็จ 1 สาย)
    public void OnWorkCompleted() => Change(-drainPerWork);

    /// กินอาหาร เพิ่มความอิ่ม (เรียกจากร้านค้า)
    public void Eat(float amount)
    {
        Change(+Mathf.Abs(amount));
        OnAte?.Invoke();
    }

    /// ลดความหิวแบบทันที (เช่น อีเวนต์พิเศษ)
    public void Drain(float amount) => Change(-Mathf.Abs(amount));

    /// เติมความหิวให้เต็ม (ใช้ตอนนอน/จบวัน)
    public void RefillFull() => Change(maxHunger - Current);

    /// เติมความหิวขึ้นไปถึงสัดส่วนที่กำหนด (0-1 ของหลอด) ถ้าตอนนี้อิ่มกว่านั้นอยู่แล้วจะไม่ลด
    /// (ใช้ตอนโดนบังคับนอนเพราะหมดเวลา: เติมไม่เต็มหลอด)
    public void RefillTo(float normalized)
    {
        float target = Mathf.Clamp01(normalized) * maxHunger;
        if (target > Current) Change(target - Current);
    }

    /// เติมความหิวเพิ่มขึ้นตามจำนวนที่กำหนด (ใช้ตอนนอน: เติมได้ไม่เต็ม ต้องซื้ออาหารเพิ่ม)
    public void RefillBy(float amount) => Change(Mathf.Abs(amount));

    /// ปรับเงินที่ได้จากงานตามความหิว (เฉพาะเงินที่ได้เพิ่ม ถ้าเป็นเงินที่เสียจะไม่แตะ)
    public int AdjustIncome(int amount)
    {
        if (amount <= 0) return amount;
        float mul = IsFamished ? famishedIncomeMultiplier : IsHungry ? hungryIncomeMultiplier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(amount * mul));
    }

    // ---------- Internal ----------

    private void Change(float delta)
    {
        bool wasStarving = IsStarving;
        bool wasHungry = IsHungry;
        bool wasFamished = IsFamished;

        float next = Mathf.Clamp(Current + delta, 0f, maxHunger);
        if (Mathf.Approximately(next, Current)) return;

        Current = next;
        RefreshUI();
        OnHungerChanged?.Invoke(Current);

        if (!wasStarving && IsStarving) OnStarving?.Invoke();
        else if (wasStarving && !IsStarving) OnRecovered?.Invoke();

        // เตือนตอนหลอดเพิ่งตกลงมาถึงระดับหิว / หิวจัด
        if (!wasFamished && IsFamished)
            NotificationBanner.Show($"หิวจัดแล้ว! เงินจากงานเหลือ {Mathf.RoundToInt(famishedIncomeMultiplier * 100f)}% รีบซื้ออาหาร", 5f);
        else if (!wasHungry && IsHungry)
            NotificationBanner.Show($"เริ่มหิวแล้ว! เงินจากงานลดเหลือ {Mathf.RoundToInt(hungryIncomeMultiplier * 100f)}% ควรซื้ออาหาร", 5f);
    }

    private void RefreshUI()
    {
        if (hungerBar != null)
        {
            hungerBar.minValue = 0f;
            hungerBar.maxValue = maxHunger;
            hungerBar.value = Current;
        }

        if (fillImage != null)
            fillImage.color = Color.Lerp(lowColor, fullColor, Normalized);

        if (hungerText != null)
            hungerText.text = $"ความหิว {Mathf.RoundToInt(Normalized * 100f)}%";
    }
}
