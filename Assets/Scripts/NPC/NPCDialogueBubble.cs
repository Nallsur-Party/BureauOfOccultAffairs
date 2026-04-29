using UnityEngine;
using TMPro;

public class NPCDialogueBubble : MonoBehaviour
{
    [SerializeField] private GameObject bubbleRoot;
    [SerializeField] private TMP_Text bubbleText;
    [SerializeField] private float hideDelay = 2f;

    private float hideTimer = -1f;

    private void Awake()
    {
        SetVisible(false);
    }

    private void Update()
    {
        if (hideTimer < 0f)
        {
            return;
        }

        hideTimer -= Time.deltaTime;

        if (hideTimer <= 0f)
        {
            SetVisible(false);
            hideTimer = -1f;
        }
    }

    public void Show(string message)
    {
        if (bubbleRoot == null || bubbleText == null)
        {
            return;
        }

        bubbleText.text = message;
        SetVisible(true);
        hideTimer = hideDelay;
    }

    public void Show(string message, float duration)
    {
        if (bubbleRoot == null || bubbleText == null)
        {
            return;
        }

        bubbleText.text = message;
        SetVisible(true);
        hideTimer = duration;
    }

    public void Hide()
    {
        SetVisible(false);
        hideTimer = -1f;
    }

    private void SetVisible(bool visible)
    {
        if (bubbleRoot == null)
        {
            return;
        }

        bubbleRoot.SetActive(visible);
    }
}
