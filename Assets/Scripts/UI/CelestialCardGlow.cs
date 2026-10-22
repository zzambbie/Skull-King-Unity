using UnityEngine;
using UnityEngine.UI;

namespace SkullKing.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CelestialCardGlow : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect=GetPixelAdjustedRect();
            const int rings=18,segments=64;
            for(int ring=0;ring<=rings;ring++)
            for(int i=0;i<=segments;i++)
            {
                float radius=(float)ring/rings,angle=i*Mathf.PI*2/segments;
                Color tint=color;
                // A visible white core and a broad smooth falloff, with no rectangular surface.
                tint.a*=radius>=1?0:Mathf.Exp(-radius*radius*4.2f)*(1-Mathf.SmoothStep(.78f,1,radius));
                mesh.AddVert(new Vector3(rect.center.x+Mathf.Cos(angle)*rect.width*.5f*radius,
                    rect.center.y+Mathf.Sin(angle)*rect.height*.5f*radius),tint,Vector2.zero);
            }
            for(int ring=0;ring<rings;ring++)for(int i=0;i<segments;i++)
            {
                int v=ring*(segments+1)+i;
                mesh.AddTriangle(v,v+segments+1,v+1);
                mesh.AddTriangle(v+1,v+segments+1,v+segments+2);
            }
        }
    }
}
