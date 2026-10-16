using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float speed = 200f;
    private RectTransform rect;
    private bool dead; // กันโดนยิงซ้ำในเฟรมเดียวกัน (Destroy ทำงานปลายเฟรม) ไม่ให้คะแนนเบิ้ล

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
            SpaceMonneyManager manager = FindFirstObjectByType<SpaceMonneyManager>();
            if (manager != null)
            {
                manager.LoseGame();
            }
            Destroy(gameObject); // ทำลายตัวเองทิ้ง
        }
    }

    public void TakeDamage()
    {
        if (dead) return;
        dead = true;

        SpaceMonneyManager manager = FindFirstObjectByType<SpaceMonneyManager>();
        if (manager != null) manager.AddScore(1);
        Destroy(gameObject);
    }
}
