using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 1 แถวในลิสต์ร้านค้า ติดไว้ที่ Object ราก ของ Prefab แถวอาหาร แล้วลากช่องต่างๆ ใส่
/// ShopUI จะสร้างแถวจาก Prefab นี้ให้เองตามจำนวนอาหาร
/// </summary>
public class ShopItemRow : MonoBehaviour
{
    [SerializeField] private Image iconImage;      // ไม่ใส่ก็ได้
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text hungerText;  // ไม่ใส่ก็ได้
    [SerializeField] private Button buyButton;

    public FoodData Food { get; private set; }

    public void Setup(FoodData food, Action<FoodData> onBuy, string priceFormat, string hungerFormat)
    {
        Food = food;

        if (nameText != null) nameText.text = food.foodName;
        if (priceText != null) priceText.text = string.Format(priceFormat, food.price);
        if (hungerText != null) hungerText.text = string.Format(hungerFormat, food.hungerRestore);

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(food.icon != null);
            iconImage.sprite = food.icon;
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onBuy?.Invoke(food));
        }
    }
}
