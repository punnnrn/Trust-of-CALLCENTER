using System;
using TMPro;
using UnityEngine;

/// <summary>
/// นาฬิกาในเกม: เดินตามเวลาจริง, แบ่งเป็นวัน, หยุด/เดินต่อได้
/// ระบบอื่นๆ (หลอดความหิว, Buff, สายตามเวลา) ให้ subscribe event ด้านล่าง
/// </summary>
public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    [Header("Time Settings")]
    [Tooltip("เวลาในเกมเดินกี่นาทีต่อ 1 วินาทีจริง (1 = วันทำงาน 9 ชม. ใช้เวลาจริง 9 นาที)")]
    [SerializeField] private float gameMinutesPerRealSecond = 1f;
    [SerializeField, Range(0, 23)] private int startHour = 9;
    [SerializeField, Range(1, 24)] private int endHour = 18;
    [SerializeField] private bool startPaused = false;

    [Header("UI (optional)")]
    [SerializeField] private TMP_Text clockText;
    [Tooltip("{0} = วันที่, {1} = ชั่วโมง, {2} = นาที")]
    [SerializeField] private string displayFormat = "วันที่ {0}   {1:00}:{2:00}";

    // ---------- Events ----------
    /// ทุกครั้งที่เวลาเดิน 1 นาทีในเกม (hour, minute)
    public event Action<int, int> OnMinuteChanged;
    /// ทุกครั้งที่ขึ้นชั่วโมงใหม่ (hour)
    public event Action<int> OnHourChanged;
    /// ตอนเริ่มวันใหม่ผ่าน StartNextDay() (day)
    public event Action<int> OnDayStarted;
    /// ตอนเวลาถึงเวลาเลิกงาน (day) นาฬิกาจะหยุดรอจนกว่าจะเรียก StartNextDay()
    public event Action<int> OnDayEnded;

    // ---------- State ----------
    public int Day { get; private set; } = 1;
    public int Hour { get; private set; }
    public int Minute { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsDayEnded { get; private set; }

    /// นาทีที่ผ่านไปในวันนี้นับจากเวลาเริ่มงาน
    public int MinutesSinceStart => (Hour * 60 + Minute) - startHour * 60;
    /// ความคืบหน้าของวัน 0..1
    public float DayProgress01 =>
        Mathf.Clamp01(MinutesSinceStart / (float)((endHour - startHour) * 60));

    private float accumulator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Hour = startHour;
        Minute = 0;
        IsPaused = startPaused;
    }

    private void Start()
    {
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (IsPaused || IsDayEnded) return;

        accumulator += Time.deltaTime * gameMinutesPerRealSecond;

        while (accumulator >= 1f && !IsDayEnded)
        {
            accumulator -= 1f;
            AdvanceMinute();
        }
    }

    // ---------- Public API ----------

    public void Pause() => IsPaused = true;
    public void Resume() => IsPaused = false;

    public void SetSpeed(float minutesPerRealSecond) =>
        gameMinutesPerRealSecond = Mathf.Max(0f, minutesPerRealSecond);

    /// ข้ามเวลาไปข้างหน้า (เช่น ตอนกินข้าว/พัก) จะยิง event ครบทุกนาที
    public void SkipMinutes(int minutes)
    {
        for (int i = 0; i < minutes && !IsDayEnded; i++)
            AdvanceMinute();
    }

    /// จบวันทันที ไม่ต้องรอถึง End Hour (ใช้กับปุ่ม "นอน") ยิง OnDayEnded เหมือนจบวันตามเวลาปกติ
    public void ForceEndDay()
    {
        if (IsDayEnded) return;

        Hour = endHour;
        Minute = 0;
        IsDayEnded = true;
        UpdateDisplay();
        OnDayEnded?.Invoke(Day);
    }

    /// เริ่มวันใหม่ (เรียกหลัง OnDayEnded)
    public void StartNextDay()
    {
        Day++;
        Hour = startHour;
        Minute = 0;
        accumulator = 0f;
        IsDayEnded = false;
        IsPaused = false;
        UpdateDisplay();
        OnDayStarted?.Invoke(Day);
    }

    public string GetTimeString() => $"{Hour:00}:{Minute:00}";

    // ---------- Internal ----------

    private void AdvanceMinute()
    {
        Minute++;
        if (Minute >= 60)
        {
            Minute = 0;
            Hour++;
        }

        UpdateDisplay();
        OnMinuteChanged?.Invoke(Hour, Minute);
        if (Minute == 0) OnHourChanged?.Invoke(Hour);

        if (Hour >= endHour)
        {
            IsDayEnded = true;
            OnDayEnded?.Invoke(Day);
        }
    }

    private void UpdateDisplay()
    {
        if (clockText != null)
            clockText.text = string.Format(displayFormat, Day, Hour, Minute);
    }
}
