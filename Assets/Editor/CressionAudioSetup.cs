#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SkullKing.Audio;
using SkullKing.Core;
using SkullKing.UI;

namespace SkullKing.Editor
{
    /// <summary>One-time authoring helper. All generated UI and audio references are saved scene objects.</summary>
    public static class CressionAudioSetup
    {
        private const string Menu = "CROSSION/Audio/Apply Temporary Audio Setup";
        private static readonly string[] Scenes = { "Assets/Scenes/LobbyScene.unity", "Assets/Scenes/GameScene.unity" };

        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("[Audio Setup] Play 모드를 종료하고 실행하세요."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Do not close a user's unsaved or untitled scene when they chose Don't Save.
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                { Debug.LogWarning("[Audio Setup] 열린 씬을 먼저 저장해 주세요. 변경하지 않았습니다."); return; }
            var previous = EditorSceneManager.GetSceneManagerSetup();
            string backup = "AudioDrafts/SceneBackups/" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            Directory.CreateDirectory(backup);
            foreach (string path in Scenes) File.Copy(path, Path.Combine(backup, Path.GetFileName(path)));
            try
            {
                AssetDatabase.Refresh();
                var sfxPrefab = MakeEffectsPrefab();
                SetupMusicPrefab("Lobby");
                SetupMusicPrefab("Gameplay");
                foreach (string path in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var roots = scene.GetRootGameObjects();
                    bool game = path.EndsWith("GameScene.unity");
                    if (!roots.Any(r => r.GetComponent<GameAudioPlayer>() != null))
                        PrefabUtility.InstantiatePrefab(sfxPrefab, scene);
                    var sources = roots.SelectMany(r => r.GetComponentsInChildren<AudioSource>(true)).ToArray();
                    var musicClip = Clip(game ? "Music/CROSSION_Gameplay" : "Music/CROSSION_Lobby");
                    var music = sources.FirstOrDefault(s => s.clip == musicClip);
                    if (music == null)
                    {
                        var p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Audio/BGM_" + (game ? "Gameplay" : "Lobby") + ".prefab");
                        music = ((GameObject)PrefabUtility.InstantiatePrefab(p, scene)).GetComponent<AudioSource>();
                    }
                    BindVolume(music, AudioVolumeBinding.Category.Music);
                    foreach (var button in roots.SelectMany(r => r.GetComponentsInChildren<Button>(true)))
                        if (button.GetComponent<ButtonClickSound>() == null) button.gameObject.AddComponent<ButtonClickSound>();
                    foreach (var panel in roots.SelectMany(r => r.GetComponentsInChildren<SettingsPanelController>(true)))
                        SetupSliders(panel);
                    if (game)
                    {
                        var manager = roots.SelectMany(r => r.GetComponentsInChildren<GameManager>(true)).First();
                        var events = manager.GetComponent<GameAudioEvents>() ?? manager.gameObject.AddComponent<GameAudioEvents>();
                        SetReference(events, "_gameManager", manager);
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("씬 저장 실패: " + path);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("[Audio Setup] 완료: 로비/게임 BGM, 효과음, 버튼 소리, BGM·SFX 슬라이더. 백업: " + backup);
            }
            catch (Exception e)
            {
                Debug.LogError("[Audio Setup] 적용 중 오류. 일부 변경됐을 수 있습니다. 원본 씬 백업: " + backup + "\n" + e);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static AudioClip Clip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + name + ".wav");
            if (clip == null) throw new InvalidOperationException("음원 Import가 끝났는지 확인하세요: " + name);
            return clip;
        }

        private static GameObject MakeEffectsPrefab()
        {
            const string path = "Assets/Prefabs/Audio/SFX_Player.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing; // Preserve later user changes to clips and gain.
            var root = new GameObject("SFX_Player");
            try
            {
                var source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = .65f;
                source.bypassReverbZones = true;
                BindVolume(source, AudioVolumeBinding.Category.Effects);
                var player = root.AddComponent<GameAudioPlayer>();
                string[] fields = { "_click", "_card", "_trickWin", "_roundSuccess", "_roundFailure" };
                string[] clips = { "UI_Click", "Card_Place", "Trick_Win", "Round_Success", "Round_Failure" };
                for (int i = 0; i < fields.Length; i++) SetReference(player, fields[i], Clip("SFX/" + clips[i]));
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void SetupMusicPrefab(string kind)
        {
            string path = "Assets/Prefabs/Audio/BGM_" + kind + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                BindVolume(root.GetComponent<AudioSource>(), AudioVolumeBinding.Category.Music);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BindVolume(AudioSource source, AudioVolumeBinding.Category category)
        {
            var binding = source.GetComponent<AudioVolumeBinding>();
            bool created = binding == null;
            if (created) binding = source.gameObject.AddComponent<AudioVolumeBinding>();
            var so = new SerializedObject(binding);
            so.FindProperty("_category").enumValueIndex = (int)category;
            if (created) so.FindProperty("_baseVolume").floatValue = source.volume;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupSliders(SettingsPanelController panel)
        {
            var so = new SerializedObject(panel);
            var master = so.FindProperty("_masterVolumeSlider").objectReferenceValue as Slider;
            var value = so.FindProperty("_masterVolumeValueText").objectReferenceValue as TMP_Text;
            if (master == null || value == null) throw new InvalidOperationException("SettingsPanel의 Master Slider/Value Text 연결이 필요합니다.");
            // Re-running must not overwrite the user's later layout changes.
            if (so.FindProperty("_musicVolumeSlider").objectReferenceValue != null &&
                so.FindProperty("_effectsVolumeSlider").objectReferenceValue != null) return;
            var window = master.transform.parent as RectTransform;
            if (window == null) throw new InvalidOperationException("설정 슬라이더의 부모 RectTransform을 확인하세요.");
            window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(window.rect.height, 720f));
            // Keep the original controls, font and slider styling; only author the additional rows.
            var music = UnityEngine.Object.Instantiate(master, window);
            music.name = "BGMVolumeSlider";
            music.onValueChanged = new Slider.SliderEvent();
            var effects = UnityEngine.Object.Instantiate(master, window);
            effects.name = "SFXVolumeSlider";
            effects.onValueChanged = new Slider.SliderEvent();
            var musicValue = UnityEngine.Object.Instantiate(value, window);
            musicValue.name = "BGMVolumeValueText";
            var effectsValue = UnityEngine.Object.Instantiate(value, window);
            effectsValue.name = "SFXVolumeValueText";
            Row(master, value, 180f, "Master", window);
            Row(music, musicValue, 60f, "BGM", window);
            Row(effects, effectsValue, -60f, "SFX", window);
            SetReference(panel, "_musicVolumeSlider", music);
            SetReference(panel, "_musicVolumeValueText", musicValue);
            SetReference(panel, "_effectsVolumeSlider", effects);
            SetReference(panel, "_effectsVolumeValueText", effectsValue);
            var fullscreen = so.FindProperty("_fullscreenToggle").objectReferenceValue as Toggle;
            var reset = so.FindProperty("_resetButton").objectReferenceValue as Button;
            if (fullscreen != null) Position(fullscreen.transform as RectTransform, 0, -180f);
            if (reset != null) Position(reset.transform as RectTransform, 0, -275f);
            // Remove only redundant old volume captions, not other user-authored labels.
            foreach (var label in window.GetComponentsInChildren<TMP_Text>(true))
                if (label != value && label != musicValue && label != effectsValue &&
                    !label.name.EndsWith("AudioCaption") &&
                    (label.text.Trim() == "전체 볼륨" || label.text.Trim() == "Master Volume"))
                    label.gameObject.SetActive(false);
        }

        private static void Row(Slider slider, TMP_Text value, float y, string caption, RectTransform window)
        {
            Position(slider.transform as RectTransform, -25, y);
            (slider.transform as RectTransform).sizeDelta = new Vector2(310, 25);
            Position(value.rectTransform, 200, y);
            value.rectTransform.sizeDelta = new Vector2(85, 40);
            value.fontSize = Mathf.Min(value.fontSize, 24f);
            value.text = "100%";
            var label = UnityEngine.Object.Instantiate(value, window);
            label.name = caption + "AudioCaption";
            label.text = caption;
            label.alignment = TextAlignmentOptions.Left;
            label.rectTransform.sizeDelta = new Vector2(310, 35);
            Position(label.rectTransform, -25, y + 40);
        }
        private static void Position(RectTransform rect, float x, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y);
        }
    }
}
#endif
