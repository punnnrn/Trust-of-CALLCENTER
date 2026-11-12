using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// เตือนผู้เล่นก่อนหมดวัน: ขึ้นแถบแจ้งเตือนที่เหลือ 60 / 30 / 10 นาที และเปลี่ยนสีนาฬิกา
public class DayEndWarning : MonoBehaviour
{
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private Color warningColor = new Color(0.85f, 0.45f, 0.05f, 1f);
    [SerializeField] private Color urgentColor = new Color(0.8f, 0.1f, 0.1f, 1f);
    [Tooltip("เหลือกี่นาทีถึงเริ่มเปลี่ยนนาฬิกาเป็นสีส้ม")]
    [SerializeField] private int warningMinutes = 60;
    [Tooltip("เหลือกี่นาทีถึงเปลี่ยนนาฬิกาเป็นสีแดง")]
    [SerializeField] private int urgentMinutes = 15;

    private GameClock clock;
    private Color normalColor = Color.black;
    private readonly HashSet<int> announced = new HashSet<int>();

    private void Start()
    {
        clock = GameClock.Instance;
        if (clock == null) { enabled = false; return; }

        if (clockText != null) normalColor = clockText.color;
        clock.OnMinuteChanged += HandleMinute;
        clock.OnDayStarted += HandleDayStarted;
    }

    private void OnDestroy()
    {
        if (clock == null) return;
        clock.OnMinuteChanged -= HandleMinute;
        clock.OnDayStarted -= HandleDayStarted;
    }

    private void HandleDayStarted(int day)
    {
        announced.Clear();
        SetClockColor(normalColor);
    }

    private void HandleMinute(int hour, int minute)
    {
        int minutesLeft = clock.EndHour * 60 - (hour * 60 + minute);
        if (minutesLeft <= 0) return;

        if (minutesLeft <= urgentMinutes) SetClockColor(urgentColor);
        else if (minutesLeft <= warningMinutes) SetClockColor(warningColor);

        Announce(minutesLeft, 60, "เหลืออีก 1 ชั่วโมงจะหมดวัน ทำงานที่ค้างให้เสร็จ แล้วกดนอนเมื่อพร้อมได้เลย");
        Announce(minutesLeft, 30, "เหลือ 30 นาทีจะหมดวันแล้ว");
        Announce(minutesLeft, 10, "ใกล้หมดวันแล้ว เหลืออีก 10 นาที");
    }

    private void Announce(int minutesLeft, int threshold, string message)
    {
        if (minutesLeft != threshold || !announced.Add(threshold)) return;
        NotificationBanner.Show(message, 5f);
    }

    private void SetClockColor(Color c)
    {
        if (clockText != null) clockText.color = c;
    }
}
