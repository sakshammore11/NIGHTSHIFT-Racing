using UnityEngine;

/// <summary>
/// NIGHTSHIFT — Audio Manager
/// Handles engine sound (pitch-mapped to speed), SFX (checkpoint, finish, countdown).
/// All sounds are procedurally generated or played from assigned clips.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Engine")]
    public AudioSource engineSource;
    [Tooltip("Idle pitch multiplier")]
    public float engineIdlePitch = 0.4f;
    [Tooltip("Max pitch at top speed")]
    public float engineMaxPitch = 2.2f;
    public float enginePitchSmoothing = 3f;
    [Tooltip("Idle volume")]
    public float engineIdleVolume = 0.3f;
    [Tooltip("Volume at max throttle")]
    public float engineMaxVolume = 0.85f;

    [Header("Skid")]
    public AudioSource skidSource;
    public float skidThreshold = 30f; // lateral velocity to start skid

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip checkpointClip;
    public AudioClip finishClip;
    public AudioClip countdownBeepClip;
    public AudioClip goClip;

    [Header("Music")]
    public AudioSource musicSource;

    [Header("References")]
    public CarController carController;

    private float currentEnginePitch;
    private bool skidding = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (engineSource != null)
        {
            engineSource.loop = true;
            engineSource.pitch = engineIdlePitch;
            engineSource.volume = engineIdleVolume;
            engineSource.Play();
        }

        if (skidSource != null)
        {
            skidSource.loop = true;
            skidSource.volume = 0f;
            skidSource.Play();
        }

        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.loop = true;
            musicSource.Play();
        }

        currentEnginePitch = engineIdlePitch;
    }

    void Update()
    {
        UpdateEngineSound();
        UpdateSkidSound();
    }

    void UpdateEngineSound()
    {
        if (engineSource == null || carController == null) return;

        float speedNorm = Mathf.Clamp01(carController.currentSpeedKMH / 200f);
        float targetPitch = Mathf.Lerp(engineIdlePitch, engineMaxPitch, speedNorm);
        float targetVolume = Mathf.Lerp(engineIdleVolume, engineMaxVolume, speedNorm);

        // Smooth pitch transition
        currentEnginePitch = Mathf.Lerp(currentEnginePitch, targetPitch,
            Time.deltaTime * enginePitchSmoothing);

        engineSource.pitch = currentEnginePitch;
        engineSource.volume = targetVolume;
    }

    void UpdateSkidSound()
    {
        if (skidSource == null || carController == null) return;

        // Detect skid from handbrake or high lateral slip
        bool shouldSkid = carController.isHandbraking ||
            (carController.currentSpeedKMH > skidThreshold && carController.isBraking);
        skidding = shouldSkid;

        float targetVol = shouldSkid ? 0.6f : 0f;
        skidSource.volume = Mathf.Lerp(skidSource.volume, targetVol, Time.deltaTime * 8f);
    }

    public void PlayCheckpointSound()
    {
        if (checkpointClip != null)
            sfxSource?.PlayOneShot(checkpointClip, 0.8f);
    }

    public void PlayFinishSound()
    {
        if (finishClip != null)
            sfxSource?.PlayOneShot(finishClip, 1f);
    }

    public void PlayCountdownBeep()
    {
        if (countdownBeepClip != null)
            sfxSource?.PlayOneShot(countdownBeepClip, 0.9f);
        else
            GenerateBeep(880f, 0.15f);
    }

    public void PlayGoSound()
    {
        if (goClip != null)
            sfxSource?.PlayOneShot(goClip, 1f);
        else
            GenerateBeep(1320f, 0.3f);
    }

    /// <summary>
    /// Generates a sine-wave beep programmatically when no clip is assigned.
    /// </summary>
    void GenerateBeep(float frequency, float duration)
    {
        int sampleRate = AudioSettings.outputSampleRate;
        int samples = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Clamp01(1f - (t / duration)); // fade out
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.5f;
        }

        AudioClip beep = AudioClip.Create("Beep", samples, 1, sampleRate, false);
        beep.SetData(data, 0);

        if (sfxSource != null)
            sfxSource.PlayOneShot(beep);
    }
}
