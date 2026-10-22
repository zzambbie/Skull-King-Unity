#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SkullKing.UI;
using SkullKing.Core;

namespace SkullKing.Editor
{
    internal static class CressionTitleUtilityIconsUpgrade
    {
        public static int ApplyToScene(Scene scene)
        {
            if(scene.path!="Assets/Scenes/LobbyScene.unity")return 0;
            var roots=scene.GetRootGameObjects();
            var stamp=roots.SelectMany(r=>r.GetComponentsInChildren<CouncilThemeStamp>(true)).FirstOrDefault();
            if(stamp==null||stamp.TitleUtilityIconsRevision>=3)return 0;
            var lobby=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyManager>(true)).FirstOrDefault();
            var settings=roots.SelectMany(r=>r.GetComponentsInChildren<SettingsPanelController>(true)).FirstOrDefault();
            if(lobby==null||settings==null)throw new InvalidOperationException("타이틀 버튼 연결을 확인하세요.");
            var menu=new SerializedObject(lobby).FindProperty("_mainMenuPanel").objectReferenceValue as GameObject;
            if(menu==null)throw new InvalidOperationException("MainMenuPanel이 없습니다.");
            var buttons=menu.GetComponentsInChildren<Button>(true);
            var single=Find(buttons,lobby,"OpenSinglePlayerSetup");
            var exit=Find(buttons,lobby,"QuitGame");
            var setting=Find(buttons,settings,"Open");
            if(single==null||exit==null||setting==null)throw new InvalidOperationException("싱글/설정/종료 버튼 이벤트를 확인하세요.");
            Canvas.ForceUpdateCanvases();
            AddIcon(single,setting,LoadCutout("Assets/Art/UI/Council/settings-gold-cutout-v3.png"));
            AddIcon(single,exit,LoadCutout("Assets/Art/UI/Council/exit-gold-cutout-v3.png"));
            var so=new SerializedObject(stamp);so.FindProperty("_titleUtilityIconsRevision").intValue=3;so.ApplyModifiedProperties();
            Debug.Log("[Council UI] 평면 도형 대신 원래 금빛 그림 질감의 투명 아이콘 적용. 버튼 배치/기능 유지.");
            return 1;
        }
        private static Sprite LoadCutout(string path)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("아이콘 소재가 없습니다: "+path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(sprite==null)throw new InvalidOperationException("아이콘 Sprite를 확인하세요: "+path);
            return sprite;
        }
        private static void AddIcon(Button reference,Button target,Sprite sprite)
        {
            var source=reference.transform.Find("CouncilVisual") as RectTransform;
            var visual=target.transform.Find("CouncilVisual") as RectTransform;
            if(source==null||visual==null)throw new InvalidOperationException("CouncilVisual이 없습니다.");
            var refIcon=source.Find("Icon") as RectTransform;
            var refLabel=source.Find("CouncilLabel") as RectTransform;
            var label=visual.Find("CouncilLabel") as RectTransform;
            if(refIcon==null||refLabel==null||label==null)throw new InvalidOperationException("메뉴 글자/아이콘 연결이 없습니다.");
            var icon=visual.Find("Icon") as RectTransform;
            if(icon==null)
            {
                var go=new GameObject("Icon",typeof(RectTransform),typeof(CanvasRenderer));
                Undo.RegisterCreatedObjectUndo(go,"Title utility glyph");go.transform.SetParent(visual,false);icon=(RectTransform)go.transform;
            }
            Undo.RecordObject(icon.gameObject,"Show title glyph");icon.gameObject.SetActive(true);
            var image=icon.GetComponent<Image>();
            if(image==null)image=Undo.AddComponent<Image>(icon.gameObject);
            Undo.RecordObject(image,"Restore painted gold artwork");
            image.enabled=true;image.sprite=sprite;image.color=Color.white;
            image.preserveAspect=true;image.raycastTarget=false;image.material=null;
            var clean=icon.Find("CleanGlyph") as RectTransform;
            if(clean!=null){Undo.RecordObject(clean.gameObject,"Hide flat glyph");clean.gameObject.SetActive(false);}
            float width=visual.rect.width/Mathf.Max(1,source.rect.width);
            float height=visual.rect.height/Mathf.Max(1,source.rect.height);
            CopyRect(refIcon,icon,width,height);CopyRect(refLabel,label,width,height);
        }
        private static void CopyRect(RectTransform from,RectTransform to,float width,float height)
        {
            Undo.RecordObject(to,"Align utility icon and label");
            to.anchorMin=from.anchorMin;to.anchorMax=from.anchorMax;to.pivot=from.pivot;
            to.anchoredPosition=Vector2.Scale(from.anchoredPosition,new Vector2(width,height));
            to.sizeDelta=Vector2.Scale(from.sizeDelta,new Vector2(width,height));
            to.localScale=from.localScale;
        }
        private static Button Find(Button[] buttons,UnityEngine.Object target,string method)
            => buttons.FirstOrDefault(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount())
                .Any(i=>b.onClick.GetPersistentTarget(i)==target&&b.onClick.GetPersistentMethodName(i)==method));
    }
}
#endif
