using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI หน้าบิลในคอม: กดปุ่มเปิดแผง (ซ่อนปุ่มเมนูคอม) แล้วโชว์บิลที่ค้างจ่ายเป็นการ์ด แยกคอลัมน์ Bills / Rents
/// กดปุ่ม Pay บนการ์ดเพื่อจ่ายด้วยเงินสด บิลที่เลยกำหนดจะถูกหักเองตอนจบวัน (ดู BillSystem)
/// </summary>
public class BillsUI : MonoBehaviour
{
    [Header("Computer Menu Control")]
    [Tooltip("กลุ่มปุ่มเมนูหลักบนจอคอมที่จะให้ซ่อนเมื่อเปิดหน้าบิล (ตัวเดียวกับที่ DebtPaymentUI / ShopUI ใช้)")]
    [SerializeField] private GameObject menuButtonPanel;

    [Header("Panel Open/Close")]
    [SerializeField] private Button openButton;
    [SerializeField] private GameObject billsPanel;
    [SerializeField] private Button closeButton;

    [Header("Cards")]
    [Tooltip("Prefab การ์ดบิล 1 ใบ (ต้องมี BillCard อยู่ที่ราก)")]
    [SerializeField] private BillCard cardPrefab;
    [Tooltip("ที่วางการ์ดค่าไฟ/ค่าน้ำ (ควรมี Grid Layout Group)")]
    [SerializeField] private Transform utilityListParent;
    [Tooltip("ที่วางการ์ดค่าเช่า ถ้าเว้นว่าง การ์ดค่าเช่าจะไปรวมในที่วางด้านบน")]
    [SerializeField] private Transform rentListParent;

    [Header("Text Formats")]
    [SerializeField] private string billDateFormat = "บิลวันที่ {0}";
    [SerializeField] private string dueDateFormat = "วันครบกำหนดชำระวันที่ {0}";
    [SerializeField] private string dueTodayText = "ชำระภายในวันนี้";
    [SerializeField] private string dueTomorrowText = "ชำระภายในวันพรุ่งนี้";
    [SerializeField] private string dueInDaysFormat = "ระยะเวลาชำระเหลือ {0} วัน";
    [SerializeField] private string amountFormat = "{0} บาท";

    [Header("Optional")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private string moneyFormat = "เงิน {0}";
    [Tooltip("โชว์ผลหลังกดจ่าย เช่น \"จ่ายค่าไฟแล้ว\" หรือ \"เงินไม่พอ\"")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private string paidFormat = "จ่าย{0}แล้ว";
    [SerializeField] private string notEnoughMoneyMessage = "เงินไม่พอ";
    [Tooltip("ป้ายเตือนนอกหน้าบิล (เช่น บนจอห้อง) โชว์จำนวนบิลที่ถึงกำหนดวันนี้ ซ่อนเองเมื่อไม่มี")]
    [SerializeField] private TMP_Text dueTodayBadgeText;
    [SerializeField] private string dueTodayBadgeFormat = "บิลถึงกำหนดวันนี้ {0} ใบ";

    private readonly List<BillCard> cards = new List<BillCard>();

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        if (billsPanel != null) billsPanel.SetActive(false);
        if (openButton != null) openButton.gameObject.SetActive(true);
    }

    private void Start()
    {
        if (BillSystem.Instance != null) BillSystem.Instance.OnBillsChanged += Refresh;
        else Debug.LogWarning("BillsUI: ไม่พบ BillSystem ใน Scene");

        if (GameClock.Instance != null) GameClock.Instance.OnDayStarted += OnDayStarted;

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnMoneyChanged += UpdateMoney;
            UpdateMoney(PlayerStats.Instance.Money);
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (BillSystem.Instance != null) BillSystem.Instance.OnBillsChanged -= Refresh;
        if (GameClock.Instance != null) GameClock.Instance.OnDayStarted -= OnDayStarted;
        if (PlayerStats.Instance != null) PlayerStats.Instance.OnMoneyChanged -= UpdateMoney;
    }

    private void OnDayStarted(int day) => Refresh();

    private void UpdateMoney(int money)
    {
        if (moneyText != null) moneyText.text = string.Format(moneyFormat, money);
    }

    public void OpenPanel()
    {
        if (billsPanel != null) billsPanel.SetActive(true);
        if (feedbackText != null) feedbackText.text = "";
        if (openButton != null) openButton.gameObject.SetActive(false);
        if (menuButtonPanel != null) menuButtonPanel.SetActive(false);
        Refresh();
    }

    public void ClosePanel()
    {
        if (billsPanel != null) billsPanel.SetActive(false);
        if (openButton != null) openButton.gameObject.SetActive(true);
        if (menuButtonPanel != null) menuButtonPanel.SetActive(true);
    }

    /// สร้างการ์ดใหม่ทั้งหมดจากบิลที่ค้างอยู่ (เรียงตามวันครบกำหนด)
    public void Refresh()
    {
        foreach (BillCard c in cards)
            if (c != null) Destroy(c.gameObject);
        cards.Clear();

        BillSystem sys = BillSystem.Instance;
        if (sys == null) return;

        int today = GameClock.Instance != null ? GameClock.Instance.Day : 1;

        if (dueTodayBadgeText != null)
        {
            int dueToday = sys.DueTodayCount(today);
            dueTodayBadgeText.gameObject.SetActive(dueToday > 0);
            dueTodayBadgeText.text = string.Format(dueTodayBadgeFormat, dueToday);
        }

        if (cardPrefab == null || utilityListParent == null) return;

        foreach (Bill bill in sys.Bills.OrderBy(b => b.dueDay).ThenBy(b => b.typeIndex).ToList())
        {
            BillType type = sys.TypeOf(bill);
            if (type == null) continue;

            Transform parent = type.category == BillCategory.Rent && rentListParent != null
                ? rentListParent : utilityListParent;

            int daysLeft = bill.dueDay - today;
            string note = daysLeft <= 0 ? dueTodayText
                        : daysLeft == 1 ? dueTomorrowText
                        : string.Format(dueInDaysFormat, daysLeft);

            BillCard card = Instantiate(cardPrefab, parent);
            card.gameObject.SetActive(true);
            card.Setup(bill, type.displayName, type.icon,
                       string.Format(billDateFormat, bill.billDay),
                       string.Format(dueDateFormat, bill.dueDay),
                       note, daysLeft <= 0,
                       string.Format(amountFormat, bill.amount), OnPayClicked);
            cards.Add(card);
        }
    }

    private void OnPayClicked(Bill bill)
    {
        BillSystem sys = BillSystem.Instance;
        if (sys == null || bill == null) return;

        BillType type = sys.TypeOf(bill);
        string billName = type != null ? type.displayName : "บิล";

        if (sys.TryPay(bill))
        {
            if (feedbackText != null) feedbackText.text = string.Format(paidFormat, billName);
        }
        else if (feedbackText != null)
        {
            feedbackText.text = notEnoughMoneyMessage;
        }
    }
}
