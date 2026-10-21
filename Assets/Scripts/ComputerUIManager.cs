using UnityEngine;

public class ComputerUIManager : MonoBehaviour
{
    [Header("Panels / Screens")]
    public GameObject minigamePanel;   // Panel มินิเกม (เช่น MinigameM)
    public GameObject shopPanel;       // Panel ร้านค้า
    public GameObject billsPanel;      // Panel บิล
    public GameObject deptPanel;       // Panel หนี้
    public GameObject menuButtonPanel; // Panel ปุ่มรวมเมนูบนหน้าจอคอม

    // ฟังก์ชันเปิดมินิเกม และซ่อน UI อื่นๆ ทั้งหมด
    public void OpenMinigame()
    {
        // 1. ซ่อน UI หน้าต่างอื่นๆ
        CloseAllPanels();

        // 2. เปิดหน้าจอมินิเกม
        if (minigamePanel != null)
        {
            minigamePanel.SetActive(true);
        }

        // Option: ถ้าต้องการรีเซ็ต/สร้างตารางเกมใหม่ทุกครั้งที่เปิด ให้เรียก GenerateGrid
        MinesGameManager gameManager = minigamePanel.GetComponentInChildren<MinesGameManager>();
        if (gameManager != null)
        {
            gameManager.GenerateGrid();
        }
    }

    // ฟังก์ชันปิดมินิเกม (กลับมาหน้าเมนูคอมพิวเตอร์)
    public void CloseMinigame()
    {
        if (minigamePanel != null)
        {
            minigamePanel.SetActive(false);
        }

        // แสดงปุ่มเมนูหลักกลับขึ้นมา
        if (menuButtonPanel != null)
        {
            menuButtonPanel.SetActive(true);
        }
    }

    // ฟังก์ชันช่วยซ่อน UI หน้าต่างทั้งหมด
    private void CloseAllPanels()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (billsPanel != null) billsPanel.SetActive(false);
        if (deptPanel != null) deptPanel.SetActive(false);
        if (menuButtonPanel != null) menuButtonPanel.SetActive(false);
    }
}