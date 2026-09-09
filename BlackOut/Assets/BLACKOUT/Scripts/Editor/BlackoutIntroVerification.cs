using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blackout.EditorTools
{
    internal static class BlackoutIntroVerification
    {
        [Serializable] private sealed class Report {
            public string scene, checkedAt;
            public int prefabInstances, renderers, colliders, materials, realtimeLights, shadowLights;
            public int floorSamples, clearRouteSegments, missingScripts, missingMaterials;
            public List<string> failures = new List<string>();
            public List<string> sourcePrefabs = new List<string>();
            public bool passed;
        }

        [MenuItem("BLACKOUT/Intro/3 Validate Environment")]
        internal static void Validate()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != BlackoutIntroBuilder.ScenePath) throw new InvalidOperationException("Open testScene first.");
            var root = scene.GetRootGameObjects().Single(g => g.name == "BLACKOUT_Intro");
            Physics.SyncTransforms();
            var report = new Report { scene = scene.path, checkedAt = DateTime.Now.ToString("O") };
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var lights = root.GetComponentsInChildren<Light>(true).Where(l => l.enabled && l.gameObject.activeInHierarchy).ToArray();
            report.renderers = renderers.Length;
            report.colliders = root.GetComponentsInChildren<Collider>(true).Count(c => c.enabled && c.gameObject.activeInHierarchy);
            report.materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m).Distinct().Count();
            report.realtimeLights = lights.Length;
            report.shadowLights = lights.Count(l => l.shadows != LightShadows.None);
            foreach (var t in transforms) {
                report.missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)) {
                    report.prefabInstances++;
                    string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                    if (!report.sourcePrefabs.Contains(path)) report.sourcePrefabs.Add(path);
                }
            }
            report.missingMaterials = renderers.Sum(r => r.sharedMaterials.Count(m => !m || !m.shader || m.shader.name == "Hidden/InternalErrorShader"));
            if (report.missingScripts != 0 || report.missingMaterials != 0) report.failures.Add("Missing scripts or materials.");
            if (report.shadowLights > 1) report.failures.Add("More than one realtime shadow light.");
            var route = new[] { new Vector3(9,0,-3), new Vector3(9,0,3), new Vector3(8,0,6), new Vector3(11,0,12), new Vector3(11,0,21), new Vector3(25,0,21), new Vector3(29,0,21) };
            CheckRoute("Intro-to-cargo", route, report);
            CheckRoute("Lift approach", new[] { new Vector3(11,0,21), new Vector3(15,0,22.5f), new Vector3(15,0,27) }, report);
            report.passed = report.failures.Count == 0;
            Directory.CreateDirectory(BlackoutIntroAudit.Output);
            File.WriteAllText(BlackoutIntroAudit.Output + "/scene-validation.json", JsonUtility.ToJson(report, true));
            Debug.Log("[BLACKOUT Intro] Environment validation " + (report.passed ? "PASS" : "REVIEW") + ": " + report.floorSamples + " floor samples, " + report.clearRouteSegments + " clear capsule routes. " + string.Join("; ", report.failures));
        }

        private static void CheckRoute(string name, Vector3[] route, Report report)
        {
            for (int i = 0; i < route.Length - 1; i++) {
                Vector3 delta = route[i + 1] - route[i];
                float length = delta.magnitude;
                if (Physics.CapsuleCast(route[i] + Vector3.up * 0.45f, route[i] + Vector3.up * 1.5f, 0.35f, delta.normalized, out var obstacle, length, ~0, QueryTriggerInteraction.Ignore))
                    report.failures.Add(name + " segment " + i + " blocked by " + obstacle.collider.name);
                else report.clearRouteSegments++;
                int steps = Mathf.CeilToInt(length / 0.5f);
                for (int s = 0; s <= steps; s++) {
                    Vector3 point = Vector3.Lerp(route[i], route[i + 1], s / (float)steps);
                    report.floorSamples++;
                    if (!Physics.Raycast(point + Vector3.up * 2, Vector3.down, out var hit, 2.25f, ~0, QueryTriggerInteraction.Ignore))
                        report.failures.Add(name + " no floor at " + point);
                    else if (hit.point.y > 0.2f || hit.point.y < -0.1f)
                        report.failures.Add(name + " unexpected floor height " + hit.point.y + " at " + point + " on " + hit.collider.name);
                }
            }
        }

        [MenuItem("BLACKOUT/Intro/4 Capture Cutaway Plan")]
        internal static void CapturePlan()
        {
            if (SceneManager.GetActiveScene().path != BlackoutIntroBuilder.ScenePath) throw new InvalidOperationException("Open testScene first.");
            var structure = GameObject.Find("BLACKOUT_Intro/01_Structure");
            var hidden = new List<Renderer>();
            try {
                foreach (Transform child in structure.transform) {
                    if (!(child.name.StartsWith("Ceiling_") || child.name.StartsWith("RoofBeam_") || child.name.StartsWith("Wall_South") || child.name.StartsWith("Wall_East"))) continue;
                    foreach (var r in child.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; hidden.Add(r); }
                }
                MCPForUnity.Editor.Tools.ManageScene.HandleCommand(JObject.FromObject(new {
                    action = "screenshot", viewPosition = new[] { 29f, 34f, -10f }, viewTarget = new[] { 12f, 0f, 13f },
                    maxResolution = 1600, outputFolder = BlackoutIntroAudit.Output, fileName = "intro-cutaway-plan.png", includeImage = false
                }));
            } finally { foreach (var r in hidden) if (r) r.enabled = true; }
            Debug.Log("[BLACKOUT Intro] Cutaway captured; roof and wall renderers restored.");
        }
    }
}
