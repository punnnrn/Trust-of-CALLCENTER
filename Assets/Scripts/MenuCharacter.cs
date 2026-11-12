using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ตัวละครบนหน้า Main Menu: สุ่มเปลี่ยนสีหน้าเอง และเปลี่ยนสีหน้าตอนเอาเมาส์ชี้ปุ่ม
/// ต้องอยู่คู่กับ ExpressionPortrait บน Image เดียวกัน
/// </summary>
[RequireComponent(typeof(ExpressionPortrait))]
public class MenuCharacter : MonoBehaviour
{
    [Header("ปุ่มที่ตัวละครจะตอบสนองตอนเมาส์ชี้ (ไม่ใส่ก็ได้)")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button tutorialButton;

    [Header("สีหน้าตอนเมาส์ชี้ปุ่ม")]
    [SerializeField] private FaceExpression onContinue = FaceExpression.Grin;
    [SerializeField] private FaceExpression onNewGame = FaceExpression.Smirk;
    [SerializeField] private FaceExpression onExit = FaceExpression.Sweat;
    [SerializeField] private FaceExpression onInfo = FaceExpression.Neutral;

    [Header("สีหน้าตอนไม่มีอะไรเกิดขึ้น (สุ่มสลับเอง)")]
    [SerializeField] private FaceExpression startExpression = FaceExpression.Grin;
    [SerializeField] private FaceExpression[] idleExpressions =
    {
        FaceExpression.Grin, FaceExpression.Neutral, FaceExpression.Smirk, FaceExpression.Flat
    };
    [SerializeField] private Vector2 idleChangeSeconds = new Vector2(3.5f, 7f);
    [Tooltip("หลังเมาส์ออกจากปุ่ม รอกี่วินาทีก่อนกลับไปสุ่มสีหน้าเอง")]
    [SerializeField] private float reactionHoldSeconds = 1f;

    private ExpressionPortrait portrait;
    private FaceExpression current;
    private float reactUntil;

    private void Start()
    {
        portrait = GetComponent<ExpressionPortrait>();
        current = startExpression;
        portrait.Show(startExpression, null, true);

        Hook(continueButton, onContinue);
        Hook(newGameButton, onNewGame);
        Hook(exitButton, onExit);
        Hook(creditsButton, onInfo);
        Hook(tutorialButton, onInfo);

        StartCoroutine(IdleLoop());
    }

    private void Express(FaceExpression e)
    {
        current = e;
        portrait.Show(e);
    }

    private IEnumerator IdleLoop()
    {
        while (true)
        {
            float wait = Random.Range(idleChangeSeconds.x, Mathf.Max(idleChangeSeconds.x, idleChangeSeconds.y));
            yield return new WaitForSecondsRealtime(wait);

            if (Time.unscaledTime < reactUntil) continue;
            if (idleExpressions == null || idleExpressions.Length == 0) continue;

            FaceExpression next = idleExpressions[Random.Range(0, idleExpressions.Length)];
            for (int i = 0; i < 4 && next == current; i++)
                next = idleExpressions[Random.Range(0, idleExpressions.Length)];
            Express(next);
        }
    }

    private void Hook(Button b, FaceExpression e)
    {
        if (b == null) return;
        EventTrigger trig = b.GetComponent<EventTrigger>();
        if (trig == null) trig = b.gameObject.AddComponent<EventTrigger>();

        AddEntry(trig, EventTriggerType.PointerEnter, () =>
        {
            reactUntil = float.MaxValue;
            Express(e);
        });
        AddEntry(trig, EventTriggerType.PointerExit, () =>
        {
            reactUntil = Time.unscaledTime + reactionHoldSeconds;
        });
    }

    private static void AddEntry(EventTrigger trig, EventTriggerType type, UnityAction action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(_ => action());
        trig.triggers.Add(entry);
    }
}
