using UnityEngine;

public class NextPosition : MonoBehaviour
{
    private Rigidbody rb;

    public float targetY;
    public float rotateSpeed = 180f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        targetY = rb.rotation.eulerAngles.y;
    }

    void FixedUpdate()
    {
        Quaternion targetRotation = Quaternion.Euler(0f, targetY, 0f);

        Quaternion nextRotation = Quaternion.RotateTowards(
            rb.rotation,
            targetRotation,
            rotateSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(nextRotation);
    }

    public void TurnLeft()
    {
        targetY -= 90f;
    }

    public void TurnRight()
    {
        targetY += 90f;
    }
}