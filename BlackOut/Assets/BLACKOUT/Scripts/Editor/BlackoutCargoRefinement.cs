using System;
using System.IO;
using System.Linq;
using Blackout.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Blackout.EditorTools
{
    internal static class BlackoutCargoRefinement
    {
        [MenuItem("BLACKOUT/Cargo Station/4 Apply inspected refinements")]
        public static void Apply()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=BlackoutCargoBuilder.ScenePath)
                throw new InvalidOperationException("Open CargoStation first.");
            var intro=GameObject.Find("BLACKOUT_CargoStation/Environment/01_LoadingBay").transform;
            intro.localPosition=Vector3.zero;intro.localRotation=Quaternion.identity;intro.localScale=Vector3.one;
            if(PrefabUtility.IsPartOfPrefabInstance(intro))PrefabUtility.RecordPrefabInstancePropertyModifications(intro);
            var gallery=GameObject.Find("BLACKOUT_CargoStation/Environment/03_SecurityGate/Architecture/SecurityServiceGallery").transform;
            var crossing=gallery.Find("Gallery_Bay_2");
            if(crossing) {
                // The kit's rail/support module has a full-height collider. Use the deck-only module over this passage.
                Object.DestroyImmediate(crossing.gameObject);
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(BlackoutIntroAudit.Kit+"Structures/Catwalk/Catwalk Long.prefab");
                var deck=(GameObject)PrefabUtility.InstantiatePrefab(asset);deck.name="OverGate_ClearSpanDeck";deck.transform.SetParent(gallery,false);deck.transform.position=new Vector3(83,0,18);
                foreach(var col in deck.GetComponentsInChildren<Collider>())col.enabled=false;
                var slab=deck.AddComponent<BoxCollider>();slab.center=new Vector3(1,4.28f,3);slab.size=new Vector3(2,.46f,6);
                foreach(var r in deck.GetComponentsInChildren<Renderer>()) {
                    r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(BlackoutCargoBuilder.Art+"/Materials/Kit_"+string.Concat(m.name.Select(c=>char.IsLetterOrDigit(c)?c:'_'))+".mat")??m).ToArray();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                }
                var steel=AssetDatabase.LoadAssetAtPath<Material>(BlackoutCargoBuilder.Art+"/Materials/ExposedSteel.mat");
                foreach(float x in new[]{83.08f,84.92f}) {
                    Box("ClearSpan_Handrail",new Vector3(x,5.18f,21),new Vector3(.075f,.075f,6),steel,gallery);
                    Box("ClearSpan_Midrail",new Vector3(x,4.84f,21),new Vector3(.055f,.055f,6),steel,gallery);
                    foreach(float z in new[]{18f,20f,22f,24f})Box("ClearSpan_RailPost",new Vector3(x,4.84f,z),new Vector3(.06f,.75f,.06f),steel,gallery);
                }
            }
            FixText();
            WeatherPaint();
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) {
                if(PathOf(light.transform).Contains("02_ContainerCorridor")||PathOf(light.transform).Contains("03_SecurityGate")) {
                    if(light.name=="LightPool")light.intensity=light.transform.parent.name switch {
                        "EntryPool"=>24, "DoglegPool"=>34, "ExitPool"=>28, "CheckpointApproach"=>32,
                        "GateWorklight"=>27,"InspectionBay"=>16,"BeyondGate"=>18,_=>light.intensity
                    };
                }
            }
            // A cross-light catches the container ribs and gate faces; overhead lamps alone flatten them.
            var corridor=GameObject.Find("BLACKOUT_CargoStation/Environment/02_ContainerCorridor/Lighting").transform;
            Fill("RakingCargoWorklight",new Vector3(33.5f,4.0f,27),new Vector3(43,2.2f,21),new Color(.95f,.7f,.44f),12,20,corridor);
            var checkpoint=GameObject.Find("BLACKOUT_CargoStation/Environment/03_SecurityGate/Lighting").transform;
            Fill("GateFaceReadability",new Vector3(74,5.5f,23),new Vector3(82,2.5f,21),new Color(.57f,.72f,.83f),16,19,checkpoint);
            OrganizeIntro(intro);
            DetailContainers();
            DetailGate();
            var interaction=GameObject.Find("BLACKOUT_CargoStation/Environment/03_SecurityGate/GameplayAnchors/PowerSocket_Interact");
            interaction.transform.position=new Vector3(77,0,16.8f);
            var gate=Object.FindFirstObjectByType<BlackoutSecurityGatePreview>();gate.SetPowered(false);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[CargoStation] Refined: intro alignment, clear-span collision, depth-tested signs, weathered paint and raking light.");
        }

        private static void FixText()
        {
            var shader=Shader.Find("BLACKOUT/Depth Tested Sign");if(!shader)throw new InvalidOperationException("Sign shader not imported.");
            foreach(var font in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(t=>t.font).Distinct()) {
                if(!font)continue;
                string characters=new string(Enumerable.Range(32,95).Select(i=>(char)i).ToArray());
                font.RequestCharactersInTexture(characters,96,FontStyle.Normal);
                string path=BlackoutCargoBuilder.Art+"/Materials/Sign_"+font.name.Replace(" ","_")+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
                material.shader=shader;material.mainTexture=font.material.mainTexture;material.color=Color.white;
                foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.font==font)) {
                    var r=text.GetComponent<MeshRenderer>();r.sharedMaterial=material;PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                }
            }
            // Persist the reusable assets, not just scene instances.
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{BlackoutCargoBuilder.Prefabs})) {
                string path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
                try {
                    foreach(var text in root.GetComponentsInChildren<TextMesh>(true)) {
                        string matPath=BlackoutCargoBuilder.Art+"/Materials/Sign_"+text.font.name.Replace(" ","_")+".mat";
                        var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(material)text.GetComponent<MeshRenderer>().sharedMaterial=material;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                } finally {PrefabUtility.UnloadPrefabContents(root);}
            }
        }

        private static void WeatherPaint()
        {
            string dir=BlackoutCargoBuilder.Art+"/Textures";
            if(!AssetDatabase.IsValidFolder(dir))AssetDatabase.CreateFolder(BlackoutCargoBuilder.Art,"Textures");
            // Deterministic authored surface texture: multi-scale coating variation, rain streaks and fine paint chips.
            const int size=512;var texture=new Texture2D(size,size,TextureFormat.RGB24,false);
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                float u=(float)x/size,v=(float)y/size;
                float broad=Mathf.PerlinNoise(u*7.2f+12,v*5.5f+8);
                float grain=Mathf.PerlinNoise(u*187+31,v*193+77);
                float streak=Mathf.PerlinNoise(u*85+6,v*2+41);
                float chip=Mathf.PerlinNoise(u*83+17,v*107+3);
                float tone=.72f+broad*.32f+(grain-.5f)*.15f;
                tone-=Mathf.Max(0,streak-.55f)*.35f;
                tone*=Mathf.Lerp(.76f,1,Mathf.SmoothStep(0,.14f,v));
                Color c=new Color(tone,tone,tone);
                if(chip>.76f||(v<.06f&&chip>.64f))c=new Color(.23f,.19f,.14f);
                pixels[y*size+x]=c;
            }
            texture.SetPixels(pixels);texture.Apply();string path=dir+"/WeatheredCoating.png";
            File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;importer.maxTextureSize=512;importer.SaveAndReimport();
            var map=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{BlackoutCargoBuilder.Art+"/Materials"})) {
                var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if(m.name.StartsWith("Container_")||m.name=="ExposedSteel"||m.name=="Concrete"||m.name=="SafetyOchre") {
                    m.SetTexture("_BaseMap",map);m.SetFloat("_Smoothness",.23f);EditorUtility.SetDirty(m);
                }
            }
        }
        private static void Fill(string name,Vector3 p,Vector3 target,Color c,float intensity,float range,Transform parent)
        {
            var t=parent.Find(name);var g=t?t.gameObject:new GameObject(name);g.transform.SetParent(parent,false);g.transform.position=p;g.transform.LookAt(target);
            var l=g.GetComponent<Light>();if(!l)l=g.AddComponent<Light>();
            l.type=LightType.Spot;l.spotAngle=105;l.innerSpotAngle=70;l.intensity=intensity;l.range=range;l.color=c;l.shadows=LightShadows.None;
        }
        private static void OrganizeIntro(Transform intro)
        {
            var structure=intro.Find("01_Structure");
            if(!structure) {
                structure=intro.Find("Architecture");
                if(!structure)return;
                var services=structure.Find("OverheadServices");
                if(!services){services=new GameObject("OverheadServices").transform;services.SetParent(structure,false);}
                foreach(var duct in structure.Cast<Transform>().Where(t=>t.name.Contains("ServiceDuct")).ToArray())duct.SetParent(services,true);
                var empty=structure.Find("GalleryAndSupports");if(empty&&empty.childCount==0)Object.DestroyImmediate(empty.gameObject);
                return;
            }
            // Only unpack the outer scene instance. Source Intro and nested kit prefabs remain unchanged.
            PrefabUtility.UnpackPrefabInstance(intro.gameObject,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
            structure.name="Architecture";
            foreach(string category in new[]{"FloorGrid","PerimeterWalls","RoofAndTrusses","StructuralColumns","Connections","GalleryAndSupports"}) {
                var group=new GameObject(category).transform;group.SetParent(structure,false);
            }
            foreach(var child in structure.Cast<Transform>().ToArray()) {
                string category=child.name.StartsWith("Floor_")?"FloorGrid":child.name.StartsWith("Wall_")?"PerimeterWalls":
                    child.name.StartsWith("Ceiling_")||child.name.StartsWith("RoofBeam_")?"RoofAndTrusses":
                    child.name.StartsWith("Column_")?"StructuralColumns":
                    child.name.Contains("Entry")||child.name.Contains("Corridor")||child.name.Contains("EndCap")?"Connections":null;
                if(category!=null)child.SetParent(structure.Find(category),true);
            }
            foreach(Transform child in intro) {
                child.name=child.name switch {"02_Cargo"=>"CargoAndStorage","03_Facilities"=>"Facilities","04_Lighting"=>"Lighting","05_Wayfinding"=>"Wayfinding","06_GameplayAnchors"=>"GameplayAnchors",_=>child.name};
            }
            OrganizeIntro(intro);
        }
        private static void DetailContainers()
        {
            foreach(string variant in new[]{"Ochre","Blue","Oxide"}) {
                string path=BlackoutCargoBuilder.Prefabs+"/Container40ft_"+variant+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try {
                    if(root.transform.Find("EndWallPressedRibs"))continue;
                    var end=new GameObject("EndWallPressedRibs").transform;end.SetParent(root.transform,false);
                    var paint=AssetDatabase.LoadAssetAtPath<Material>(BlackoutCargoBuilder.Art+"/Materials/Container_"+variant+"_Ribs.mat");
                    for(int i=0;i<9;i++) {
                        var rib=GameObject.CreatePrimitive(PrimitiveType.Cube);rib.name="PressedEndRib";rib.transform.SetParent(end,false);
                        rib.transform.localPosition=new Vector3(-6.14f,1.3f,-1.05f+i*.26f);rib.transform.localScale=new Vector3(.08f,2.34f,.10f);
                        rib.GetComponent<Renderer>().sharedMaterial=paint;Object.DestroyImmediate(rib.GetComponent<Collider>());
                    }
                    var mesh=new Mesh {name="Container_EndWall_"+variant};
                    var children=end.GetComponentsInChildren<MeshFilter>();
                    mesh.CombineMeshes(children.Select(c=>new CombineInstance{mesh=c.sharedMesh,transform=end.worldToLocalMatrix*c.transform.localToWorldMatrix}).ToArray(),true,true);
                    AssetDatabase.CreateAsset(mesh,BlackoutCargoBuilder.Art+"/Meshes/"+mesh.name+".asset");
                    foreach(var child in children)Object.DestroyImmediate(child.gameObject);
                    end.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;end.gameObject.AddComponent<MeshRenderer>().sharedMaterial=paint;
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                } finally {PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        private static void DetailGate()
        {
            string path=BlackoutCargoBuilder.Prefabs+"/SecurityGate_PowerLinked.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try {
                if(root.transform.Find("DoorPocketHousings"))return;
                var steel=AssetDatabase.LoadAssetAtPath<Material>(BlackoutCargoBuilder.Art+"/Materials/ExposedSteel.mat");
                var dark=AssetDatabase.LoadAssetAtPath<Material>(BlackoutCargoBuilder.Art+"/Materials/Charcoal.mat");
                var ochre=AssetDatabase.LoadAssetAtPath<Material>(BlackoutCargoBuilder.Art+"/Materials/SafetyOchre.mat");
                var pockets=new GameObject("DoorPocketHousings").transform;pockets.SetParent(root.transform,false);
                foreach(float side in new[]{-1f,1f}) {
                    LocalBox("SlidingLeafPocket",new Vector3(-.28f,2.2f,side*4.65f),new Vector3(.9f,4.4f,3.15f),steel,pockets,true);
                    LocalBox("PocketInspectionPanel",new Vector3(-.76f,2.2f,side*4.65f),new Vector3(.065f,3.85f,2.6f),dark,pockets,false);
                    LocalBox("SafetyEdge",new Vector3(-.80f,2.2f,side*3.2f),new Vector3(.065f,4.2f,.12f),ochre,pockets,false);
                    foreach(float y in new[]{.4f,2.2f,4f})foreach(float dz in new[]{-1.25f,1.25f})
                        Cylinder("PocketBolt",new Vector3(-.84f,y,side*4.65f+dz),.035f,.035f,steel,pockets,true);
                    for(int i=0;i<8;i++)LocalBox("VentLouvre",new Vector3(-.81f,.65f+i*.14f,side*4.65f),new Vector3(.035f,.045f,1.8f),steel,pockets,false);
                }
                var turret=root.transform.Find("Turret_VisualMount");
                foreach(var child in turret.Cast<Transform>().Where(t=>t.name=="Barrel"||t.name=="Muzzle"||t.name=="Swivel").ToArray())Object.DestroyImmediate(child.gameObject);
                Cylinder("YawBearing",new Vector3(-.65f,4.7f,3.6f),.29f,.32f,dark,turret,false);
                foreach(float z in new[]{3.4f,3.8f}) {
                    Cylinder("CoolingJacket",new Vector3(-1.6f,4.4f,z),.095f,.65f,steel,turret,true);
                    Cylinder("BarrelTube",new Vector3(-2.04f,4.4f,z),.065f,.60f,dark,turret,true);
                    Cylinder("MuzzleCollar",new Vector3(-2.35f,4.4f,z),.11f,.17f,steel,turret,true);
                    Cylinder("DarkBore",new Vector3(-2.443f,4.4f,z),.068f,.018f,dark,turret,true);
                }
                for(int i=0;i<6;i++)LocalBox("ReceiverCoolingFin",new Vector3(-1.37f+i*.11f,4.44f,3.98f),new Vector3(.05f,.29f,.06f),dark,turret,false);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            } finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void LocalBox(string name,Vector3 p,Vector3 s,Material mat,Transform parent,bool collision)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=s;
            go.GetComponent<Renderer>().sharedMaterial=mat;if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        private static void Cylinder(string name,Vector3 p,float radius,float length,Material mat,Transform parent,bool alongX)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;
            go.transform.localScale=new Vector3(radius*2,length*.5f,radius*2);if(alongX)go.transform.localRotation=Quaternion.Euler(0,0,90);
            go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        private static void Box(string name,Vector3 p,Vector3 s,Material mat,Transform parent)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=mat;
        }
        private static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    }
}
