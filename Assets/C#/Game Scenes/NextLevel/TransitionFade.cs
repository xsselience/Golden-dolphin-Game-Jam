using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TransitionFade : MonoBehaviour
{
    public static TransitionFade Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeGroup;   // 满屏黑 Image 的 CanvasGroup
    [SerializeField] private float defaultFadeTime = 0.5f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        fadeGroup.alpha = 1f;   // 开局全黑
        fadeGroup.blocksRaycasts = false;

        // 每当新场景加载，自动淡入
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 新场景进来时先全黑，然后淡入
        fadeGroup.alpha = 1f;
        //StartCoroutine(FadeIn(defaultFadeTime));
    }

    /// <summary>淡入（黑→正常）</summary>
    public void FadeIn(float duration = -1)
    {
        if (duration < 0) duration = defaultFadeTime;
        StartCoroutine(FadeInRoutine(duration));
    }

    /// <summary>淡出（正常→黑），完成后执行回调</summary>
    public void FadeOut(float duration, System.Action onComplete = null)
    {
        if (duration < 0) duration = defaultFadeTime;
        StartCoroutine(FadeOutRoutine(duration, onComplete));
    }

    IEnumerator FadeInRoutine(float duration)
    {
        fadeGroup.alpha = 1f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = 1f - (t / duration);
            yield return null;
        }
        fadeGroup.alpha = 0f;
    }

    IEnumerator FadeOutRoutine(float duration, System.Action onComplete)
    {
        fadeGroup.alpha = 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = t / duration;
            yield return null;
        }
        fadeGroup.alpha = 1f;
        onComplete?.Invoke();
    }
}
