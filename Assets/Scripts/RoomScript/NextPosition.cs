using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NextPosition : MonoBehaviour
{
    private Rigidbody rb;
    private Tween rotationTween;

    public float targetY;
    public float rotateSpeed = 180f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        targetY = rb.rotation.eulerAngles.y;
    }

    private void OnDisable()
    {
        rotationTween?.Kill();
        rotationTween = null;
    }

    public void TurnLeft()
    {
        targetY -= 90f;
        StartRotationTween();
    }

    public void TurnRight()
    {
        targetY += 90f;
        StartRotationTween();
    }

    private void StartRotationTween()
    {
        if (rb == null || rotateSpeed <= 0f) return;

        rotationTween?.Kill();

        Quaternion targetRotation = Quaternion.Euler(0f, targetY, 0f);
        float angle = Quaternion.Angle(rb.rotation, targetRotation);
        if (angle <= Mathf.Epsilon)
        {
            rb.MoveRotation(targetRotation);
            rotationTween = null;
            return;
        }

        Quaternion startRotation = rb.rotation;
        float rotationProgress = 0f;
        rotationTween = DOTween
            .To(
                () => rotationProgress,
                value =>
                {
                    rotationProgress = value;
                    rb.MoveRotation(Quaternion.SlerpUnclamped(
                        startRotation,
                        targetRotation,
                        value));
                },
                1f,
                angle / rotateSpeed)
            .SetEase(Ease.Linear)
            .SetUpdate(UpdateType.Fixed)
            .OnComplete(() => rotationTween = null);
    }
}
