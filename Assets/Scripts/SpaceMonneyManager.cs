using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("Cooldown & Hunger")]
    [Tooltip("จำนวนวินาทีคูลดาวน์ตามเวลาในเกม (เช่น 60 = 1 นาทีในเกม, 10800 = 3 ชั่วโมงในเกม)")]
    public int cooldownInGameSeconds = 60;
    [Tooltip("ความหิวที่ลดลงทุกครั้งที่จบรอบ (ชนะ/แพ้/กดปิดกลางเกม)")]
    public float hungerCostPerRound = 10f;
    [Tooltip("ไม่ใส่ก็ได้ ใช้โชว์ \"ติด Cooldown... เริ่มเวลาในเกม: HH:MM:SS\" จะโผล่เองตอนติดคูลดาวน์ และซ่อนเองเมื่อพร้อมเล่น")]
    public TMP_Text cooldownText;
    [Tooltip("ไม่ใช้แล้ว (ปุ่ม/ไอคอนจะกดได้ตลอด ถ้าติดคูลดาวน์จะเปิดหน้าต่างมาโชว์เวลา) เว้นว่างได้")]
    public Button startButton;

    [Header("Audio")]
    [Tooltip("AudioSource สำหรับเล่นเสียงในมินิเกม (ถ้าไม่ใส่ สคริปต์จะหาให้อัตโนมัติ)")]
    public AudioSource audioSource;
    [Tooltip("ไฟล์เสียงตอนยิงกระสุน (.wav / .mp3)")]
    public AudioClip shootSound;
    [SerializeField, Range(0f, 1f)] private float shootSVolume = 1f;

    private bool isPlaying = false;

    /// กำลังเล่นรอบอยู่ไหม (SpaceshipController ใช้เช็คว่าให้ควบคุมยานได้หรือยัง)
    public bool IsPlaying => isPlaying;
    private int targetTotalInGameSeconds = -1; // เวลาในเกม (วินาทีรวม) ที่คูลดาวน์จะหมด (-1 = พร้อมเล่น)
    private string cooldownMessage = "";
    private Coroutine cooldownRoutine;

    private void Start()
    {
        UpdateScoreUI();

        // เช็ค AudioSource ถ้ายังไม่ได้ลากใส่ใน Inspector ให้ดึงจาก GameObject นี้อัตโนมัติ
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)

            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }

    private void Update()
    {
        if (!isPlaying)
        {
            UpdateCooldownUI();
            return;
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnEnemy();
            spawnTimer = 0f;
        }
    }

    /// ฟังก์ชันสำหรับเรียกเล่นเสียงยิงกระสุน (สามารถเรียกจาก SpaceshipController ตอนกดยิงได้)
    public void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound, shootSVolume);
        }
    }

    /// กดไอคอนเกมบนคอม (ผูกกับ OnClick ของไอคอน): เปิดหน้าต่างเกมเสมอ
    /// ถ้าติดคูลดาวน์ จะโชว์เวลาในเกมที่เล่นได้อีกครั้ง แล้วเริ่มรอบใหม่ให้เองเมื่อคูลดาวน์หมด (เหมือน MalwareGameManager)
    public void StartGamePlay()
    {
        if (isPlaying) return;

        if (IsOnCooldown())
        {
            // เปิดหน้าต่างมาดูเวลาคูลดาวน์ (ซ่อนหน้าแพ้/ชนะเก่าที่อาจค้างอยู่)
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            if (computerMenuButtonPanel != null) computerMenuButtonPanel.SetActive(false);
            if (SpaceMonneyUI != null) SpaceMonneyUI.SetActive(true);

            UpdateCooldownUI();
            if (cooldownRoutine == null) cooldownRoutine = StartCoroutine(CooldownRoutine());
            return;
        }

        BeginRound();
    }

    private void BeginRound()
    {
        StopCooldownRoutine();

        isPlaying = true;
        score = 0;
        wave = 1;
        enemiesSpawned = 0; // รีเซ็ตจำนวนศัตรูเป็น 0
        spawnInterval = 1.5f; // รีเซ็ตความเร็ว
        UpdateScoreUI();
        UpdateCooldownUI();

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
        if (!isPlaying) return;
        isPlaying = false;
        FinishRound(); // เริ่มคูลดาวน์ + ลดความหิว
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
        FinishRound(); // เริ่มคูลดาวน์ + ลดความหิว (เล่นแล้วถือว่าทำงาน แม้แพ้)
        ClearEntities(); // เคลียร์ของบนจอ
        if (losePanel != null) losePanel.SetActive(true); // โชว์ UI แพ้
    }

    // ฟังก์ชันปุ่มออกเกม/กลับเมนู (ใช้ผูกกับปุ่ม Close หน้าแพ้-ชนะ)
    public void CloseSpaceMonneyUI()
    {
        // ปิดกลางรอบ = ยอมแพ้ ยังนับว่าจบรอบ (กันกดปิดหนีก่อนแพ้เพื่อเลี่ยงคูลดาวน์/ความหิว)
        if (isPlaying) FinishRound();

        StopCooldownRoutine(); // ปิดหน้าต่างแล้วไม่ต้องเริ่มรอบใหม่ให้เอง (เปิดไอคอนใหม่ค่อยเริ่ม)

        isPlaying = false;
        ClearEntities();

        if (SpaceMonneyUI != null) SpaceMonneyUI.SetActive(false);
        if (computerMenuButtonPanel != null) computerMenuButtonPanel.SetActive(true);
    }

    /// เรียกจาก CallCenterManager ตอนจบวัน/นอน: ปิดมินิเกมทั้งหมดแล้วรีเซ็ตให้เหมือนเพิ่งเปิดคอมขึ้นมาใหม่
    /// (ไม่คิดคูลดาวน์/ความหิว และล้างคูลดาวน์ให้พร้อมเล่นตั้งแต่เช้าวันใหม่)
    public void ResetForNewDay()
    {
        isPlaying = false;
        targetTotalInGameSeconds = -1;
        StopCooldownRoutine();
        ClearEntities();

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (SpaceMonneyUI != null) SpaceMonneyUI.SetActive(false);
        if (computerMenuButtonPanel != null) computerMenuButtonPanel.SetActive(true);

        UpdateCooldownUI();
    }

    // ---------------- Cooldown & Hunger ----------------

    private void FinishRound()
    {
        // เริ่มคูลดาวน์ตามเวลาในเกม แล้วเตรียมข้อความโชว์เวลาที่จะเล่นได้อีกครั้ง
        targetTotalInGameSeconds = GetCurrentInGameSeconds() + cooldownInGameSeconds;

        int targetHour = (targetTotalInGameSeconds / 3600) % 24;
        int targetMinute = (targetTotalInGameSeconds % 3600) / 60;
        int targetSecond = targetTotalInGameSeconds % 60;
        cooldownMessage = $"⏳ ติด Cooldown... เล่นอีกครั้งได้ในเวลา: {targetHour:D2}:{targetMinute:D2}:{targetSecond:D2}";

        if (HungerSystem.Instance != null)
            HungerSystem.Instance.Drain(hungerCostPerRound);

        // รอจนคูลดาวน์หมดแล้วเริ่มรอบใหม่ให้เอง ถ้าหน้าต่างเกมยังเปิดอยู่
        StopCooldownRoutine();
        cooldownRoutine = StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        while (IsOnCooldown())
            yield return new WaitForSeconds(0.2f);

        cooldownRoutine = null;
        UpdateCooldownUI();

        // เริ่มรอบใหม่ให้เอง เฉพาะตอนหน้าต่างเกมยังเปิดอยู่
        if (SpaceMonneyUI == null || SpaceMonneyUI.activeSelf)
            BeginRound();
    }

    private void StopCooldownRoutine()
    {
        if (cooldownRoutine == null) return;
        StopCoroutine(cooldownRoutine);
        cooldownRoutine = null;
    }

    private bool IsOnCooldown() => targetTotalInGameSeconds > GetCurrentInGameSeconds();

    /// วินาทีรวมตามเวลาในเกม (นับตั้งแต่วันที่ 1) ใช้สูตรเดียวกับ MalwareGameManager
    private int GetCurrentInGameSeconds()
    {
        if (GameClock.Instance != null)
        {
            int day = GameClock.Instance.Day;
            int hour = GameClock.Instance.Hour;
            int minute = GameClock.Instance.Minute;

            return (day * 86400) + (hour * 3600) + (minute * 60);
        }

        return (int)Time.time;
    }

    private void UpdateCooldownUI()
    {
        bool onCooldown = IsOnCooldown();

        if (cooldownText != null)
        {
            if (cooldownText.gameObject.activeSelf != onCooldown)
                cooldownText.gameObject.SetActive(onCooldown);

            if (onCooldown && cooldownText.text != cooldownMessage)
                cooldownText.text = cooldownMessage;
        }
    }

    // ---------------- Cleanup ----------------

    void ClearEntities()
    {
        // ทำลายศัตรูและกระสุนที่ค้างอยู่ทั้งหมด (รวมที่อยู่ใน UI ที่ซ่อนอยู่ด้วย ไม่พึ่ง Tag)
        foreach (EnemyAI e in FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Destroy(e.gameObject);

        foreach (Bullet b in FindObjectsByType<Bullet>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Destroy(b.gameObject);
    }
}