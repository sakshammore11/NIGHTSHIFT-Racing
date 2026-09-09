using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// NIGHTSHIFT — Garage Controller
/// Allows inspecting the car, changing color, viewing stats, and launching race.
/// </summary>
public class GarageController : MonoBehaviour
{
    [Header("Car Representation")]
    public Transform carTransform;
    public MeshRenderer carBodyRenderer;
    public float rotationSpeed = 25f;

    [Header("UI Buttons")]
    public Button backButton;
    public Button startRaceButton;

    [Header("Color Buttons")]
    public Button colorBlueBtn;
    public Button colorRedBtn;
    public Button colorYellowBtn;
    public Button colorBlackBtn;

    [Header("Colors")]
    public Color blueColor = new Color(0.05f, 0.15f, 0.45f, 1f);
    public Color redColor = new Color(0.65f, 0.05f, 0.08f, 1f);
    public Color yellowColor = new Color(0.9f, 0.7f, 0.05f, 1f);
    public Color blackColor = new Color(0.05f, 0.05f, 0.06f, 1f);

    void Start()
    {
        backButton?.onClick.AddListener(BackToMenu);
        startRaceButton?.onClick.AddListener(StartRace);

        colorBlueBtn?.onClick.AddListener(() => SetCarColor(blueColor));
        colorRedBtn?.onClick.AddListener(() => SetCarColor(redColor));
        colorYellowBtn?.onClick.AddListener(() => SetCarColor(yellowColor));
        colorBlackBtn?.onClick.AddListener(() => SetCarColor(blackColor));

        Time.timeScale = 1f;
    }

    void Update()
    {
        if (carTransform != null)
        {
            carTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    void SetCarColor(Color color)
    {
        if (carBodyRenderer != null)
        {
            carBodyRenderer.material.color = color;
        }
    }

    void BackToMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    void StartRace()
    {
        SceneManager.LoadScene("RaceScene");
    }
}
