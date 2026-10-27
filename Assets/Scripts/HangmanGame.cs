using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

public class HangmanGame : MonoBehaviour
{
    [Header("References")]
    public GameSequence level1;               

    [Header("Sheet on the table")]
    public GameObject sheet;                  
    public CanvasGroup sheetGroup;
    public float sheetFadeTime = 1.5f;
    public TMP_Text wordText;                 
    public TMP_Text attemptsText;             
    public TMP_Text wrongLettersText;         

    [Header("Game")]
    public string word = "dolor";
    public int maxAttempts = 10;
    public string ignoredLetters = "cp";      
    public bool restartOnLose = true;
    public float restartDelay = 2.5f;

    [Header("Danger")]
    public int dangerChances = 3;        
    public HangmanEnemy enemy;

    [Header("Sounds (optional)")]

    [Header("Music")]
    public AudioClip level2Music;        
    public float musicFadeInTime = 2f;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip winSound;
    public AudioClip loseSound;

    public UnityEvent onWon;
    public UnityEvent onLost;

    bool active;
    bool finished;
    int attemptsLeft;
    char[] revealed;
    readonly HashSet<char> guessed = new HashSet<char>();
    readonly List<char> wrongLetters = new List<char>();

    void Awake()
    {
        if (sheet != null) sheet.SetActive(false);
    }

   
    public void StartGame()
    {
        word = word.ToLowerInvariant();
        ResetState();
        if (level1 != null) level1.PlayAmbience(level2Music, musicFadeInTime);
        StartCoroutine(StartRoutine());
    }

    IEnumerator StartRoutine()
    {
        sheet.SetActive(true);
        sheetGroup.alpha = 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / sheetFadeTime;
            sheetGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }

        
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInput;
            Keyboard.current.onTextInput += OnTextInput;
        }
        active = true;
    }

    void OnDisable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;
    }

    void ResetState()
    {
        attemptsLeft = maxAttempts;
        revealed = new char[word.Length];
        for (int i = 0; i < revealed.Length; i++) revealed[i] = '_';

        guessed.Clear();
        wrongLetters.Clear();
        finished = false;
        UpdateTexts();
        if (enemy != null) enemy.ResetEnemy();
    }

    void OnTextInput(char raw)
    {
        if (!active || finished || PauseMenu.IsPaused) return;

        char c = Normalize(raw);
        if (!char.IsLetter(c)) return;               
        if (ignoredLetters.IndexOf(c) >= 0) return; 
        if (!guessed.Add(c)) return;                 

        bool found = false;
        for (int i = 0; i < word.Length; i++)
        {
            if (word[i] == c)
            {
                revealed[i] = c;
                found = true;
            }
        }

        if (found)
        {
            Play(correctSound);
            UpdateTexts();

            if (System.Array.IndexOf(revealed, '_') < 0)
            {
                finished = true;
                Play(winSound);
                onWon.Invoke();
            }
        }
        else
        {
            attemptsLeft--;
            wrongLetters.Add(c);
            Play(wrongSound);
            UpdateTexts();

            if (attemptsLeft <= 0)
            {
                StartCoroutine(LoseRoutine());
            }
            else if (enemy != null)
            {
                if (attemptsLeft == dangerChances) enemy.Appear();
                else if (attemptsLeft < dangerChances) enemy.Approach();
            }
        }
    }

    IEnumerator LoseRoutine()
    {
        finished = true;
        Play(loseSound);
        onLost.Invoke();

        if (enemy != null)
        {
            yield return StartCoroutine(enemy.Attack());
            yield break;
        }

        if (!restartOnLose) yield break;

        yield return new WaitForSeconds(restartDelay);
        ResetState();
    }

    void UpdateTexts()
    {
        if (wordText != null)
            wordText.text = string.Join(" ", revealed);         

        if (attemptsText != null)
            attemptsText.text = "Oportunidades: " + attemptsLeft;

        if (wrongLettersText != null)
            wrongLettersText.text = wrongLetters.Count > 0
                ? "Fallos: " + string.Join(" ", wrongLetters)
                : "";
    }

   
    char Normalize(char c)
    {
        c = char.ToLowerInvariant(c);
        switch (c)
        {
            case '\u00e1': return 'a';
            case '\u00e9': return 'e';
            case '\u00ed': return 'i';
            case '\u00f3': return 'o';
            case '\u00fa':
            case '\u00fc': return 'u';
            default: return c;
        }
    }

    void Play(AudioClip clip)
    {
        if (level1 != null) level1.PlayFx(clip);
    }
}
