using System;
using System.Collections.Generic;
using UnityEngine;

/// เฝ้าดูคูลดาวน์ของมินิเกมทั้งหมด (ทำงานตลอดแม้ปิดหน้าจอคอม) แล้วเด้งแจ้งเตือนเมื่อพร้อมเล่นอีกครั้ง
public class CooldownWatcher : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("ต้องเป็นสคริปต์ที่ implement ICooldownProvider")]
        public MonoBehaviour source;
        public string gameName = "มินิเกม";
        [NonSerialized] public bool wasCooling;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private int lastDay = -1;

    private static bool IsCooling(Entry e)
    {
        ICooldownProvider p = e.source as ICooldownProvider;
        return p != null && p.IsCoolingDown;
    }

    private static int CurrentDay()
    {
        return GameClock.Instance != null ? GameClock.Instance.Day : 0;
    }

    private void Start()
    {
        lastDay = CurrentDay();
        foreach (Entry e in entries) e.wasCooling = IsCooling(e);
    }

    private void Update()
    {
        int day = CurrentDay();
        bool dayChanged = day != lastDay;
        lastDay = day;
        bool dayOver = GameClock.Instance != null && GameClock.Instance.IsDayEnded;

        foreach (Entry e in entries)
        {
            bool cooling = IsCooling(e);

            // คูลดาวน์หมดเพราะข้ามวัน/จบวัน ไม่ต้องแจ้งเตือน
            if (e.wasCooling && !cooling && !dayChanged && !dayOver)
                NotificationBanner.Show(e.gameName + " พร้อมเล่นแล้ว!", 3f);

            e.wasCooling = cooling;
        }
    }
}
