using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.SceneManagement;


public class GameOverScreen : MonoBehaviour
{
    [Header("References")]
    public GameSequence level1;         
    public HangmanGame hangman;         

    [Header("Screen")]
    public GameObject panel;            
    public CanvasGroup panelGroup;      
    public TMP_Text titleText;          
    public string title = "Ritual fallido";
    public float fadeTime = 1.5f;

    [Header("Video")]
    public VideoPlayer videoPlayer;     
    public GameObject videoScreen;     

    [Header("Buttons")]
    public GameObject buttonsRoot;      
    public Button retryButton;          
    public Button menuButton;           
    public float buttonsDelay = 1.5f;   
    public float retryFadeTime = 0.5f;

    bool shown;
    bool retrying;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (buttonsRoot != null) buttonsRoot.SetActive(false);

        if (retryButton != null) retryButton.onClick.AddListener(OnRetryPressed);
        if (menuButton != null) menuButton.onClick.AddListener(OnMenuPressed);
    }

    
    System.Action retryAction;

    
    public void Show()
    {
        ShowWithRetry(() => { if (hangman != null) hangman.StartGame(); });
    }

   
    public void ShowWithRetry(System.Action retry)
    {
        if (shown) return;
        shown = true;
        retryAction = retry;
        StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        PauseMenu.CanPause = false;
        if (level1 != null && level1.lookController != null)
            level1.lookController.enabled = false;

       
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (titleText != null) titleText.text = title;
        if (buttonsRoot != null) buttonsRoot.SetActive(false);

        panel.SetActive(true);
        panelGroup.alpha = 0f;

        
        bool hasVideo = videoPlayer != null && videoPlayer.clip != null;
        if (hasVideo)
        {
            videoPlayer.isLooping = true;
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared) yield return null;

            if (videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
            {
                float scale = level1 != null ? level1.level2Volume : 1f;
                videoPlayer.SetDirectAudioVolume(0, AudioListener.volume * scale);
            }

            if (videoScreen != null) videoScreen.SetActive(true);
            videoPlayer.Play();
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, fadeTime);
            panelGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }

        yield return new WaitForSeconds(buttonsDelay);
        if (buttonsRoot != null) buttonsRoot.SetActive(true);
    }

    // ---------- Buttons ----------

    void OnRetryPressed()
    {
        if (retrying) return;
        retrying = true;
        StartCoroutine(RetryRoutine());
    }

    IEnumerator RetryRoutine()
    {
        if (buttonsRoot != null) buttonsRoot.SetActive(false);

        
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, retryFadeTime);
            panelGroup.alpha = 1f - Mathf.Clamp01(t);
            yield return null;
        }

        if (videoPlayer != null) videoPlayer.Stop();
        if (videoScreen != null) videoScreen.SetActive(false);
        panel.SetActive(false);

        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (level1 != null && level1.lookController != null)
            level1.lookController.enabled = true;
        PauseMenu.CanPause = true;

        shown = false;
        retrying = false;

        
        if (retryAction != null) retryAction();
    }

    void OnMenuPressed()
    {
        if (retrying) return;
        retrying = true;                   

       
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}