using UnityEngine;

/// <summary>
/// ข้อมูลของสายเข้า 1 สาย สร้างได้จาก Project > Create > CallGame > Call
/// </summary>
[CreateAssetMenu(fileName = "NewCall", menuName = "CallGame/Call")]
public class CallData : ScriptableObject
{
    [Header("Caller")]
    public string callerName = "ไม่ทราบชื่อ";
    public Sprite callerPortrait; // ไม่ใส่ก็ได้

    [Header("เงื่อนไขวันที่ (สำหรับสุ่มเลือกสาย)")]
    [Tooltip("สายนี้จะเริ่มโทรมาได้ตั้งแต่วันที่เท่าไหร่ (1 = โทรมาได้ตั้งแต่วันแรก)")]
    public int minDay = 1;
    [Tooltip("สายนี้โทรมาได้ถึงวันที่เท่าไหร่ ใส่ 0 = ไม่จำกัด โทรมาได้ตลอดหลังจาก minDay")]
    public int maxDay = 0;

    [Header("เงื่อนไขเงิน (สำหรับสุ่มเลือกสาย)")]
    [Tooltip("ต้องมีเงินสดในมืออย่างน้อยเท่านี้ สายนี้ถึงจะโทรเข้ามาได้ (0 = ไม่จำกัด) นับเงินสดไม่ใช่หนี้")]
    public int minMoney = 0;

    [Header("Dialogue (ตอนกดรับสาย)")]
    [TextArea(2, 4)] public string[] dialogueLines;

    [Header("ข้อความบนปุ่มตอนตัดสินใจ (หลังคุยจบ)")]
    public string acceptLabel = "รับงาน";
    public string declineLabel = "ไม่รับ";

    [Header("ผลลัพธ์ถ้า 'ตกลง' (ปุ่มซ้าย/Accept)")]
    public int acceptMoney;
    [TextArea(2, 4)] public string acceptResult;

    [Header("ผลลัพธ์ถ้า 'ปฏิเสธ' (ปุ่ม Decline)")]
    public int declineMoney;
    [TextArea(2, 4)] public string declineResult;

    [Header("ผลลัพธ์ถ้า 'วางสาย' หรือสายหมดเวลา (ไม่ได้รับสาย)")]
    public int hangUpMoney;
    [TextArea(2, 4)] public string hangUpResult = "คุณไม่ได้รับสายนี้";
}
