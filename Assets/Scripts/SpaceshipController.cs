using UnityEngine;
using UnityEngine.EventSystems;

public class SpaceshipController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 500f;
    public RectTransform screenBounds; // ขอบเขตหน้าจอคอมพิวเตอร์
    private RectTransform rectTransform;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform shootSpawnPoint;
    public Transform bulletParent; // เอาไว้เก็บกระสุนไม่ให้รก Hierarchy

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        // ตอน Pause ไม่รับ input (ไม่งั้นคลิกปุ่มในเมนู Pause แล้วยิงกระสุนสะสมไว้)
        if (Time.timeScale == 0f) return;

        Move();

        // คลิกเมาส์ที่เป็นการกดปุ่ม UI (รับสาย/วางสาย/Start/Close ฯลฯ) ไม่นับเป็นการยิง
        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Input.GetKeyDown(KeyCode.Space) || (Input.GetMouseButtonDown(0) && !pointerOverUI))
        {
            Shoot();
        }
    }

    void Move()
    {
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput = 1f;

        Vector2 pos = rectTransform.anchoredPosition;
        pos.x += moveInput * moveSpeed * Time.deltaTime;

        // ล็อกไม่ให้ยานวิ่งทะลุกอบจอ (ปรับค่าตามความกว้างหน้าจอ UI)
        pos.x = Mathf.Clamp(pos.x, -350f, 350f);
        rectTransform.anchoredPosition = pos;
    }

    void Shoot()
    {
        if (bulletPrefab != null && shootSpawnPoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, shootSpawnPoint.position, Quaternion.identity, bulletParent);

            // สั่งเล่นเสียงยิงผ่าน SpaceMonneyManager
            SpaceMonneyManager manager = FindObjectOfType<SpaceMonneyManager>();
            if (manager != null)
            {
                manager.PlayShootSound();
            }
        }
    }
}