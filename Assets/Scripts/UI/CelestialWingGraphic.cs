using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

namespace SkullKing.UI
{
    /// <summary>Root-anchored, continuously deforming feather surface rather than a rigid translated picture.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CelestialWingGraphic : MaskableGraphic
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField,Range(12,48)] private int _columns=28;
        [SerializeField,Range(8,24)] private int _rows=14;
        private float _spread=1,_fold,_flap;
        public override Texture mainTexture => _sprite!=null ? _sprite.texture : Texture2D.whiteTexture;
        public void SetSprite(Sprite sprite) { _sprite=sprite;raycastTarget=false;SetAllDirty(); }
        public void SetPose(float spread,float fold,float flap)
        { _spread=Mathf.Clamp01(spread);_fold=Mathf.Clamp01(fold);_flap=flap;SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(_sprite==null)return;
            float width=rectTransform.rect.width,height=rectTransform.rect.height;
            Vector4 uv=DataUtility.GetOuterUV(_sprite);
            int cols=Mathf.Max(2,_columns),rows=Mathf.Max(2,_rows);
            for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++)
            {
                float u=(float)x/cols,v=(float)y/rows;
                float distance=Mathf.Clamp01((.97f-u)/.97f);
                float span=Mathf.Lerp(.17f,1,_spread);
                // Preserve a flat, readable feather silhouette. Never invert individual mesh columns.
                float px=(u-.97f)*width*span;
                float featherLag=Mathf.Sin(_flap-distance*2.2f)*Mathf.Sin(Mathf.PI*distance);
                float py=(v-.52f)*height*Mathf.Lerp(.65f,1,_spread)
                    +featherLag*height*.018f*(1-_fold)
                    -_fold*height*.045f*distance*distance;
                Color tint=color;
                float shade=1-_fold*distance*.16f;
                tint.r*=shade;tint.g*=shade;tint.b*=shade;
                mesh.AddVert(new Vector3(px,py,0),tint,new Vector2(Mathf.Lerp(uv.x,uv.z,u),Mathf.Lerp(uv.y,uv.w,v)));
            }
            for(int y=0;y<rows;y++)for(int x=0;x<cols;x++)
            {
                int i=y*(cols+1)+x;
                mesh.AddTriangle(i,i+cols+1,i+1);
                mesh.AddTriangle(i+1,i+cols+1,i+cols+2);
            }
        }
    }
}
