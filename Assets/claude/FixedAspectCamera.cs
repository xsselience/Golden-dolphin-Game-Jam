using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class FixedAspectCamera : MonoBehaviour
{
    [Header("目标设计分辨率(空洞骑士基准16:9)")]
    public int designWidth = 1920;
    public int designHeight = 1080;

    [Header("调试输出")]
    public bool enableDebugLog = true;

    private Camera _mainCam;
    private float _targetAspect;
    private float _lastScreenAspect;

    void Awake()
    {
        _mainCam = Camera.main;
        _targetAspect = (float)designWidth / designHeight;
        _lastScreenAspect = -999f;
    }

    void Update()
    {
        if (_mainCam == null) return;

        float curW = Screen.width;
        float curH = Screen.height;
        float curAspect = curW / curH;

        // 仅屏幕尺寸发生变化时执行，避免刷屏
        if (Mathf.Abs(curAspect - _lastScreenAspect) > 0.001f)
        {
            _lastScreenAspect = curAspect;

            if (enableDebugLog)
            {
                Debug.Log($"[FixedAspectCamera] 屏幕变化 | 屏幕:{curW}×{curH} | 当前比例:{curAspect:F3} | 目标比例:{_targetAspect:F3}");
            }

            Rect viewportRect;
            if (Mathf.Abs(curAspect - _targetAspect) > 0.001f)
            {
                if (curAspect > _targetAspect)
                {
                    // 屏幕更宽 → 左右黑边
                    float scale = _targetAspect / curAspect;
                    viewportRect = new Rect((1f - scale) / 2f, 0f, scale, 1f);
                    if (enableDebugLog)
                        Debug.Log($"[FixedAspectCamera] 窗口过宽，添加左右黑边，viewport:{viewportRect}");
                }
                else
                {
                    // 屏幕更高 → 上下黑边
                    float scale = curAspect / _targetAspect;
                    viewportRect = new Rect(0f, (1f - scale) / 2f, 1f, scale);
                    if (enableDebugLog)
                        Debug.Log($"[FixedAspectCamera] 窗口过高，添加上下黑边，viewport:{viewportRect}");
                }
            }
            else
            {
                // 比例完全匹配，铺满
                viewportRect = new Rect(0, 0, 1, 1);
                if (enableDebugLog)
                    Debug.Log($"[FixedAspectCamera] 比例匹配，铺满屏幕，viewport:{viewportRect}");
            }

            _mainCam.rect = viewportRect;
        }
    }
}
