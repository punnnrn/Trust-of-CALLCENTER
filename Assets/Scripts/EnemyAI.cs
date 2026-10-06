using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float speed = 200f;
    private RectTransform rect;

    private void Start() 
    { 
        rect = GetComponent<RectTransform>(); 
    }

    private void Update()
    {
        // เลื่อนลงด้านล่าง
        rect.anchoredPosition += Vector2.down * speed * Time.deltaTime;

        // ถ้าศัตรูหลุดลงไปต่ำกว่าจอ (ปรับค่า -500 ให้พอดีกับขอบล่างจอของคุณ)
        if (rect.anchoredPosition.y < -300f) 
        {
            // สั่งให้ Manager ทำระบบแพ้เกม
            SpaceMonneyManager manager = FindObjectOfType<SpaceMonneyManager>();
            if (manager != null)
            {
                manager.LoseGame();
            }
            Destroy(gameObject); // ทำลายตัวเองทิ้ง
        }
    }

    public void TakeDamage()
    {
        SpaceMonneyManager manager = FindObjectOfType<SpaceMonneyManager>();
        if (manager != null) manager.AddScore(1);
        Destroy(gameObject);
    }
}