using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// แสดงหน้าชนะเกมทันทีที่หนี้หมด (PlayerStats.OnDebtCleared)
/// ใช้ Time.timeScale = 0 หยุดเกมไว้เหมือนระบบ Pause เพื่อไม่ให้สายเข้ามากวนตอนจบเกมแล้ว
/// ยังไม่มีเงื่อนไข "แพ้" ในระบบนี้ (ตามที่ตกลงไว้ก่อนหน้า)
/// </summary>
public class WinScreenManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TMP_Text winText;
    [SerializeField] private Button backToMenuButton;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool hasWon;

    private void Awake()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(BackToMainMenu);
    }

    private void Start()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnDebtCleared += HandleDebtCleared;
        else
            Debug.LogWarning("WinScreenManager: ไม่พบ PlayerStats ใน Scene");
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnDebtCleared -= HandleDebtCleared;
    }

    private void HandleDebtCleared()
    {
        if (hasWon) return; // กันเรียกซ้ำ
        hasWon = true;

        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (winPanel != null)
        {
            winPanel.SetActive(true);
            if (winText != null)
            {
                int day = GameClock.Instance != null ? GameClock.Instance.Day : 1;
                winText.text = $"คุณใช้หนี้หมดแล้ว!\n\nใช้เวลา {day} วัน";
            }
        }
    }

    private void BackToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
