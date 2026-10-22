#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SkullKing.UI;

namespace SkullKing.Editor
{
    internal static class CressionCardEffectChecks
    {
        public static int RunChecks()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var root=new GameObject("Effect check",typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(root,scene);
                var rootRect=(RectTransform)root.transform;rootRect.sizeDelta=new Vector2(1920,1080);
                var effect=root.AddComponent<WhaleWingTransition>();
                var visual=Rect("Curtain",rootRect,Vector2.zero,new Vector2(1920,1080));
                var cover=Rect("Cover",visual,Vector2.zero,Vector2.zero).gameObject.AddComponent<Image>();
                var left=Rect("Left",visual,Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();
                var right=Rect("Right",visual,Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();
                var stack=Rect("Stack",rootRect,new Vector2(90,20),new Vector2(400,400));
                var card=Rect("Submitted",stack,new Vector2(0,50),new Vector2(120,175));
                card.localScale=Vector3.one*1.3f;
                var before=card.anchoredPosition;
                Ref(effect,"_visualRoot",visual);Ref(effect,"_innerCover",cover);
                Ref(effect,"_leftWing",left);Ref(effect,"_rightWing",right);
                Invoke(effect,"ConfigureCardFootprint",card,stack,Vector2.zero);
                Require(visual.sizeDelta.x>150&&visual.sizeDelta.x<220,"Card-local width");
                Require(visual.sizeDelta.y>225&&visual.sizeDelta.y<330,"Showcase and final-pose coverage");
                Require(cover.rectTransform.anchorMin==new Vector2(.5f,.5f)&&cover.rectTransform.anchorMax==new Vector2(.5f,.5f),"No stretched screen flash");
                Require(cover.rectTransform.sizeDelta==visual.sizeDelta,"Cover follows card bounds");
                Require(!cover.raycastTarget,"No full-screen input blocker");
                Invoke(effect,"SetClosingPose",0f);
                Require(Mathf.Abs(left.rectTransform.anchoredPosition.x)<200&&Mathf.Abs(right.rectTransform.anchoredPosition.x)<200,"Wings spawn by card, not screen edges");
                Require(left.color.a==0&&right.color.a==0,"Initial materialization transparency");
                Invoke(effect,"SetClosingPose",1f);
                Require(cover.color.a==0,"No rectangular flash at swap point");
                Require(Mathf.Abs(left.rectTransform.anchoredPosition.x)<30,"Wings close over card");
                Require(card.anchoredPosition==before,"Effect never moves submitted card");
                var glow=Rect("Aura",visual,Vector2.zero,Vector2.one).gameObject.AddComponent<CelestialCardGlow>();
                Ref(effect,"_cardGlow",glow);
                Invoke(effect,"SetClosingPose",0f);
                var dimSize=glow.rectTransform.sizeDelta;
                Require(glow.color.a==0,"Glow starts transparent");
                Invoke(effect,"SetClosingPose",1f);
                Require(glow.color.a>.99f&&glow.enabled&&glow.gameObject.activeSelf,"Glow is visible at swap");
                Require(glow.rectTransform.sizeDelta.x>dimSize.x,"Light swells continuously from the center");
                Require(glow.transform.GetSiblingIndex()==visual.childCount-1,"Glow draws above the feathers");
                return 14+RunMorphChecks(rootRect);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        private static int RunMorphChecks(RectTransform root)
        {
            var sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f));
            try
            {
                var rect=Rect("Morph check",root,Vector2.zero,new Vector2(100,100));
                var graphic=rect.gameObject.AddComponent<CelestialPoseMorphGraphic>();
                var a=new CelestialPoseContour{Sprite=sprite,VerticalRange=new Vector2(.1f,.9f),
                    LeftBounds=new[]{new Vector2(.1f,.3f),new Vector2(.1f,.3f),new Vector2(.1f,.3f)},
                    RightBounds=new[]{new Vector2(.6f,.9f),new Vector2(.6f,.9f),new Vector2(.6f,.9f)}};
                var b=new CelestialPoseContour{Sprite=sprite,VerticalRange=new Vector2(.1f,.9f),
                    LeftBounds=new[]{new Vector2(.3f,.5f),new Vector2(.3f,.5f),new Vector2(.3f,.5f)},
                    RightBounds=a.RightBounds};
                graphic.SetPose(a,a,b,.5f);
                using(var mesh=new VertexHelper())
                {
                    typeof(CelestialPoseMorphGraphic).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic)
                        .Invoke(graphic,new object[]{mesh});
                    var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,0);
                    Require(Mathf.Abs(vertex.position.x-(-30))<.01f,"Halfway silhouette actually moves, not just opacity");
                    Require(mesh.currentVertCount==102,"Two continuous wing meshes");
                    for(int i=0;i<mesh.currentVertCount;i++)
                    {
                        mesh.PopulateUIVertex(ref vertex,i);
                        Require(!float.IsNaN(vertex.position.x)&&!float.IsNaN(vertex.position.y),"Finite morph vertices");
                    }
                    Require(rect.localScale==Vector3.one&&rect.localRotation==Quaternion.identity,"No reversed wing transforms");
                }
                return 4;
            }
            finally{UnityEngine.Object.DestroyImmediate(sprite);}
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=pos;rect.sizeDelta=size;return rect;
        }
        private static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value)
        {var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        private static void Invoke(object target,string method,params object[] args)
            => typeof(WhaleWingTransition).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
        private static void Require(bool result,string description)
        {if(!result)throw new InvalidOperationException("[Card Effect Checks] "+description);}
    }
}
#endif
