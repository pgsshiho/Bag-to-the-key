using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    public enum GradientMode { Vertical, Horizontal, Diagonal, VerticalCenter, HorizontalCenter }
    public GradientMode mode = GradientMode.Vertical;
    public Color color1 = Color.white;
    public Color color2 = new Color(1f, 1f, 1f, 0f);

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        int count = vh.currentVertCount;
        if (count == 0) return;

        UIVertex vertex = new UIVertex();
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            Vector3 pos = vertex.position;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        float width = maxX - minX;
        float height = maxY - minY;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            
            // 유니티 기본 UI 색상(흰색)을 베이스로 가져와서 깨짐 방지
            Color targetColor = vertex.color; 
            float ratio = 0f;

            switch (mode)
            {
                case GradientMode.Vertical:
                    ratio = (vertex.position.y - minY) / height;
                    targetColor = Color.Lerp(color2, color1, ratio);
                    break;
                case GradientMode.Horizontal:
                    ratio = (vertex.position.x - minX) / width;
                    targetColor = Color.Lerp(color1, color2, ratio);
                    break;
                case GradientMode.Diagonal:
                    float ratioX = (vertex.position.x - minX) / width;
                    float ratioY = (vertex.position.y - minY) / height;
                    ratio = (ratioX + (1f - ratioY)) / 2f;
                    targetColor = Color.Lerp(color1, color2, ratio);
                    break;
                case GradientMode.VerticalCenter:
                    float vRatio = (vertex.position.y - minY) / height;
                    ratio = Mathf.Abs(vRatio - 0.5f) * 2f;
                    targetColor = Color.Lerp(color1, color2, ratio);
                    break;
                case GradientMode.HorizontalCenter:
                    float hRatio = (vertex.position.x - minX) / width;
                    ratio = Mathf.Abs(hRatio - 0.5f) * 2f;
                    targetColor = Color.Lerp(color1, color2, ratio);
                    break;
            }

            // 중요: 기존 UI의 알파값과 자연스럽게 곱해지도록 처리
            vertex.color = targetColor;
            vh.SetUIVertex(vertex, i);
        }
    }
}