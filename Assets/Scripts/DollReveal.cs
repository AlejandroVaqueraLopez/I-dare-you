using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

public class DollReveal : MonoBehaviour
{
    [Header("Doll")]
    public GameObject doll;              
    public Material fadeMaterial;        
    public float dollFadeTime = 2f;

    [Header("Blood")]
    public CanvasGroup bloodGroup;       
    public float bloodFadeTime = 3f;

    [Header("Examine prompt")]
    public GameObject examinePrompt;    
    public Camera playerCamera;
    public float maxDistance = 6f;
    public UnityEvent onExamine;

    bool ready;
    bool examined;

    Renderer[] renderers;
    Material[][] originalMaterials;
    MaterialPropertyBlock block;

    void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        block = new MaterialPropertyBlock();

        if (bloodGroup != null) bloodGroup.alpha = 0f;
        if (examinePrompt != null) examinePrompt.SetActive(false);
        if (doll != null) doll.SetActive(false);
    }

    public void Reveal()
    {
        StartCoroutine(RevealRoutine());
    }

    IEnumerator RevealRoutine()
    {
        StartCoroutine(FadeBlood());
        yield return StartCoroutine(FadeDoll(0f, 1f, true));
        ready = true;
    }

    public IEnumerator HideDoll()
    {
        yield return StartCoroutine(FadeDoll(1f, 0f, false));
        doll.SetActive(false);
    }

    IEnumerator FadeBlood()
    {
        if (bloodGroup == null) yield break;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / bloodFadeTime;
            bloodGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }
    }

    IEnumerator FadeDoll(float from, float to, bool activateFirst)
    {
        if (doll == null) yield break;

        SwapToFadeMaterial();
        SetDollAlpha(from);
        if (activateFirst) doll.SetActive(true);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dollFadeTime;
            SetDollAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t)));
            yield return null;
        }
        SetDollAlpha(to);

        RestoreMaterials();
    }

    void SwapToFadeMaterial()
    {
        
        var list = new List<Renderer>();
        foreach (var r in doll.GetComponentsInChildren<Renderer>(true))
            if (r.GetComponent<TMP_Text>() == null) list.Add(r);
        renderers = list.ToArray();

        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
            var fade = new Material[originalMaterials[i].Length];
            for (int m = 0; m < fade.Length; m++) fade[m] = fadeMaterial;
            renderers[i].sharedMaterials = fade;
        }
    }

    void RestoreMaterials()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].SetPropertyBlock(null);
            renderers[i].sharedMaterials = originalMaterials[i];
        }
    }

    void SetDollAlpha(float a)
    {
        block.SetColor("_BaseColor", new Color(1f, 1f, 1f, a));
        foreach (var r in renderers)
            r.SetPropertyBlock(block);
    }

    void Update()
    {
        if (PauseMenu.IsPaused) return; 

        if (!ready || examined || doll == null) return;

        bool hovering = IsLookingAtDoll();

        if (examinePrompt != null)
            examinePrompt.SetActive(hovering);

       
        if (hovering && Mouse.current.leftButton.wasPressedThisFrame)
        {
            examined = true;
            if (examinePrompt != null) examinePrompt.SetActive(false);
            onExamine.Invoke();
        }
    }

    bool IsLookingAtDoll()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance)) return false;
        return hit.collider.transform.IsChildOf(doll.transform);
    }
}