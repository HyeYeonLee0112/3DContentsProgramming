using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Blackout.EditorTools
{
    // Measures source prefabs and renders real Unity previews before scene assembly.
    internal static class BlackoutIntroAudit
    {
        internal const string Kit = "Assets/ThirdParty/SciFi Warehouse Kit/Prefabs/";
        internal const string Output = "Library/BlackoutIntro";
        internal static readonly string[] Candidates = {
            "Structures/Floor/Floor Tile 01.prefab", "Structures/Floor/Floor Tile 04.prefab",
            "Structures/Walls/Wall Plain.prefab", "Structures/Walls/Wall Corner.prefab",
            "Structures/Walls/Wall BayDoor.prefab", "Structures/Walls/Wall Corridor.prefab",
            "Structures/Walls/Wall Fan.prefab", "Structures/Walls/Wall Window.prefab",
            "Structures/Ceiling/Ceiling Closed.prefab", "Structures/Ceiling/Ceiling Skylight.prefab",
            "Props/Shelves/Shelf Variation 01.prefab", "Props/Shelves/Shelf Variation 03.prefab",
            "Props/Crates Barrels Pallets/Pallet Variations/Pallet Variation 02.prefab", "Props/Crates Barrels Pallets/Pallet Variations/Pallet Variation 05.prefab",
            "Props/Crates Barrels Pallets/Crate Long.prefab", "Props/Crates Barrels Pallets/Barrel.prefab",
            "Props/Misc Props/Cart.prefab", "Props/Ducts/Duct Straight.prefab",
            "Structures/Structure Props/Fusebox 01.prefab", "Props/Misc Props/Hanging Light.prefab",
            "Props/Misc Props/Wall Light.prefab", "Structures/Catwalk/Catwalk Long Rails.prefab",
            "Structures/Catwalk/Short Stairs Wide.prefab", "Structures/Pillars and Beams/Pillar.prefab",
            "Structures/Corridors/Corridor Passthrough.prefab", "Structures/Catwalk/Catwalk Long.prefab",
            "Structures/Catwalk/Catwalk Rails Long.prefab", "Structures/Pillars and Beams/Ceiling Support.prefab"
        };

        [Serializable] internal sealed class Record {
            public string path, guid;
            public Vector3 center, size, min, max;
            public int renderers, colliders, lights, rigidbodies, triangles;
            public string[] materials, shaders, scripts;
        }
        [Serializable] internal sealed class Report { public List<Record> prefabs = new List<Record>(); }

        [MenuItem("BLACKOUT/Intro/1 Audit Prefabs")]
        internal static void Audit()
        {
            Directory.CreateDirectory(Output);
            var report = new Report();
            int rows = (Candidates.Length + 3) / 4;
            var sheet = new Texture2D(1024, rows * 256, TextureFormat.RGB24, false);
            try {
                for (int i = 0; i < Candidates.Length; i++) {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + Candidates[i]);
                    if (!asset) throw new InvalidOperationException("Missing prefab: " + Candidates[i]);
                    var preview = new PreviewRenderUtility();
                    try {
                        var go = preview.InstantiatePrefabInScene(asset);
                        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        go.transform.localScale = Vector3.one;
                        var renderers = go.GetComponentsInChildren<Renderer>(true);
                        var bounds = BoundsOf(go);
                        var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m).Distinct().ToArray();
                        report.prefabs.Add(new Record {
                            path = Kit + Candidates[i], guid = AssetDatabase.AssetPathToGUID(Kit + Candidates[i]),
                            center = bounds.center, size = bounds.size, min = bounds.min, max = bounds.max,
                            renderers = renderers.Length, colliders = go.GetComponentsInChildren<Collider>(true).Length,
                            lights = go.GetComponentsInChildren<Light>(true).Length,
                            rigidbodies = go.GetComponentsInChildren<Rigidbody>(true).Length,
                            triangles = go.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh).Sum(f => {
                                long count = 0; for (int s = 0; s < f.sharedMesh.subMeshCount; s++) count += (long)f.sharedMesh.GetIndexCount(s); return (int)(count / 3);
                            }),
                            materials = materials.Select(m => AssetDatabase.GetAssetPath(m)).ToArray(),
                            shaders = materials.Select(m => m.shader ? m.shader.name : "MISSING").Distinct().ToArray(),
                            scripts = go.GetComponentsInChildren<MonoBehaviour>(true).Select(m => m ? m.GetType().FullName : "MISSING").ToArray()
                        });
                        foreach (var light in go.GetComponentsInChildren<Light>(true)) light.enabled = false;
                        foreach (var audio in go.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
                        preview.camera.fieldOfView = 32;
                        preview.camera.nearClipPlane = 0.01f;
                        preview.camera.farClipPlane = 200;
                        preview.camera.clearFlags = CameraClearFlags.SolidColor;
                        preview.camera.backgroundColor = new Color(0.17f, 0.19f, 0.21f);
                        preview.ambientColor = new Color(0.38f, 0.38f, 0.4f);
                        preview.lights[0].intensity = 1.3f;
                        preview.lights[0].transform.rotation = Quaternion.Euler(40, 35, 0);
                        preview.lights[1].intensity = 0.9f;
                        preview.lights[1].transform.rotation = Quaternion.Euler(20, 210, 0);
                        float radius = Mathf.Max(bounds.extents.magnitude, 0.2f);
                        var direction = new Vector3(1.3f, 0.75f, 1).normalized;
                        preview.camera.transform.position = bounds.center + direction * radius * 3.9f;
                        preview.camera.transform.LookAt(bounds.center);
                        preview.BeginStaticPreview(new Rect(0, 0, 256, 256));
                        preview.Render(true, false);
                        var image = preview.EndStaticPreview();
                        File.WriteAllBytes(Path.Combine(Output, "prefab_" + i.ToString("D2") + ".png"), image.EncodeToPNG());
                        sheet.SetPixels((i % 4) * 256, (rows - 1 - i / 4) * 256, 256, 256, image.GetPixels());
                        Object.DestroyImmediate(image);
                    } finally { preview.Cleanup(); }
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(Output, "prefab-contact-sheet.png"), sheet.EncodeToPNG());
                File.WriteAllText(Path.Combine(Output, "prefab-audit.json"), JsonUtility.ToJson(report, true));
                Debug.Log("[BLACKOUT Intro] Audited and rendered " + report.prefabs.Count + " source prefabs. " + Output);
            } finally { Object.DestroyImmediate(sheet); }
        }

        internal static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("No renderer: " + go.name);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
