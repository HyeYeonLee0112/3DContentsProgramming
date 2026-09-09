using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Blackout.EditorTools
{
    // Scene authoring only. The generated scene needs no generator at runtime.
    internal static class BlackoutIntroBuilder
    {
        internal const string ScenePath = "Assets/BLACKOUT/Scenes/Sandbox/testScene.unity";
        private const string MaterialFolder = "Assets/BLACKOUT/Art/Materials/Intro";
        private static Scene scene;
        private static Transform root, structure, cargo, facilities, lights, details;
        private static Material yellow, charcoal, cyan, red, white;
        private static readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
        private static readonly Color Yellow = new Color(0.82f, 0.54f, 0.14f);
        private static readonly Color Cyan = new Color(0.4f, 0.83f, 0.9f);

        [MenuItem("BLACKOUT/Intro/2 Build testScene")]
        internal static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("testScene already exists; edit its instances instead of rebuilding over user edits.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your current scene before creating testScene.");
            if (!File.Exists(BlackoutIntroAudit.Output + "/prefab-audit.json")) throw new InvalidOperationException("Run the prefab audit first.");
            foreach (var candidate in BlackoutIntroAudit.Candidates)
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(BlackoutIntroAudit.Kit + candidate)) throw new InvalidOperationException("Missing prefab " + candidate);

            Folder(MaterialFolder); Folder("Assets/BLACKOUT/Scenes/Sandbox");
            materials.Clear();
            yellow = Solid("SafetyYellow", Yellow, 0.0f);
            charcoal = Solid("SignBacking", new Color(0.045f, 0.055f, 0.065f), 0);
            cyan = Solid("PowerIndicator", Cyan * 0.35f, 0.18f);
            red = Solid("EmergencyIndicator", new Color(0.65f, 0.08f, 0.04f), 0.25f);
            white = Solid("LabelWhite", new Color(0.8f, 0.85f, 0.86f), 0.1f);

            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            root = Group("BLACKOUT_Intro", null);
            structure = Group("01_Structure", root);
            cargo = Group("02_Cargo", root);
            facilities = Group("03_Facilities", root);
            lights = Group("04_Lighting", root);
            details = Group("05_Wayfinding", root);
            var markers = Group("06_GameplayAnchors", root);

            // Source floor corner is (0,0,0); playable top is y=0, not its renderer centre.
            for (int z = 0; z < 5; z++) for (int x = 0; x < 4; x++) {
                Place("Structures/Floor/Floor Tile " + ((x + z) % 3 == 0 ? "04" : "01") + ".prefab", "Floor_" + x + "_" + z, new Vector3(x * 6, 0, z * 6), 0, structure);
                bool skylight = x == 1 && (z == 1 || z == 3);
                Place("Structures/Ceiling/Ceiling " + (skylight ? "Skylight" : "Closed") + ".prefab", "Ceiling_" + x + "_" + z, new Vector3(x * 6, 9.2f, z * 6), 0, structure);
            }
            for (int z = 0; z < 5; z++) {
                Place("Structures/Walls/Wall " + (z == 3 ? "Fan" : "Plain") + ".prefab", "Wall_West_" + z, new Vector3(0, 0, z * 6), 0, structure);
                Place("Structures/Walls/Wall " + (z == 1 ? "BayDoor" : z == 3 ? "Corridor" : "Plain") + ".prefab", "Wall_East_" + z, new Vector3(24, 0, (z + 1) * 6), 180, structure);
            }
            for (int x = 0; x < 4; x++) {
                Place("Structures/Walls/Wall " + (x == 1 ? "Corridor" : "Plain") + ".prefab", "Wall_South_" + x, new Vector3((x + 1) * 6, 0, 0), -90, structure);
                Place("Structures/Walls/Wall " + (x == 2 ? "BayDoor" : "Plain") + ".prefab", "Wall_North_" + x, new Vector3(x * 6, 0, 30), 90, structure);
            }
            Place("Structures/Corridors/Corridor Passthrough.prefab", "StaffEntry", new Vector3(6, 0, -6), 0, structure);
            Place("Structures/Corridors/Corridor Passthrough.prefab", "NextArea_ExitCorridor", new Vector3(24, 0, 24), 90, structure);
            Place("Structures/Corridors/Corridor Deadend.prefab", "StaffEntry_EndCap", new Vector3(12, 0, -6), -90, structure);
            Place("Structures/Corridors/Corridor Deadend.prefab", "CargoExit_EndCap", new Vector3(30, 0, 24), 180, structure);
            for (int z = 0; z <= 5; z++) {
                Place("Structures/Pillars and Beams/Pillar.prefab", "Column_W_" + z, new Vector3(0.45f, 0, z * 6), 0, structure);
                Place("Structures/Pillars and Beams/Pillar.prefab", "Column_E_" + z, new Vector3(23.55f, 0, z * 6), 0, structure);
            }
            for (int z = 0; z <= 5; z++) for (int x = 0; x < 4; x++)
                Place("Structures/Pillars and Beams/Ceiling Support.prefab", "RoofBeam_" + x + "_" + z, new Vector3(x * 6, 8.9f, z * 6), 0, structure);

            // Tall storage is against walls; low freight makes middle-distance foregrounds.
            Ground("Props/Shelves/Shelf Variation 01.prefab", "Storage_West_A", new Vector3(2.4f, 0, 9.8f), 90, cargo);
            Ground("Props/Shelves/Shelf Variation 03.prefab", "Storage_West_B", new Vector3(2.4f, 0, 18), 90, cargo);
            Ground("Props/Shelves/Shelf Variation 01.prefab", "Storage_East_A", new Vector3(21.6f, 0, 9), 90, cargo);
            Ground("Props/Shelves/Shelf Variation 03.prefab", "Storage_East_B", new Vector3(20, 0, 2.1f), 0, cargo);
            for (int i = 0; i < 6; i++) {
                float x = i < 3 ? 6.8f : 17.4f;
                float z = i < 3 ? 7.5f + i * 2.1f : 10.5f + (i - 3) * 2.1f;
                string variant = i % 2 == 0 ? "02" : "05";
                Ground("Props/Crates Barrels Pallets/Pallet Variations/Pallet Variation " + variant + ".prefab", "Freight_" + i, new Vector3(x, 0, z), i % 2 == 0 ? 0 : 90, cargo);
            }
            Ground("Props/Misc Props/Cart.prefab", "InterruptedDelivery_Cart", new Vector3(7.2f, 0, 4.2f), -18, cargo);
            Ground("Props/Crates Barrels Pallets/Crate Long.prefab", "DeliveryCrate", new Vector3(5.2f, 0, 4.4f), 90, cargo);
            for (int i = 0; i < 3; i++) Ground("Props/Crates Barrels Pallets/Barrel.prefab", "MaintenanceBarrel_" + i, new Vector3(2.1f + i * 1.1f, 0, 26.5f), 0, cargo);
            for (int z = 0; z < 5; z++) {
                Place("Props/Ducts/Duct Straight.prefab", "West_ServiceDuct_" + z, new Vector3(1.3f, 7.3f, z * 6), 0, structure);
                Place("Props/Ducts/Duct Straight.prefab", "East_ServiceDuct_" + z, new Vector3(22.7f, 7.3f, z * 6), 0, structure);
            }

            // A static freight-lift mock-up using the same kit's structural parts.
            var lift = Group("FreightLift_EnvironmentPrototype", facilities);
            Place("Structures/Floor/Floor Tile 04.prefab", "LiftPlatform_Static", new Vector3(12, 0.08f, 24), 0, lift);
            Place("Structures/Catwalk/Catwalk Long Rails.prefab", "UpperGallery_Left_A", new Vector3(0, 0, 30), 90, lift);
            Place("Structures/Catwalk/Catwalk Long Rails.prefab", "UpperGallery_Left_B", new Vector3(6, 0, 30), 90, lift);
            Place("Structures/Catwalk/Catwalk Long Rails.prefab", "UpperGallery_Right", new Vector3(18, 0, 30), 90, lift);
            Box("LiftGuide_Left", new Vector3(12.15f, 4.2f, 29.1f), new Vector3(0.16f, 8.4f, 0.28f), yellow, lift);
            Box("LiftGuide_Right", new Vector3(17.85f, 4.2f, 29.1f), new Vector3(0.16f, 8.4f, 0.28f), yellow, lift);
            Box("LiftFrontSafetyEdge", new Vector3(15, 0.085f, 24.05f), new Vector3(5.8f, 0.018f, 0.1f), yellow, lift);
            Sign("LiftLabel", "FREIGHT LIFT", new Vector3(15, 5.9f, 29.55f), 0, 3.5f, 0.65f, Yellow);
            Sign("LiftStatus", "POWER REQUIRED   0 / 2", new Vector3(15, 5.25f, 29.54f), 0, 3.5f, 0.36f, new Color(0.8f, 0.83f, 0.84f));
            Place("Structures/Structure Props/Fusebox 01.prefab", "LiftPowerSocket_Anchor", new Vector3(18.9f, 1.8f, 29.6f), 90, lift);
            Sign("LiftSocketLabel", "LIFT  0 / 2", new Vector3(18.9f, 2.3f, 29.1f), 0, 1.4f, 0.32f, Cyan);

            // Local worklight panel, encountered before collecting any power.
            Place("Structures/Pillars and Beams/Pillar.prefab", "Worklight_ServiceColumn", new Vector3(6.2f, 0, 6), 0, facilities);
            Place("Structures/Structure Props/Fusebox 01.prefab", "FirstPowerSocket_Anchor", new Vector3(6.42f, 1.85f, 6), 0, facilities);
            Sign("FirstSocketLabel", "WORKLIGHTS\nPOWER  0 / 1", new Vector3(6.76f, 2.15f, 6), -90, 1.7f, 0.72f, Cyan);
            Box("SocketIndicator_Unpowered", new Vector3(6.8f, 1.32f, 6), new Vector3(0.02f, 0.1f, 0.35f), cyan, facilities);

            // Floor markings are functional lane boundaries, kept narrow and above the floor.
            for (int i = 0; i < 7; i++) {
                Box("Lane_W_" + i, new Vector3(8.5f, 0.012f, 3 + i * 3), new Vector3(0.09f, 0.012f, 1.6f), yellow, details);
                Box("Lane_E_" + i, new Vector3(13.5f, 0.012f, 3 + i * 3), new Vector3(0.09f, 0.012f, 1.6f), yellow, details);
            }
            for (int i = 0; i < 4; i++) Box("ExitLane_" + i, new Vector3(15 + i * 2.5f, 0.012f, 21), new Vector3(1.2f, 0.012f, 0.09f), yellow, details);
            Sign("ExitLabel", "CARGO ACCESS  >", new Vector3(23.7f, 4.1f, 21), 90, 3.6f, 0.55f, Yellow);
            Sign("NextAreaLabel", "CARGO TRANSFER\nAUTHORIZED PERSONNEL", new Vector3(29.65f, 2.2f, 21), 90, 3.3f, 0.9f, new Color(0.8f, 0.83f, 0.84f));
            Sign("EntryLabel", "CARGO TERMINAL  /  STAFF ACCESS", new Vector3(9, 4.0f, 0.25f), 180, 4.5f, 0.5f, Yellow);
            Sign("StorageLabel", "LOADING BAY", new Vector3(0.28f, 6.3f, 13), -90, 5.0f, 0.8f, new Color(0.72f, 0.75f, 0.74f));

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.23f, 0.28f, 0.34f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.21f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.11f, 0.13f);
            RenderSettings.ambientIntensity = 1;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.055f, 0.068f, 0.081f); RenderSettings.fogDensity = 0.009f;
            for (int z = 0; z < 4; z++) for (int x = 0; x < 2; x++)
                Place("Props/Misc Props/Hanging Light.prefab", "Worklight_Off_" + x + "_" + z, new Vector3(7 + x * 11, 8.6f, 4 + z * 7), 0, lights);
            LightAt("Skylight_Key", new Vector3(9, 8.5f, 12), new Color(0.6f, 0.73f, 0.89f), 6.5f, 28, LightType.Spot, new Vector3(90, 0, 0), true, 118);
            LightAt("Entrance_Auxiliary", new Vector3(9, 3.8f, -2), new Color(0.69f, 0.77f, 0.86f), 2.2f, 12);
            LightAt("FirstSocket_Readability", new Vector3(7.5f, 3, 6), new Color(0.5f, 0.67f, 0.72f), 1.1f, 7);
            LightAt("CargoExit_Auxiliary", new Vector3(25.5f, 3.5f, 21), new Color(0.83f, 0.66f, 0.4f), 7.0f, 14);
            LightAt("Lift_AmbientFill", new Vector3(15, 6.8f, 26), new Color(0.5f, 0.62f, 0.73f), 3.8f, 15);
            LightAt("Storage_SoftFill", new Vector3(4, 5.8f, 19), new Color(0.48f, 0.58f, 0.67f), 1.3f, 12);
            Place("Props/Misc Props/Wall Light.prefab", "EmergencyFixture", new Vector3(23.7f, 3, 6.5f), 180, lights);
            LightAt("Emergency_Red", new Vector3(23.1f, 3, 6.5f), new Color(0.77f, 0.08f, 0.03f), 1.3f, 6);
            Box("EmergencyLens", new Vector3(23.42f, 3, 6.5f), new Vector3(0.03f, 0.35f, 0.3f), red, lights);

            Anchor("PlayerSpawn", new Vector3(9, 0.05f, -3), markers);
            Anchor("FirstLook_Lift", new Vector3(9, 1.8f, 2), markers);
            Anchor("FirstSocket_Approach", new Vector3(8, 0, 6), markers);
            Anchor("NextArea_Trigger", new Vector3(25, 0, 21), markers);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera"; camera.transform.SetParent(root);
            camera.transform.position = new Vector3(9, 1.85f, 1.2f);
            camera.transform.LookAt(new Vector3(14.4f, 2.7f, 26));
            var cam = camera.GetComponent<Camera>(); cam.fieldOfView = 64; cam.nearClipPlane = 0.08f; cam.farClipPlane = 100;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            cam.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var volumeGo = new GameObject("Intro_Tonemapping", typeof(Volume)); volumeGo.transform.SetParent(lights);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            profile.Add<ColorAdjustments>().postExposure.Override(0.3f);
            profile.Add<Bloom>().intensity.Override(0.12f);
            AssetDatabase.CreateAsset(profile, MaterialFolder + "/IntroVolume.asset");
            var volume = volumeGo.GetComponent<Volume>(); volume.isGlobal = true; volume.sharedProfile = profile;
            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save testScene.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root.gameObject;
            SceneView.lastActiveSceneView?.LookAt(new Vector3(12, 2, 15), Quaternion.Euler(22, 15, 0), 24, false);
            Debug.Log("[BLACKOUT Intro] Saved " + ScenePath + ". Static environment only; validate camera views and routes next.");
        }

        private static Transform Group(string name, Transform parent) { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }
        private static void Anchor(string name, Vector3 point, Transform parent) { var t = Group(name, parent); t.position = point; }
        private static GameObject Place(string relative, string name, Vector3 position, float yaw, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlackoutIntroAudit.Kit + relative);
            if (!prefab) throw new InvalidOperationException("Missing " + relative);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.name = name; go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) {
                r.sharedMaterials = r.sharedMaterials.Select(ProjectMaterial).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            foreach (var l in go.GetComponentsInChildren<Light>(true)) l.enabled = false;
            foreach (var a in go.GetComponentsInChildren<AudioSource>(true)) a.enabled = false;
            foreach (var b in go.GetComponentsInChildren<Rigidbody>(true)) { b.isKinematic = true; b.useGravity = false; }
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            return go;
        }
        private static GameObject Ground(string relative, string name, Vector3 bottomCenter, float yaw, Transform parent)
        {
            var go = Place(relative, name, Vector3.zero, yaw, parent);
            var b = BlackoutIntroAudit.BoundsOf(go);
            go.transform.position = bottomCenter - new Vector3(b.center.x, b.min.y, b.center.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            return go;
        }
        private static Material ProjectMaterial(Material source)
        {
            if (!source) throw new InvalidOperationException("A source material is missing.");
            if (materials.TryGetValue(source, out var cached)) return cached;
            var result = new Material(source); result.name = "Intro_" + source.name;
            if (result.shader.name != "Universal Render Pipeline/Lit") throw new InvalidOperationException("Unverified shader: " + source.name);
            if (result.HasProperty("_Metallic")) result.SetFloat("_Metallic", Mathf.Min(result.GetFloat("_Metallic"), 0.45f));
            if (result.HasProperty("_Smoothness")) result.SetFloat("_Smoothness", Mathf.Min(result.GetFloat("_Smoothness"), 0.38f));
            result.SetColor("_EmissionColor", Color.black); result.DisableKeyword("_EMISSION");
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string safe = string.Concat(source.name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
            AssetDatabase.CreateAsset(result, MaterialFolder + "/" + safe + "_" + guid.Substring(0, 6) + ".mat");
            materials[source] = result; return result;
        }
        private static Material Solid(string name, Color color, float emission)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.name = "Intro_" + name;
            mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", 0.25f);
            if (emission > 0) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * emission); }
            AssetDatabase.CreateAsset(mat, MaterialFolder + "/" + name + ".mat"); return mat;
        }
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.position = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat;
            if (parent == details || name.Contains("Indicator") || name.Contains("Lens")) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        private static void Sign(string name, string text, Vector3 point, float yaw, float width, float height, Color color)
        {
            var holder = Group(name, details); holder.position = point; holder.rotation = Quaternion.Euler(0, yaw, 0);
            var backing = Box(name + "_Plate", Vector3.zero, new Vector3(width, height, 0.035f), charcoal, holder);
            backing.transform.localPosition = Vector3.zero; backing.transform.localRotation = Quaternion.identity;
            Object.DestroyImmediate(backing.GetComponent<Collider>());
            var label = new GameObject("Label", typeof(TextMesh)); label.transform.SetParent(holder, false); label.transform.localPosition = new Vector3(0, 0, -0.025f);
            var tm = label.GetComponent<TextMesh>(); tm.text = text; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); tm.fontSize = 64; tm.characterSize = 0.065f; tm.color = color;
            label.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            var b = label.GetComponent<Renderer>().bounds; float longest = Mathf.Max(b.size.x, b.size.z);
            float fit = Mathf.Min((width * 0.88f) / Mathf.Max(longest, 0.01f), (height * 0.72f) / Mathf.Max(b.size.y, 0.01f));
            label.transform.localScale = Vector3.one * fit;
        }
        private static void LightAt(string name, Vector3 point, Color color, float intensity, float range, LightType type = LightType.Point, Vector3 angles = default, bool shadows = false, float spotAngle = 80)
        {
            var go = new GameObject(name, typeof(Light)); go.transform.SetParent(lights, false); go.transform.position = point; go.transform.eulerAngles = angles;
            var light = go.GetComponent<Light>(); light.type = type; light.color = color; light.intensity = intensity; light.range = range;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None; light.spotAngle = spotAngle; light.innerSpotAngle = spotAngle * 0.6f;
            light.shadowBias = 0.03f; light.shadowNormalBias = 0.2f;
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); Folder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
