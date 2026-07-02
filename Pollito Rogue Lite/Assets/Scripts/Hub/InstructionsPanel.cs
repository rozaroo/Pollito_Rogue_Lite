using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstructionsPanel : MonoBehaviour
{
    public RectTransform panelTransform;
    void Start()
    {
        Hide();
    }
    private IEnumerator ScaleOverTime(RectTransform target, Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        target.localScale = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float factor = t / duration;
            target.localScale = Vector3.Lerp(from, to, factor);
            yield return null;
        }
        target.localScale = to;
    }
    public void Show() 
    {
        StartCoroutine(ScaleOverTime(panelTransform, Vector3.zero, new Vector3(1.33329999f, 8.32574749f, 1f), 0.2f));
    }
    public void Hide() 
    {
        StartCoroutine(ScaleOverTime(panelTransform, panelTransform.localScale, Vector3.zero, 0.2f));
    }
}
