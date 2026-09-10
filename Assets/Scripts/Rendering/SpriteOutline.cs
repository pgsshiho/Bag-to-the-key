using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteOutline : MonoBehaviour
{
    private const int DirectionCount = 8;
    private const string GeneratedChildPrefix = "__SpriteOutline_";
    private const string DefaultMaterialResourcePath = "Rendering/SpriteOutlineMaterial";

    private static readonly Vector2[] Directions =
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right,
        new Vector2(1f, 1f).normalized,
        new Vector2(1f, -1f).normalized,
        new Vector2(-1f, 1f).normalized,
        new Vector2(-1f, -1f).normalized
    };

    [SerializeField] private Color color = Color.black;
    [SerializeField, Min(0f)] private float width = .03f;
    [SerializeField] private bool includeDiagonals = true;
    [SerializeField, Min(1)] private int sortingOrderOffset = 1;
    [SerializeField] private Material outlineMaterial;

    private SpriteRenderer sourceRenderer;
    private Material resolvedDefaultMaterial;
    private readonly SpriteRenderer[] outlineRenderers = new SpriteRenderer[DirectionCount];

    public Color Color
    {
        get => color;
        set
        {
            color = value;
            Refresh();
        }
    }

    public float Width
    {
        get => width;
        set
        {
            width = Mathf.Max(0f, value);
            Refresh();
        }
    }

    private void OnEnable()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
        EnsureRenderers();
        Refresh();
    }

    private void LateUpdate() => Refresh();

    private void OnValidate()
    {
        width = Mathf.Max(0f, width);
        sortingOrderOffset = Mathf.Max(1, sortingOrderOffset);
        if (!isActiveAndEnabled) return;
        sourceRenderer = GetComponent<SpriteRenderer>();
        EnsureRenderers();
        Refresh();
    }

    private void OnDisable() => SetGeneratedObjectsActive(false);

    private void OnDestroy()
    {
        for (int i = 0; i < DirectionCount; i++)
        {
            SpriteRenderer outlineRenderer = outlineRenderers[i];
            if (outlineRenderer == null) continue;
            GameObject generatedObject = outlineRenderer.gameObject;
            if (Application.isPlaying)
                Destroy(generatedObject);
            else
                DestroyImmediate(generatedObject);
        }
    }

    [ContextMenu("Refresh Outline")]
    public void Refresh()
    {
        if (sourceRenderer == null) return;
        EnsureRenderers();

        Material material = ResolveMaterial();
        bool outlineVisible = sourceRenderer.enabled && sourceRenderer.sprite != null &&
                              width > 0f && color.a > 0f && material != null;
        int activeCount = includeDiagonals ? DirectionCount : 4;

        for (int i = 0; i < DirectionCount; i++)
        {
            SpriteRenderer outlineRenderer = outlineRenderers[i];
            bool active = outlineVisible && i < activeCount;
            if (outlineRenderer.gameObject.activeSelf != active)
                outlineRenderer.gameObject.SetActive(active);
            if (!active) continue;

            outlineRenderer.transform.localPosition = Directions[i] * width;
            outlineRenderer.sprite = sourceRenderer.sprite;
            outlineRenderer.color = color;
            outlineRenderer.flipX = sourceRenderer.flipX;
            outlineRenderer.flipY = sourceRenderer.flipY;
            outlineRenderer.drawMode = sourceRenderer.drawMode;
            outlineRenderer.size = sourceRenderer.size;
            outlineRenderer.tileMode = sourceRenderer.tileMode;
            outlineRenderer.adaptiveModeThreshold = sourceRenderer.adaptiveModeThreshold;
            outlineRenderer.maskInteraction = sourceRenderer.maskInteraction;
            outlineRenderer.spriteSortPoint = sourceRenderer.spriteSortPoint;
            outlineRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            outlineRenderer.sortingOrder = sourceRenderer.sortingOrder - sortingOrderOffset;
            outlineRenderer.sharedMaterial = material;
        }
    }

    private void EnsureRenderers()
    {
        for (int i = 0; i < DirectionCount; i++)
        {
            if (outlineRenderers[i] != null) continue;

            string childName = GeneratedChildPrefix + i;
            Transform existing = transform.Find(childName);
            GameObject child = existing != null ? existing.gameObject : new GameObject(childName);
            child.hideFlags = HideFlags.HideAndDontSave;
            child.transform.SetParent(transform, false);
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = child.AddComponent<SpriteRenderer>();
            outlineRenderers[i] = renderer;
        }
    }

    private Material ResolveMaterial()
    {
        if (outlineMaterial != null) return outlineMaterial;
        if (resolvedDefaultMaterial == null)
            resolvedDefaultMaterial = Resources.Load<Material>(DefaultMaterialResourcePath);
        return resolvedDefaultMaterial;
    }

    private void SetGeneratedObjectsActive(bool active)
    {
        for (int i = 0; i < DirectionCount; i++)
        {
            if (outlineRenderers[i] != null)
                outlineRenderers[i].gameObject.SetActive(active);
        }
    }
}
