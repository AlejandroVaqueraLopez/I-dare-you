using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class HangmanEnemy : MonoBehaviour
{
    [Header("References")]
    public GameSequence level1;                  
    public GameObject enemy;                    
    public Transform enemyHead;                 
    public Animator enemyAnimator;               
    public string appearTrigger = "";           
    public string attackTrigger = "";            
    public string walkBool = "IsWalking";        
    public float facingOffset = 0f;            

    [Header("Player camera")]
    public Camera mainCamera;                    
    public CinemachineCamera playerCamera;
    public CinemachinePanTilt panTilt;           
    public CinemachineCameraOffset cameraOffset;

    [Header("Danger phase (last chances)")]
    public AudioClip dangerMusic;
    public float musicCrossfadeTime = 3f;
    public AudioClip dangerSound;                
    public AudioClip secondPointSound;           
    public Transform[] approachPoints;           
    public float approachTime = 1.5f;

    [Header("Final attack")]
    public AudioClip attackSound;
    public AudioClip rushSound;                  
    public float rushTime = 1.2f;
    public float grabDistance = 0.6f;            
    public float yankTime = 0.25f;               
    public float grabShakeTime = 1.2f;
    public float grabShakeAmount = 0.25f;
    public float grabRoll = 15f;
    public CanvasGroup blackout;                 
    public float blackoutTime = 0.6f;
    public bool reloadSceneAfter = false;
    public float reloadDelay = 3f;

    public UnityEvent onCaught;

    bool appeared;
    int step;
    Vector3 startPos;
    Quaternion startRot;

    void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        startPos = enemy.transform.position;
        startRot = enemy.transform.rotation;
        enemy.SetActive(false);

        if (blackout != null) blackout.alpha = 0f;
    }

    // ---------- Danger phase ----------

    public void Appear()
    {
        if (appeared) return;
        appeared = true;

        enemy.SetActive(true);
        FaceCamera(enemy.transform);
        SetTrigger(appearTrigger);

        level1.PlayFx(dangerSound);
        if (dangerMusic != null)
            level1.CrossfadeAmbience(dangerMusic, musicCrossfadeTime);
    }

    
    public void Approach()
    {
        if (!appeared || step >= approachPoints.Length) return;

        int index = step++;
        
        if (index == 1) level1.PlayFx(secondPointSound);

        StartCoroutine(MoveTo(approachPoints[index].position, approachTime));
    }

    IEnumerator MoveTo(Vector3 target, float time)
    {
        Transform t = enemy.transform;
        Vector3 from = t.position;

        SetWalking(true);

        float k = 0f;
        while (k < 1f)
        {
            k += Time.deltaTime / Mathf.Max(0.01f, time);
            t.position = Vector3.Lerp(from, target, Mathf.SmoothStep(0f, 1f, k));
            FaceCamera(t);
            yield return null;
        }

        SetWalking(false);
    }

    // ---------- Final attack ----------

    public IEnumerator Attack()
    {
        PauseMenu.CanPause = false;
        if (level1.lookController != null) level1.lookController.enabled = false;

        if (!appeared) Appear();

        Transform et = enemy.transform;
        level1.PlayFx(attackSound);
        level1.StopAmbience(true);               

        
        level1.PlayFx(rushSound);
        SetWalking(true);

        Vector3 camPos = mainCamera.transform.position;
        Vector3 from = et.position;
        Vector3 away = from - camPos;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = -mainCamera.transform.forward;
        Vector3 target = camPos + away.normalized * grabDistance;
        target.y = from.y;

        float k = 0f;
        while (k < 1f)
        {
            k += Time.deltaTime / Mathf.Max(0.01f, rushTime);
            et.position = Vector3.Lerp(from, target, Mathf.Clamp01(k * k)); 
            FaceCamera(et);
            yield return null;
        }

        
        SetWalking(false);
        SetTrigger(attackTrigger);
        yield return StartCoroutine(YankCamera());

        
        float baseFov = playerCamera.Lens.FieldOfView;
        float seed = Random.value * 100f;
        float t = 0f;
        while (t < grabShakeTime)
        {
            t += Time.deltaTime;
            ApplyShake(seed, 1f, baseFov);
            yield return null;
        }

        // 4. Black out
        if (blackout != null)
        {
            float b = 0f;
            while (b < 1f)
            {
                b += Time.deltaTime / Mathf.Max(0.01f, blackoutTime);
                blackout.alpha = Mathf.Clamp01(b);
                ApplyShake(seed, 1f, baseFov);
                yield return null;
            }
        }
        ApplyShake(seed, 0f, baseFov);           

        PauseMenu.CanPause = true;
        onCaught.Invoke();

        if (reloadSceneAfter)
        {
            yield return new WaitForSeconds(reloadDelay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    IEnumerator YankCamera()
    {
        if (panTilt == null) yield break;

        Transform head = enemyHead != null ? enemyHead : enemy.transform;
        Vector3 dir = head.position - mainCamera.transform.position;

        float targetPan = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float targetTilt = -Mathf.Asin(Mathf.Clamp(dir.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;

        float startPan = panTilt.PanAxis.Value;
        float startTilt = panTilt.TiltAxis.Value;

        float k = 0f;
        while (k < 1f)
        {
            k += Time.deltaTime / Mathf.Max(0.01f, yankTime);
            float e = Mathf.SmoothStep(0f, 1f, k);
            SetAxes(Mathf.LerpAngle(startPan, targetPan, e), Mathf.Lerp(startTilt, targetTilt, e));
            yield return null;
        }
    }

    void SetAxes(float pan, float tilt)
    {
        var p = panTilt.PanAxis;
        p.Value = pan;
        panTilt.PanAxis = p;

        var t = panTilt.TiltAxis;
        t.Value = Mathf.Clamp(tilt, t.Range.x, t.Range.y);
        panTilt.TiltAxis = t;
    }

    void ApplyShake(float seed, float strength, float baseFov)
    {
        if (cameraOffset != null)
            cameraOffset.Offset = new Vector3(Noise(seed, 1f), Noise(seed, 2f), Noise(seed, 3f) * 0.3f)
                                  * grabShakeAmount * strength;

        var lens = playerCamera.Lens;
        lens.Dutch = Noise(seed, 4f) * grabRoll * strength;
        lens.FieldOfView = baseFov - 12f * strength;   
        playerCamera.Lens = lens;
    }

    float Noise(float seed, float channel)
    {
        return (Mathf.PerlinNoise(Time.time * 25f + seed, channel * 10f) - 0.5f) * 2f;
    }

    // ---------- Helpers ----------

    void FaceCamera(Transform t)
    {
        Vector3 dir = mainCamera.transform.position - t.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            t.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, facingOffset, 0f);
    }

    void SetTrigger(string trigger)
    {
        if (enemyAnimator != null && !string.IsNullOrEmpty(trigger))
            enemyAnimator.SetTrigger(trigger);
    }

    void SetWalking(bool walking)
    {
        if (enemyAnimator != null && !string.IsNullOrEmpty(walkBool))
            enemyAnimator.SetBool(walkBool, walking);
    }

    
    public void ResetEnemy()
    {
        StopAllCoroutines();
        appeared = false;
        step = 0;

        SetWalking(false);
        enemy.SetActive(false);
        enemy.transform.SetPositionAndRotation(startPos, startRot);
        if (blackout != null) blackout.alpha = 0f;
    }
}