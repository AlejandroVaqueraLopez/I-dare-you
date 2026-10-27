using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

public class CardDuel : MonoBehaviour
{
    [Header("References")]
    public GameSequence level1;         
    public GameOverScreen gameOver;     

    [Header("Deck")]
    public Transform deck;
    public Transform revealSpot;
    public GameObject cardPrefab;      
    public Sprite coverSprite;
    public Sprite blackSprite;
    public Sprite whiteSprite;
    public int blackCards = 10;
    public int whiteCards = 10;
    public int riggedTopCards = 2;
    public float liftHeight = 0.12f;
    public float drawTime = 0.7f;

    [Header("Rules")]
    public int pointsToWin = 5;
    public float enemyThinkTime = 1.5f;
    public float slapDelay = 0.4f;
    public float afterSlapPause = 1f;

    [Header("Hand prints (one per point, start disabled)")]
    public GameObject[] darknessPrints;
    public GameObject[] playerPrints;

    [Header("Table shake")]
    public Transform tablePivot;
    public Transform[] tableObjects;
    public Transform[] candlesToWobble;
    public Vector3 tiltAxis = Vector3.right;
    public float tiltDegrees = 2f;
    public float candleWobbleDegrees = 6f;
    public float shakeTime = 1f;

    [Header("UI (optional)")]
    public TMP_Text promptText;
    public TMP_Text scoreText;
    public string yourTurnText = "Tu turno\n[Clic] Sacar carta     [R] Barajar";
    public string darknessTurnText = "Turno de la Oscuridad...";

    [Header("Sounds (optional)")]
    public AudioClip drawSound;
    public AudioClip shuffleSound;
    public AudioClip slapSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip myTurnSound;       

    [Header("Music (optional)")]
    public AudioClip music;             
    public float musicFadeTime = 2f;

    [Header("Darkness animation (optional)")]
    public Animator darknessAnimator;
    public string drawTrigger = "";
    public string slapTrigger = "";

    [Header("Darkness attack (when the player loses)")]
    public HangmanEnemy darknessAttack;  

    [Header("Last chance warning")]
    public int dangerPointsLeft = 1;    
    public Transform darknessHead;      
    public Vector3 headTwist = new Vector3(0f, 70f, 40f);  
    public float headTurnTime = 0.25f;
    public AudioClip headTurnSound;

    public UnityEvent onPlayerWon;

    readonly List<bool> cards = new List<bool>();
    int playerScore;
    int darknessScore;
    int choice;
    bool warned;
    float headWeight;
    Coroutine loop;
    Coroutine headRoutine;
    GameObject currentCard;

    Vector3[] tablePos;
    Quaternion[] tableRot;
    Quaternion[] candleRot;

    void Awake()
    {
        CacheTable();
        HideAllPrints();
        SetPrompt("");
        if (scoreText != null) scoreText.gameObject.SetActive(false);
    }

   
    void LateUpdate()
    {
        if (darknessHead != null && headWeight > 0.001f)
            darknessHead.localRotation *= Quaternion.Euler(headTwist * headWeight);
    }

   

    public void StartDuel()
    {
        if (loop != null) StopCoroutine(loop);
        if (currentCard != null) Destroy(currentCard);
        StopHeadTwist();

       
        if (darknessAttack != null)
        {
            darknessAttack.ResetEnemy();
            darknessAttack.enemy.SetActive(true);
        }

        if (music != null) level1.CrossfadeAmbience(music, musicFadeTime);

        deck.gameObject.SetActive(true);
        playerScore = 0;
        darknessScore = 0;
        warned = false;
        HideAllPrints();
        RestoreTable();
        BuildDeck(true);
        UpdateScore();
        if (scoreText != null) scoreText.gameObject.SetActive(true);

        loop = StartCoroutine(DuelLoop());
    }

    IEnumerator DuelLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
           
            SetPrompt(yourTurnText);
            level1.PlayFx(myTurnSound);
            choice = 0;
            while (choice == 0)
            {
                ReadInput();
                yield return null;
            }
            SetPrompt("");

