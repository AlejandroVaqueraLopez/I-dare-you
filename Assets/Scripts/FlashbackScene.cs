using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Video;
using static UnityEngine.Rendering.DebugUI.Table;

public class FlashbackScene : MonoBehaviour
{
    [Header("References")]
    public GameSequence level1;                
    public CinemachineCamera playerCamera;     
    public CinemachineCameraOffset cameraOffset;
    public GameObject[] hideDuringFlashback;   

    [Header("Headache shake")]
    public float shakeDuration = 6f;
    public float shakeAmount = 0.08f;          
    public float maxRoll = 6f;              
    public float fovPulse = 6f;               
    public float shakeSpeed = 6f;             
    public float pulseRate = 1.6f;            

    [Header("Red flashback")]
    public CanvasGroup redOverlay;
    [Range(0f, 1f)] public float maxRedAlpha = 0.6f;
    public Volume flashbackVolume;            

    [Header("Sounds (all optional)")]
    public AudioClip flashbackSound;          
    public AudioClip heartbeatClip;           
    public AudioClip ringingClip;            
    [Range(0f, 1f)] public float heartbeatVolume = 0.9f;
    [Range(0f, 1f)] public float ringingVolume = 0.4f;

    [Header("Cutscene")]
    public CanvasGroup cutsceneRoot;          
    public GameObject videoScreen;             
    public VideoPlayer videoPlayer;
    public float fadeToBlackTime = 1f;
    public bool allowSkip = true;             

    [Header("After the video")]
    public bool returnToGame = true;         
    public float fadeBackTime = 1.5f;

    public UnityEvent onFinished;

    float baseFov;
    bool videoDone;
    bool suppressFinished;
    AudioSource heartbeatSource;
    AudioSource ringingSource;

    float Scale => level1 != null ? level1.level2Volume : 1f;

    void Start()
    {
        if (redOverlay != null) redOverlay.alpha = 0f;
        if (flashbackVolume != null) flashbackVolume.weight = 0f;
        if (videoScreen != null) videoScreen.SetActive(false);
        baseFov = playerCamera.Lens.FieldOfView;

        heartbeatSource = CreateLoop("Heartbeat", heartbeatClip);
        ringingSource = CreateLoop("Ringing", ringingClip);
    }

   
    public void Play()
    {
        StartCoroutine(FlashbackRoutine());
    }

   
    public Coroutine PlayCustom(VideoClip clip)
    {
        return StartCoroutine(CustomRoutine(clip, allowSkip, returnToGame));
    }

    // Ending: the video can't be skipped and the screen stays black afterwards
    public Coroutine PlayEnding(VideoClip clip)
    {
        return StartCoroutine(CustomRoutine(clip, false, false));
    }

    IEnumerator CustomRoutine(VideoClip clip, bool skip, bool back)
    {
        bool oldSkip = allowSkip;
        bool oldBack = returnToGame;

        if (clip != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = clip;
        }

        allowSkip = skip;
        returnToGame = back;
        suppressFinished = true;

        yield return StartCoroutine(FlashbackRoutine());

        suppressFinished = false;
        allowSkip = oldSkip;
        returnToGame = oldBack;
    }

    IEnumerator FlashbackRoutine()
    {
        PauseMenu.CanPause = false;

     
        if (level1 != null && level1.lookController != null)
            level1.lookController.enabled = false;
        SetHud(false);
        
        if (level1 != null) level1.PlayFx(flashbackSound);
        videoPlayer.Prepare(); 

       
       
        float elapsed = 0f;
        float seed = Random.value * 100f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float grow = Mathf.SmoothStep(0f, 1f, elapsed / shakeDuration);

         
            float pulse = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.time * pulseRate * Mathf.PI)), 4f);
            float strength = grow * (0.4f + 0.6f * pulse);

            ApplyShake(seed, strength, pulse * grow);

            if (redOverlay != null) redOverlay.alpha = maxRedAlpha * strength;
            if (flashbackVolume != null) flashbackVolume.weight = grow;
            if (heartbeatSource != null) heartbeatSource.volume = heartbeatVolume * grow;
            if (ringingSource != null) ringingSource.volume = ringingVolume * grow;

            yield return null;
        }

        
        cutsceneRoot.gameObject.SetActive(true);
        cutsceneRoot.alpha = 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeToBlackTime;
            float k = Mathf.Clamp01(t);
            cutsceneRoot.alpha = k;
            if (heartbeatSource != null) heartbeatSource.volume = heartbeatVolume * (1f - k) * Scale;
            if (ringingSource != null) ringingSource.volume = ringingVolume * (1f - k) * Scale;
            yield return null;
        }

        
        ApplyShake(seed, 0f, 0f);
        if (redOverlay != null) redOverlay.alpha = 0f;
        if (flashbackVolume != null) flashbackVolume.weight = 0f;
        if (heartbeatSource != null) heartbeatSource.Stop();
        if (ringingSource != null) ringingSource.Stop();

       
        while (!videoPlayer.isPrepared) yield return null;

        videoDone = false;
        videoPlayer.loopPointReached += OnVideoEnd;
        videoScreen.SetActive(true);
        videoPlayer.Play();

        while (!videoDone)
        {
            if (allowSkip && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                videoPlayer.Stop();
                break;
            }
            yield return null;
        }
        videoPlayer.loopPointReached -= OnVideoEnd;

       
        if (returnToGame)
        {
            videoScreen.SetActive(false);

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / fadeBackTime;
                cutsceneRoot.alpha = 1f - Mathf.Clamp01(t);
                yield return null;
            }
            cutsceneRoot.gameObject.SetActive(false);

            if (level1 != null && level1.lookController != null)
                level1.lookController.enabled = true;
            SetHud(true);
        }

        PauseMenu.CanPause = true; 
        if (!suppressFinished) onFinished.Invoke();
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        videoDone = true;
    }

    void ApplyShake(float seed, float strength, float pulse)
    {
        cameraOffset.Offset = new Vector3(
            Noise(seed, 1f),
            Noise(seed, 2f),
            Noise(seed, 3f) * 0.3f) * shakeAmount * strength;

        var lens = playerCamera.Lens;
        lens.Dutch = Noise(seed, 4f) * maxRoll * strength;
        lens.FieldOfView = baseFov + pulse * fovPulse;
        playerCamera.Lens = lens;
    }

   
    float Noise(float seed, float channel)
    {
        return (Mathf.PerlinNoise(Time.time * shakeSpeed + seed, channel * 10f) - 0.5f) * 2f;
    }

    void SetHud(bool visible)
    {
        foreach (var go in hideDuringFlashback)
            if (go != null) go.SetActive(visible);
    }

    AudioSource CreateLoop(string objectName, AudioClip clip)
    {
        if (clip == null) return null;

        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        return src;
    }
}