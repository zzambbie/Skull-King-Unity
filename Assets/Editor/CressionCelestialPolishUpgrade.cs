#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SkullKing.UI;

namespace SkullKing.Editor
{
    internal static class CressionCelestialPolishUpgrade
    {
        private const string WingPath="Assets/Art/Effects/WhiteWhale/seraph-embrace-poses-v4.png";
        public static int ApplyToScene(Scene scene)
        {
            if(scene.path!="Assets/Scenes/GameScene.unity")return 0;
            var roots=scene.GetRootGameObjects();
            var stamp=roots.SelectMany(r=>r.GetComponentsInChildren<CouncilThemeStamp>(true)).FirstOrDefault();
            if(stamp==null||stamp.CelestialRigRevision>=4)return 0;
            var ui=roots.SelectMany(r=>r.GetComponentsInChildren<UIBindingManager>(true)).FirstOrDefault();
            if(ui==null)throw new InvalidOperationException("UIBindingManager가 없습니다.");
            var binding=new SerializedObject(ui);
            var effect=binding.FindProperty("_whaleWingTransition").objectReferenceValue as WhaleWingTransition;
            if(effect==null)throw new InvalidOperationException("천상강림 연출 연결이 없습니다.");
            AssetDatabase.ImportAsset(WingPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(WingPath) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("새 날개 소재를 가져오지 못했습니다.");
            importer.textureType=TextureImporterType.Default;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            const string poseAtlas="Assets/Art/Effects/WhiteWhale/seraph-embrace-poses-v4.councilatlas";
            AssetDatabase.ImportAsset(poseAtlas,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var assets=AssetDatabase.LoadAllAssetsAtPath(poseAtlas).OfType<Sprite>().ToArray();
            var poses=new Sprite[6];
            poses[0]=AssetDatabase.LoadMainAssetAtPath(poseAtlas) as Sprite;
            for(int i=1;i<6;i++)poses[i]=assets.FirstOrDefault(s=>s.name=="Embrace"+i.ToString("00"));
            if(poses.Any(p=>p==null))throw new InvalidOperationException("날개 연속 자세 6개를 확인하세요.");
            var so=new SerializedObject(effect);
            var visual=so.FindProperty("_visualRoot").objectReferenceValue as RectTransform;
            if(visual==null)throw new InvalidOperationException("날개 시각 영역이 없습니다.");
            var rigTransform=visual.Find("SeraphMeshRig");
            if(rigTransform==null)
            {
                var go=new GameObject("SeraphMeshRig",typeof(RectTransform),typeof(CelestialWingRig));
                Undo.RegisterCreatedObjectUndo(go,"Seraph feather rig");
                go.layer=visual.gameObject.layer;go.transform.SetParent(visual,false);
                rigTransform=go.transform;
                var rect=(RectTransform)rigTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
                rect.offsetMin=rect.offsetMax=Vector2.zero;
            }
            var rig=rigTransform.GetComponent<CelestialWingRig>();
            var rigSO=new SerializedObject(rig);
            // Keep archived mesh layers for recovery, but never render them alongside the painted pair.
            foreach(var legacy in rigTransform.GetComponentsInChildren<CelestialWingGraphic>(true))
                legacy.gameObject.SetActive(false);
            var frames=rigSO.FindProperty("_poseFrames");frames.arraySize=6;
            for(int i=0;i<6;i++)frames.GetArrayElementAtIndex(i).objectReferenceValue=poses[i];
            rigSO.FindProperty("_poseScale").floatValue=2.25f;
            foreach(string name in new[]{"PaintedPoseFrom","PaintedPoseTo"})
            {
                var child=rigTransform.Find(name);
                if(child==null)
                {
                    var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                    Undo.RegisterCreatedObjectUndo(go,"Painted angel poses");
                    go.layer=visual.gameObject.layer;go.transform.SetParent(rigTransform,false);child=go.transform;
                }
                var graphic=child.GetComponent<Image>();graphic.raycastTarget=false;graphic.preserveAspect=true;
                graphic.sprite=poses[0];graphic.color=Color.clear;
                rigSO.FindProperty(name=="PaintedPoseFrom"?"_poseFrom":"_poseTo").objectReferenceValue=graphic;
            }
            var contours=BuildContours(poses);
            var data=rigSO.FindProperty("_contours");data.arraySize=contours.Length;
            for(int i=0;i<contours.Length;i++)
            {
                var entry=data.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Sprite").objectReferenceValue=contours[i].Sprite;
                entry.FindPropertyRelative("VerticalRange").vector2Value=contours[i].VerticalRange;
                WriteBounds(entry.FindPropertyRelative("LeftBounds"),contours[i].LeftBounds);
                WriteBounds(entry.FindPropertyRelative("RightBounds"),contours[i].RightBounds);
            }
            foreach(string name in new[]{"MorphPoseFrom","MorphPoseTo"})
            {
                var child=rigTransform.Find(name);
                if(child==null)
                {
                    var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(CelestialPoseMorphGraphic));
                    Undo.RegisterCreatedObjectUndo(go,"Continuous wing contour");
                    go.layer=visual.gameObject.layer;go.transform.SetParent(rigTransform,false);child=go.transform;
                }
                var graphic=child.GetComponent<CelestialPoseMorphGraphic>();graphic.raycastTarget=false;graphic.color=Color.clear;
                rigSO.FindProperty(name=="MorphPoseFrom"?"_morphFrom":"_morphTo").objectReferenceValue=graphic;
            }
            foreach(string name in new[]{"PaintedPoseFrom","PaintedPoseTo"})rigTransform.Find(name).gameObject.SetActive(false);
            rigSO.ApplyModifiedProperties();
            foreach(string field in new[]{"_leftWing","_rightWing"})
            {
                var old=so.FindProperty(field).objectReferenceValue as Image;
                if(old!=null){Undo.RecordObject(old,"Hide rigid wing image");old.enabled=false;}
            }
            var aura=visual.Find("CardAura");
            if(aura==null)
            {
                var go=new GameObject("CardAura",typeof(RectTransform),typeof(CanvasRenderer),typeof(CelestialCardGlow));
                Undo.RegisterCreatedObjectUndo(go,"Soft central bloom");
                go.transform.SetParent(visual,false);aura=go.transform;
            }
            var glow=aura.GetComponent<CelestialCardGlow>();glow.raycastTarget=false;
            AssetDatabase.ImportAsset("Assets/Shaders/CelestialBloom.shader",ImportAssetOptions.ForceSynchronousImport);
            var shader=Shader.Find("CROSSION/UI/Celestial Bloom");
            if(shader==null)throw new InvalidOperationException("천상강림 발광 셰이더를 가져오지 못했습니다.");
            const string materialPath="Assets/Art/Effects/WhiteWhale/CelestialBloom.mat";
            var bloom=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(bloom==null){bloom=new Material(shader){name="CelestialBloom"};AssetDatabase.CreateAsset(bloom,materialPath);}
            glow.material=bloom;glow.enabled=true;aura.gameObject.SetActive(true);
            aura.SetAsLastSibling();
            so.FindProperty("_wingRig").objectReferenceValue=rig;
            so.FindProperty("_cardGlow").objectReferenceValue=glow;
            so.FindProperty("_cardFocusArea").objectReferenceValue=binding.FindProperty("_trickArea").objectReferenceValue;
            so.FindProperty("_closeSeconds").floatValue=2.1f;
            so.FindProperty("_coveredSeconds").floatValue=.5f;
            so.FindProperty("_openSeconds").floatValue=1.15f;
            so.ApplyModifiedProperties();
            var receipt=new SerializedObject(stamp);
            receipt.FindProperty("_celestialRigRevision").intValue=4;receipt.ApplyModifiedProperties();
            Debug.Log("[천상강림] 윤곽 메시 연속 변형, 크기 약 30% 축소, 중앙 가산 발광 적용. 타이틀과 게임 규칙은 유지.");
            return 1;
        }
        private static void WriteBounds(SerializedProperty property,Vector2[] values)
        {
            property.arraySize=values.Length;
            for(int i=0;i<values.Length;i++)property.GetArrayElementAtIndex(i).vector2Value=values[i];
        }
        private static CelestialPoseContour[] BuildContours(Sprite[] poses)
        {
            // Analyse existing alpha in memory only. No source pixels are edited or regenerated.
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(WingPath)))throw new InvalidOperationException("날개 PNG를 읽지 못했습니다.");
                var pixels=texture.GetPixels32();
                var output=new CelestialPoseContour[poses.Length];
                const int rows=40;
                for(int i=0;i<poses.Length;i++)
                {
                    var rect=poses[i].rect;int ox=(int)rect.x,oy=(int)rect.y,w=(int)rect.width,h=(int)rect.height;
                    int minY=h-1,maxY=0;
                    for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                        if(pixels[(oy+y)*texture.width+ox+x].a>=80){minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                    if(maxY<=minY)throw new InvalidOperationException("날개 윤곽이 비어 있습니다: "+i);
                    minY=Mathf.Max(0,minY-3);maxY=Mathf.Min(h-1,maxY+3);
                    var c=new CelestialPoseContour{Sprite=poses[i],VerticalRange=new Vector2((float)minY/h,(float)maxY/h),
                        LeftBounds=new Vector2[rows+1],RightBounds=new Vector2[rows+1]};
                    for(int row=0;row<=rows;row++)
                    {
                        int y=Mathf.RoundToInt(Mathf.Lerp(minY,maxY,(float)row/rows));
                        for(int side=0;side<2;side++)
                        {
                            int start=side*w/2,end=(side+1)*w/2,lo=end,hi=start;
                            for(int sy=Mathf.Max(minY,y-2);sy<=Mathf.Min(maxY,y+2);sy++)
                                for(int x=start;x<end;x++)if(pixels[(oy+sy)*texture.width+ox+x].a>=80)
                                {lo=Mathf.Min(lo,x);hi=Mathf.Max(hi,x);}
                            Vector2 bounds;
                            if(hi<lo){float middle=side==0?.35f:.65f;bounds=new Vector2(middle-.002f,middle+.002f);}
                            else bounds=new Vector2((float)Mathf.Max(start,lo-3)/w,(float)Mathf.Min(end,hi+4)/w);
                            (side==0?c.LeftBounds:c.RightBounds)[row]=bounds;
                        }
                    }
                    output[i]=c;
                }
                return output;
            }
            finally{UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
#endif
