using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// NIGHTSHIFT — Main Menu Controller
/// Handles Start Race, Garage, Settings, Quit buttons.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button startRaceButton;
    public Button garageButton;
    public Button quitButton;

    [Header("Animated Car")]
    public Transform carShowcase;
    public float rotationSpeed = 20f;

    [Header("Title")]
    public TextMeshProUGUI titleText;

    void Start()
    {
        // Auto-find buttons if missing from inspector assignment
        if (startRaceButton == null)
            startRaceButton = GameObject.Find("Btn_STARTRACE")?.GetComponent<Button>() ?? GameObject.Find("StartButton")?.GetComponent<Button>() ?? GameObject.Find("StartRaceButton")?.GetComponent<Button>();
        if (garageButton == null)
            garageButton = GameObject.Find("Btn_GARAGE")?.GetComponent<Button>() ?? GameObject.Find("GarageButton")?.GetComponent<Button>();
        if (quitButton == null)
            quitButton = GameObject.Find("Btn_QUIT")?.GetComponent<Button>() ?? GameObject.Find("QuitButton")?.GetComponent<Button>();

        if (startRaceButton != null)
        {
            startRaceButton.onClick.RemoveAllListeners();
            startRaceButton.onClick.AddListener(StartRace);
        }
        if (garageButton != null)
        {
            garageButton.onClick.RemoveAllListeners();
            garageButton.onClick.AddListener(OpenGarage);
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }

        // Ensure timescale is 1
        Time.timeScale = 1f;
    }

    void Update()
    {
        // Keyboard fallback: press Space or Enter anywhere on Main Menu to start race immediately
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[NIGHTSHIFT] Key pressed on Main Menu -> Starting Race!");
            StartRace();
        }

        // Slowly rotate the showcase car
        if (carShowcase != null)
            carShowcase.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    public void StartRace()
    {
        Debug.Log("[NIGHTSHIFT] Loading RaceScene...");
        SceneManager.LoadScene("RaceScene");
    }

    public void OpenGarage()
    {
        Debug.Log("[NIGHTSHIFT] Loading GarageScene...");
        SceneManager.LoadScene("GarageScene");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
