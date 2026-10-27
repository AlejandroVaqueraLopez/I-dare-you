using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Level3Transition : MonoBehaviour
{
    [Header("References")]
    public GameSequence level1;
    public FlashbackScene flashback;
    public CardDuel duel;

    [Header("Level 2 cleanup")]
    public GameObject level2Enemy;       
    public CanvasGroup hangmanSheet;    
    public float startDelay = 1f;
    public float fadeTime = 2f;
    public float delayBeforeFlashback = 1.5f;

    [Header("Flashback")]
    public VideoClip video3;

    [Header("Table")]
    public GameObject[] candles;         
    public GameObject candleToKeep;      
    public GameObject darkness;          
    public AudioClip level3Music;        
    public float musicFadeTime = 2f;

    bool started;
    Light[] keepLights;
    float[] keepIntensity;

    void Awake()
    {
        if (darkness != null) darkness.SetActive(false);
        if (duel != null && duel.deck != null) duel.deck.gameObject.SetActive(false);

        
        if (candleToKeep != null)
        {
            keepLights = candleToKeep.GetComponentsInChildren<Light>(true);
            keepIntensity = new float[keepLights.Length];
            for (int i = 0; i < keepLights.Length; i++)
                keepIntensity[i] = keepLights[i].intensity;
        }
    }

    
    public void Begin()
    {
        if (started) return;
        started = true;
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        yield return new WaitForSeconds(startDelay);

        
        StartCoroutine(FadeSheet());
        yield return StartCoroutine(FadeOutEnemy());
        yield return new WaitForSeconds(delayBeforeFlashback);

        
        Coroutine fb = flashback.PlayCustom(video3);
        while (!flashback.cutsceneRoot.gameObject.activeInHierarchy || flashback.cutsceneRoot.alpha < 0.99f)
            yield return null;
        SetupTable();
        yield return fb;

        
        if (level3Music != null) level1.CrossfadeAmbience(level3Music, musicFadeTime);
        duel.StartDuel();
    }

    void SetupTable()
    {
        foreach (var c in candles)
            if (c != null && c != candleToKeep) c.SetActive(false);

        if (candleToKeep != null) RelightCandle();
        if (darkness != null) darkness.SetActive(true);
        if (duel != null && duel.deck != null) duel.deck.gameObject.SetActive(true);
    }

    void RelightCandle()
    {
        candleToKeep.SetActive(true);

        for (int i = 0; i < keepLights.Length; i++)
        {
            keepLights[i].enabled = true;
            keepLights[i].intensity = keepIntensity[i] > 0.01f ? keepIntensity[i] : 1f;
        }
        foreach (var ps in candleToKeep.GetComponentsInChildren<ParticleSystem>(true))
            ps.Play(true);
        foreach (var mb in candleToKeep.GetComponentsInChildren<MonoBehaviour>(true))
            mb.enabled = true;
    }

    // ---------- Fades ----------

    IEnumerator FadeSheet()
    {
        if (hangmanSheet == null) yield break;

        float start = hangmanSheet.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.05f, fadeTime);
            hangmanSheet.alpha = Mathf.Lerp(start, 0f, Mathf.Clamp01(t));
            yield return null;
        }
        hangmanSheet.gameObject.SetActive(false);
    }

    IEnumerator FadeOutEnemy()
    {
        if (level2Enemy == null) yield break;

       
        var mats = new List<Material>();
        foreach (var r in level2Enemy.GetComponentsInChildren<Renderer>())
        {
            foreach (var m in r.materials)
            {
                MakeTransparent(m);
                mats.Add(m);
            }
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.05f, fadeTime);
            float a = 1f - Mathf.Clamp01(t);

            foreach (var m in mats)
            {
                if (!m.HasProperty("_BaseColor")) continue;
                Color c = m.GetColor("_BaseColor");
                c.a = a;
                m.SetColor("_BaseColor", c);
            }
            yield return null;
        }

        level2Enemy.SetActive(false);
    }

    
    static void MakeTransparent(Material m)
    {
        if (!m.HasProperty("_Surface")) return;

        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
