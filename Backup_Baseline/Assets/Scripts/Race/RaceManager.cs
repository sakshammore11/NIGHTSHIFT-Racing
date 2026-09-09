using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// NIGHTSHIFT — Race Manager
/// Manages the full race lifecycle: Countdown → Race → Finish → Results
/// </summary>
public class RaceManager : MonoBehaviour
{
    public enum RaceState { WaitingToStart, Countdown, Racing, Finished, Paused }

    [Header("References")]
    public CarController playerCar;
    public CheckpointSystem checkpointSystem;
    public HUDController hud;
    public AudioManager audioManager;

    [Header("Countdown")]
    public float countdownDuration = 3f;

    [Header("Results")]
    public float resultsDisplayTime = 5f;

    // State
    public RaceState currentState { get; private set; } = RaceState.WaitingToStart;
    public float raceTime { get; private set; }
    public float bestTime { get; private set; }
    private bool timerRunning = false;
    private bool gamePaused = false;

    // Singleton for easy access
    public static RaceManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        bestTime = PlayerPrefs.GetFloat("NIGHTSHIFT_BestTime", 0f);

        // Auto align track, car, and camera coordinates at runtime
        RaceSceneAutoAlign.PerformAlignment();
    }

    void Start()
    {
        // Wire Endless Highway Manager
        if (EndlessHighwayManager.Instance == null)
        {
            GameObject mgr = new GameObject("EndlessHighwayManager");
            mgr.AddComponent<EndlessHighwayManager>();
        }

        // Disable car until countdown finishes
        if (playerCar != null)
            playerCar.inputEnabled = false;

        // Wire checkpoint events
        if (checkpointSystem != null)
        {
            checkpointSystem.onCheckpointPassed.AddListener(OnCheckpointPassed);
            checkpointSystem.onFinishReached.AddListener(OnFinishReached);
        }

        StartCoroutine(StartCountdown());
    }

    void Update()
    {
        if (timerRunning)
            raceTime += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        // Update HUD timer
        hud?.UpdateTimer(raceTime);
    }

    IEnumerator StartCountdown()
    {
        currentState = RaceState.Countdown;
        hud?.ShowCountdown(true);

        // Count 3-2-1-GO
        for (int i = (int)countdownDuration; i > 0; i--)
        {
            hud?.SetCountdownText(i.ToString());
            audioManager?.PlayCountdownBeep();
            yield return new WaitForSeconds(1f);
        }

        hud?.SetCountdownText("GO!");
        audioManager?.PlayGoSound();

        yield return new WaitForSeconds(0.8f);

        hud?.ShowCountdown(false);

        // Enable car and start timer
        if (playerCar != null)
            playerCar.inputEnabled = true;

        timerRunning = true;
        currentState = RaceState.Racing;
        hud?.SetObjective("Drive endless highway & dodge traffic!");
    }

    void OnCheckpointPassed(int current, int total)
    {
        hud?.UpdateCheckpoints(current, total);
        audioManager?.PlayCheckpointSound();
    }

    void OnFinishReached()
    {
        // In Endless Highway Mode, passing gates awards bonus points and continues infinitely!
        audioManager?.PlayFinishSound();
        hud?.SetObjective("GATE PASSED! +1000 BONUS PTS!");

        // Ensure car input stays active for endless driving
        if (playerCar != null)
            playerCar.inputEnabled = true;
    }

    IEnumerator ReturnToMenuAfterResults()
    {
        yield return new WaitForSeconds(resultsDisplayTime);
        // Results screen stays — player must manually restart or go to menu
    }

    public void RestartRace()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    public void TogglePause()
    {
        if (currentState == RaceState.Finished) return;

        gamePaused = !gamePaused;
        Time.timeScale = gamePaused ? 0f : 1f;
        currentState = gamePaused ? RaceState.Paused : RaceState.Racing;
        hud?.ShowPauseMenu(gamePaused);
    }
}
