using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ทำให้ภาพห้องมืดลงตามเวลาในเกม (เช้า -> สาย -> เย็น -> กลางคืน)
/// วางสคริปต์นี้บน Image เต็มจอที่อยู่ถัดจาก Room (ใต้ UI อื่นๆ) แล้วมันจะเปลี่ยนสี/ความทึบของ Image ให้เอง
/// ปรับสีและเวลาได้ที่ลิสต์ keys ใน Inspector (hour = ชั่วโมงในเกม, alpha = ความมืด)
/// </summary>
[RequireComponent(typeof(Image))]
public class DayNightTint : MonoBehaviour
{
    [System.Serializable]
    public struct TintKey
    {
        public float hour;
        public Color color;
        public TintKey(float h, Color c) { hour = h; color = c; }
    }

    [SerializeField]
    private TintKey[] keys = new TintKey[]
    {
        new TintKey(9f,  new Color(1.00f, 0.90f, 0.70f, 0.06f)), // เช้า: แสงอุ่นจางๆ
        new TintKey(12f, new Color(1.00f, 1.00f, 1.00f, 0.00f)), // เที่ยง: สว่างปกติ
        new TintKey(15f, new Color(1.00f, 0.80f, 0.50f, 0.12f)), // บ่ายแก่: เริ่มอมส้ม
        new TintKey(17f, new Color(0.95f, 0.45f, 0.20f, 0.30f)), // เย็น: ส้มอ่อนตะวันตกดิน
        new TintKey(18f, new Color(0.06f, 0.08f, 0.26f, 0.68f)), // กลางคืน: น้ำเงินเข้ม มืด
    };

    [Tooltip("ยิ่งมาก ยิ่งเปลี่ยนสีเร็ว")]
    [SerializeField] private float smoothSpeed = 1.5f;

    private Image img;
    private GameClock clock;
    private Color target;

    private void Awake()
    {
        img = GetComponent<Image>();
        img.raycastTarget = false; // ไม่บังการกดปุ่ม
    }

    private void Start()
    {
        clock = GameClock.Instance;
        if (clock == null) { enabled = false; return; }

        clock.OnMinuteChanged += HandleChanged;
        clock.OnDayStarted += HandleDay;
        clock.OnDayEnded += HandleDay;
        Refresh(true);
    }

    private void OnDestroy()
    {
        if (clock == null) return;
        clock.OnMinuteChanged -= HandleChanged;
        clock.OnDayStarted -= HandleDay;
        clock.OnDayEnded -= HandleDay;
    }

    private void HandleChanged(int hour, int minute) => Refresh(false);
    private void HandleDay(int day) => Refresh(false);

    private void Refresh(bool snap)
    {
        if (clock == null) return;
        target = Evaluate(clock.Hour + clock.Minute / 60f);
        if (snap) img.color = target;
    }

    private void Update()
    {
        // ค่อยๆ เปลี่ยนสี (ใช้ unscaled เผื่อ timeScale = 0)
        img.color = Color.Lerp(img.color, target, 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime));
    }

    private Color Evaluate(float h)
    {
        if (keys == null || keys.Length == 0) return Color.clear;
        if (h <= keys[0].hour) return keys[0].color;
        for (int i = 1; i < keys.Length; i++)
        {
            if (h <= keys[i].hour)
            {
                float t = Mathf.InverseLerp(keys[i - 1].hour, keys[i].hour, h);
                return Color.Lerp(keys[i - 1].color, keys[i].color, t);
            }
        }
        return keys[keys.Length - 1].color;
    }
}
