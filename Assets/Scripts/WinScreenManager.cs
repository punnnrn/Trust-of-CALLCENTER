using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// หน้าชนะเกม: หนี้หมดแล้วยังไม่ขึ้นทันที ต้องผ่านจบวัน (นอนหรือหมดเวลา) ก่อน
/// CallCenterManager.EndDay เป็นคนเช็คว่าหนี้หมดไหม แล้วเรียก ShowWin() ให้
/// ระหว่างที่หนี้หมดแต่ยังไม่จบวัน จะโชว์ข้อความบอกให้ไปนอน (ถ้าใส่ Pending Hint ไว้)
/// ใช้ Time.timeScale = 0 หยุดเกมไว้เหมือนระบบ Pause เพื่อไม่ให้สายเข้ามากวนตอนจบเกมแล้ว
/// </summary>
public class WinScreenManager : MonoBehaviour
{
    public static WinScreenManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TMP_Text winText;
    [SerializeField] private Button backToMenuButton;

    [Header("Hint (optional)")]
    [Tooltip("ข้อความ/Object ที่โผล่ตอนหนี้หมดแล้วแต่ยังไม่จบวัน เช่น \"หนี้หมดแล้ว! ไปนอนเพื่อจบเกม\" หายเองถ้าหนี้กลับมา")]
    [SerializeField] private GameObject pendingHint;

    [Header("Room")]
    [Tooltip("ตอนชนะจะปิดคอมและซ่อนปุ่มเตียง/ปุ่มคอม ไม่ใส่ก็ได้ ถ้าเว้นว่างจะหา RoomManager ในซีนให้เอง")]
    [SerializeField] private RoomManager room;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool hasWon;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (winPanel != null) winPanel.SetActive(false);
        if (pendingHint != null) pendingHint.SetActive(false);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(BackToMainMenu);
    }

    private void Start()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnDebtChanged += HandleDebtChanged;
            HandleDebtChanged(PlayerStats.Instance.Debt);
        }
        else
        {
            Debug.LogWarning("WinScreenManager: ไม่พบ PlayerStats ใน Scene");
        }
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnDebtChanged -= HandleDebtChanged;
        if (Instance == this) Instance = null;
    }

    private void HandleDebtChanged(int debt)
    {
        if (pendingHint != null) pendingHint.SetActive(!hasWon && debt <= 0);
    }

    /// เรียกจาก CallCenterManager.EndDay ตอนจบวันที่หนี้เป็น 0: ลบเซฟ หยุดเกม แล้วโชว์หน้าชนะ
    public void ShowWin()
    {
        if (hasWon) return; // กันเรียกซ้ำ
        hasWon = true;

        SaveSystem.Delete(); // จบเกมแล้ว ลบเซฟ ไม่ให้ Continue กลับมาเล่นเกมที่ชนะไปแล้ว

        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (pendingHint != null) pendingHint.SetActive(false);

        RoomManager r = room != null ? room : FindFirstObjectByType<RoomManager>();
        if (r != null) r.LockForGameOver(); // ปิดคอมที่เปิดค้างอยู่ ซ่อนปุ่มเตียง/คอม

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
