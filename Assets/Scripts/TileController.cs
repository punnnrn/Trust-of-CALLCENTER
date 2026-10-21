using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TileController : MonoBehaviour
{
    [SerializeField] private Image contentImage;
    [SerializeField] private Button button;

    [Header("Sprites")]
    public Sprite gemSprite;
    public Sprite bombSprite;

    private bool isBomb = false;
    private bool isRevealed = false;
    private MinesGameManager gameManager;
    private Coroutine blinkCoroutine;

    public void Setup(bool isBomb, MinesGameManager manager)
    {
        // ยกเลิกการกระพริบเก่า (ถ้ามี)
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        
        this.isBomb = isBomb;
        this.gameManager = manager;
        isRevealed = false;
        
        // คืนค่าสีปกติของ Image
        contentImage.color = Color.white;
        contentImage.gameObject.SetActive(false);
        button.interactable = true;
        
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickTile);
    }

    private void OnClickTile()
    {
        if (isRevealed) return;

        isRevealed = true;
        button.interactable = false;
        contentImage.gameObject.SetActive(true);

        if (isBomb)
        {
            contentImage.sprite = bombSprite;
            StartBlinking(); // เริ่มกระพริบช่องระเบิดที่ผู้เล่นกดโดน
            gameManager.GameOver();
        }
        else
        {
            contentImage.sprite = gemSprite;
            gameManager.AddScore();
        }
    }

    // ฟังก์ชันเปิดเผยช่องทั้งหมดเมื่อจบเกม (ระเบิดลูกอื่นที่ไม่โดนกดจะเปิดโชว์ปกติ ไม่กระพริบ)
    public void RevealAll()
    {
        if (!isRevealed)
        {
            isRevealed = true;
            contentImage.gameObject.SetActive(true);
            contentImage.sprite = isBomb ? bombSprite : gemSprite;
            button.interactable = false;
        }
    }

    // สั่งเริ่มการกระพริบ
    public void StartBlinking()
    {
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    // Coroutine สลับสีแดงและสีปกติเพื่อทำให้กระพริบ
    private IEnumerator BlinkRoutine()
    {
        Color normalColor = Color.white;
        Color flashColor = Color.red; // สีแดงตอนกระพริบ (หรือเปลี่ยนเป็น Color.clear เพื่อให้จางหาย-ติด)

        while (true)
        {
            contentImage.color = flashColor;
            yield return new WaitForSeconds(0.2f); // ระยะเวลาสีแดง
            contentImage.color = normalColor;
            yield return new WaitForSeconds(0.2f); // ระยะเวลาสีปกติ
        }
    }

    private void OnDisable()
    {
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
    }
}