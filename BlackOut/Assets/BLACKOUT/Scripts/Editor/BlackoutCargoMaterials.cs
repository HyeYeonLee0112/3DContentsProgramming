using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blackout.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Blackout.EditorTools
{
    // Project-owned surface pass. Never changes imported kit materials, source meshes or Intro.prefab.
    public static class BlackoutCargoMaterials
    {
        public const string Materials = "Assets/BLACKOUT/Art/Materials/CargoStation";
        public const string Textures = "Assets/BLACKOUT/Art/Textures/CargoStation";
        public const string Meshes = "Assets/BLACKOUT/Art/CargoStation/Meshes/MetricUV";
        public static string Output => Path.GetFullPath("../docs/images/production/cargo-materials");
        private const int Size = 1024;
        private static readonly Dictionary<string, Material> Library = new Dictionary<string, Material>();
        private static readonly List<string> TextureChecks = new List<string>();

        [MenuItem("BLACKOUT/Cargo Station/7 Create and apply surface materials")]
        public static void BuildAndApply()
        {
            RequireScene();
            Folder(Materials); Folder(Textures); Folder(Meshes); Directory.CreateDirectory(Output);
            string geometryBefore = GeometrySignature();
            TextureChecks.Clear(); Library.Clear();
            BuildFamily("Concrete", "Concrete_Source.png", false);
            BuildFamily("CoatedSteel", "CoatedSteel_Source.png", false);
            BuildFamily("DampConcrete", "Concrete_Source.png", true);
            BuildMetal("BrushedSteel", false);
            BuildMetal("GalvanizedSteel", true);
            Surface("Concrete_Dry", "Concrete", new Color(.78f,.80f,.77f), .333333f, .65f);
            Surface("Concrete_Damp", "DampConcrete", new Color(.72f,.75f,.73f), .333333f, .55f);
            Surface("Concrete_Barrier", "Concrete", new Color(.65f,.68f,.65f), 1f, .7f);
            Surface("Paint_Blue", "CoatedSteel", new Color(.40f,.57f,.66f), .7f, .35f);
            Surface("Paint_Oxide", "CoatedSteel", new Color(.68f,.37f,.25f), .7f, .35f);
            Surface("Paint_Ochre", "CoatedSteel", new Color(.77f,.56f,.28f), .7f, .35f);
            Surface("Paint_Gate", "CoatedSteel", new Color(.46f,.51f,.52f), .75f, .3f);
            Surface("Paint_Graphite", "CoatedSteel", new Color(.31f,.36f,.38f), .75f, .22f);
            Surface("Safety_Ochre", "CoatedSteel", new Color(.94f,.61f,.20f), 1f, .25f);
            Surface("Steel_Brushed", "BrushedSteel", new Color(.73f,.76f,.77f), 1.5f, .3f);
            Surface("Steel_Galvanized", "GalvanizedSteel", new Color(.66f,.70f,.70f), 1.2f, .25f);
            Surface("Steel_Gunmetal", "BrushedSteel", new Color(.34f,.39f,.42f), 1.2f, .2f);
            var rubber = Surface("Rubber_Cable", "CoatedSteel", new Color(.085f,.095f,.10f), 2f, .22f);
            rubber.SetTexture("_MetallicGlossMap", null); rubber.DisableKeyword("_METALLICSPECGLOSSMAP");
            rubber.SetFloat("_Metallic", 0); rubber.SetFloat("_Smoothness", .12f);
            var glass = Surface("Glass_Smoked", "CoatedSteel", new Color(.13f,.24f,.27f), .45f, .015f);
            // Existing booth is a closed cube, not a hollow room: polished opaque tinted glazing avoids false transparency.
            glass.SetTexture("_MetallicGlossMap", null); glass.DisableKeyword("_METALLICSPECGLOSSMAP");
            glass.SetFloat("_Metallic", .25f); glass.SetFloat("_Smoothness", .91f);
            AssetDatabase.SaveAssets();

            // Shared reusable prefabs get the same surfaces. Text and power-state materials are explicitly excluded.
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { BlackoutCargoBuilder.Prefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try { ApplyTo(root, false); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var sceneRoot = GameObject.Find("BLACKOUT_CargoStation");
            int changed = ApplyTo(sceneRoot, true);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            File.WriteAllLines(Output + "/texture-seams.txt", TextureChecks);
            bool unchanged = geometryBefore == GeometrySignature();
            File.WriteAllText(Output + "/geometry-preserved.json", "{\"geometryCollidersAndSignsUnchanged\":" + unchanged.ToString().ToLowerInvariant() + "}");
            if (!unchanged) throw new InvalidOperationException("Surface pass unexpectedly changed geometry, colliders or signage.");
            Debug.Log("[Cargo Materials] Applied renderer changes: " + changed + "; surface materials: " + Library.Count);
        }

        private static Material Surface(string name, string family, Color tint, float repeats, float normal)
        {
            string path = Materials + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.name = name;
            material.SetColor("_BaseColor", tint);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/" + family + "_BaseColor.png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/" + family + "_Normal.png"));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/" + family + "_MetalSmooth.png"));
            material.SetTextureScale("_BaseMap", Vector2.one * repeats);
            material.SetFloat("_BumpScale", normal); material.SetFloat("_Smoothness", 1);
            material.SetFloat("_WorkflowMode", 1); material.SetFloat("_SmoothnessTextureChannel", 0);
            material.SetFloat("_Metallic", 0); material.SetFloat("_Surface", 0); material.SetFloat("_AlphaClip", 0);
            material.SetColor("_EmissionColor", Color.black); material.DisableKeyword("_EMISSION");
            material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.enableInstancing = true; EditorUtility.SetDirty(material); Library[name] = material;
            return material;
        }

        private static int ApplyTo(GameObject root, bool scene)
        {
            int changed = 0;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.GetComponent<TextMesh>()) continue;
                var filter = renderer.GetComponent<MeshFilter>(); if (!filter || !filter.sharedMesh) continue;
                string path = PathOf(renderer.transform);
                bool floor = path.Contains("/FloorGrid/");
                bool metric = floor;
                bool modified = false;
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    Material original = slots[i]; if (!original) continue;
                    string name = original.name;
                    if (AssetDatabase.GetAssetPath(original).StartsWith(Materials + "/")) continue;
                    string key = null;
                    if (floor)
                    {
                        // Damp only near exposed outer edges / entry; the central route remains mostly dry.
                        Vector3 c = renderer.bounds.center;
                        key = (c.z < 16 && c.x > 30 && c.x < 66) || (c.x > 29 && c.x < 36 && c.z > 18 && c.z < 25)
                            ? "Concrete_Damp" : "Concrete_Dry";
                    }
                    else if (name.StartsWith("Container_Blue")) key = "Paint_Blue";
                    else if (name.StartsWith("Container_Ochre")) key = "Paint_Ochre";
                    else if (name.StartsWith("Container_Oxide")) key = "Paint_Oxide";
                    else if (name == "Concrete") key = "Concrete_Barrier";
                    else if (name == "SafetyOchre" || name == "SafetyYellow") key = "Safety_Ochre";
                    else if (name == "BoothGlass") key = "Glass_Smoked";
                    else if (name == "ExposedSteel")
                    {
                        key = path.Contains("SharedPowerConduit") ? "Steel_Galvanized" :
                            path.Contains("OpeningHeader") || path.Contains("RoofCanopy") || path.Contains("WestSide") || path.Contains("EastSide") ? "Paint_Gate" :
                            path.Contains("ArmouredPanel") || path.Contains("SlidingLeafPocket") ? "Paint_Gate" :
                            path.Contains("Turret") ? "Steel_Gunmetal" : "Steel_Brushed";
                    }
                    else if (name == "Charcoal")
                    {
                        key = path.Contains("Cable") || path.Contains("Seal") || path.Contains("BlackBand") ? "Rubber_Cable" :
                            path.Contains("Turret") ? "Steel_Gunmetal" : "Paint_Graphite";
                    }
                    if (key != null)
                    {
                        slots[i] = Library[key]; metric = true; modified = true;
                    }
                    else if (!name.Contains("Sign") && !name.Contains("Indicator") && !name.Contains("Lens") &&
                             !name.Contains("Power") && !name.Contains("Warning") && !name.Contains("Stencil") &&
                             !name.Contains("Light_Mat") && !name.Contains("Windows") && original.shader.name == "Universal Render Pipeline/Lit")
                    {
                        // Retain the kit's texture atlas, alpha cutouts and normal maps; only harmonize its finish.
                        slots[i] = KitCopy(original); modified = true;
                    }
                }
                if (!modified) continue;
                if (metric) MetricUV(filter);
                Undo.RecordObject(renderer, "Apply cargo surface materials"); renderer.sharedMaterials = slots;
                EditorUtility.SetDirty(renderer);
                if (scene && PrefabUtility.IsPartOfPrefabInstance(renderer))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                }
                changed++;
            }
            return changed;
        }

        private static Material KitCopy(Material source)
        {
            string id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string name = "KitFinish_" + source.name + "_" + id.Substring(0, 6);
            string path = Materials + "/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!result) { result = new Material(source); AssetDatabase.CreateAsset(result, path); }
            result.name = name;
            bool walls = name.Contains("Walls"), ceiling = name.Contains("Ceiling"), metal = name.Contains("Duct") || name.Contains("Structure") || name.Contains("Catwalk");
            result.SetColor("_BaseColor", walls ? new Color(.90f,.93f,.91f) : ceiling ? new Color(.64f,.68f,.69f) : new Color(.83f,.85f,.81f));
            result.SetFloat("_Smoothness", walls ? .22f : metal ? .48f : .28f);
            result.SetFloat("_Metallic", walls ? .12f : metal ? .65f : .22f);
            if (result.HasProperty("_BumpScale")) result.SetFloat("_BumpScale", walls ? .7f : .8f);
            result.SetColor("_EmissionColor", Color.black); result.DisableKeyword("_EMISSION");
            result.enableInstancing = true; EditorUtility.SetDirty(result); return result;
        }

        private static void MetricUV(MeshFilter filter)
        {
            Mesh source = filter.sharedMesh;
            if (AssetDatabase.GetAssetPath(source).StartsWith(Meshes + "/")) return;
            Vector3 scale = filter.transform.lossyScale;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long fileId);
            string id = Hash128.Compute(guid + ":" + fileId + ":" + source.name + ":" + scale.ToString("F4")).ToString();
            string path = Meshes + "/Surface_" + id + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh)
            {
                mesh = Object.Instantiate(source); mesh.name = "MetricUV_" + source.name;
                Vector3[] points = mesh.vertices, normals = mesh.normals;
                var uv = new Vector2[points.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    Vector3 p = Vector3.Scale(points[i], scale), n = normals[i];
                    Vector3 a = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
                    uv[i] = a.y >= a.x && a.y >= a.z ? new Vector2(p.x, p.z * -Mathf.Sign(n.y)) :
                        a.x >= a.z ? new Vector2(p.z * -Mathf.Sign(n.x), p.y) : new Vector2(p.x * Mathf.Sign(n.z), p.y);
                }
                mesh.uv = uv; mesh.RecalculateTangents(); AssetDatabase.CreateAsset(mesh, path);
            }
            Undo.RecordObject(filter, "Set metre-scale surface UVs"); filter.sharedMesh = mesh; EditorUtility.SetDirty(filter);
        }

        private static void BuildFamily(string family, string sourceName, bool damp)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            source.LoadImage(File.ReadAllBytes(Path.GetFullPath("../Tools/ArtSources/CargoStation/" + sourceName)));
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                pixels[y * Size + x] = source.GetPixelBilinear((x + .5f) / Size, (y + .5f) / Size);
            Object.DestroyImmediate(source);
            // Deterministic import normalization only: reconcile opposite edge strips without painting new image content.
            const int edge = 24;
            for (int y = 0; y < Size; y++) for (int x = 0; x < edge; x++)
            {
                int a = y * Size + x, b = y * Size + Size - 1 - x;
                Color mean = (pixels[a] + pixels[b]) * .5f;
                float mix = 1f - Mathf.SmoothStep(0, 1, (float)x / edge);
                pixels[a] = Color.Lerp(pixels[a], mean, mix); pixels[b] = Color.Lerp(pixels[b], mean, mix);
            }
            for (int y = 0; y < edge; y++) for (int x = 0; x < Size; x++)
            {
                int a = y * Size + x, b = (Size - 1 - y) * Size + x;
                Color mean = (pixels[a] + pixels[b]) * .5f;
                float mix = 1f - Mathf.SmoothStep(0, 1, (float)y / edge);
                pixels[a] = Color.Lerp(pixels[a], mean, mix); pixels[b] = Color.Lerp(pixels[b], mean, mix);
            }
            bool concrete = family.Contains("Concrete");
            var mask = new Color[pixels.Length]; var height = new float[pixels.Length];
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                int i = y * Size + x; float l = pixels[i].grayscale;
                float wet = damp ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.43f, .65f, Periodic((float)x/(Size-1),(float)y/(Size-1),4,37))) : 0;
                float metal = concrete ? 0 : Mathf.Lerp(.025f, .72f, 1 - Mathf.SmoothStep(0,1,Mathf.InverseLerp(.26f,.47f,l)));
                float smooth = concrete ? Mathf.Lerp(.14f + l*.12f, .83f, wet) : Mathf.Clamp(.30f + l*.20f, .26f,.48f);
                mask[i] = new Color(metal, 1, 0, smooth);
                height[i] = l * (concrete ? .8f : .28f);
                if (damp) pixels[i] *= Mathf.Lerp(1, .65f, wet);
                pixels[i].a = 1;
            }
            SaveFamily(family, pixels, mask, height);
        }

        private static void BuildMetal(string family, bool galvanized)
        {
            var pixels = new Color[Size * Size]; var mask = new Color[pixels.Length]; var height = new float[pixels.Length];
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                float u = (float)x/(Size-1), v = (float)y/(Size-1);
                float noise = Periodic(u,v,galvanized ? 45 : 16,galvanized ? 23 : 51);
                float brush = Mathf.Sin(v*Mathf.PI*2*180)*.014f + Mathf.Sin(v*Mathf.PI*2*67)*.009f;
                float tone = galvanized ? .58f + noise*.24f : .66f + noise*.09f + brush;
                int i = y*Size+x; pixels[i] = new Color(tone,tone,tone,1);
                mask[i] = new Color(galvanized ? .88f : .96f,1,0,galvanized ? .32f+noise*.15f : .55f+noise*.13f);
                height[i] = galvanized ? noise*.1f : brush;
            }
            SaveFamily(family,pixels,mask,height);
        }

        private static float Periodic(float u,float v,float frequency,float seed)
        {
            float a = Mathf.PerlinNoise(u*frequency+seed,v*frequency+seed);
            float b = Mathf.PerlinNoise((u-1)*frequency+seed,v*frequency+seed);
            float c = Mathf.PerlinNoise(u*frequency+seed,(v-1)*frequency+seed);
            float d = Mathf.PerlinNoise((u-1)*frequency+seed,(v-1)*frequency+seed);
            return Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(c,d,u),v);
        }

        private static void SaveFamily(string family,Color[] pixels,Color[] mask,float[] height)
        {
            var normals = new Color[pixels.Length];
            for (int y=0;y<Size;y++) for (int x=0;x<Size;x++)
            {
                // Periodic finite differences. These are artist-controlled micro-relief, not measured scan normals.
                float dx = height[y*Size+(x+1)%Size]-height[y*Size+(x+Size-1)%Size];
                float dy = height[((y+1)%Size)*Size+x]-height[((y+Size-1)%Size)*Size+x];
                Vector3 n = new Vector3(-dx*3,-dy*3,1).normalized;
                normals[y*Size+x] = new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            }
            // Match border normal values as well, for mip-safe repetition.
            MatchEdges(normals);
            WriteTexture(family+"_BaseColor",pixels,false,false);
            WriteTexture(family+"_Normal",normals,true,true);
            WriteTexture(family+"_MetalSmooth",mask,true,false);
            float seam = 0;
            for (int i=0;i<Size;i++)
            {
                seam = Mathf.Max(seam,ColorDelta(pixels[i*Size],pixels[i*Size+Size-1]));
                seam = Mathf.Max(seam,ColorDelta(pixels[i],pixels[(Size-1)*Size+i]));
            }
            TextureChecks.Add(family+": size=1024x1024; baseColor opposite-edge max difference="+seam.ToString("F6"));
            var preview = new Texture2D(768,768,TextureFormat.RGB24,false);
            var grid = new Color[768*768];
            for(int y=0;y<768;y++)for(int x=0;x<768;x++)grid[y*768+x]=pixels[((y%256)*4)*Size+(x%256)*4];
            preview.SetPixels(grid);preview.Apply();File.WriteAllBytes(Output+"/tile-3x3-"+family+".png",preview.EncodeToPNG());Object.DestroyImmediate(preview);
        }

        private static void MatchEdges(Color[] pixels)
        {
            for(int y=0;y<Size;y++){int a=y*Size,b=a+Size-1;pixels[a]=pixels[b]=(pixels[a]+pixels[b])*.5f;}
            for(int x=0;x<Size;x++){int a=x,b=(Size-1)*Size+x;pixels[a]=pixels[b]=(pixels[a]+pixels[b])*.5f;}
        }
        private static float ColorDelta(Color a,Color b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b));

        private static void WriteTexture(string name,Color[] pixels,bool linear,bool normal)
        {
            var texture = new Texture2D(Size,Size,TextureFormat.RGBA32,false,linear);
            texture.SetPixels(pixels);texture.Apply();string path=Textures+"/"+name+".png";
            File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=!linear;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
            importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.maxTextureSize=1024;
            importer.isReadable=false;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.alphaSource=name.EndsWith("MetalSmooth")?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
            importer.SaveAndReimport();
        }

        [MenuItem("BLACKOUT/Cargo Station/8 Bake material reflections")]
        public static void BakeReflections()
        {
            RequireScene();
            string folder=BlackoutCargoBuilder.Art+"/Reflections";Folder(folder);
            var parent=GameObject.Find("BLACKOUT_CargoStation/Presentation").transform;
            var group=parent.Find("MaterialReflections");
            if(!group){group=new GameObject("MaterialReflections").transform;group.SetParent(parent,false);}
            Vector3[] centres={new Vector3(15,3,15),new Vector3(48,3,24),new Vector3(78,3,24)};
            string[] names={"LoadingBay","ContainerCorridor","SecurityGate"};
            for(int i=0;i<names.Length;i++)
            {
                var t=group.Find(names[i]);if(!t){t=new GameObject(names[i]).transform;t.SetParent(group,false);}
                t.position=centres[i];var probe=t.GetComponent<ReflectionProbe>();if(!probe)probe=t.gameObject.AddComponent<ReflectionProbe>();
                probe.mode=ReflectionProbeMode.Baked;probe.resolution=128;probe.hdr=true;probe.boxProjection=true;
                probe.size=i==0?new Vector3(30,10,30):i==1?new Vector3(36,10,24):new Vector3(24,10,24);
                probe.center=new Vector3(0,1.5f,0);probe.blendDistance=2;probe.nearClipPlane=.25f;probe.farClipPlane=80;
                probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.045f,.055f,.065f);
                string path=folder+"/"+names[i]+".exr";
                if(!Lightmapping.BakeReflectionProbe(probe,path))throw new InvalidOperationException("Probe bake failed: "+names[i]);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                // A manually baked cubemap must use the serialized Custom slot. bakedTexture belongs to
                // scene LightingData and an assignment there alone does not survive scene reload.
                probe.mode=ReflectionProbeMode.Custom;
                probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Texture>(path);EditorUtility.SetDirty(probe);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("BLACKOUT/Cargo Station/9 Capture material review")]
        public static void Capture()
        {
            RequireScene();Directory.CreateDirectory(Output);
            // Warm dynamic font atlases after scene reload without resizing or rewriting any Korean text.
            foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!label.font)continue;label.font.RequestCharactersInTexture(label.text,label.fontSize,label.fontStyle);
                label.GetComponent<Renderer>().sharedMaterial.mainTexture=label.font.material.mainTexture;
            }
            var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
            var gate=Object.FindFirstObjectByType<BlackoutSecurityGatePreview>();bool powered=gate.Powered;
            try
            {
                gate.SetPowered(false);
                Shot(camera,"01-container-entry",new Vector3(32,2,20.8f),new Vector3(42,2.3f,25.5f));
                Shot(camera,"02-container-dogleg",new Vector3(35,2,25.5f),new Vector3(55,2.2f,24.5f));
                Shot(camera,"03-security-closed",new Vector3(69,2.1f,22.8f),new Vector3(82,3,20.8f));
                Shot(camera,"04-power-socket",new Vector3(74.5f,1.9f,19.3f),new Vector3(78,1.5f,16.8f));
                gate.SetPowered(true);Shot(camera,"05-security-powered",new Vector3(74,2.1f,21.5f),new Vector3(83,3,21));
                Shot(camera,"06-loading-bay",new Vector3(24,2.1f,21),new Vector3(12,2.8f,12));
            }
            finally{gate.SetPowered(powered);camera.transform.SetPositionAndRotation(position,rotation);}
        }

        private static void Shot(Camera camera,string name,Vector3 p,Vector3 target)
        {
            camera.transform.position=p;camera.transform.LookAt(target);
            var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);
            var old=RenderTexture.active;var previous=camera.targetTexture;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
        }

        private static void RequireScene()
        {
            if(SceneManager.GetActiveScene().path!=BlackoutCargoBuilder.ScenePath||EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open CargoStation in Edit Mode first.");
        }
        private static string GeometrySignature()
        {
            var root=GameObject.Find("BLACKOUT_CargoStation");
            var lines=new List<string>();
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m=r.GetComponent<MeshFilter>();
                if(m && m.sharedMesh) lines.Add(PathOf(r.transform)+"|"+r.transform.position.ToString("F4")+"|"+r.transform.rotation.ToString("F4")+"|"+r.transform.lossyScale.ToString("F4")+"|"+m.sharedMesh.vertexCount+"|"+m.sharedMesh.bounds.ToString());
            }
            foreach(var c in root.GetComponentsInChildren<Collider>(true))lines.Add("collision|"+PathOf(c.transform)+"|"+c.enabled+"|"+c.bounds.ToString());
            foreach(var t in root.GetComponentsInChildren<TextMesh>(true))lines.Add("text|"+PathOf(t.transform)+"|"+t.text+"|"+t.font.name+"|"+t.transform.localScale.ToString("F4"));
            lines.Sort(StringComparer.Ordinal);return Hash128.Compute(string.Join("\n",lines)).ToString();
        }

        public static string Validate()
        {
            RequireScene();var issues=new List<string>();
            var renderers=GameObject.Find("BLACKOUT_CargoStation").GetComponentsInChildren<Renderer>(true);
            var materials=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().ToArray();
            foreach(var r in renderers)foreach(var m in r.sharedMaterials)
                if(!m||!m.shader||!m.shader.isSupported)issues.Add("Missing or unsupported: "+PathOf(r.transform));
            var maps=AssetDatabase.FindAssets("t:Texture2D",new[]{Textures});
            foreach(string guid in maps)
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var map=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(map.width!=1024||map.height!=1024||!importer.mipmapEnabled||importer.isReadable||importer.wrapMode!=TextureWrapMode.Repeat)issues.Add("Texture settings: "+path);
                if(path.Contains("_BaseColor")!=importer.sRGBTexture)issues.Add("Color space: "+path);
                if(path.Contains("_Normal")&&importer.textureType!=TextureImporterType.NormalMap)issues.Add("Normal type: "+path);
            }
            int floor=renderers.Count(r=>PathOf(r.transform).Contains("/FloorGrid/"));
            int dry=renderers.Count(r=>r.sharedMaterials.Any(m=>m&&m.name=="Concrete_Dry"));
            int damp=renderers.Count(r=>r.sharedMaterials.Any(m=>m&&m.name=="Concrete_Damp"));
            if(floor!=dry+damp)issues.Add("Not all floor renderers received concrete surfaces.");
            var probes=Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
            if(probes.Any(p=>!p.texture))issues.Add("Missing baked reflection texture.");
            var report=new SurfaceReport{checkedUtc=DateTime.UtcNow.ToString("o"),materials=materials.Count(m=>AssetDatabase.GetAssetPath(m).StartsWith(Materials)),textureMaps=maps.Length,dryFloorTiles=dry,dampFloorTiles=damp,bakedProbes=probes.Count(p=>p.mode==ReflectionProbeMode.Custom&&p.customBakedTexture),issues=issues.ToArray()};
            string json=JsonUtility.ToJson(report,true);File.WriteAllText(Output+"/validation.json",json);
            if(issues.Count>0)throw new InvalidOperationException(json);return json;
        }

        public static void RefineAfterReview()
        {
            RequireScene();
            string geometryBefore=GeometrySignature();
            var tints=new Dictionary<string,Color>{
                {"Paint_Blue",new Color(.40f,.57f,.66f)}, {"Paint_Oxide",new Color(.68f,.37f,.25f)},
                {"Paint_Ochre",new Color(.77f,.56f,.28f)}, {"Paint_Gate",new Color(.46f,.51f,.52f)},
                {"Paint_Graphite",new Color(.31f,.36f,.38f)}
            };
            foreach(var pair in tints){var m=AssetDatabase.LoadAssetAtPath<Material>(Materials+"/"+pair.Key+".mat");m.SetColor("_BaseColor",pair.Value);EditorUtility.SetDirty(m);}
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{Materials}))
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if(!m.name.StartsWith("KitFinish_"))continue;
                bool walls=m.name.Contains("Walls"),ceiling=m.name.Contains("Ceiling");
                m.SetColor("_BaseColor",walls?new Color(.90f,.93f,.91f):ceiling?new Color(.64f,.68f,.69f):new Color(.83f,.85f,.81f));EditorUtility.SetDirty(m);
            }
            var paint=AssetDatabase.LoadAssetAtPath<Material>(Materials+"/Paint_Gate.mat");
            foreach(var r in GameObject.Find("BLACKOUT_CargoStation").GetComponentsInChildren<MeshRenderer>(true))
            {
                if(r.name!="OpeningHeader"&&r.name!="RoofCanopy"&&r.name!="WestSide"&&r.name!="EastSide")continue;
                if(!PathOf(r.transform).Contains("03_SecurityGate"))continue;
                r.sharedMaterial=paint;EditorUtility.SetDirty(r);if(PrefabUtility.IsPartOfPrefabInstance(r))PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            if(geometryBefore!=GeometrySignature())throw new InvalidOperationException("Refinement altered protected geometry.");
        }
        [Serializable] private sealed class SurfaceReport
        {
            public string checkedUtc;
            public int materials,textureMaps,dryFloorTiles,dampFloorTiles,bakedProbes;
            public string[] issues;
            public bool performanceVerified=false;
        }
        private static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        private static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
}
