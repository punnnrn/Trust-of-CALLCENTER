using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// การ์ดบิล 1 ใบในหน้าบิล ติดไว้ที่ราก ของ Prefab การ์ดบิล แล้วลากช่องต่างๆ ใส่ (ช่องไหนไม่ใช้เว้นว่างได้)
/// BillsUI จะสร้างการ์ดจาก Prefab นี้ให้เองตามจำนวนบิลที่ค้างจ่าย
/// </summary>
public class BillCard : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;     // เช่น "ค่าไฟ"
    [SerializeField] private TMP_Text billDateText;  // เช่น "วันที่ 2"
    [SerializeField] private TMP_Text dueDateText;   // เช่น "วันที่ 5"
    [SerializeField] private TMP_Text dueNoteText;   // เช่น "วันนี้" / "พรุ่งนี้" / "เหลือ 2 วัน"
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button payButton;
    [SerializeField] private Color urgentColor = new Color(0.9f, 0.6f, 0.1f);
    [SerializeField] private Color normalColor = new Color(0.55f, 0.55f, 0.6f);

    public Bill Bill { get; private set; }

    public void Setup(Bill bill, string title, Sprite icon, string billDate, string dueDate,
                      string dueNote, bool urgent, string amount, Action<Bill> onPay)
    {
        Bill = bill;

        if (titleText != null) titleText.text = title;
        if (billDateText != null) billDateText.text = billDate;
        if (dueDateText != null) dueDateText.text = dueDate;
        if (amountText != null) amountText.text = amount;

        if (dueNoteText != null)
        {
            dueNoteText.text = dueNote;
            dueNoteText.color = urgent ? urgentColor : normalColor;
        }

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(icon != null);
            iconImage.sprite = icon;
        }

        if (payButton != null)
        {
            payButton.onClick.RemoveAllListeners();
            payButton.onClick.AddListener(() => onPay?.Invoke(bill));
        }
    }
}
