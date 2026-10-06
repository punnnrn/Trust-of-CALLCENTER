using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Tooltip("ความเร็วกระสุน (ถ้าใส่ค่าบวกจะพุ่งขึ้น)")]
    public float speed = 800f; 
    
    [Tooltip("ระยะห่างที่จะถือว่ายิงโดนศัตรู (ปรับให้พอดีกับขนาดภาพ)")]
    public float hitDistance = 60f; 

    private RectTransform rect;

    private void Start() 
    { 
        rect = GetComponent<RectTransform>(); 
    }

    private void Update()
    {
        // 1. บังคับเลื่อนขึ้นด้านบน
        rect.anchoredPosition += Vector2.up * speed * Time.deltaTime;

        // ถ้านอกจอให้ทำลายทิ้ง
        if (rect.anchoredPosition.y > 135f) 
        {
            Destroy(gameObject);
            return;
        }

        // 2. เช็คการชนกับศัตรูด้วยระยะห่าง (ไม่ต้องใช้ BoxCollider2D)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in enemies)
        {
            if (enemyObj != null)
            {
                RectTransform enemyRect = enemyObj.GetComponent<RectTransform>();
                
                // คำนวณระยะห่างระหว่างกระสุนกับศัตรูตัวนั้นๆ
                float distance = Vector2.Distance(rect.anchoredPosition, enemyRect.anchoredPosition);
                
                // ถ้าระยะห่างน้อยกว่า hitDistance ถือว่ายิงโดน
                if (distance < hitDistance) 
                {
                    EnemyAI enemy = enemyObj.GetComponent<EnemyAI>();
                    if (enemy != null) enemy.TakeDamage(); // สั่งศัตรูตาย และได้คะแนน
                    
                    Destroy(gameObject); // ทำลายกระสุนทิ้ง
                    break; 
                }
            }
        }
    }
}