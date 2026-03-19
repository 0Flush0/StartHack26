using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EffectTest : MonoBehaviour
{
    [Header("Animation Settings")]
    public float duration = 0.3f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public GameObject map;

    private bool isCollapsed = false;

    private List<Transform> children = new List<Transform>();
    private Coroutine currentRoutine;

    void Start()
    {
        // Cache all direct children
        children.AddRange(map.GetComponentsInChildren<Transform>()); 
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        float targetScale = isCollapsed ? 1f : 0f;
        currentRoutine = StartCoroutine(AnimateScaleY(targetScale));

        isCollapsed = !isCollapsed;
    }

    IEnumerator AnimateScaleY(float targetY)
    {
        float time = 0f;

        // Store initial scales
        List<Vector3> initialScales = new List<Vector3>();
        foreach (var child in children)
        {
            initialScales.Add(child.localScale);
        }

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float easedT = easeCurve.Evaluate(t);

            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] == null) continue;

                Vector3 scale = initialScales[i];
                scale.y = Mathf.Lerp(scale.y, targetY, easedT);
                children[i].localScale = scale;
            }

            yield return null;
        }

        // Ensure final value is exact
        foreach (var child in children)
        {
            if (child == null) continue;

            Vector3 scale = child.localScale;
            scale.y = targetY;
            child.localScale = scale;
        }
    }
}