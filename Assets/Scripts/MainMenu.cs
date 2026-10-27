using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenu : MonoBehaviour
{
    // GameSequence checks this so the game doesn't start behind the menu
    public static bool IsOpen { get; private set; }

    [Header("References")]
    public IntroVideo intro;            // the level 1 video
    public GameObject panel;            // the full-screen menu panel
    public CanvasGroup panelGroup;      // CanvasGroup on that panel

    [Header("Background video")]
    public VideoPlayer menuVideo;       // loops behind the buttons
    public GameObject videoScreen;      // the Raw Image showing the Render Texture

    [Header("Buttons")]
    public Button startButton;          // "Iniciar juego"
    public Button quitButton;           // "Salir del juego"
    public float fadeTime = 0.6f;

    bool starting;
    float audioScale = 1f;

    void Awake()
    {
        IsOpen = true;

        panel.SetActive(true);
        panelGroup.alpha = 1f;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;
        if (videoScreen != null) videoScreen.SetActive(false);

        startButton.onClick.AddListener(OnStartPressed);
        quitButton.onClick.AddListener(OnQuitPressed);
    }

    IEnumerator Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (menuVideo == null || menuVideo.clip == null) yield break;

        menuVideo.isLooping = true;
        menuVideo.Prepare();
        while (!menuVideo.isPrepared) yield return null;

        if (videoScreen != null) videoScreen.SetActive(true);
        menuVideo.Play();
    }

    void Update()
    {
        // Direct audio ignores the master volume, so it follows the config slider here
        if (menuVideo != null && menuVideo.isPlaying
            && menuVideo.audioOutputMode == VideoAudioOutputMode.Direct)
            menuVideo.SetDirectAudioVolume(0, AudioListener.volume * audioScale);
    }

    void OnStartPressed()
    {
        if (starting) return;
        starting = true;
        StartCoroutine(StartRoutine());
    }

    IEnumerator StartRoutine()
    {
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        // The intro covers the screen with black first, so the room never flashes
        if (intro != null) intro.Play();
        IsOpen = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.05f, fadeTime);
            float k = Mathf.Clamp01(t);
            panelGroup.alpha = 1f - k;
            audioScale = 1f - k;
            yield return null;
        }

        if (menuVideo != null) menuVideo.Stop();
        if (videoScreen != null) videoScreen.SetActive(false);
        panel.SetActive(false);
    }

    void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}