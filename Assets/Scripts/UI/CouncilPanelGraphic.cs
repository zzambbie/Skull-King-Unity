using UnityEngine;
using UnityEngine.UI;

namespace SkullKing.UI
{
    /// <summary>Resolution-independent chamfered enamel panel. Colors and edges remain Inspector-editable.</summary>
    [AddComponentMenu("UI/CROSSION Council Panel")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CouncilPanelGraphic : MaskableGraphic
    {
        [SerializeField] private Color _bottom = new Color(.018f, .031f, .058f, .98f);
        [SerializeField] private Color _top = new Color(.055f, .09f, .14f, .98f);
        [SerializeField] private Color _gold = new Color(.73f, .54f, .27f, .95f);
        [SerializeField, Min(0)] private float _corner = 15;
        [SerializeField, Min(0)] private float _edge = 1.5f;
        [SerializeField] private bool _innerLine = true;
        [SerializeField, Range(0,.2f)] private float _textureStrength;

        protected override void OnEnable()
        {
            // Older scene instances predate RequireComponent; repair the rendering dependency before registration.
            if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }

        public void Configure(Color bottom, Color top, Color gold, float corner, float edge)
        {
            _bottom = bottom; _top = top; _gold = gold; _corner = corner; _edge = edge;
            raycastTarget = false;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0) return;
            float edge = Mathf.Clamp(_edge, 0, Mathf.Min(rect.width, rect.height) / 8);
            var outer = Octagon(rect, _corner);
            var innerRect = Inset(rect, edge);
            var inner = Octagon(innerRect, Mathf.Max(0, _corner - edge));
            Ring(mesh, outer, inner, _gold * color);
            int start = mesh.currentVertCount;
            for (int i = 0; i < inner.Length; i++)
            {
                float t = Mathf.InverseLerp(rect.yMin, rect.yMax, inner[i].y);
                mesh.AddVert(inner[i], Color.Lerp(_bottom, _top, t) * color, Vector2.zero);
            }
            for (int i = 1; i < inner.Length - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1);
            if (_textureStrength > 0 && rect.width > 50 && rect.height > 40)
            {
                for (float y = rect.yMin+14; y < rect.yMax-12; y += 7)
                {
                    int v = mesh.currentVertCount;
                    Color stripe = new Color(.65f,.8f,.9f,_textureStrength) * color;
                    float left = rect.xMin+_corner+5, right = rect.xMax-_corner-5;
                    mesh.AddVert(new Vector3(left,y),stripe,Vector2.zero);
                    mesh.AddVert(new Vector3(right,y),stripe,Vector2.zero);
                    mesh.AddVert(new Vector3(right,y+.6f),stripe,Vector2.zero);
                    mesh.AddVert(new Vector3(left,y+.6f),stripe,Vector2.zero);
                    mesh.AddTriangle(v,v+1,v+2); mesh.AddTriangle(v,v+2,v+3);
                }
            }
            if (_innerLine && rect.width > 36 && rect.height > 28)
            {
                var lineRect = Inset(rect, edge + 4);
                Color fine = _gold * color; fine.a *= .3f;
                Ring(mesh, Octagon(lineRect, Mathf.Max(0, _corner - edge - 3)),
                    Octagon(Inset(lineRect, .65f), Mathf.Max(0, _corner - edge - 3.65f)), fine);
            }
        }
        private static Rect Inset(Rect rect, float inset)
            => new Rect(rect.x + inset, rect.y + inset, Mathf.Max(0, rect.width - inset * 2), Mathf.Max(0, rect.height - inset * 2));
        private static Vector3[] Octagon(Rect r, float corner)
        {
            float c = Mathf.Clamp(corner, 0, Mathf.Min(r.width, r.height) * .35f);
            return new[] { new Vector3(r.xMin+c,r.yMax), new Vector3(r.xMax-c,r.yMax),
                new Vector3(r.xMax,r.yMax-c), new Vector3(r.xMax,r.yMin+c),
                new Vector3(r.xMax-c,r.yMin), new Vector3(r.xMin+c,r.yMin),
                new Vector3(r.xMin,r.yMin+c), new Vector3(r.xMin,r.yMax-c) };
        }
        private static void Ring(VertexHelper mesh, Vector3[] outside, Vector3[] inside, Color tint)
        {
            int start = mesh.currentVertCount;
            for (int i = 0; i < outside.Length; i++)
            { mesh.AddVert(outside[i], tint, Vector2.zero); mesh.AddVert(inside[i], tint, Vector2.zero); }
            for (int i = 0; i < outside.Length; i++)
            {
                int next = (i + 1) % outside.Length;
                mesh.AddTriangle(start+i*2, start+next*2, start+i*2+1);
                mesh.AddTriangle(start+next*2, start+next*2+1, start+i*2+1);
            }
        }
    }
}
