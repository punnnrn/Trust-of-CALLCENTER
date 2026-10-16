using UnityEngine;

public class ComputerSpaceWar : MonoBehaviour
{
    [Header("Computer UI")]
    public GameObject computerScreenPanel;
    public GameObject computerMenuButtonPanel;

    [Header("Space War")]
    public GameObject SpaceMonneyUI;

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
        // ปิดมินิเกม
        if (SpaceMonneyUI != null)
            SpaceMonneyUI.SetActive(false);

        // แสดงปุ่มเลือกเมนูกลับมา
        if (computerMenuButtonPanel != null)
            computerMenuButtonPanel.SetActive(true);
    }
}
