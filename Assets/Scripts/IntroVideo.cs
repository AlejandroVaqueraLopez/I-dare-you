using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public class IntroVideo : MonoBehaviour
{
    
    public static bool IsPlaying { get; private set; }

    [Header("References")]
    public GameSequence level1;
    public VideoPlayer videoPlayer;
    public CanvasGroup introRoot;      
    public GameObject videoScreen;      

    [Header("Settings")]
    public bool playOnStart = true;         
    public bool waitForMainMenu = true;     
    public bool allowSkip = true;           
    public float startDelay = 0.5f;        
    public float fadeOutTime = 1f;          

    public UnityEvent onFinished;

    bool videoDone;
    bool running;

    bool CanPlayVideo()
    {
        return playOnStart && videoPlayer != null && videoPlayer.clip != null;
    }

    void ShowBlack()
    {
        IsPlaying = true;
        introRoot.gameObject.SetActive(true);
        introRoot.alpha = 1f;
        videoScreen.SetActive(false);
    }

    void Awake()
    {
        IsPlaying = false;

        if (waitForMainMenu)
        {
            if (introRoot != null) introRoot.gameObject.SetActive(false);
            return;
        }

        if (CanPlayVideo()) ShowBlack();
        else if (introRoot != null) introRoot.gameObject.SetActive(false);
    }

    IEnumerator Start()
    {
        if (waitForMainMenu || !IsPlaying) yield break;
        yield return StartCoroutine(Run());
    }

   
    public void Play()
    {
        if (running || IsPlaying) return;

        if (!CanPlayVideo())
        {
            if (level1 != null) level1.StartAmbience();
            onFinished.Invoke();
            return;
        }

        ShowBlack();
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        running = true;
        videoPlayer.Prepare();

       
        yield return null;
        PauseMenu.CanPause = false;
        Cursor.visible = false;

        
        bool skipped = false;
        float wait = startDelay;
        while (wait > 0f || !videoPlayer.isPrepared)
        {
            wait -= Time.unscaledDeltaTime;
            if (SkipPressed()) { skipped = true; break; }
            yield return null;
        }

        if (!skipped)
        {
            if (videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
                videoPlayer.SetDirectAudioVolume(0, AudioListener.volume);

            videoDone = false;
            videoPlayer.loopPointReached += OnVideoEnd;
            videoScreen.SetActive(true);
            videoPlayer.Play();

            while (!videoDone)
            {
                if (SkipPressed())
                {
                    videoPlayer.Stop();
                    break;
                }
                yield return null;
            }
            videoPlayer.loopPointReached -= OnVideoEnd;
        }

        videoScreen.SetActive(false);

        
        yield return null;
        IsPlaying = false;
        PauseMenu.CanPause = true;
        Cursor.visible = true;
        if (level1 != null) level1.StartAmbience();

        
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeOutTime;
            introRoot.alpha = 1f - Mathf.Clamp01(t);
            yield return null;
        }
        introRoot.gameObject.SetActive(false);

        running = false;
        onFinished.Invoke();
    }

    bool SkipPressed()
    {
        if (!allowSkip) return false;
        var kb = Keyboard.current;
        return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        videoDone = true;
    }
}