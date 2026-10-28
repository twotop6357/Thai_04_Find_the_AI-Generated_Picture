using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    /// <summary>이미지 에셋을 수정하지 않고 둥근 내부 영역의 스텐실 마스크를 그린다.</summary>
    [AddComponentMenu("UI/Rounded Rect Mask Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedRectMaskGraphic : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float cornerRadius = 22f;
        private const int CornerSegments = 12;
        // Optional subpixel aperture traced from the existing background; round masks elsewhere keep their original path.
        [SerializeField, HideInInspector] private Vector2[] backgroundContour = System.Array.Empty<Vector2>();
        [SerializeField, HideInInspector] private int[] backgroundTriangles = System.Array.Empty<int>();

        public void SetBackgroundContour(Vector2[] contour, int[] triangles)
        {
            if (contour == null || triangles == null || contour.Length < 3 || triangles.Length % 3 != 0 ||
                System.Array.Exists(triangles, index => index < 0 || index >= contour.Length))
            {
                Debug.LogError("[RoundedRectMaskGraphic] The background aperture contour is invalid.", this);
                return;
            }
            backgroundContour = contour;
            backgroundTriangles = triangles;
            SetVerticesDirty();
        }

        public float CornerRadius
        {
            get => cornerRadius;
            set
            {
                value = Mathf.Max(0f, value);
                if (Mathf.Approximately(cornerRadius, value)) return;
                cornerRadius = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;
            if (backgroundContour.Length >= 3 && backgroundTriangles.Length >= 3)
            {
                foreach (var point in backgroundContour)
                    vertices.AddVert(new Vector2(rect.xMin + point.x * rect.width, rect.yMin + point.y * rect.height), color, Vector2.zero);
                for (int i = 0; i < backgroundTriangles.Length; i += 3)
                    vertices.AddTriangle(backgroundTriangles[i], backgroundTriangles[i + 1], backgroundTriangles[i + 2]);
                return;
            }
            float radius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            vertices.AddVert(rect.center, color, Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                var center = new Vector2(
                    corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                    corner < 2 ? rect.yMax - radius : rect.yMin + radius);
                for (int step = 0; step <= CornerSegments; step++)
                {
                    float angle = (corner * 90f + step * 90f / CornerSegments) * Mathf.Deg2Rad;
                    vertices.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                        color, Vector2.zero);
                }
            }
            int outlineCount = 4 * (CornerSegments + 1);
            for (int i = 0; i < outlineCount; i++)
                vertices.AddTriangle(0, i + 1, (i + 1) % outlineCount + 1);
        }
    }
}
