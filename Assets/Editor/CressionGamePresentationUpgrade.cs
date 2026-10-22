#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SkullKing.UI;

namespace SkullKing.Editor
{
    internal static class CressionGamePresentationUpgrade
    {
        public static int ApplyToScene(Scene scene)
        {
            if(scene.path!="Assets/Scenes/GameScene.unity") return 0;
            var canvas=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true))
                .FirstOrDefault(c=>c.isRootCanvas&&c.name=="GameCanvas");
            if(canvas==null) throw new InvalidOperationException("GameCanvas가 없습니다.");
            var stamp=canvas.GetComponent<CouncilThemeStamp>();
            if(stamp!=null&&stamp.GameVisualsRevision>=1) return 0;
            CressionCardEffectChecks.RunChecks();
            var table=canvas.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t=>t.name=="TableBackground");
            if(table==null) throw new InvalidOperationException("TableBackground가 없습니다.");
            var existing=table.Find("DimensionalTable");
            if(existing==null)
            {
                var go=new GameObject("DimensionalTable",typeof(RectTransform),typeof(CanvasRenderer),typeof(CouncilTableGraphic));
                Undo.RegisterCreatedObjectUndo(go,"Dimensional table surface");
                go.layer=table.gameObject.layer;
                go.transform.SetParent(table,false); go.transform.SetAsFirstSibling();
                var rect=(RectTransform)go.transform; rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
                rect.offsetMin=rect.offsetMax=Vector2.zero;
                go.GetComponent<CouncilTableGraphic>().raycastTarget=false;
            }
            foreach(var panel in canvas.GetComponentsInChildren<CouncilPanelGraphic>(true))
            {
                if(panel.GetComponentInParent<SupportWindowsController>(true)!=null ||
                    panel.GetComponentInParent<SettingsPanelController>(true)!=null) continue;
                var settings=new SerializedObject(panel);
                settings.FindProperty("_textureStrength").floatValue=.035f;
                settings.ApplyModifiedProperties();
                bool info=panel.transform.parent.name.StartsWith("PlayerInfo_");
                if(panel.transform.parent.name=="CenterPlayAreaBg")
                {
                    panel.Configure(new Color(.02f,.045f,.07f,.12f),new Color(.05f,.1f,.15f,.18f),
                        new Color(.82f,.65f,.34f,.22f),14,1);
                    continue;
                }
                panel.Configure(info?new Color(.025f,.055f,.09f,.95f):new Color(.023f,.04f,.075f,.97f),
                    info?new Color(.08f,.18f,.22f,.95f):new Color(.08f,.13f,.22f,.97f),
                    new Color(.82f,.65f,.34f,.85f),14,1.5f);
            }
            var ui=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UIBindingManager>(true)).FirstOrDefault();
            if(ui==null) throw new InvalidOperationException("UIBindingManager가 없습니다.");
            var binding=new SerializedObject(ui);
            var effect=binding.FindProperty("_whaleWingTransition").objectReferenceValue as WhaleWingTransition;
            if(effect==null) throw new InvalidOperationException("기존 천상강림 연출을 찾지 못했습니다.");
            var so=new SerializedObject(effect);
            so.FindProperty("_cardFocusArea").objectReferenceValue=binding.FindProperty("_trickArea").objectReferenceValue;
            so.FindProperty("_closeSeconds").floatValue=1.05f;
            so.FindProperty("_coveredSeconds").floatValue=.35f;
            so.FindProperty("_openSeconds").floatValue=.7f;
            so.FindProperty("_cardWingSize").vector2Value=new Vector2(1.5f,1.2f);
            var visual=so.FindProperty("_visualRoot").objectReferenceValue as RectTransform;
            if(visual==null) throw new InvalidOperationException("천상강림 WingCurtain이 없습니다.");
            var aura=visual.Find("CardAura");
            if(aura==null)
            {
                var go=new GameObject("CardAura",typeof(RectTransform),typeof(CanvasRenderer),typeof(CelestialCardGlow));
                Undo.RegisterCreatedObjectUndo(go,"Local celestial glow");
                go.transform.SetParent(visual,false);go.transform.SetAsFirstSibling();aura=go.transform;
            }
            var glow=aura.GetComponent<CelestialCardGlow>();
            glow.raycastTarget=false;glow.color=new Color(1,1,1,0);
            so.FindProperty("_cardGlow").objectReferenceValue=glow;
            so.ApplyModifiedProperties();
            if(stamp==null) stamp=Undo.AddComponent<CouncilThemeStamp>(canvas.gameObject);
            var receipt=new SerializedObject(stamp);
            receipt.FindProperty("_gameVisualsRevision").intValue=1;
            receipt.ApplyModifiedProperties();
            Debug.Log("[CROSSION] GameScene 색감/질감 및 카드 중심 천상강림 적용. 규칙과 타이틀은 변경하지 않았습니다.");
            return 1;
        }
    }
}
#endif
