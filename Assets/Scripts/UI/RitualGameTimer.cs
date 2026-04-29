using System.Collections;
using UnityEngine;

public class RitualGameTimer : MonoBehaviour
{
    private const string BestScorePrefsKey = "BureauOfOccultAffairs.BestRitualScore";

    [SerializeField] private float gameDurationSeconds = 300f;
    [SerializeField] private RitualTimerUI timerUI;
    [SerializeField] private RitualPointsUI pointsUI;
    [SerializeField] private PlayerController playerController;

    private float remainingSeconds;
    private bool isTimerActive = true;
    private bool isAwaitingChoice;
    private int bestScore;
    private Coroutine hideTimerMessageCoroutine;

    private void Awake()
    {
        Time.timeScale = 1f;
        remainingSeconds = Mathf.Max(0f, gameDurationSeconds);
        bestScore = PlayerPrefs.GetInt(BestScorePrefsKey, 0);

        if (timerUI == null)
        {
            timerUI = FindObjectOfType<RitualTimerUI>();
        }

        if (pointsUI == null)
        {
            pointsUI = FindObjectOfType<RitualPointsUI>();
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
    }

    private void Start()
    {
        RefreshTimerUI();
    }

    private void Update()
    {
        if (isAwaitingChoice)
        {
            HandleChoiceInput();
            return;
        }

        if (!isTimerActive)
        {
            return;
        }

        remainingSeconds -= Time.unscaledDeltaTime;
        if (remainingSeconds <= 0f)
        {
            remainingSeconds = 0f;
            BeginEndChoice();
            return;
        }

        RefreshTimerUI();
    }

    public void ForceEndGame()
    {
        if (isAwaitingChoice || !isTimerActive)
        {
            return;
        }

        remainingSeconds = 0f;
        BeginEndChoice();
    }

    public void RestartTimer()
    {
        isAwaitingChoice = false;
        isTimerActive = true;
        remainingSeconds = Mathf.Max(0f, gameDurationSeconds);

        ResumeGameplay();
        RefreshTimerUI();
    }

    public void ContinueWithoutTimer()
    {
        isAwaitingChoice = false;
        isTimerActive = false;

        ResumeGameplay();

        if (timerUI != null)
        {
            timerUI.ShowTimerDisabled();
            StartHideTimerMessageRoutine();
        }

        Debug.Log("Ritual timer disabled. Game continues without countdown.");
    }

    private void HandleChoiceInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartTimer();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            ContinueWithoutTimer();
        }
    }

    private void BeginEndChoice()
    {
        if (isAwaitingChoice)
        {
            return;
        }

        isAwaitingChoice = true;
        int currentScore = pointsUI != null ? pointsUI.CurrentPoints : 0;
        int previousBestScore = bestScore;
        int newBestScore = Mathf.Max(previousBestScore, currentScore);
        bool isNewRecord = currentScore > previousBestScore;

        SaveBestScore(newBestScore);

        if (timerUI != null)
        {
            timerUI.ShowEndChoice(currentScore, newBestScore, isNewRecord);
        }

        PauseGameplay();
    }

    private void SaveBestScore(int newBestScore)
    {
        bestScore = newBestScore;

        PlayerPrefs.SetInt(BestScorePrefsKey, newBestScore);
        PlayerPrefs.Save();
    }

    private void RefreshTimerUI()
    {
        if (timerUI != null)
        {
            timerUI.SetCountdown(remainingSeconds);
        }
    }

    private void PauseGameplay()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        Time.timeScale = 0f;
    }

    private void ResumeGameplay()
    {
        Time.timeScale = 1f;

        if (playerController != null)
        {
            playerController.enabled = true;
        }
    }

    private void StartHideTimerMessageRoutine()
    {
        if (hideTimerMessageCoroutine != null)
        {
            StopCoroutine(hideTimerMessageCoroutine);
        }

        hideTimerMessageCoroutine = StartCoroutine(HideTimerMessageAfterDelay());
    }

    private IEnumerator HideTimerMessageAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1f);

        if (timerUI != null)
        {
            timerUI.ClearText();
        }

        hideTimerMessageCoroutine = null;
    }
}
