using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>ใครเป็นคนพูดในประโยคนี้ (ใช้เลือกว่าจะโชว์รูปใคร)</summary>
public enum DialogueSpeaker
{
    Caller = 0, // คนในสาย
    Player = 1  // ตัวละครหลักของเรา
}

/// <summary>ประโยคเดียวใน dialogue: เลือกคนพูด + พิมพ์ข้อความ</summary>
[Serializable]
public class DialogueLine
{
    public DialogueSpeaker speaker = DialogueSpeaker.Caller;
    [TextArea(2, 4)] public string text;
}

/// <summary>ช่วงเวลาในวัน เช่น ช่วงเช้า 09:00 - 12:00 (เวลาในเกม ตาม GameClock)</summary>
[Serializable]
public class CallTimeWindow
{
    [Tooltip("ชื่อช่วง ไว้ดูเองใน Inspector เช่น ช่วงเช้า")]
    public string label = "ช่วงเช้า";
    [Range(0, 23)] public int startHour = 9;
    [Range(0, 59)] public int startMinute = 0;
    [Range(0, 24)] public int endHour = 12;
    [Range(0, 59)] public int endMinute = 0;

    /// อยู่ในช่วงนี้ไหม (นับ เริ่ม ถึง ก่อนจบ) รองรับช่วงข้ามเที่ยงคืน เช่น 22:00 - 02:00
    public bool Contains(int hour, int minute)
    {
        int now = hour * 60 + minute;
        int s = startHour * 60 + startMinute;
        int e = endHour * 60 + endMinute;
        if (s == e) return true;          // เริ่ม = จบ ถือว่าทั้งวัน
        if (s < e) return now >= s && now < e;
        return now >= s || now < e;       // ข้ามเที่ยงคืน
    }
}

/// <summary>
/// ข้อมูลของสายเข้า 1 สาย สร้างได้จาก Project > Create > CallGame > Call
/// </summary>
[CreateAssetMenu(fileName = "NewCall", menuName = "CallGame/Call")]
public class CallData : ScriptableObject
{
    [Header("Caller")]
    public string callerName = "ไม่ทราบชื่อ";
    public Sprite callerPortrait; // ไม่ใส่ก็ได้
    [Tooltip("รูปตัวละครหลักของสายนี้ (ไม่ใส่ = ไม่โชว์รูปตอนตัวละครหลักพูด)")]
    [FormerlySerializedAs("playerPortraitOverride")]
    public Sprite playerPortrait;

    [Header("เงื่อนไขวันที่ (สำหรับสุ่มเลือกสาย)")]
    [Tooltip("สายนี้จะเริ่มโทรมาได้ตั้งแต่วันที่เท่าไหร่ (1 = โทรมาได้ตั้งแต่วันแรก)")]
    public int minDay = 1;
    [Tooltip("สายนี้โทรมาได้ถึงวันที่เท่าไหร่ ใส่ 0 = ไม่จำกัด โทรมาได้ตลอดหลังจาก minDay")]
    public int maxDay = 0;

    [Tooltip("สายนี้โทรเข้ามาได้ทั้งหมดกี่ครั้ง (นับทุกครั้งที่สายจบ ไม่ว่ารับหรือพลาด) 0 = ไม่จำกัด")]
    public int maxCalls = 0;

    [Header("เงื่อนไขเงิน (สำหรับสุ่มเลือกสาย)")]
    [Tooltip("ต้องมีเงินสดในมืออย่างน้อยเท่านี้ สายนี้ถึงจะโทรเข้ามาได้ (0 = ไม่จำกัด) นับเงินสดไม่ใช่หนี้")]
    public int minMoney = 0;

    [Header("ช่วงเวลาที่สายนี้โทรเข้ามาได้")]
    [Tooltip("ปล่อยว่าง (Size 0) = โทรมาได้ทุกเวลา\nเพิ่มได้กี่ช่วงก็ได้ เช่น ช่วงเช้า 09:00-12:00 + ช่วงเย็น 16:00-18:00\nถ้าอยู่ในช่วงใดช่วงหนึ่ง สายนี้ก็เข้ามาได้")]
    public CallTimeWindow[] timeWindows;

    [Header("Dialogue (ตอนกดรับสาย) เลือกคนพูดได้ทีละประโยค")]
    public DialogueLine[] conversation;

    [Header("ข้อความบนปุ่มตอนตัดสินใจ (หลังคุยจบ)")]
    public string acceptLabel = "รับงาน";
    public string declineLabel = "ไม่รับ";

    [Header("ผลลัพธ์ถ้า 'ตกลง' (ปุ่มซ้าย/Accept)")]
    public int acceptMoney;
    [Tooltip("จะโทรมาอีกในกี่วัน 0 = ไม่โทรมาอีกเลย, 1 = วันถัดไป, 2 = อีก 2 วัน ...")]
    public int acceptCallbackDays = 1;
    [Tooltip("บทสนทนาหลังกดตกลง ถ้าใส่ไว้ จะใช้แทนข้อความด้านล่าง")]
    public DialogueLine[] acceptReply;
    [TextArea(2, 4)] public string acceptResult;

    [Header("ผลลัพธ์ถ้า 'ปฏิเสธ' (ปุ่ม Decline)")]
    public int declineMoney;
    [Tooltip("จะโทรมาอีกในกี่วัน 0 = ไม่โทรมาอีกเลย, 1 = วันถัดไป, 2 = อีก 2 วัน ...")]
    public int declineCallbackDays = 1;
    [Tooltip("บทสนทนาหลังกดปฏิเสธ ถ้าใส่ไว้ จะใช้แทนข้อความด้านล่าง")]
    public DialogueLine[] declineReply;
    [TextArea(2, 4)] public string declineResult;

    [Header("ผลลัพธ์ถ้า 'วางสาย' หรือสายหมดเวลา (ไม่ได้รับสาย)")]
    public int hangUpMoney;
    [Tooltip("จะโทรมาอีกในกี่วัน 0 = ไม่โทรมาอีกเลย, 1 = วันถัดไป, 2 = อีก 2 วัน ...")]
    public int hangUpCallbackDays = 1;
    [Tooltip("บทสนทนาหลังวางสาย (เล่นเฉพาะตอนเปิดโทรศัพท์แล้วกดวางสาย)")]
    public DialogueLine[] hangUpReply;
    [TextArea(2, 4)] public string hangUpResult = "คุณไม่ได้รับสายนี้";

    /// บทสนทนาตอนรับสาย
    public DialogueLine[] GetConversation()
    {
        return conversation != null ? conversation : new DialogueLine[0];
    }

    /// สายนี้เข้าได้ในเวลานี้ไหม
    public bool IsInTimeWindow(int hour, int minute)
    {
        if (timeWindows == null || timeWindows.Length == 0) return true;
        foreach (CallTimeWindow w in timeWindows)
            if (w != null && w.Contains(hour, minute)) return true;
        return false;
    }
}
