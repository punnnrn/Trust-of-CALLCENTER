using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinesGameManager : MonoBehaviour
{
    [Header("Settings")]
    public int totalTiles = 25;   
    public int bombCount = 3;     
    public int rewardPerGem = 10; 
    public float gameOverDelay = 2.0f; 

    [Header("Cooldown Settings (In-Game Seconds)")]
    [Tooltip("จำนวนวินาทีคูลดาวน์ตามเวลาในเกม (เช่น 60 = 1 นาทีในเกม)")]
    public int cooldownInGameSeconds = 60; // ⏱️ คูลดาวน์เป็นวินาทีในเกม

    [Header("References")]
    public Transform gridContainer;
    public GameObject tilePrefab;
    public Sprite gemSprite;
    public Sprite bombSprite;

    [Header("UI Text Display")]
    public TextMeshProUGUI currentRewardText; 
    public TextMeshProUGUI totalWinText;      
    public TextMeshProUGUI cooldownText;      // ลาก Text สำหรับโชว์เวลา คูลดาวน์ มาใส่

    [Header("Win / Lose Panels")]
    public GameObject winPanel;   
    public GameObject losePanel;  

    [Header("Computer UI Panels")]
    public GameObject minigamePanel;   
    public GameObject menuButtonPanel; 

    private List<TileController> tiles = new List<TileController>();
    private int currentReward = 0;
    private int revealedGemsCount = 0;
    private int totalGemsNeeded = 0;

    private int targetTotalInGameSeconds = -1; // วินาทีรวมในเกมที่จะหมด Cooldown
    private Coroutine cooldownCoroutine;

    void Start()
    {
        // ปิดหน้าจอไว้ตอนเริ่มเกมหลัก
        if (minigamePanel != null) minigamePanel.SetActive(false);
    }

    public void GenerateGrid()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);

        currentReward = 0;
        revealedGemsCount = 0;
        totalGemsNeeded = totalTiles - bombCount;
        UpdateRewardUI();

        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }
        tiles.Clear();

        List<bool> tileTypes = new List<bool>();
        for (int i = 0; i < totalTiles; i++)
        {
            tileTypes.Add(i < bombCount); 
        }
        
        for (int i = 0; i < tileTypes.Count; i++)
        {
            bool temp = tileTypes[i];
            int randomIndex = UnityEngine.Random.Range(i, tileTypes.Count);
            tileTypes[i] = tileTypes[randomIndex];
            tileTypes[randomIndex] = temp;
        }

        for (int i = 0; i < totalTiles; i++)
        {
            GameObject newTileObj = Instantiate(tilePrefab, gridContainer);
            TileController tile = newTileObj.GetComponent<TileController>();
            
            tile.gemSprite = gemSprite;
            tile.bombSprite = bombSprite;
            tile.Setup(tileTypes[i], this);

            tiles.Add(tile);
        }
    }

    public void AddScore()
    {
        revealedGemsCount++;
        currentReward += rewardPerGem; 
        UpdateRewardUI();

        if (revealedGemsCount >= totalGemsNeeded)
        {
            GameWin();
        }
    }

    private void UpdateRewardUI()
    {
        if (currentRewardText != null)
        {
            currentRewardText.text = $"เงินสะสม: {currentReward}";
        }
    }

    public void GameWin()
    {
        AddRewardToPlayer();
        if (winPanel != null) winPanel.SetActive(true);
        StartCooldown();
    }

    public void GameOver()
    {
        Debug.Log("เหยียบระเบิด! ช่องที่กดกำลังกระพริบ...");
        
        foreach (var tile in tiles)
        {
            tile.RevealAll();
        }

        AddRewardToPlayer();
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(gameOverDelay);

        if (totalWinText != null)
        {
            totalWinText.text = $"ได้รับเงินรางวัล: {currentReward}";
        }

        if (losePanel != null)
        {
            losePanel.SetActive(true);
        }

        StartCooldown();
    }

    private void AddRewardToPlayer()
    {
        if (currentReward > 0 && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddMoney(currentReward);
        }
    }

    // ----------------------------------------------------------------------
    // ⏱️ ระบบ COOLDOWN ตามเวลาในเกม
    // ----------------------------------------------------------------------

    private void StartCooldown()
    {
        
        targetTotalInGameSeconds = GetCurrentInGameSeconds() + cooldownInGameSeconds;
        LockTilesForCooldown();

        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(InGameCooldownRoutine());
    }

    private void LockTilesForCooldown()
    {
        // ล็อคกระเบื้องทุกช่องในตารางไม่ให้กดได้ระหว่างคูลดาวน์
        foreach (var tile in tiles)
        {
            Button btn = tile.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
    }

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

    private IEnumerator InGameCooldownRoutine()
    {
        if (cooldownText != null) cooldownText.gameObject.SetActive(true);

        int currentInGameSeconds = GetCurrentInGameSeconds();
        int lastInGameSeconds = currentInGameSeconds;

        while (targetTotalInGameSeconds > currentInGameSeconds)
        {
            // 💡 เช็กว่ามีการกดข้ามวันหรือเวลาร่นถอยหลังกลับไปวันใหม่หรือไม่
            if (currentInGameSeconds < lastInGameSeconds)
            {
                // พลอยข้ามวัน ให้หลุดจาก Loop คูลดาวน์ทันที
                break;
            }

            lastInGameSeconds = currentInGameSeconds;

            int targetHour = (targetTotalInGameSeconds / 3600) % 24;
            int targetMinute = (targetTotalInGameSeconds % 3600) / 60;
            int targetSecond = targetTotalInGameSeconds % 60;

            string targetTimeString = $"{targetHour:D2}:{targetMinute:D2}:{targetSecond:D2}";

            if (cooldownText != null)
            {
                cooldownText.text = $"⏳ ติด Cooldown... เริ่มเวลาในเกม: {targetTimeString}";
            }

            yield return new WaitForSeconds(0.2f);
            currentInGameSeconds = GetCurrentInGameSeconds();
        }

        if (cooldownText != null) cooldownText.gameObject.SetActive(false);

        GenerateGrid();
        
    }

    // ----------------------------------------------------------------------
    // UI Panel Open/Close (ปุ่มเปิด / ปุ่ม X ปิดเกม)
    // ----------------------------------------------------------------------

    public void OpenMinigame()
    {
        if (menuButtonPanel != null) menuButtonPanel.SetActive(false);
        if (minigamePanel != null) minigamePanel.SetActive(true);

        int currentInGameSeconds = GetCurrentInGameSeconds();

        // เช็กว่ายังมีคูลดาวน์เหลืออยู่ไหม
        if (targetTotalInGameSeconds > currentInGameSeconds)
        {
            LockTilesForCooldown();
            
            
            if (cooldownCoroutine == null)
            {
                cooldownCoroutine = StartCoroutine(InGameCooldownRoutine());
            }
        }
        else
        {
            GenerateGrid();
        }
    }

    public void CloseMinigame()
    {
        
        if (minigamePanel != null) minigamePanel.SetActive(false);
        if (menuButtonPanel != null) menuButtonPanel.SetActive(true);
    }
}