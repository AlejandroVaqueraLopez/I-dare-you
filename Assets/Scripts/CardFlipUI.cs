using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CardFlipUI : MonoBehaviour
{
    public Image cardImage;
    public Sprite backSprite;
    public Sprite frontSprite;
    public bool startFaceUp = false;

    public float duration = 0.5f;
    public float popScale = 1.15f;

    bool flipping;
    bool faceUp;
    RectTransform rt;

    Vector3 originalScale;

    public bool IsFaceUp => faceUp;
    public bool IsFlipping => flipping;
    public bool Matched { get; private set; }

    public IEnumerator Disappear(float fadeTime = 0.4f)
    {
        Matched = true;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Color c = cardImage.color;
        float startAlpha = c.a;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeTime;
            c.a = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(t));
            cardImage.color = c;
            yield return null;
        }

        cardImage.enabled = false;
    }

    void Awake()
    {
        rt = (RectTransform)transform;
        originalScale = rt.localScale;
        faceUp = startFaceUp;
        cardImage.sprite = faceUp ? frontSprite : backSprite;
    }

    public void ResetCard(Sprite newFront)
    {
        StopAllCoroutines();
        flipping = false;
        faceUp = false;
        Matched = false;
        frontSprite = newFront;

        rt.localRotation = Quaternion.identity;
        rt.localScale = originalScale;

        cardImage.sprite = backSprite;
        cardImage.enabled = true;
        Color c = cardImage.color;
        c.a = 0f;                     
        cardImage.color = c;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    public IEnumerator FadeIn(float fadeTime = 0.5f)
    {
        Color c = cardImage.color;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeTime;
            c.a = Mathf.Clamp01(t);
            cardImage.color = c;
            yield return null;
        }
    }

    public void Flip()
    {
        if (!flipping) StartCoroutine(FlipRoutine());
    }

    public IEnumerator FlipRoutine()
    {
        flipping = true;
        float half = duration * 0.5f;
        Vector3 baseScale = rt.localScale;

       
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            float k = Mathf.Clamp01(t);
            rt.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, 90f, k), 0f);
            rt.localScale = baseScale * Mathf.Lerp(1f, popScale, k);
            yield return null;
        }

       
        faceUp = !faceUp;
        cardImage.sprite = faceUp ? frontSprite : backSprite;

      
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            float k = Mathf.Clamp01(t);
            rt.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-90f, 0f, k), 0f);
            rt.localScale = baseScale * Mathf.Lerp(popScale, 1f, k);
            yield return null;
        }

        rt.localRotation = Quaternion.identity;
        rt.localScale = baseScale;
        flipping = false;
    }
}