using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

namespace SkullKing.UI
{
    [Serializable]
    public sealed class CelestialPoseContour
    {
        public Sprite Sprite;
        public Vector2 VerticalRange;
        public Vector2[] LeftBounds;
        public Vector2[] RightBounds;
        public bool Valid => Sprite!=null&&LeftBounds!=null&&RightBounds!=null&&
            LeftBounds.Length>2&&LeftBounds.Length==RightBounds.Length;
    }

    /// <summary>Both neighbouring painted poses are warped to a shared moving silhouette before blending.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CelestialPoseMorphGraphic : MaskableGraphic
    {
        private CelestialPoseContour _source,_from,_to;
        private float _mix;
        public override Texture mainTexture => _source?.Sprite!=null?_source.Sprite.texture:Texture2D.whiteTexture;
        public void SetPose(CelestialPoseContour source,CelestialPoseContour from,CelestialPoseContour to,float mix)
        {
            bool textureChanged=_source?.Sprite!=source.Sprite;
            _source=source;_from=from;_to=to;_mix=Mathf.Clamp01(mix);
            raycastTarget=false;
            if(textureChanged)SetMaterialDirty();
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if(_source==null||!_source.Valid||_from==null||!_from.Valid||_to==null||!_to.Valid)return;
            int rows=_source.LeftBounds.Length-1;
            if(_from.LeftBounds.Length!=rows+1||_to.LeftBounds.Length!=rows+1)return;
            const int columns=16;
            var rect=GetPixelAdjustedRect();
            Vector4 uv=DataUtility.GetOuterUV(_source.Sprite);
            Vector2 vertical=Vector2.Lerp(_from.VerticalRange,_to.VerticalRange,_mix);
            for(int side=0;side<2;side++)
            {
                int start=mesh.currentVertCount;
                var source=side==0?_source.LeftBounds:_source.RightBounds;
                var from=side==0?_from.LeftBounds:_from.RightBounds;
                var to=side==0?_to.LeftBounds:_to.RightBounds;
                for(int y=0;y<=rows;y++)
                {
                    float v=(float)y/rows;
                    var target=Vector2.Lerp(from[y],to[y],_mix);
                    float sourceY=Mathf.Lerp(_source.VerticalRange.x,_source.VerticalRange.y,v);
                    float targetY=Mathf.Lerp(vertical.x,vertical.y,v);
                    for(int x=0;x<=columns;x++)
                    {
                        float u=(float)x/columns;
                        float sourceX=Mathf.Lerp(source[y].x,source[y].y,u);
                        float targetX=Mathf.Lerp(target.x,target.y,u);
                        mesh.AddVert(new Vector3(rect.xMin+targetX*rect.width,rect.yMin+targetY*rect.height),
                            color,new Vector2(Mathf.Lerp(uv.x,uv.z,sourceX),Mathf.Lerp(uv.y,uv.w,sourceY)));
                    }
                }
                for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
                {
                    int i=start+y*(columns+1)+x;
                    mesh.AddTriangle(i,i+columns+1,i+1);
                    mesh.AddTriangle(i+1,i+columns+1,i+columns+2);
                }
            }
        }
    }
}
