using System.Collections;
using UnityEngine;

public class CardFlip : MonoBehaviour
{
    public float duration = 0.5f;
    public float liftHeight = 0.15f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    bool flipping;
    bool faceUp;

    public void Flip()
    {
        if (!flipping) StartCoroutine(FlipRoutine());
    }

    public IEnumerator FlipRoutine()
    {
        flipping = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
       
        Quaternion endRot = startRot * Quaternion.Euler(0f, 180f, 0f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = ease.Evaluate(Mathf.Clamp01(t));

            transform.rotation = Quaternion.Slerp(startRot, endRot, k);
          
            transform.position = startPos + Vector3.up * Mathf.Sin(k * Mathf.PI) * liftHeight;
            yield return null;
        }

        transform.rotation = endRot;
        transform.position = startPos;
        faceUp = !faceUp;
        flipping = false;
    }
}
