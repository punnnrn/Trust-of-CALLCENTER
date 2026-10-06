using UnityEngine;
using TMPro;

public class SpaceMonneyManager : MonoBehaviour
{
    [Header("UI & Scoring")]
    public TMP_Text scoreText;
    public TMP_Text waveText;
    private int score = 0;
    private int wave = 1;

    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;
    public Transform enemyParent;
    public float spawnInterval = 1.5f;
    private float spawnTimer;
    private int enemiesSpawned = 0; // ตัวนับจำนวนศัตรูที่เสกออกมาแล้ว

    [Header("Game Panels")]
    public GameObject SpaceMonneyUI;
    public GameObject computerMenuButtonPanel; 
    public GameObject winPanel;  
    public GameObject losePanel; 

    //ตั้งค่ารางวัล
    [Header("Rewards")]
    [Tooltip("เงินรางวัลขั้นต่ำ")]
    public int minReward = 100;
    [Tooltip("เงินรางวัลสูงสุด")]
    public int maxReward = 500;
    [Tooltip("Text สำหรับโชว์ว่าได้เงินเท่าไหร่ (ในหน้า WinPanel)")]
    public TMP_Text rewardText;

    private bool isPlaying = false;

    private void Start()
    {
        UpdateScoreUI();
    }

    private void Update()
    {
        if (!isPlaying) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnEnemy();
            spawnTimer = 0f;
        }
    }

    public void StartGamePlay()
    {
        isPlaying = true;
        score = 0;
        wave = 1;
        enemiesSpawned = 0; // รีเซ็ตจำนวนศัตรูเป็น 0
        spawnInterval = 1.5f; // รีเซ็ตความเร็ว
        UpdateScoreUI();
        
        // ซ่อนหน้าต่างแพ้/ชนะ ไว้ก่อนตอนเริ่มเกม
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        
        if (computerMenuButtonPanel != null) computerMenuButtonPanel.SetActive(false);
        if (SpaceMonneyUI != null) SpaceMonneyUI.SetActive(true);
    }

    void SpawnEnemy()
    {
        // ถ้าเสกศัตรูครบ 20 ตัวแล้ว ให้หยุดเสก
        if (enemiesSpawned >= 20) return;

        if (enemyPrefab != null && enemyParent != null)
        {
            float randomX = Random.Range(-300f, 300f);
            Vector3 spawnPos = new Vector3(randomX, 120f, 0f);

            GameObject enemy = Instantiate(enemyPrefab, enemyParent);
            enemy.GetComponent<RectTransform>().anchoredPosition = spawnPos;
            
           
            // คำนวณความเร็วต่ำสุด-สูงสุด ตามเวฟปัจจุบัน (เวฟยิ่งสูง ยิ่งเร็ว)
            float minSpeed = 100f + (wave * 50f); 
            float maxSpeed = 150f + (wave * 70f); 
            
            // สุ่มความเร็วในกรอบที่กำหนด
            float randomSpeed = Random.Range(minSpeed, maxSpeed);

            // ส่งค่าความเร็วไปให้สคริปต์ EnemyAI ของตัวที่เพิ่งเสกออกมา
            EnemyAI enemyScript = enemy.GetComponent<EnemyAI>();
            if (enemyScript != null)
            {
                enemyScript.speed = randomSpeed;
            }
            // ---------------------------------------------

            enemiesSpawned++; // นับว่าเสกมาแล้วกี่ตัว
        }
    }

    public void AddScore(int amount)
    {
        if (!isPlaying) return;

        score += amount;

        // 20 ตัว / 5 เวฟ = เปลี่ยนเวฟทุกๆ การยิงได้ 4 ตัว
        if (score > 0 && score % 4 == 0 && wave < 5)
        {
            wave++;
            if (spawnInterval > 0.6f) spawnInterval -= 0.2f; // เร็วขึ้น
        }

        UpdateScoreUI();

        // ถ้ายิงครบ 20 ตัว = ชนะเกม
        if (score >= 20)
        {
            WinGame();
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (waveText != null) waveText.text = "Wave: " + wave;
    }

    public void WinGame()
    {
        isPlaying = false;
        ClearEntities(); // เคลียร์ของบนจอ
        if (winPanel != null) winPanel.SetActive(true); // โชว์ UI ชนะ

        int randomReward = Random.Range(minReward, maxReward + 1);

        // อัปเดตข้อความบนจอให้ผู้เล่นเห็นว่าได้เงินเท่าไหร่
        if (rewardText != null)
        {
            rewardText.text = "ได้รับเงิน: " + randomReward; 
        }

        // แจกเงินเข้ากระเป๋าผู้เล่นจริงๆ ผ่าน PlayerStats
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddMoney(randomReward);
            Debug.Log("สุ่มได้เงินรางวัล: " + randomReward);
        }
    }

    // ฟังก์ชันใหม่: โดนเรียกตอนศัตรูหลุดรอดไปได้
    public void LoseGame()
    {
        if (!isPlaying) return;
        isPlaying = false;
        ClearEntities(); // เคลียร์ของบนจอ
        if (losePanel != null) losePanel.SetActive(true); // โชว์ UI แพ้
    }

    // ฟังก์ชันปุ่มออกเกม/กลับเมนู (ใช้ผูกกับปุ่ม Close หน้าแพ้-ชนะ)
    public void CloseSpaceMonneyUI()
    {
        isPlaying = false;
        ClearEntities();
        
        if (SpaceMonneyUI != null) SpaceMonneyUI.SetActive(false);
        if (computerMenuButtonPanel != null) computerMenuButtonPanel.SetActive(true);
    }

    void ClearEntities()
    {
        // ทำลายศัตรูและกระสุนที่ค้างอยู่บนจอ
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in enemies) Destroy(e);
    }
}