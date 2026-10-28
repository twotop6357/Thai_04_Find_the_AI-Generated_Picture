using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    /// <summary>Replaceable, sprite-free circle or cross for a classification result.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ResultMarkGraphic : MaskableGraphic
    {
        [SerializeField] private bool isCorrect = true;
        public bool IsCorrect => isCorrect;

        public void SetResult(bool correct)
        {
            isCorrect = correct;
            color = correct ? new Color32(113, 171, 231, 255) : new Color32(233, 37, 16, 255);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * 0.43f;
            float thickness = radius * 0.24f;
            Vector2 center = rect.center;
            if (isCorrect)
            {
                const int segments = 64;
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2 / segments;
                    float b = (i + 1) * Mathf.PI * 2 / segments;
                    Vector2 first = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Vector2 second = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                    Quad(mesh, center + first * radius, center + second * radius,
                        center + second * (radius - thickness), center + first * (radius - thickness));
                }
            }
            else
            {
                float end = radius * 0.72f;
                Line(mesh, center + new Vector2(-end, -end), center + new Vector2(end, end), thickness);
                Line(mesh, center + new Vector2(-end, end), center + new Vector2(end, -end), thickness);
            }
        }

        private void Line(VertexHelper mesh, Vector2 from, Vector2 to, float width)
        {
            Vector2 normal = new Vector2(-(to - from).y, (to - from).x).normalized * width * 0.5f;
            Quad(mesh, from - normal, from + normal, to + normal, to - normal);
        }

        private void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, color, Vector2.zero);
            mesh.AddVert(b, color, Vector2.zero);
            mesh.AddVert(c, color, Vector2.zero);
            mesh.AddVert(d, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