            if (choice == 2)
            {
                yield return StartCoroutine(ShuffleRoutine());
            }
            else
            {
                yield return StartCoroutine(DrawRoutine(false));
                if (HasWinner()) break;
            }

          
            SetPrompt(darknessTurnText);
            yield return new WaitForSeconds(enemyThinkTime);
            SetPrompt("");
            yield return StartCoroutine(DrawRoutine(true));
            if (HasWinner()) break;
        }

        yield return StartCoroutine(EndRoutine());
    }

    void ReadInput()
    {
        if (PauseMenu.IsPaused) return;

        var mouse = Mouse.current;
        var kb = Keyboard.current;

        if (mouse != null && mouse.leftButton.wasPressedThisFrame) choice = 1;
        else if (kb != null && kb.rKey.wasPressedThisFrame) choice = 2;
    }

    bool HasWinner()
    {
        return playerScore >= pointsToWin || darknessScore >= pointsToWin;
    }

  

    IEnumerator DrawRoutine(bool byDarkness)
    {
        if (cards.Count == 0) BuildDeck(false);

        bool black = cards[cards.Count - 1];
        cards.RemoveAt(cards.Count - 1);

        if (byDarkness) Trigger(drawTrigger);
        level1.PlayFx(drawSound);

        currentCard = Instantiate(cardPrefab, deck.position + Vector3.up * 0.002f, deck.rotation);
        Transform card = currentCard.transform;
        SpriteRenderer rend = currentCard.GetComponentInChildren<SpriteRenderer>();
        rend.sprite = coverSprite;

        Quaternion baseRot = deck.rotation;
        Vector3 from = card.position;
        Vector3 to = revealSpot.position;
        bool swapped = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.05f, drawTime);
            float k = Mathf.Clamp01(t);

            float move = Mathf.SmoothStep(0f, 1f, k);
            card.position = Vector3.Lerp(from, to, move) + Vector3.up * Mathf.Sin(move * Mathf.PI) * liftHeight;

          
            float angle;
            if (k < 0.5f)
            {
                angle = Mathf.Lerp(0f, 90f, k * 2f);
            }
            else
            {
                if (!swapped)
                {
                    rend.sprite = black ? blackSprite : whiteSprite;
                    swapped = true;
                }
                angle = Mathf.Lerp(-90f, 0f, (k - 0.5f) * 2f);
            }
            card.rotation = baseRot * Quaternion.Euler(0f, angle, 0f);

            yield return null;
        }
        card.SetPositionAndRotation(to, baseRot);

        yield return new WaitForSeconds(slapDelay);

       
        if (black) darknessScore++;
        else playerScore++;

        yield return StartCoroutine(SlapRoutine(black));
        UpdateScore();
        CheckDanger();

        yield return new WaitForSeconds(afterSlapPause);
        Destroy(currentCard);
        currentCard = null;
    }

    IEnumerator ShuffleRoutine()
    {
        level1.PlayFx(shuffleSound);
        Shuffle(cards);

        Vector3 pos = deck.position;
        Quaternion rot = deck.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.6f;
            float s = Mathf.Sin(t * Mathf.PI * 12f) * (1f - Mathf.Clamp01(t));
            deck.position = pos + deck.right * 0.01f * s;
            yield return null;
        }
        deck.SetPositionAndRotation(pos, rot);
    }

   

    void CheckDanger()
    {
        if (warned || HasWinner()) return;
        if (pointsToWin - darknessScore > dangerPointsLeft) return;

        warned = true;
        headRoutine = StartCoroutine(HeadTurnRoutine());
    }

    IEnumerator HeadTurnRoutine()
    {
        level1.PlayFx(headTurnSound);

       
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.05f, headTurnTime);
            headWeight = Mathf.Clamp01(t);
            yield return null;
        }

       
        while (true)
        {
            headWeight = 1f + (Mathf.PerlinNoise(Time.time * 20f, 0.5f) - 0.5f) * 0.12f;
            yield return null;
        }
    }

    void StopHeadTwist()
    {
        if (headRoutine != null) StopCoroutine(headRoutine);
        headRoutine = null;
        headWeight = 0f;
    }

   

    IEnumerator SlapRoutine(bool darkness)
    {
        level1.PlayFx(slapSound);
        if (darkness) Trigger(slapTrigger);

        if (darkness) ShowPrint(darknessPrints, darknessScore);
        else ShowPrint(playerPrints, playerScore);

        yield return StartCoroutine(ShakeTable(darkness ? 1f : -1f));
    }

    IEnumerator ShakeTable(float sign)
    {
        Transform pivot = tablePivot != null ? tablePivot : (tableObjects.Length > 0 ? tableObjects[0] : null);
        Vector3 pivotPos = pivot != null ? pivot.position : Vector3.zero;
        Vector3 axis = tiltAxis.normalized;

        float prevTable = 0f, prevX = 0f, prevZ = 0f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.05f, shakeTime);
            float k = Mathf.Clamp01(t);
            float env = (1f - k) * (1f - k);

     
            float tableAngle = sign * tiltDegrees * Mathf.Sin(k * Mathf.PI * 5f) * env;
            float candleX = sign * candleWobbleDegrees * Mathf.Sin(k * Mathf.PI * 5f) * env;
            float candleZ = candleWobbleDegrees * 0.7f * Mathf.Sin(k * Mathf.PI * 4.3f) * env;

            if (pivot != null)
            {
                foreach (var o in tableObjects)
                    if (o != null) o.RotateAround(pivotPos, axis, tableAngle - prevTable);
            }

            foreach (var c in candlesToWobble)
                if (c != null)
                    c.Rotate(new Vector3(candleX - prevX, 0f, candleZ - prevZ), Space.World);

            prevTable = tableAngle;
            prevX = candleX;
            prevZ = candleZ;
            yield return null;
        }

        RestoreTable();
    }

    void CacheTable()
    {
        tablePos = new Vector3[tableObjects.Length];
        tableRot = new Quaternion[tableObjects.Length];
        for (int i = 0; i < tableObjects.Length; i++)
        {
            if (tableObjects[i] == null) continue;
            tablePos[i] = tableObjects[i].position;
            tableRot[i] = tableObjects[i].rotation;
        }

        candleRot = new Quaternion[candlesToWobble.Length];
        for (int i = 0; i < candlesToWobble.Length; i++)
            if (candlesToWobble[i] != null) candleRot[i] = candlesToWobble[i].rotation;
    }

    void RestoreTable()
    {
        for (int i = 0; i < tableObjects.Length; i++)
            if (tableObjects[i] != null) tableObjects[i].SetPositionAndRotation(tablePos[i], tableRot[i]);

        for (int i = 0; i < candlesToWobble.Length; i++)
            if (candlesToWobble[i] != null) candlesToWobble[i].rotation = candleRot[i];
    }

    // ---------- End ----------

    IEnumerator EndRoutine()
    {
        SetPrompt("");
        yield return new WaitForSeconds(1.5f);

        if (darknessScore >= pointsToWin)
        {
            StopHeadTwist();
            level1.PlayFx(loseSound);

          
            if (darknessAttack != null)
                yield return darknessAttack.Attack();
            else
                yield return new WaitForSeconds(0.5f);

         
            if (gameOver != null) gameOver.ShowWithRetry(StartDuel);
        }
        else
        {
            StopHeadTwist();
            level1.PlayFx(winSound);
            onPlayerWon.Invoke();
        }
    }

    // ---------- Helpers ----------

    void BuildDeck(bool rigged)
    {
        var pool = new List<bool>();
        for (int i = 0; i < blackCards; i++) pool.Add(true);
        for (int i = 0; i < whiteCards; i++) pool.Add(false);
        Shuffle(pool);

      
        if (rigged && riggedTopCards > 0)
        {
            int moved = 0;
            for (int i = 0; i < pool.Count && moved < riggedTopCards; i++)
            {
                if (!pool[i]) continue;
                pool.RemoveAt(i);
                i--;
                moved++;
            }
            for (int m = 0; m < moved; m++) pool.Add(true);
        }

        cards.Clear();
        cards.AddRange(pool);
    }

    static void Shuffle(List<bool> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void ShowPrint(GameObject[] prints, int score)
    {
        if (prints == null || prints.Length == 0) return;
        int index = Mathf.Clamp(score - 1, 0, prints.Length - 1);
        if (prints[index] != null) prints[index].SetActive(true);
    }

    void HideAllPrints()
    {
        foreach (var p in darknessPrints) if (p != null) p.SetActive(false);
        foreach (var p in playerPrints) if (p != null) p.SetActive(false);
    }

    void UpdateScore()
    {
        if (scoreText != null)
            scoreText.text = "Oscuridad " + darknessScore + "  -  " + playerScore + " Tú";
    }

    void SetPrompt(string text)
    {
        if (promptText == null) return;
        promptText.text = text;
        promptText.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    void Trigger(string trigger)
    {
        if (darknessAnimator != null && !string.IsNullOrEmpty(trigger))
            darknessAnimator.SetTrigger(trigger);
    }
}