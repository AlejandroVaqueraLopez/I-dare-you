using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }
    static bool canPause = true;
    static PauseMenu instance;

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void RefreshHints()
    {
        
        if (hintsText != null && state == State.Playing)
            hintsText.SetActive(canPause);
    }

    
    public static bool CanPause
    {
        get { return canPause; }
        set
        {
            canPause = value;
            if (instance != null) instance.RefreshHints();
        }
    }

    enum State { Playing, Paused, Config }

    [Header("HUD")]
    public GameObject hintsText;     
    bool hintsWereActive;

    [Header("Screens")]
    public GameObject pausePanel;           
    public GameObject configPanel;          

    [Header("Blur")]
    public Volume blurVolume;               
    public float blurFadeTime = 0.2f;

    [Header("Configuration screen")]
    public Slider volumeSlider;
    public TMP_Text volumeValueText;        
    public Button resumeButton;             
    public Button quitButton;               

    [Header("Player look")]
    public CinemachineInputAxisController lookController;

    State state = State.Playing;
    float targetBlur;
    CursorLockMode savedLock;
    bool savedVisible;
    bool lookWasEnabled;

    const string VolumeKey = "MasterVolume";

    void Start()
    {
        IsPaused = false;
        CanPause = true;
        Time.timeScale = 1f;

        pausePanel.SetActive(false);
        configPanel.SetActive(false);
        if (blurVolume != null) blurVolume.weight = 0f;

        
        float saved = PlayerPrefs.GetFloat(VolumeKey, 1f);
        AudioListener.volume = saved;
        volumeSlider.SetValueWithoutNotify(saved);
        UpdateVolumeText(saved);

        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        resumeButton.onClick.AddListener(Resume);
        quitButton.onClick.AddListener(OnQuitPressed);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && (CanPause || IsPaused))
        {
            if (kb.pKey.wasPressedThisFrame) TogglePause();
            if (kb.cKey.wasPressedThisFrame) ToggleConfig();
        }

       
        if (blurVolume != null)
            blurVolume.weight = Mathf.MoveTowards(
                blurVolume.weight, targetBlur,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, blurFadeTime));
    }

    void TogglePause()
    {
        if (state == State.Playing) Enter(State.Paused);
        else Resume();                  
    }

    void ToggleConfig()
    {
        if (state == State.Config) Resume();
        else Enter(State.Config);       
    }

    void Enter(State newState)
    {
        if (state == State.Playing)
        {
            if (hintsText != null)
            {
                hintsWereActive = hintsText.activeSelf;
                hintsText.SetActive(false);
            }
            
            savedLock = Cursor.lockState;
            savedVisible = Cursor.visible;
            lookWasEnabled = lookController != null && lookController.enabled;
            if (lookController != null) lookController.enabled = false;

            Time.timeScale = 0f;
            IsPaused = true;
        }

        state = newState;
        pausePanel.SetActive(newState == State.Paused);
        configPanel.SetActive(newState == State.Config);
        targetBlur = 1f;

        if (newState == State.Config)
        {
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Resume()
    {
        if (state == State.Playing) return;

        state = State.Playing;
        pausePanel.SetActive(false);
        configPanel.SetActive(false);
        targetBlur = 0f;

        if (hintsText != null && hintsWereActive)
            hintsText.SetActive(true);

        Time.timeScale = 1f;
        Cursor.lockState = savedLock;
        Cursor.visible = savedVisible;
        if (lookController != null && lookWasEnabled) lookController.enabled = true;

        PlayerPrefs.Save();
        StartCoroutine(ClearPausedNextFrame());
    }

    
    IEnumerator ClearPausedNextFrame()
    {
        yield return null;
        IsPaused = false;
    }

    void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        UpdateVolumeText(value);
    }

    void UpdateVolumeText(float value)
    {
        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    void OnQuitPressed()
    {
        
        Time.timeScale = 1f;
        IsPaused = false;
        CanPause = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
