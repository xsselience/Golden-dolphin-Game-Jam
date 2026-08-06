using UnityEngine;
using System.Collections;

public class DisappearingPlatform : MonoBehaviour
{
    [Header("触发后多久消失")]
    public float disappearDelay = 0.8f;
    [Header("消失后多久复原")]
    public float recoverDelay = 2f;
    [Header("淡入淡出时间")]
    public float fadeDuration = 0.3f;
    [Header("仅勾选player层！不要勾Ground")]
    public LayerMask playerLayer;
    [Header("顶面采样射线数量，宽平台可设置12~15")]
    public int sampleCount = 12;

    private SpriteRenderer _sr;
    private Collider2D _col;
    private Color _originColor;

    private enum State
    {
        Normal,
        CountDown,
        Vanish,
        Gone
    }
    private State _curState;
    private float _timer;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();

        if (_sr != null) _originColor = _sr.color;
        if (_col == null) _col = gameObject.AddComponent<BoxCollider2D>();
        _col.isTrigger = false;
        _curState = State.Normal;
        _timer = 0f;
    }

    void FixedUpdate()
    {
        bool hitOnce = CheckPlayerTouchAnyRay();

        switch (_curState)
        {
            case State.Normal:
                // ✅只要一瞬间命中，直接开启倒计时，不需要持续站上面
                if (hitOnce)
                {
                    _curState = State.CountDown;
                    _timer = 0;
                    Debug.Log("检测到玩家，开始倒计时");
                }
                break;

            case State.CountDown:
                _timer += Time.fixedDeltaTime;
                if (_timer >= disappearDelay)
                {
                    StartCoroutine(PlatformVanish());
                    _curState = State.Vanish;
                }
                break;
        }
    }

    private bool CheckPlayerTouchAnyRay()
    {
        float checkHeight = 0.35f;
        Bounds localBounds = _col.bounds;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / (sampleCount - 1);
            Vector2 worldPoint = Vector2.Lerp(
                new Vector2(localBounds.min.x, localBounds.max.y),
                new Vector2(localBounds.max.x, localBounds.max.y),
                t
            );
            Vector2 rayOrigin = new Vector2(worldPoint.x, worldPoint.y + 0.1f);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, checkHeight, playerLayer);
            Debug.DrawRay(rayOrigin, Vector2.down * checkHeight, Color.green, Time.fixedDeltaTime * 2);

            if (hit.collider != null)
            {
                return true;
            }
        }
        return false;
    }

    IEnumerator PlatformVanish()
    {
        if (_sr != null)
        {
            float t = 0;
            Color start = _sr.color;
            Color target = new Color(start.r, start.g, start.b, 0);
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                _sr.color = Color.Lerp(start, target, t / fadeDuration);
                yield return null;
            }
            _sr.color = target;
        }

        _col.enabled = false;
        _curState = State.Gone;

        yield return new WaitForSeconds(recoverDelay);

        _col.enabled = true;
        _curState = State.Normal;
        _timer = 0;
        if (_sr != null)
        {
            _sr.color = _originColor;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (_col == null) return;
        Bounds b = _col.bounds;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / (sampleCount - 1);
            Vector2 p = Vector2.Lerp(new Vector2(b.min.x, b.max.y), new Vector2(b.max.x, b.max.y), t);
            Vector2 origin = new Vector2(p.x, p.y + 0.1f);
            Gizmos.DrawLine(origin, origin + Vector2.down * 0.35f);
        }
    }
}