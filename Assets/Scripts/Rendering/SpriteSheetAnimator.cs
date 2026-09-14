using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpriteSheetAnimator : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer target;

    [SerializeField]
    private Sprite[] frames;

    [SerializeField, Min(0.1f)]
    private float framesPerSecond = 4f;

    [SerializeField]
    private bool randomizeStart = true;

    private float elapsed;

    public void Configure(SpriteRenderer renderer, Sprite[] animationFrames, float speed)
    {
        target = renderer;
        frames = animationFrames;
        framesPerSecond = Mathf.Max(0.1f, speed);
        ApplyFrame(0);
    }

    private void OnEnable()
    {
        if (target == null)
            target = GetComponent<SpriteRenderer>();
        elapsed = randomizeStart && frames != null && frames.Length > 0
            ? Random.Range(0f, frames.Length / framesPerSecond)
            : 0f;
        ApplyCurrentFrame();
    }

    private void Update()
    {
        if (frames == null || frames.Length < 2 || target == null)
            return;
        elapsed += Time.deltaTime;
        ApplyCurrentFrame();
    }

    private void ApplyCurrentFrame()
    {
        if (frames == null || frames.Length == 0)
            return;
        ApplyFrame(Mathf.FloorToInt(elapsed * framesPerSecond) % frames.Length);
    }

    private void ApplyFrame(int index)
    {
        if (target != null && frames != null && frames.Length > 0)
            target.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }
}
