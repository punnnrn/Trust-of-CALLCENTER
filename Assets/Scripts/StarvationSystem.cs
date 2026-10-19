using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// เงื่อนไขแพ้: ไม่ได้กินอาหารติดต่อกันครบ N วัน = แพ้ (ลบเซฟ เริ่มใหม่หมด)
/// "กินอาหาร" = ซื้ออาหารจากร้านค้า (HungerSystem.Eat ถูกเรียก) อย่างน้อย 1 ครั้งในวันนั้น
/// ตอนจบวัน ถ้าวันนั้นไม่ได้กินเลย ตัวนับจะ +1 ถ้ากินแล้วตัวนับกลับเป็น 0
/// ตัวนับถูกเซฟตอนจบวันด้วย (CallCenterManager เป็นคนเรียกเก็บและเรียกแสดงหน้าแพ้)
/// หมายเหตุ: หลอดความหิวยังถูกเติมเต็มตอนนอนตามเดิม เงื่อนไขแพ้นี้ไม่ผูกกับค่าในหลอด
/// </summary>
public class StarvationSystem : MonoBehaviour
{
    public static StarvationSystem Instance { get; private set; }

    [Header("Rule")]
    [Tooltip("ไม่ได้กินอาหารติดต่อกันกี่วันถึงจะแพ้")]
    [SerializeField, Min(1)] private int daysWithoutFoodToLose = 3;

    [Header("Warning UI (optional)")]
    [Tooltip("ข้อความเตือนบนจอ จะโผล่เมื่อมีวันที่ไม่ได้กินสะสมอยู่ และวันนี้ยังไม่ได้กิน")]
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private string warningFormat = "ไม่ได้กินอาหารมา {0} วันแล้ว! (แพ้เมื่อครบ {1} วัน)";

    [Header("Lose Screen")]
    [SerializeField] private GameObject losePanel;
    [SerializeField] private TMP_Text loseText;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private string loseMessage = "คุณไม่ได้กินอาหารติดต่อกัน {0} วัน\n\nอยู่รอดมาได้ {1} วัน";

    [Header("Room")]
    [Tooltip("ตอนแพ้จะปิดคอมและซ่อนปุ่มเตียง/ปุ่มคอม ไม่ใส่ก็ได้ ถ้าเว้นว่างจะหา RoomManager ในซีนให้เอง")]
    [SerializeField] private RoomManager room;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    /// จำนวนวันที่ผ่านมาแล้วที่ไม่ได้กินอาหารติดต่อกัน (ไม่นับวันนี้ที่ยังไม่จบ)
    public int DaysWithoutFood { get; private set; }
    public int Limit => daysWithoutFoodToLose;
    public bool HasLost => DaysWithoutFood >= daysWithoutFoodToLose;

    private bool ateToday;
    private bool lost;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (SaveSystem.Loaded != null)
            DaysWithoutFood = Mathf.Max(0, SaveSystem.Loaded.daysWithoutFood);

        if (losePanel != null) losePanel.SetActive(false);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(BackToMainMenu);
    }

    private void Start()
    {
        if (HungerSystem.Instance != null) HungerSystem.Instance.OnAte += HandleAte;
        else Debug.LogWarning("StarvationSystem: ไม่พบ HungerSystem ใน Scene");

        if (GameClock.Instance != null) GameClock.Instance.OnDayEnded += HandleDayEnded;
        else Debug.LogWarning("StarvationSystem: ไม่พบ GameClock ใน Scene");

        RefreshWarning();
    }

    private void OnDestroy()
    {
        if (HungerSystem.Instance != null) HungerSystem.Instance.OnAte -= HandleAte;
        if (GameClock.Instance != null) GameClock.Instance.OnDayEnded -= HandleDayEnded;
        if (Instance == this) Instance = null;
    }

    private void HandleAte()
    {
        ateToday = true;
        RefreshWarning();
    }

    /// จบวัน: วันนี้ไม่ได้กิน ตัวนับ +1 / กินแล้ว ตัวนับกลับเป็น 0
    private void HandleDayEnded(int day)
    {
        DaysWithoutFood = ateToday ? 0 : DaysWithoutFood + 1;
        ateToday = false;
        RefreshWarning();
    }

    private void RefreshWarning()
    {
        if (warningText == null) return;

        bool show = DaysWithoutFood > 0 && !ateToday;
        warningText.gameObject.SetActive(show);
        if (show) warningText.text = string.Format(warningFormat, DaysWithoutFood, daysWithoutFoodToLose);
    }

    /// เรียกจาก CallCenterManager.EndDay เมื่อ HasLost: ลบเซฟ หยุดเกม แล้วโชว์หน้าแพ้
    public void TriggerLose(int day)
    {
        ShowLose(string.Format(loseMessage, daysWithoutFoodToLose, day));
    }

    /// แพ้ด้วยเหตุผลอื่น (เช่น จ่ายบิลไม่ไหว) ใช้หน้าแพ้อันเดียวกัน แต่ข้อความตามที่ส่งมา
    public void TriggerLoseWithMessage(string message)
    {
        ShowLose(message);
    }

    private void ShowLose(string message)
    {
        if (lost) return;
        lost = true;

        SaveSystem.Delete(); // แพ้แล้ว เริ่มใหม่หมด ไม่ให้ Continue กลับมา

        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (warningText != null) warningText.gameObject.SetActive(false);

        RoomManager r = room != null ? room : FindFirstObjectByType<RoomManager>();
        if (r != null) r.LockForGameOver(); // ซ่อนปุ่มเตียง/คอม ปิดคอมที่เปิดค้างอยู่

        if (losePanel != null)
        {
            losePanel.SetActive(true);
            if (loseText != null) loseText.text = message;
        }
    }

    private void BackToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
