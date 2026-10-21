using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MinesGameManager : MonoBehaviour
{
    [Header("Settings")]
    public int totalTiles = 25;   
    public int bombCount = 3;     
    public int rewardPerGem = 10; 
    public float gameOverDelay = 2.0f; 
    [Header("References")]
    public Transform gridContainer;
    public GameObject tilePrefab;
    public Sprite gemSprite;
    public Sprite bombSprite;

    [Header("UI Text Display")]
    public TextMeshProUGUI currentRewardText; 
    public TextMeshProUGUI totalWinText;       

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

    void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        StopAllCoroutines();

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

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
            int randomIndex = Random.Range(i, tileTypes.Count);
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
    }

    // เมื่อกดโดนระเบิด
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
    }

    private void AddRewardToPlayer()
    {
        if (currentReward > 0 && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddMoney(currentReward);
        }
    }

    public void OpenMinigame()
    {
        if (menuButtonPanel != null) menuButtonPanel.SetActive(false);
        if (minigamePanel != null) minigamePanel.SetActive(true);
        GenerateGrid();
    }

    public void CloseMinigame()
    {
        StopAllCoroutines();
        if (minigamePanel != null) minigamePanel.SetActive(false);
        if (menuButtonPanel != null) menuButtonPanel.SetActive(true);
    }
}