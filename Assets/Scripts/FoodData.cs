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
}
