using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// หลอดความหิว: ไม่ลดไปตามเวลาแล้ว แต่ลดลงทุกครั้งที่ "ทำงานเสร็จ" (รับสายแล้วคุยจนจบ 1 สาย)
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
    /// เพิ่งกินจนพ้นสถานะหิวจัด
    public event Action OnRecovered;

    public float Current { get; private set; }
    public float Max => maxHunger;
    public float Normalized => maxHunger <= 0f ? 0f : Current / maxHunger;
    public bool IsStarving => Current <= 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        float initial = SaveSystem.Loaded != null ? SaveSystem.Loaded.hunger : startHunger;
        Current = Mathf.Clamp(initial, 0f, maxHunger);
    }

    private void Start() => RefreshUI();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------- Public API ----------

    /// เรียกทุกครั้งที่ "ทำงานเสร็จ" 1 ชิ้น (เช่น คุยสายจนจบแล้วตัดสินใจเสร็จ 1 สาย)
    public void OnWorkCompleted() => Change(-drainPerWork);

    /// กินอาหาร เพิ่มความอิ่ม (เรียกจากร้านค้า)
    public void Eat(float amount) => Change(+Mathf.Abs(amount));

    /// ลดความหิวแบบทันที (เช่น อีเวนต์พิเศษ)
    public void Drain(float amount) => Change(-Mathf.Abs(amount));

    /// เติมความหิวให้เต็ม (ใช้ตอนนอน/จบวัน)
    public void RefillFull() => Change(maxHunger - Current);

    // ---------- Internal ----------

    private void Change(float delta)
    {
        bool wasStarving = IsStarving;

        float next = Mathf.Clamp(Current + delta, 0f, maxHunger);
        if (Mathf.Approximately(next, Current)) return;

        Current = next;
        RefreshUI();
        OnHungerChanged?.Invoke(Current);

        if (!wasStarving && IsStarving) OnStarving?.Invoke();
        else if (wasStarving && !IsStarving) OnRecovered?.Invoke();
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
