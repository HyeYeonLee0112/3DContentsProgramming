using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blackout.EditorTools
{
    // Fixed environmental signage, not a runtime language-selection system.
    internal static class BlackoutCargoKoreanSigns
    {
        internal const string FontPath = "Assets/BLACKOUT/UI/Fonts/Pretendard/Pretendard-SemiBold.ttf";
        private const string MaterialPath = BlackoutCargoBuilder.Art + "/Materials/Sign_Pretendard-SemiBold.mat";
        private static readonly Dictionary<string, string> Korean = new Dictionary<string, string>
        {
            { "02 / CONTAINER TRANSIT", "02 / 컨테이너 통로" },
            { "SECURITY 03   >", "보안 구역 03   >" },
            { "CHECKPOINT 03  /  18 m  >", "보안 검문소 03 / 18 m  >" },
            { "CARGO INSPECTION / AUTHORIZED STAFF", "화물 검사 / 관계자 외 출입 금지" },
            { "03  /  SECURITY CHECKPOINT", "03 / 보안 검문소" },
            { "POWER REQUIRED   /   AUTOMATED DEFENCE LINKED", "전력 필요 / 공급 시 포탑도 가동" },
            { "FREIGHT LIFT 04\nNEXT SECTOR / SEALED", "04 화물 승강기\n다음 구역 / 폐쇄" },
            { "BLACKOUT / CARGO TERMINAL", "블랙아웃 / 화물 터미널" },
            { "MAX. GROSS  30,480 KG\nTARE             3,740 KG", "최대 총중량 30,480 kg\n자체 중량 3,740 kg" },
            { "SECURITY", "보안" },
            { "INSERT 1 CELL\nDOOR + TURRET", "전력 셀 1개 투입\n보안문·포탑 연동" },
            { "OFFLINE / LOCKED", "전력 없음 / 잠김" },
            { "FREIGHT LIFT", "화물 승강기" },
            { "POWER REQUIRED   0 / 2", "필요 전력   0 / 2" },
            { "LIFT  0 / 2", "승강기  0 / 2" },
            { "WORKLIGHTS\nPOWER  0 / 1", "작업등\n전력  0 / 1" },
            { "CARGO ACCESS  >", "화물 통로  >" },
            { "CARGO TRANSFER\nAUTHORIZED PERSONNEL", "화물 이송 구역\n관계자 전용" },
            { "CARGO TERMINAL  /  STAFF ACCESS", "화물 터미널 / 직원 출입구" },
            { "LOADING BAY", "화물 하역장" }
        };

        internal static string Translate(string text) => Korean.TryGetValue(text, out var translated) ? translated : text;
        internal static bool HasKorean(string text) => text.Any(c => c >= '\uAC00' && c <= '\uD7A3');

        [MenuItem("BLACKOUT/Cargo Station/5 Apply Korean signs")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != BlackoutCargoBuilder.ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open CargoStation in Edit Mode first.");
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var shader = Shader.Find("BLACKOUT/Depth Tested Sign");
            if (!font || !shader) throw new InvalidOperationException("Korean font or sign shader is missing.");
            string glyphs = new string(string.Concat(Korean.Values).Where(c => !char.IsWhiteSpace(c)).Distinct().ToArray());
            var missing = glyphs.Where(c => !font.HasCharacter(c)).ToArray();
            if (missing.Length != 0) throw new InvalidOperationException("Font missing glyphs: " + new string(missing));
            font.RequestCharactersInTexture(glyphs, 96, FontStyle.Normal);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, MaterialPath); }
            material.shader = shader; material.mainTexture = font.material.mainTexture; material.color = Color.white;

            // Update the reusable prefabs first; the original Intro prefab is intentionally excluded.
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { BlackoutCargoBuilder.Prefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var label in root.GetComponentsInChildren<TextMesh>(true)) SetLabel(label, font, material);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }

            var texts = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TextMesh>(true)).ToArray();
            foreach (var label in texts) SetLabel(label, font, material);
            // Atlas allocation can grow while loading the prefab contents.
            material.mainTexture = font.material.mainTexture;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Validate();
        }

        private static void SetLabel(TextMesh label, Font font, Material material)
        {
            string translated = Translate(label.text);
            if (!HasKorean(translated)) return; // Equipment IDs stay in their existing Latin font.
            var renderer = label.GetComponent<MeshRenderer>();
            var holder = label.transform.parent;
            var plate = holder.Cast<Transform>().FirstOrDefault(t => t.name == "Plate" || t.name.EndsWith("_Plate"));
            Vector2 available;
            if (plate) available = new Vector2(plate.localScale.x * .88f, plate.localScale.y * .72f);
            else if (holder.name == "ServiceMark") available = new Vector2(3.24f, .15f);
            else available = Vector2.Scale(renderer.localBounds.size, label.transform.localScale);
            label.font = font; label.fontSize = 96; label.text = translated;
            label.transform.localScale = Vector3.one;
            renderer.sharedMaterial = material;
            font.RequestCharactersInTexture(translated, 96, FontStyle.Normal);
            Vector3 size = renderer.localBounds.size;
            float fit = Mathf.Min(available.x / Mathf.Max(size.x, .001f), available.y / Mathf.Max(size.y, .001f));
            label.transform.localScale = Vector3.one * fit;
            EditorUtility.SetDirty(label); EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(label.transform);
            if (PrefabUtility.IsPartOfPrefabInstance(label))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(label.transform);
            }
        }

        [MenuItem("BLACKOUT/Cargo Station/6 Validate Korean signs")]
        public static void Validate()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != BlackoutCargoBuilder.ScenePath) throw new InvalidOperationException("Open CargoStation first.");
            var texts = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TextMesh>(true)).ToArray();
            var report = new Report { koreanLabels = texts.Count(t => HasKorean(t.text)) };
            var issues = new List<string>();
            foreach (var text in texts)
            {
                if (Korean.ContainsKey(text.text)) issues.Add("Untranslated: " + text.text);
                if (!HasKorean(text.text)) continue;
                if (AssetDatabase.GetAssetPath(text.font) != FontPath) issues.Add("Wrong font: " + text.transform.parent.name);
                foreach (char c in text.text.Where(c => !char.IsWhiteSpace(c)).Distinct())
                    if (!text.font.HasCharacter(c)) issues.Add("Missing glyph: " + c);
                var plate = text.transform.parent.Cast<Transform>().FirstOrDefault(t => t.name == "Plate" || t.name.EndsWith("_Plate"));
                var bounds = text.GetComponent<MeshRenderer>().localBounds.size;
                var size = Vector3.Scale(bounds, text.transform.localScale);
                if (plate && (size.x > plate.localScale.x * .91f || size.y > plate.localScale.y * .76f))
                    issues.Add("Overflow: " + text.transform.parent.name);
                // Unity defers mesh generation for inactive labels after a scene reload.
                if (text.gameObject.activeInHierarchy && (size.x < .01f || size.y < .01f))
                    issues.Add("Empty text mesh: " + text.transform.parent.name);
            }
            report.issues = issues.Distinct().ToArray();
            Directory.CreateDirectory(BlackoutCargoVerification.Output);
            File.WriteAllText(BlackoutCargoVerification.Output + "/korean-sign-validation.json", JsonUtility.ToJson(report, true));
            Debug.Log("[Korean signs] " + JsonUtility.ToJson(report));
            if (report.issues.Length != 0) throw new InvalidOperationException("Korean signage validation failed; inspect the report.");
        }
        [Serializable] private sealed class Report
        {
            public string font = "Pretendard SemiBold";
            public int koreanLabels;
            public string[] issues;
        }
    }
}
