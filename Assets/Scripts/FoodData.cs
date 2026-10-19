using UnityEngine;

/// <summary>
/// ข้อมูลอาหาร 1 อย่างในร้านค้า สร้างได้จาก Project > Create > CallGame > Food
/// </summary>
[CreateAssetMenu(fileName = "NewFood", menuName = "CallGame/Food")]
public class FoodData : ScriptableObject
{
    public string foodName = "อาหาร";
    public Sprite icon; // ไม่ใส่ก็ได้
    [Tooltip("ราคา (เงินสด)")]
    public int price = 50;
    [Tooltip("ความหิวที่เติมให้เมื่อกินอาหารนี้")]
    public float hungerRestore = 20f;

    [Header("สต็อกในร้าน (สุ่มใหม่ทุกวัน)")]
    [Tooltip("จำนวนต่ำสุดที่ร้านมีของวันนั้น (0 = บางวันอาจไม่มีเลย)")]
    public int minStock = 0;
    [Tooltip("จำนวนสูงสุดที่ร้านมีของวันนั้น")]
    public int maxStock = 3;
    [Tooltip("ติ๊กถ้าอยากให้มีขายไม่อั้นทุกวัน (ไม่สนค่า Min/Max Stock) เหมาะกับอาหารพื้นฐานกันผู้เล่นหาของกินไม่ได้เลย")]
    public bool unlimitedStock = false;
}
