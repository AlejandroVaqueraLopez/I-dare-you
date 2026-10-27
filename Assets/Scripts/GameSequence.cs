using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class GameSequence : MonoBehaviour
{
    [Header("UI")]
    public GameObject instructionPanel;
    public CanvasGroup panelGroup;
    public Button startButton;        
    public float fadeDuration = 0.3f;

    [Header("Progress")]
    public TMP_Text pairsText;

    [Header("Audio - Sources (optional, created automatically if empty)")]
    public AudioSource ambienceSource; 
    public AudioSource voiceSource;   
    public AudioSource fxSource;      

    [Header("Audio - Ambience")]
    public AudioClip ambienceClip;
    [Range(0f, 1f)] public float ambienceVolume = 0.5f;
    public float ambienceFadeOut = 2f;

    [Header("Audio - Voices")]
    public AudioClip introVoice;       
    public AudioClip winVoice;       
    [Range(0f, 1f)] public float voiceVolume = 1f;

    [Header("Audio - FX")]
    public AudioClip startSound;     
    public AudioClip flipSound;        
    public AudioClip matchSound;      
    public AudioClip mismatchSound;   
    public AudioClip winSound;        
    [Range(0f, 1f)] public float fxVolume = 1f;

    [Header("Level 2 Volume")]
    [Range(0f, 1f)] public float level2Volume = 1f;  

    public void RefreshVolumes()
    {
        if (voiceSource != null) voiceSource.volume = voiceVolume * level2Volume;
        if (ambienceSource != null && ambienceSource.isPlaying)
            ambienceSource.volume = ambienceVolume * level2Volume;
    }

    void OnValidate()
    {
        if (Application.isPlaying) RefreshVolumes();
    }

    [Header("Cards")]
    public CardFlipUI[] cards;         
    public Transform cardsContainer;   
    public bool shuffle = true;
    public float delayBeforeFlip = 0.5f;
    public float delayBetweenCards = 0.3f;
    public float previewTime = 2f;   
    public float mismatchDelay = 0.8f;

    [Header("Interaction")]
    public Camera playerCamera;
    public float maxClickDistance = 6f;

    [Header("Player look")]

    public CinemachineInputAxisController lookController; 

    [Header("Level end")]
    public DollReveal dollReveal;

    bool started;
    bool canPlay;
    bool busy;
    CardFlipUI first;
    int pairsFound;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        SetupAudio();

        foreach (var card in cards)
            card.gameObject.SetActive(false);

        if (shuffle) ShuffleSlots();

        instructionPanel.SetActive(true);
        if (panelGroup != null) panelGroup.alpha = 1f;
        SetGameplayInput(false);

        UpdatePairsText();
        if (pairsText != null) pairsText.gameObject.SetActive(false);

        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (!IntroVideo.IsPlaying && !MainMenu.IsOpen) StartAmbience();
    }

    void Update()
    {


      
        if (PauseMenu.IsPaused || IntroVideo.IsPlaying || MainMenu.IsOpen) return;



        var kb = Keyboard.current;

        if (!started)
        {
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                StartGame();
            return;
        }

        if (canPlay && !busy && Mouse.current.leftButton.wasPressedThisFrame)
            TryPickCard();
    }

    void StartGame()
    {
        if (started) return;
        started = true;

        PlayFx(startSound);
        PlayVoice(introVoice);

        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        // 1. Fade the instruction panel
        if (panelGroup != null)
        {
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / fadeDuration;
                panelGroup.alpha = 1f - Mathf.Clamp01(t);
                yield return null;
            }
        }
        instructionPanel.SetActive(false);
        SetGameplayInput(true);
        if (pairsText != null) pairsText.gameObject.SetActive(true);

        // 2. Show the cards and give them click colliders
        foreach (var card in cards)
            card.gameObject.SetActive(true);

        yield return null; // let the layout group position them
        foreach (var card in cards)
            AddCollider(card);

        yield return new WaitForSeconds(delayBeforeFlip);

        // 3. Reveal all faces
        foreach (var card in cards)
        {
            card.Flip();
            if (delayBetweenCards > 0f)
                yield return new WaitForSeconds(delayBetweenCards);
        }

        // 4. Let the player memorize, then hide them again
        yield return new WaitForSeconds(previewTime);
        foreach (var card in cards)
        {
            card.Flip();
            if (delayBetweenCards > 0f)
                yield return new WaitForSeconds(delayBetweenCards);
        }
        yield return new WaitForSeconds(0.6f);

        canPlay = true;
    }

    void TryPickCard()
    {
       
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, maxClickDistance)) return;

        var card = hit.collider.GetComponent<CardFlipUI>();
        if (card == null || card.Matched || card.IsFaceUp || card.IsFlipping) return;

        PlayFx(flipSound);
        card.Flip();

        if (first == null)
        {
            first = card;
        }
        else
        {
            StartCoroutine(CheckPair(first, card));
            first = null;
        }
    }

    IEnumerator CheckPair(CardFlipUI a, CardFlipUI b)
    {
        busy = true;
        yield return new WaitForSeconds(a.duration + 0.2f); 

      
        bool match = System.Array.IndexOf(cards, a) / 2 == System.Array.IndexOf(cards, b) / 2;

        if (match)
        {
            PlayFx(matchSound);
            yield return new WaitForSeconds(0.3f);
            StartCoroutine(a.Disappear());
            yield return StartCoroutine(b.Disappear());

            pairsFound++;
            UpdatePairsText();

            if (pairsFound >= cards.Length / 2)
                OnGameWon();
        }
        else
        {
            PlayFx(mismatchSound);
            yield return new WaitForSeconds(mismatchDelay);
            a.Flip();
            b.Flip();
            yield return new WaitForSeconds(a.duration + 0.1f);
        }

        busy = false;
    }

    void UpdatePairsText()
    {
        if (pairsText != null)
            pairsText.text = $"Pairs: {pairsFound}/{cards.Length / 2}";
    }

    void OnGameWon()
    {
        if (dollReveal != null) dollReveal.Reveal();

        canPlay = false;

        PlayFx(winSound);
        PlayVoice(winVoice);
        StartCoroutine(FadeOutAmbience());

        Debug.Log("Level 1 completed!");
      
    }

    // ---------- Audio ----------

    void SetupAudio()
    {
        if (ambienceSource == null) ambienceSource = CreateSource("Ambience");
        if (voiceSource == null) voiceSource = CreateSource("Voices");
        if (fxSource == null) fxSource = CreateSource("FX");

        ambienceSource.clip = ambienceClip;
        ambienceSource.loop = true;
        ambienceSource.volume = ambienceVolume * level2Volume;

        voiceSource.loop = false;
        voiceSource.volume = voiceVolume * level2Volume;
        fxSource.loop = false;
    }

    public void StartAmbience()
    {
        if (ambienceClip != null && !ambienceSource.isPlaying)
            ambienceSource.Play();

    }

   
    public void PlayAmbience(AudioClip clip = null, float fadeInTime = 2f)
    {
        ambienceSource.clip = clip != null ? clip : ambienceClip;
        if (ambienceSource.clip == null) return;

        ambienceSource.loop = true;
        ambienceSource.volume = 0f;
        ambienceSource.Play();
        StartCoroutine(FadeInAmbience(fadeInTime));
    }

    IEnumerator FadeInAmbience(float time)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, time);
            ambienceSource.volume = Mathf.Lerp(0f, ambienceVolume * level2Volume, Mathf.Clamp01(t));
            yield return null;
        }
    }

    
    public void CrossfadeAmbience(AudioClip clip, float time = 2f)
{
    StartCoroutine(CrossfadeRoutine(clip, time));
}

