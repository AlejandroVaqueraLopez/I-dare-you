using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class GameEnding : MonoBehaviour
{
    [Header("References")]
    public FlashbackScene flashback;
    public VideoClip finalVideo;

    [Header("Timing")]
    public float delayBeforeFlashback = 1.5f;   // pause after winning
    public float blackHold = 1f;                // black pause after the video

    bool started;

    // Hook this to CardDuel > On Player Won ()
    public void Begin()
    {
        if (started) return;
        started = true;
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        PauseMenu.CanPause = false;

        yield return new WaitForSeconds(delayBeforeFlashback);

        // Headache + final video (no skip), the screen stays black at the end
        yield return flashback.PlayEnding(finalVideo);

        PauseMenu.CanPause = false;     // no pausing on the black screen
        yield return new WaitForSeconds(blackHold);

        // Reloading the scene shows the main menu again, with everything reset
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
