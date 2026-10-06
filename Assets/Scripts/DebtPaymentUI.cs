using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI สำหรับจ่ายหนี้: กดปุ่มเพื่อเอาเงินสดที่มีไปโปะหนี้ (ผู้เล่นต้องกดเอง ไม่หักอัตโนมัติ)
/// วางปุ่มนี้ไว้ตรงไหนก็ได้ที่ผู้เล่นเข้าถึงได้ตลอด เช่น มุมจอ หรือในหน้าสรุปผลจบวัน
/// </summary>
public class DebtPaymentUI : MonoBehaviour
{
    [Header("Panel Open/Close")]
    [Tooltip("ปุ่มที่ลอยอยู่ปกติ กดแล้วเปิดแผงจ่ายหนี้ (ปุ่มนี้จะซ่อนตัวเองตอนแผงเปิด)")]
    [SerializeField] private Button openButton;
    [Tooltip("แผง/Panel ที่เก็บ UI จ่ายหนี้ทั้งหมด")]
    [SerializeField] private GameObject debtPanel;
    [Tooltip("ปุ่มปิดแผง อยู่ข้างในแผงนี้")]
    [SerializeField] private Button closeButton;

    [Header("UI")]
    [Tooltip("โชว์ยอดหนี้คงเหลือ อัปเดตอัตโนมัติทุกครั้งที่หนี้เปลี่ยน")]
    [SerializeField] private TMP_Text debtAmountText;
    [SerializeField] private string debtAmountFormat = "หนี้คงเหลือ {0}";
    [SerializeField] private Button payAllButton;
    [Tooltip("ไม่ใส่ก็ได้ ใช้แสดงผลหลังจ่าย เช่น \"จ่ายไป 300\"")]
    [SerializeField] private TMP_Text feedbackText;

    [Header("Optional: จ่ายเป็นจำนวนที่กำหนดเอง")]
    [Tooltip("ถ้าใส่ Input Field นี้ไว้ ปุ่มข้างล่างจะจ่ายตามจำนวนที่พิมพ์แทนการจ่ายทั้งหมด")]
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private Button payAmountButton;

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        if (payAllButton != null)
            payAllButton.onClick.AddListener(PayAll);

        if (payAmountButton != null)
            payAmountButton.onClick.AddListener(PayCustomAmount);

        // เริ่มต้นให้แผงปิดไว้ก่อน เหลือแค่ปุ่มเปิดให้กด
        if (debtPanel != null) debtPanel.SetActive(false);
        if (openButton != null) openButton.gameObject.SetActive(true);
    }

    private void Start()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnDebtChanged += UpdateDebtDisplay;
            UpdateDebtDisplay(PlayerStats.Instance.Debt);
        }
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnDebtChanged -= UpdateDebtDisplay;
    }

    private void UpdateDebtDisplay(int debt)
    {
        if (debtAmountText != null)
            debtAmountText.text = string.Format(debtAmountFormat, debt);
    }

    /// เปิดแผงจ่ายหนี้ และซ่อนปุ่มเปิดไว้ก่อน
    public void OpenPanel()
    {
        if (debtPanel != null) debtPanel.SetActive(true);
        if (openButton != null) openButton.gameObject.SetActive(false);
    }

    /// ปิดแผงจ่ายหนี้ แล้วเอาปุ่มเปิดกลับมาโชว์
    public void ClosePanel()
    {
        if (debtPanel != null) debtPanel.SetActive(false);
        if (openButton != null) openButton.gameObject.SetActive(true);
    }

    /// จ่ายหนี้ด้วยเงินสดทั้งหมดที่มีตอนนี้
    public void PayAll()
    {
        if (PlayerStats.Instance == null) return;

        int paid = PlayerStats.Instance.PayDebtWithAllMoney();
        ShowFeedback(paid);
    }

    /// จ่ายตามจำนวนที่พิมพ์ใน Input Field
    public void PayCustomAmount()
    {
        if (PlayerStats.Instance == null || amountInput == null) return;

        if (int.TryParse(amountInput.text, out int amount))
        {
            int paid = PlayerStats.Instance.PayDebt(amount);
            ShowFeedback(paid);
            amountInput.text = "";
        }
    }

    private void ShowFeedback(int paidAmount)
    {
        if (feedbackText == null) return;

        feedbackText.text = paidAmount > 0
            ? $"จ่ายหนี้ไป {paidAmount}"
            : "ไม่มีเงินพอจ่าย หรือหนี้หมดแล้ว";
    }
}
