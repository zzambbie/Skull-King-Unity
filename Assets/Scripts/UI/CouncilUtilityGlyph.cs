using UnityEngine;
using UnityEngine.UI;

namespace SkullKing.UI
{
    /// <summary>Transparent, mesh-only utility symbols: no atlas rectangle behind the gold silhouette.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CouncilUtilityGlyph : MaskableGraphic
    {
        public enum Symbol { Settings, Exit }
        [SerializeField] private Symbol _symbol;
        public void SetSymbol(Symbol symbol)
        { _symbol=symbol;raycastTarget=false;SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if(_symbol==Symbol.Settings)
            {
                const int steps=80;
                for(int i=0;i<steps;i++)
                {
                    float a=i*Mathf.PI*2/steps,b=(i+1)*Mathf.PI*2/steps;
                    // Ten broad teeth, with the center left genuinely transparent.
                    float r=(i%8<4)?.45f:.35f;
                    Quad(mesh,Polar(a,.16f),Polar(a,r),Polar(b,r),Polar(b,.16f));
                }
            }
            else
            {
                // Open doorway and an outward arrow; no enclosing image tile or decorative rectangle.
                Quad(mesh,new Vector2(-.38f,-.39f),new Vector2(-.38f,.39f),
                    new Vector2(-.27f,.39f),new Vector2(-.27f,-.39f));
                Quad(mesh,new Vector2(-.27f,.39f),new Vector2(-.02f,.32f),
                    new Vector2(-.02f,-.32f),new Vector2(-.27f,-.39f));
                Quad(mesh,new Vector2(.01f,-.045f),new Vector2(.01f,.045f),
                    new Vector2(.29f,.045f),new Vector2(.29f,-.045f));
                Triangle(mesh,new Vector2(.23f,-.19f),new Vector2(.23f,.19f),new Vector2(.46f,0));
            }
        }
        private static Vector2 Polar(float angle,float radius)
            => new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
        private Vector3 Point(Vector2 value)
        {
            Rect rect=GetPixelAdjustedRect();
            return rect.center+value*Mathf.Min(rect.width,rect.height);
        }
        private void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            int start=mesh.currentVertCount;
            mesh.AddVert(Point(a),color,Vector2.zero);mesh.AddVert(Point(b),color,Vector2.zero);
            mesh.AddVert(Point(c),color,Vector2.zero);mesh.AddVert(Point(d),color,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
        private void Triangle(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c)
        {
            int start=mesh.currentVertCount;
            mesh.AddVert(Point(a),color,Vector2.zero);mesh.AddVert(Point(b),color,Vector2.zero);
            mesh.AddVert(Point(c),color,Vector2.zero);mesh.AddTriangle(start,start+1,start+2);
        }
    }
}
