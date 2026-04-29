using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class RitualTimerUI : MonoBehaviour
{
    private TMP_Text timerText;

    private void Awake()
    {
        timerText = GetComponent<TMP_Text>();
    }

    public void SetCountdown(float remainingSeconds)
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void ShowEndChoice(int currentScore, int bestScore, bool isNewRecord)
    {
        if (timerText == null)
        {
            return;
        }

        string recordLine = isNewRecord
            ? $"Новый рекорд: {bestScore}"
            : $"Рекорд: {bestScore}";

        timerText.text =
            $"Время вышло\n" +
            $"Очки: {currentScore}\n" +
            $"{recordLine}\n\n" +
            "R - перезапустить таймер\n" +
            "C - продолжить без таймера";
    }

    public void ShowTimerDisabled()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.text = "Таймер отключен";
    }

    public void ClearText()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.text = string.Empty;
    }
}