IEnumerator CrossfadeRoutine(AudioClip clip, float time)
{
    float half = Mathf.Max(0.01f, time) * 0.5f;

    float startVolume = ambienceSource.volume;
    float t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime / half;
        ambienceSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(t));
        yield return null;
    }

    ambienceSource.clip = clip;
    ambienceSource.loop = true;
    ambienceSource.Play();

    t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime / half;
        ambienceSource.volume = Mathf.Lerp(0f, ambienceVolume, Mathf.Clamp01(t));
        yield return null;
    }
}

public void StopAmbience(bool immediately = false)
{
    if (immediately) ambienceSource.Stop();
    else StartCoroutine(FadeOutAmbience());
}

    AudioSource CreateSource(string sourceName)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D sound
        return src;
    }

   
    public void PlayFx(AudioClip clip)
    {
        if (clip != null) fxSource.PlayOneShot(clip, fxVolume * level2Volume);
    }

    void PlayVoice(AudioClip clip)
    {
        if (clip == null) return;
        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.volume = voiceVolume * level2Volume;
        voiceSource.Play();
    }

    IEnumerator FadeOutAmbience()
    {
        float startVolume = ambienceSource.volume;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, ambienceFadeOut);
            ambienceSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(t));
            yield return null;
        }
        ambienceSource.Stop();
    }

    // ---------- Helpers ----------

    void AddCollider(CardFlipUI card)
    {
        var rt = (RectTransform)card.transform;
        var col = card.GetComponent<BoxCollider>();
        if (col == null) col = card.gameObject.AddComponent<BoxCollider>();
        col.center = rt.rect.center;
        col.size = new Vector3(rt.rect.width, rt.rect.height, 1f);
    }

    void ShuffleSlots()
    {
      
        var slots = new List<int>();
        for (int i = 0; i < cards.Length; i++) slots.Add(i);

        for (int i = slots.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }

        for (int i = 0; i < cards.Length; i++)
            cards[i].transform.SetSiblingIndex(slots[i]);
    }

    void SetGameplayInput(bool enabled)
    {
        if (lookController != null)
            lookController.enabled = enabled;

        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enabled;
    }
}