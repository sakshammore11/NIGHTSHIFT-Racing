using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// NIGHTSHIFT — HUD Controller
/// Manages all in-game UI: speedometer, timer, checkpoints, countdown, pause menu, results.
/// Requires TextMeshPro (included in URP template packages).
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Speed")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI gearText;

    [Header("Timer")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI bestTimeText;

    [Header("Checkpoints")]
    public TextMeshProUGUI checkpointText;

    [Header("Objective")]
    public TextMeshProUGUI objectiveText;
    public CanvasGroup objectiveGroup;

    [Header("Countdown")]
    public GameObject countdownPanel;
    public TextMeshProUGUI countdownText;

    [Header("Pause Menu")]
    public GameObject pauseMenuPanel;
    public Button resumeButton;
    public Button restartButtonPause;
    public Button mainMenuButtonPause;

    [Header("Results")]
    public GameObject resultsPanel;
    public TextMeshProUGUI resultTimeText;
    public TextMeshProUGUI resultBestTimeText;
    public Button restartButtonResults;
    public Button mainMenuButtonResults;

    [Header("Car Reference")]
    public CarController carController;

    private Coroutine objectiveFadeCoroutine;

    void Start()
    {
        if (carController == null)
        {
            GameObject car = GameObject.Find("PlayerCar") ?? GameObject.FindWithTag("Player");
            if (car != null) carController = car.GetComponent<CarController>();
        }

        // Wire pause buttons
        resumeButton?.onClick.AddListener(() => RaceManager.Instance?.TogglePause());
        restartButtonPause?.onClick.AddListener(() => RaceManager.Instance?.RestartRace());
        mainMenuButtonPause?.onClick.AddListener(() => RaceManager.Instance?.GoToMainMenu());
        restartButtonResults?.onClick.AddListener(() => RaceManager.Instance?.RestartRace());
        mainMenuButtonResults?.onClick.AddListener(() => RaceManager.Instance?.GoToMainMenu());

        // Initial state
        ShowCountdown(false);
        ShowPauseMenu(false);
        if (resultsPanel) resultsPanel.SetActive(false);
    }

    void Update()
    {
        UpdateSpeedometer();
    }

    void UpdateSpeedometer()
    {
        if (carController == null)
        {
            GameObject car = GameObject.Find("PlayerCar") ?? GameObject.FindWithTag("Player");
            if (car != null) carController = car.GetComponent<CarController>();
        }

        if (carController == null || speedText == null) return;
        float speed = carController.currentSpeedKMH;
        speedText.text = $"{Mathf.RoundToInt(speed)} KM/H";
    }

    public void UpdateTimer(float time)
    {
        if (timerText == null) return;
        timerText.text = FormatTime(time);
    }

    public void UpdateCheckpoints(int current, int total)
    {
        if (checkpointText == null) return;
        checkpointText.text = $"{current} / {total}";
    }

    public void SetObjective(string message)
    {
        if (objectiveText == null) return;
        objectiveText.text = message;
        if (objectiveFadeCoroutine != null) StopCoroutine(objectiveFadeCoroutine);
        objectiveFadeCoroutine = StartCoroutine(FadeObjective());
    }

    IEnumerator FadeObjective()
    {
        if (objectiveGroup == null) yield break;
        objectiveGroup.alpha = 1f;
        yield return new WaitForSeconds(3f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            objectiveGroup.alpha = 1f - t;
            yield return null;
        }
        objectiveGroup.alpha = 0f;
    }

    public void ShowCountdown(bool show)
    {
        if (countdownPanel) countdownPanel.SetActive(show);
    }

    public void SetCountdownText(string text)
    {
        if (countdownText == null) return;
        countdownText.text = text;
        // Scale punch animation
        StopAllCoroutines();
        StartCoroutine(PunchScale(countdownText.transform));
    }

    IEnumerator PunchScale(Transform t)
    {
        Vector3 original = t.localScale;
        t.localScale = original * 1.4f;
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            t.localScale = Vector3.Lerp(original * 1.4f, original, elapsed / 0.3f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = original;
    }

    public void ShowPauseMenu(bool show)
    {
        if (pauseMenuPanel) pauseMenuPanel.SetActive(show);
    }

    public void ShowResults(float time, float bestTime)
    {
        if (resultsPanel) resultsPanel.SetActive(true);
        if (resultTimeText) resultTimeText.text = $"Your Time: {FormatTime(time)}";
        if (resultBestTimeText)
        {
            if (Mathf.Abs(time - bestTime) < 0.01f)
                resultBestTimeText.text = "NEW BEST TIME!";
            else
                resultBestTimeText.text = $"Best Time: {FormatTime(bestTime)}";
        }
    }

    string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        int ms = Mathf.FloorToInt((seconds * 100f) % 100f);
        return $"{mins:00}:{secs:00}.{ms:00}";
    }
}
