using UnityEngine;
using UnityEngine.UI;

namespace SkullKing.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/CROSSION Dimensional Table")]
    public sealed class CouncilTableGraphic : MaskableGraphic
    {
        [SerializeField] private Color _bottom = new Color(.018f,.028f,.055f);
        [SerializeField] private Color _top = new Color(.045f,.105f,.14f);
        [SerializeField] private Color _jade = new Color(.12f,.65f,.55f,.15f);
        [SerializeField] private Color _violet = new Color(.46f,.2f,.7f,.16f);
        [SerializeField] private Color _gold = new Color(.85f,.65f,.33f,.18f);
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width <= 0 || r.height <= 0) return;
            mesh.AddVert(new Vector3(r.xMin,r.yMin),_bottom*color,Vector2.zero);
            mesh.AddVert(new Vector3(r.xMin,r.yMax),_top*color,Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax,r.yMax),_top*color,Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax,r.yMin),_bottom*color,Vector2.zero);
            mesh.AddTriangle(0,1,2); mesh.AddTriangle(0,2,3);
            Glow(mesh,new Vector2(r.xMin+r.width*.18f,r.center.y),r.width*.43f,_jade);
            Glow(mesh,new Vector2(r.xMax-r.width*.18f,r.center.y+r.height*.1f),r.width*.42f,_violet);
            Vector2 center = r.center + new Vector2(0,r.height*.025f);
            float radius = Mathf.Min(r.width,r.height)*.19f;
            Ring(mesh,center,radius,1.5f,_gold);
            Ring(mesh,center,radius*1.08f,.8f,new Color(.38f,.8f,.72f,.12f));
            Ring(mesh,center,radius*1.18f,.65f,new Color(.75f,.6f,.4f,.08f));
            for (int i=0;i<24;i++)
            {
                float a=i*Mathf.PI*2/24;
                Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                Line(mesh,center+d*radius*1.1f,center+d*radius*(i%3==0?1.17f:1.13f),1,_gold);
            }
            for (float y=r.yMin+5;y<r.yMax;y+=8)
                Line(mesh,new Vector2(r.xMin,y),new Vector2(r.xMax,y),.55f,new Color(.6f,.75f,.85f,.017f));
        }
        private void Glow(VertexHelper mesh, Vector2 center,float radius,Color tint)
        {
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;
                int n=mesh.currentVertCount;
                mesh.AddVert(center,tint*color,Vector2.zero);
                Color outside=tint*color; outside.a=0;
                mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,outside,Vector2.zero);
                mesh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,outside,Vector2.zero);
                mesh.AddTriangle(n,n+1,n+2);
            }
        }
        private void Ring(VertexHelper mesh,Vector2 center,float radius,float width,Color tint)
        {
            for(int i=0;i<96;i++)
            {
                float a=i*Mathf.PI*2/96,b=(i+1)*Mathf.PI*2/96;
                Line(mesh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                    center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width,tint);
            }
        }
        private void Line(VertexHelper mesh,Vector2 from,Vector2 to,float width,Color tint)
        {
            var delta=to-from;
            var normal=new Vector2(-delta.y,delta.x).normalized*width*.5f;
            int n=mesh.currentVertCount;
            mesh.AddVert(from-normal,tint*color,Vector2.zero); mesh.AddVert(from+normal,tint*color,Vector2.zero);
            mesh.AddVert(to+normal,tint*color,Vector2.zero); mesh.AddVert(to-normal,tint*color,Vector2.zero);
            mesh.AddTriangle(n,n+1,n+2); mesh.AddTriangle(n,n+2,n+3);
        }
    }
}
