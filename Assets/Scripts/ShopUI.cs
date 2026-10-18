using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI ร้านค้าในคอม: กดปุ่มเปิดแผง (ซ่อนปุ่มเมนูคอม) แล้วโชว์ลิสต์อาหารจากรายการ FoodData
/// กดซื้อ: ต้องมีเงินพอ แล้วจะหักเงินสดและเติมความหิวให้ (ซื้อตอนอิ่มแล้วก็เสียเงินเปล่า ถือเป็นความพลาดของผู้เล่น)
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Computer Menu Control")]
    [Tooltip("กลุ่มปุ่มเมนูหลักบนจอคอมที่จะให้ซ่อนเมื่อเปิดร้านค้า (ตัวเดียวกับที่ DebtPaymentUI ใช้)")]
    [SerializeField] private GameObject menuButtonPanel;

    [Header("Panel Open/Close")]
    [Tooltip("ปุ่มในคอมที่กดแล้วเปิดร้านค้า")]
    [SerializeField] private Button openButton;
    [Tooltip("แผงร้านค้าทั้งหมด")]
    [SerializeField] private GameObject shopPanel;
    [Tooltip("ปุ่มปิดร้าน อยู่ข้างในแผงนี้ (กลับไปหน้าเมนูคอม)")]
    [SerializeField] private Button closeButton;

    [Header("Food List")]
    [Tooltip("Prefab ของ 1 แถวอาหาร (ต้องมี ShopItemRow อยู่ที่ราก)")]
    [SerializeField] private ShopItemRow rowPrefab;
    [Tooltip("ที่วางแถว เช่น Content ของ Scroll View (ควรมี Vertical Layout Group)")]
    [SerializeField] private Transform listParent;
    [SerializeField] private List<FoodData> foods = new List<FoodData>();
    [SerializeField] private string priceFormat = "{0} บาท";
    [SerializeField] private string hungerFormat = "หิว +{0}";

    [Header("Optional")]
    [Tooltip("โชว์เงินสดที่มีตอนนี้ในหน้าร้าน อัปเดตเองเมื่อเงินเปลี่ยน")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private string moneyFormat = "เงิน {0}";
    [Tooltip("โชว์ผลหลังกดซื้อ เช่น \"ซื้อ ข้าวกล่อง แล้ว\" หรือ \"เงินไม่พอ\"")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private string boughtFormat = "ซื้อ {0} แล้ว";
    [SerializeField] private string notEnoughMoneyMessage = "เงินไม่พอ";

    private readonly List<ShopItemRow> rows = new List<ShopItemRow>();

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        if (shopPanel != null) shopPanel.SetActive(false);
        if (openButton != null) openButton.gameObject.SetActive(true);
    }

    private void Start()
    {
        BuildList();

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnMoneyChanged += UpdateMoney;
            UpdateMoney(PlayerStats.Instance.Money);
        }
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnMoneyChanged -= UpdateMoney;
    }

    private void UpdateMoney(int money)
    {
        if (moneyText != null) moneyText.text = string.Format(moneyFormat, money);
    }

    /// สร้างแถวอาหารจากลิสต์ foods (ล้างของเก่าก่อน เรียกซ้ำได้ถ้าเปลี่ยนรายการ)
    public void BuildList()
    {
        foreach (ShopItemRow r in rows)
            if (r != null) Destroy(r.gameObject);
        rows.Clear();

        if (rowPrefab == null || listParent == null) return;

        foreach (FoodData food in foods)
        {
            if (food == null) continue;
            ShopItemRow row = Instantiate(rowPrefab, listParent);
            row.gameObject.SetActive(true);
            row.Setup(food, OnBuyClicked, priceFormat, hungerFormat);
            rows.Add(row);
        }
    }

    /// เปิดร้าน ซ่อนปุ่มเปิดกับปุ่มเมนูคอม
    public void OpenPanel()
    {
        if (shopPanel != null) shopPanel.SetActive(true);
        if (feedbackText != null) feedbackText.text = "";
        if (openButton != null) openButton.gameObject.SetActive(false);
        if (menuButtonPanel != null) menuButtonPanel.SetActive(false);
    }

    /// ปิดร้าน กลับไปหน้าเมนูคอม
    public void ClosePanel()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (openButton != null) openButton.gameObject.SetActive(true);
        if (menuButtonPanel != null) menuButtonPanel.SetActive(true);
    }

    /// ผู้เล่นกดซื้อ: เงินไม่พอไม่ให้ซื้อ / ไม่งั้นหักเงินแล้วเติมความหิว (ไม่กันซื้อตอนอิ่ม)
    private void OnBuyClicked(FoodData food)
    {
        PlayerStats stats = PlayerStats.Instance;
        HungerSystem hunger = HungerSystem.Instance;
        if (stats == null || hunger == null || food == null) return;

        if (!stats.TrySpend(food.price))
        {
            ShowFeedback(notEnoughMoneyMessage);
            return;
        }

        hunger.Eat(food.hungerRestore);
        ShowFeedback(string.Format(boughtFormat, food.foodName));
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
    }
}
