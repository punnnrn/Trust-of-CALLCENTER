using UnityEngine;

public class ComputerSpaceWar : MonoBehaviour
{
    [Header("Computer UI")]
    public GameObject computerScreenPanel;
    public GameObject computerMenuButtonPanel;

    [Header("Space War")]
    public GameObject SpaceMonneyUI;
    [Tooltip("ไม่ใส่ก็ได้ ถ้าเว้นว่างจะหา SpaceMonneyManager ในซีนให้เอง")]
    public SpaceMonneyManager manager;

    public void StartSpaceWar()
    {
        // ซ่อนเฉพาะปุ่มเลือกเมนู
        if (computerMenuButtonPanel != null)
            computerMenuButtonPanel.SetActive(false);

        // เปิดมินิเกม
        if (SpaceMonneyUI != null)
            SpaceMonneyUI.SetActive(true);

        Debug.Log("Space War Started!");
    }

    public void ExitSpaceWar()
    {
        // ปิดผ่าน SpaceMonneyManager เพื่อให้นับเป็น "จบรอบ" (คูลดาวน์ + ความหิว) ถ้ากำลังเล่นค้างอยู่
        // ไม่งั้นกดออกกลางเกมจะเลี่ยงคูลดาวน์ได้ และเกมจะยังเสกศัตรูอยู่เบื้องหลัง
        SpaceMonneyManager m = manager != null ? manager : FindFirstObjectByType<SpaceMonneyManager>();
        if (m != null)
        {
            m.CloseSpaceMonneyUI();
            return;
        }

        // ไม่พบ manager: ปิดแบบเดิม
        if (SpaceMonneyUI != null)
            SpaceMonneyUI.SetActive(false);

        if (computerMenuButtonPanel != null)
            computerMenuButtonPanel.SetActive(true);
    }
}
