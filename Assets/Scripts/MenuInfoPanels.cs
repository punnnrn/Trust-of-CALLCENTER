using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// หน้า Main Menu: ปุ่มเปิดรายชื่อผู้พัฒนา (Credits) และปุ่มเปิดหน้าสอนเล่น (Tutorial) พร้อมปุ่มกลับของแต่ละหน้า
/// วางสคริปต์นี้ไว้ใน Object ไหนก็ได้ในซีน MainMenu (เช่น Object เดียวกับ MainMenuManager)
/// แล้วลากปุ่ม/แผงใส่ช่องใน Inspector ช่องไหนไม่ใช้เว้นว่างได้
/// กด Esc ตอนเปิดหน้าไหนอยู่ จะเท่ากับกดปุ่มกลับ
/// </summary>
public class MenuInfoPanels : MonoBehaviour
{
    [Header("เมนูหลัก")]
    [Tooltip("Object ที่เก็บปุ่มเมนูหลัก (New Game / Continue / ...) จะถูกซ่อนตอนเปิด Credits หรือ Tutorial " +
             "แล้วกลับมาโชว์ตอนกดกลับ ไม่ใส่ก็ได้ (ถ้าไม่ใส่ แผงจะวางทับเมนูอยู่ ต้องบังเมนูเอง)")]
    [SerializeField] private GameObject mainMenuButtonsRoot;

    [Header("รายชื่อผู้พัฒนา (Credits)")]
    [SerializeField] private Button creditsButton;      // ปุ่มบนเมนูหลัก
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private Button creditsBackButton;  // ปุ่มกลับ อยู่ใน creditsPanel

    [Header("หน้าสอนเล่น (Tutorial)")]
    [SerializeField] private Button tutorialButton;     // ปุ่มบนเมนูหลัก
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Button tutorialBackButton; // ปุ่มกลับ อยู่ใน tutorialPanel

    private void Awake()
    {
        if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
        if (creditsBackButton != null) creditsBackButton.onClick.AddListener(CloseAll);

        if (tutorialButton != null) tutorialButton.onClick.AddListener(OpenTutorial);
        if (tutorialBackButton != null) tutorialBackButton.onClick.AddListener(CloseAll);

        CloseAll(); // เริ่มซีนด้วยเมนูหลักอย่างเดียว
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && IsAnyPanelOpen())
            CloseAll();
    }

    /// เรียกจากปุ่ม "รายชื่อผู้พัฒนา" (หรือผูก OnClick ใน Inspector ก็ได้)
    public void OpenCredits()
    {
        Show(creditsPanel);
    }

    /// เรียกจากปุ่ม "สอนเล่น" (หรือผูก OnClick ใน Inspector ก็ได้)
    public void OpenTutorial()
    {
        Show(tutorialPanel);
    }

    /// เรียกจากปุ่ม "กลับ": ปิดทุกหน้า กลับมาที่เมนูหลัก
    public void CloseAll()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (mainMenuButtonsRoot != null) mainMenuButtonsRoot.SetActive(true);
    }

    private void Show(GameObject panel)
    {
        if (panel == null) return;

        if (creditsPanel != null) creditsPanel.SetActive(panel == creditsPanel);
        if (tutorialPanel != null) tutorialPanel.SetActive(panel == tutorialPanel);
        if (mainMenuButtonsRoot != null) mainMenuButtonsRoot.SetActive(false);
    }

    private bool IsAnyPanelOpen()
    {
        return (creditsPanel != null && creditsPanel.activeSelf)
            || (tutorialPanel != null && tutorialPanel.activeSelf);
    }
}