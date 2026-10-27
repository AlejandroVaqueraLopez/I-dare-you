using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleAftermath : MonoBehaviour
{
    [Header("References")]
    public GameSequence level1;            

    [Header("Candles")]
    public GameObject[] candles;          
    public Light[] extraLights;            
    public bool deactivateCandles = true; 
    public float candleFadeTime = 2f;

    [Header("Cards")]
    public float cardFadeTime = 1f;

    [Header("Skull")]
    public GameObject skull;               
    public float skullDelay = 1.5f;        
    public Vector3 pushDirection = new Vector3(-1f, 0f, 0f); 
    public float pushForce = 1.5f;

    [Header("Flashback")]
    public FlashbackScene flashback;
    public float flashbackDelay = 3f;   

  
    public void Play()
    {
        Debug.Log("PuzzleAftermath: Play called");
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
      
        foreach (var card in level1.cards)
            StartCoroutine(card.Disappear(cardFadeTime));

        yield return StartCoroutine(PutOutCandles());

        yield return new WaitForSeconds(skullDelay);

        if (flashback != null)
        {
            yield return new WaitForSeconds(flashbackDelay);
            flashback.Play();
        }
        DropSkull();
    }

    IEnumerator PutOutCandles()
    {
        var lights = new List<Light>(extraLights);

        foreach (var c in candles)
        {
            if (c == null) continue;

            lights.AddRange(c.GetComponentsInChildren<Light>());

           
            foreach (var ps in c.GetComponentsInChildren<ParticleSystem>())
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

           
            foreach (var mb in c.GetComponentsInChildren<MonoBehaviour>())
                mb.enabled = false;
        }

        var startIntensity = new float[lights.Count];
        for (int i = 0; i < lights.Count; i++)
            startIntensity[i] = lights[i].intensity;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / candleFadeTime;
            float k = Mathf.Clamp01(t);
            for (int i = 0; i < lights.Count; i++)
                if (lights[i] != null)
                    lights[i].intensity = Mathf.Lerp(startIntensity[i], 0f, k);
            yield return null;
        }

        if (deactivateCandles)
            foreach (var c in candles)
                if (c != null) c.SetActive(false);
    }

    void DropSkull()
    {
        if (skull == null) return;

        if (!skull.TryGetComponent(out Rigidbody rb))
            rb = skull.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(pushDirection.normalized * pushForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * pushForce, ForceMode.Impulse);
    }
}
