using UnityEngine;

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
        Move();
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
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
        }
    }
}