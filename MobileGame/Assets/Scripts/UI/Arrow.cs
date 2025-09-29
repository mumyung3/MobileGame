using System.Collections;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private float animationHeight = 30f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool playOnStart = true;

    private Vector3 startPosition;
    private bool isAnimating = false;

    private void Start()
    {
        // 시작 위치 저장
        startPosition = transform.localPosition;

        if (playOnStart)
        {
            StartAnimation();
        }
    }

    public void StartAnimation()
    {
        if (!isAnimating)
        {
            StartCoroutine(VerticalAnimation());
        }
    }

    public void StopAnimation()
    {
        isAnimating = false;
        StopAllCoroutines();

        // 원래 위치로 복귀
        transform.localPosition = startPosition;
    }

    private IEnumerator VerticalAnimation()
    {
        isAnimating = true;

        while (isAnimating)
        {
            float elapsedTime = 0f;
            float cycleDuration = 1f / animationSpeed;

            // 위로 올라가는 애니메이션
            while (elapsedTime < cycleDuration && isAnimating)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / cycleDuration;
                float curveValue = animationCurve.Evaluate(progress);

                Vector3 currentPosition = startPosition;
                currentPosition.y += curveValue * animationHeight;
                transform.localPosition = currentPosition;

                yield return null;
            }

            elapsedTime = 0f;

            // 아래로 내려가는 애니메이션
            while (elapsedTime < cycleDuration && isAnimating)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / cycleDuration;
                float curveValue = animationCurve.Evaluate(1f - progress);

                Vector3 currentPosition = startPosition;
                currentPosition.y += curveValue * animationHeight;
                transform.localPosition = currentPosition;

                yield return null;
            }
        }
    }

    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = Mathf.Max(0.1f, speed);
    }

    public void SetAnimationHeight(float height)
    {
        animationHeight = height;
    }

    public bool IsAnimating()
    {
        return isAnimating;
    }

    // 애니메이션 재시작 (위치 초기화 후 시작)
    public void RestartAnimation()
    {
        StopAnimation();
        startPosition = transform.localPosition;
        StartAnimation();
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 basePosition = Application.isPlaying ? startPosition : transform.localPosition;

        // 애니메이션 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.TransformPoint(basePosition), Vector3.one * 0.2f);

        Gizmos.color = Color.green;
        Vector3 topPosition = basePosition;
        topPosition.y += animationHeight;
        Gizmos.DrawWireCube(transform.TransformPoint(topPosition), Vector3.one * 0.2f);

        // 애니메이션 경로 표시
        Gizmos.color = Color.cyan;
        Vector3 worldStart = transform.TransformPoint(basePosition);
        Vector3 worldEnd = transform.TransformPoint(topPosition);
        Gizmos.DrawLine(worldStart, worldEnd);
    }
}