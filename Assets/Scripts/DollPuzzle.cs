using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Cinemachine;

public class DollPuzzle : MonoBehaviour
{
    [Header("References")]
    public GameSequence level1;         
    public DollReveal dollReveal;
    public Transform cardsContainer;    

    [Header("Close-up")]
    public GameObject closeUpCamera;    
    public CinemachineBrain brain;      
    public float fallbackBlendTime = 2f;
    public GameObject[] hideDuringCloseUp;

    [Header("Number on the doll's head")]
    public TMP_Text numberText;
    public string numberToShow = "28/--/1-";
    public float numberFadeTime = 1f;
    public float numberHoldTime = 5f;

    [Header("Cards")]
    [Tooltip("Same order the player must follow: 2 8 / 0 5 / 1 8")]
    public Sprite[] sequence;
    public float wrongDelay = 0.6f;

    [Header("Sounds (optional)")]
    public AudioClip flipSound;
    public AudioClip correctSound;
    public AudioClip wrongSound;

    public UnityEvent onPuzzleCompleted;

    bool canPlay;
    bool busy;
    int step;

    void Start()
    {
        if (numberText != null) numberText.alpha = 0f;
    }

   
    public void StartPuzzle()
    {
        StartCoroutine(PuzzleRoutine());
    }

    IEnumerator PuzzleRoutine()
    {
        if (brain == null) brain = FindFirstObjectByType<CinemachineBrain>();

        // 1. The camera moves close to the doll
        SetHud(false);
        if (level1.lookController != null) level1.lookController.enabled = false;
        closeUpCamera.SetActive(true);
        yield return WaitForBlend();

        // 2. The number appears on the head
        yield return ShowNumber();

        // 3. The camera returns to its original place
        closeUpCamera.SetActive(false);
        yield return WaitForBlend();
        if (level1.lookController != null) level1.lookController.enabled = true;
        SetHud(true);

        // 4. The cards change to the new symbols
        yield return PrepareCards();
        step = 0;
        canPlay = true;
    }

    IEnumerator WaitForBlend()
    {
        yield return null;
        yield return null; 
        if (brain == null)
        {
            yield return new WaitForSeconds(fallbackBlendTime);
            yield break;
        }
        while (brain.IsBlending) yield return null;
    }

    IEnumerator ShowNumber()
    {
        numberText.text = numberToShow;
        yield return FadeText(0f, 1f, numberFadeTime);
        yield return new WaitForSeconds(numberHoldTime);
        yield return FadeText(1f, 0f, numberFadeTime);
        numberText.text = "";
    }

    IEnumerator FadeText(float from, float to, float time)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            numberText.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }
        numberText.alpha = to;
    }

    IEnumerator PrepareCards()
    {
        var cards = level1.cards;

        if (sequence.Length != cards.Length)
        {
            Debug.LogError("DollPuzzle: Sequence needs exactly " + cards.Length + " sprites.");
            yield break;
        }

        ShuffleSlots(cards);
        for (int i = 0; i < cards.Length; i++)
            cards[i].ResetCard(sequence[i]);

        yield return null; 
        foreach (var card in cards)
            StartCoroutine(card.FadeIn());

        yield return new WaitForSeconds(0.6f);
    }

    void Update()
    {
        if (PauseMenu.IsPaused) return;

        if (!canPlay || busy) return;
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryPickCard();
    }

    void TryPickCard()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, level1.maxClickDistance)) return;

        var card = hit.collider.GetComponent<CardFlipUI>();
        if (card == null || card.Matched || card.IsFaceUp || card.IsFlipping) return;

        StartCoroutine(PickRoutine(card));
    }

    IEnumerator PickRoutine(CardFlipUI card)
    {
        busy = true;

        level1.PlayFx(flipSound);
        card.Flip();
        yield return new WaitForSeconds(card.duration + 0.15f);

        if (card.frontSprite == sequence[step])
        {
           
            level1.PlayFx(correctSound);
            step++;

            if (step >= sequence.Length)
            {
                yield return StartCoroutine(FinishPuzzle());
                yield break;
            }
        }
        else
        {
           
            level1.PlayFx(wrongSound);
            yield return new WaitForSeconds(wrongDelay);
            card.Flip();
            yield return new WaitForSeconds(card.duration + 0.1f);
        }

        busy = false;
    }

    IEnumerator FinishPuzzle()
    {
        canPlay = false;
        yield return new WaitForSeconds(1f);
        yield return dollReveal.HideDoll();   
        onPuzzleCompleted.Invoke();
    }

    void SetHud(bool visible)
    {
        foreach (var go in hideDuringCloseUp)
            if (go != null) go.SetActive(visible);
    }

    void ShuffleSlots(CardFlipUI[] cards)
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
}
