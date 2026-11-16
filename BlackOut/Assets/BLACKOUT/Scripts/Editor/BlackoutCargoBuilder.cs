using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blackout.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Blackout.EditorTools
{
    // Builds only its own new scene. Never overwrites the user's Intro prefab or testScene.
    internal static class BlackoutCargoBuilder
    {
        internal const string ScenePath = "Assets/BLACKOUT/Scenes/Levels/CargoStation.unity";
        internal const string Art = "Assets/BLACKOUT/Art/CargoStation";
        internal const string Prefabs = "Assets/BLACKOUT/Prefabs/Environment/CargoStation";
        private static Scene scene;
        private static Material steel, dark, ochre, white, cyan, red, glow, concrete, glass;
        private static Font font;
        private static readonly Dictionary<Material, Material> kitMaterials = new Dictionary<Material, Material>();
        private static readonly Color Warm = new Color(1f, .72f, .4f);
        private static readonly Color Cool = new Color(.48f, .69f, .82f);

        [MenuItem("BLACKOUT/Cargo Station/1 Create connected scene (new only)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("CargoStation already exists. Edit the scene; do not rebuild over it.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before building.");
            Folder(Art + "/Materials"); Folder(Art + "/Meshes"); Folder(Prefabs); Folder("Assets/BLACKOUT/Scenes/Levels");
            kitMaterials.Clear();
            steel = Mat("ExposedSteel", new Color(.25f,.29f,.30f), .55f);
            dark = Mat("Charcoal", new Color(.055f,.068f,.075f), .25f);
            ochre = Mat("SafetyOchre", new Color(.65f,.37f,.075f), .1f);
            white = Mat("StencilIvory", new Color(.73f,.76f,.71f), 0);
            cyan = Mat("PowerCyan", new Color(.2f,.55f,.61f), .1f, 1.5f);
            red = Mat("WarningRed", new Color(.52f,.025f,.015f), .1f, 1.1f);
            glow = Mat("WorklightLens", new Color(1,.77f,.48f), 0, 2.5f);
            concrete = Mat("Concrete", new Color(.26f,.28f,.27f), 0);
            glass = Mat("BoothGlass", new Color(.055f,.15f,.18f), .65f);
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/BLACKOUT/UI/Fonts/BarlowCondensed/BarlowCondensed-SemiBold.ttf");
            if (!font) throw new InvalidOperationException("Required Barlow Condensed font missing.");
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = Group("BLACKOUT_CargoStation", null);
            var zones = Group("Environment", root);
            var introAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BLACKOUT/Prefabs/Environment/Intro.prefab");
            if (!introAsset) throw new InvalidOperationException("Existing Intro prefab missing.");
            var intro = (GameObject)PrefabUtility.InstantiatePrefab(introAsset, scene);
            intro.name = "01_LoadingBay"; intro.transform.SetParent(zones, false);
            intro.transform.localPosition = Vector3.zero;
            // The changes below are scene-instance overrides only.
            foreach (var t in intro.GetComponentsInChildren<Transform>(true))
                if (t.name == "CargoExit_EndCap" || t.name == "NextAreaLabel" || t.GetComponent<Camera>()) {
                    t.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
                }
            var corridor = Group("02_ContainerCorridor", zones);
            var checkpoint = Group("03_SecurityGate", zones);
            ContainerAssets();
            Corridor(corridor);
            Checkpoint(checkpoint);
            var cameras = Group("Presentation", root);
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); cam.tag = "MainCamera";
            cam.transform.SetParent(cameras, false); cam.transform.position = new Vector3(32, 2.1f, 21);
            cam.transform.LookAt(new Vector3(39, 2.0f, 25));
            var camera = cam.GetComponent<Camera>(); camera.nearClipPlane = .1f; camera.farClipPlane = 150; camera.fieldOfView = 65;
            camera.backgroundColor = new Color(.025f,.035f,.045f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.20f,.25f,.30f);
            RenderSettings.ambientEquatorColor = new Color(.13f,.16f,.19f);
            RenderSettings.ambientGroundColor = new Color(.07f,.085f,.10f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.045f,.062f,.077f); RenderSettings.fogDensity = .009f;
            if (!Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Any()) {
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
                profile.Add<ColorAdjustments>().postExposure.Override(.45f);
                profile.Add<Bloom>().intensity.Override(.16f);
                AssetDatabase.CreateAsset(profile, Art + "/CargoVolume.asset");
                var volume = Group("ColorAndExposure", cameras).gameObject.AddComponent<Volume>(); volume.isGlobal = true; volume.sharedProfile = profile;
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Scene save failed.");
            BlackoutCargoRefinement.Apply();
            BlackoutCargoKoreanSigns.Apply();
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[CargoStation] Created connected environment: " + ScenePath);
        }

        private static void Corridor(Transform zone)
        {
            var architecture = Group("Architecture", zone);
            Hall(architecture, 30, 66, 12, 36, true, true);
            var cargo = Group("CargoStacks", zone);
            Stack(cargo, "SouthRow_Entry", "Ochre", new Vector3(37,0,14.2f), 0, true);
            Stack(cargo, "SouthRow_Maintenance", "Blue", new Vector3(51,0,14.2f), 0, false);
            Stack(cargo, "NorthRow_Archive", "Blue", new Vector3(37,0,33.6f), 0, true);
            Stack(cargo, "NorthRow_Sealed", "Oxide", new Vector3(51,0,33.6f), 0, true);
            Stack(cargo, "Dogleg_EntryScreen", "Oxide", new Vector3(42,0,21), 0, true);
            Stack(cargo, "Dogleg_ExitScreen", "Blue", new Vector3(57,0,28), 0, true);
            var props = Group("MaintenanceAndCover", zone);
            Ground("Props/Crates Barrels Pallets/Pallet Variations/Pallet Variation 02.prefab", "Pallet_RecoveryPocket", V(45,0,27.5f), 90, props);
            Ground("Props/Crates Barrels Pallets/Pallet Variations/Pallet Variation 05.prefab", "Pallet_GateApproach", V(61,0,18), 0, props);
            Ground("Props/Misc Props/Cart.prefab", "MaintenanceCart", V(53,0,17), 90, props);
            Ground("Props/Crates Barrels Pallets/Barrel.prefab", "ServiceDrum", V(31.4f,0,29.5f), 0, props);
            var facilities = Group("Facilities", zone);
            Ground("Structures/Structure Props/Fusebox 01.prefab", "WorklightCircuitBox", V(30.6f,0,27), 0, facilities);
            var details = Group("Wayfinding", zone);
            Sign("EntrySector", "02 / CONTAINER TRANSIT", V(30.4f,3.3f,21), 90, 4.1f,.7f, white.color, details);
            Sign("TurnToGate", "SECURITY 03   >", V(41,2.0f,22.28f), 180, 3.4f,.55f, white.color, details);
            Sign("ExitDirection", "CHECKPOINT 03  /  18 m  >", V(57,1.8f,26.70f), 0, 3.7f,.55f, white.color, details);
            Lane(details, V(32.7f,.012f,21), V(32.7f,.012f,25), true);
            Lane(details, V(32.7f,.012f,25), V(50,.012f,25), true);
            Lane(details, V(50,.012f,25), V(50,.012f,21), true);
            Lane(details, V(50,.012f,21), V(65,.012f,21), true);
            var services = Group("OverheadServices", zone);
            for (int x = 33; x <= 63; x += 6) {
                Box("CableTray_Span_" + x, V(x,6.25f,25), V(5.95f,.12f,.45f), steel, services, false);
                Box("TrayHanger_" + x, V(x,7.65f,25), V(.04f,2.7f,.04f), dark, services, false);
            }
            var lighting = Group("Lighting", zone);
            WorkLight("EntryPool", V(33.3f,5.7f,24.5f), Warm, 8, 16, lighting, true);
            WorkLight("DoglegPool", V(45,7.3f,26), Cool, 11, 19, lighting, true);
            WorkLight("ExitPool", V(59,6.6f,22), Warm, 10, 19, lighting, false);
            var anchors = Group("GameplayAnchors", zone);
            Anchor("Entry_FromLoadingBay", V(31.5f,0,21), anchors);
            Anchor("Tracker_FirstEncounter", V(42,0,25.5f), anchors);
            Anchor("ResidualCharge_RecoveryPocket", V(46.3f,0,28.5f), anchors);
            Anchor("Cover_ContainerCorner", V(49,0,23.8f), anchors);
            Anchor("Exit_ToSecurityGate", V(65,0,21), anchors);
        }

        private static void Checkpoint(Transform zone)
        {
            var architecture = Group("Architecture", zone);
            Hall(architecture, 66, 90, 12, 36, false, false);
            var partitions = Group("CheckpointPartition", architecture);
            // The 6 m opening is closed by a separate, reusable two-leaf gate.
            Place("Structures/Walls/Wall Plain.prefab", "SouthSecurityBulkhead", V(82,0,12), 0, partitions);
            Place("Structures/Walls/Wall Plain.prefab", "NorthSecurityBulkhead", V(82,0,24), 0, partitions);
            Place("Structures/Walls/Wall Fan.prefab", "ServiceBulkhead", V(82,0,30), 0, partitions);
            Box("OpeningHeader", V(82,6.7f,21), V(.65f,4.6f,6), steel, partitions, true);
            var facilities = Group("Facilities", zone);
            Gate(facilities);
            var booth = Group("InspectionBooth", facilities);
            Box("ConcretePlinth", V(76,.18f,31.8f), V(7,.36f,5.5f), concrete, booth, true);
            Box("LowerFrontPanel", V(76,1,29.1f), V(7,1.6f,.18f), dark, booth, true);
            Box("GlazedFront", V(76,2.2f,29.1f), V(7,1,.06f), glass, booth, true);
            Box("RoofCanopy", V(76,3,31.8f), V(7.5f,.28f,5.8f), steel, booth, true);
            Box("WestSide", V(72.5f,1.65f,31.8f), V(.2f,2.6f,5.5f), steel, booth, true);
            Box("EastSide", V(79.5f,1.65f,31.8f), V(.2f,2.6f,5.5f), steel, booth, true);
            for (int i = 0; i < 4; i++) Box("WindowMullion_" + i, V(72.6f+i*2.25f,2.2f,29.03f), V(.075f,1.2f,.14f), steel, booth, false);
            Sign("InspectionLabel", "CARGO INSPECTION / AUTHORIZED STAFF", V(76,2.9f,28.9f), 0, 6.3f,.36f, white.color, booth);
            var catwalk = Group("SecurityServiceGallery", architecture);
            for (int z = 12; z < 36; z += 6)
                Place("Structures/Catwalk/Catwalk Long Rails.prefab", "Gallery_Bay_" + ((z-12)/6+1), V(83,0,z), 0, catwalk);
            var cover = Group("InspectionCargoAndBarriers", zone);
            Stack(cover, "QuarantineContainer", "Ochre", V(68,0,29.2f), 90, false);
            Ground("Props/Crates Barrels Pallets/Pallet Variations/Pallet Variation 05.prefab", "InspectionPallet_South", V(73,0,15.5f), 90, cover);
            Ground("Props/Crates Barrels Pallets/Pallet Variations/Pallet Variation 02.prefab", "InspectionPallet_North", V(76.5f,0,26), 0, cover);
            foreach (float x in new[] {70f,75f}) Barrier("ApproachCrashBarrier_" + x, V(x,0,18), cover);
            foreach (float z in new[] {18.0f,24.0f}) {
                Bollard("ThresholdBollard_W_"+z, V(80,0,z), cover);
                Bollard("ThresholdBollard_E_"+z, V(85.5f,0,z), cover);
            }
            var signs = Group("Wayfinding", zone);
            Sign("GateHeader", "03  /  SECURITY CHECKPOINT", V(81.45f,6,21), 90, 7.1f,.9f, white.color, signs);
            Sign("GateSubheader", "POWER REQUIRED   /   AUTOMATED DEFENCE LINKED", V(81.40f,5.27f,21), 90, 6.6f,.33f, new Color(.88f,.56f,.22f), signs);
            Sign("BoothNumber", "S-03", V(72.37f,2.2f,31.5f), 90, 2,.85f, white.color, signs);
            Sign("NextZoneNotice", "FREIGHT LIFT 04\nNEXT SECTOR / SEALED", V(89.65f,2.7f,21), 90, 4.5f,1.1f, white.color, signs);
            Lane(signs,V(66,.012f,21),V(79,.012f,21),true);
            HazardStrip(signs, V(79.1f,.016f,21), .8f, 5.4f);
            var lighting = Group("Lighting", zone);
            WorkLight("CheckpointApproach", V(72,7.3f,22), Cool, 12, 23, lighting, true);
            WorkLight("GateWorklight", V(78.5f,6.8f,21), Warm, 9, 17, lighting, true);
            WorkLight("InspectionBay", V(76,5.8f,30), Cool, 5, 12, lighting, false);
            WorkLight("BeyondGate", V(87,6.8f,21), Cool, 7, 16, lighting, false);
            var anchors = Group("GameplayAnchors", zone);
            Anchor("Approach_FromContainerCorridor", V(67,0,21), anchors);
            Anchor("PowerSocket_Interact", V(77.3f,0,17), anchors);
            Anchor("GateThreshold", V(82,0,21), anchors);
            Anchor("TurretTargetLane", V(72,1.2f,21), anchors);
            Anchor("Exit_ToFreightLift_Unbuilt", V(88,0,21), anchors);
        }

        private static void Hall(Transform parent, int x0, int x1, int z0, int z1, bool westEntry, bool eastExit)
        {
            var floor = Group("FloorGrid", parent); var walls = Group("PerimeterWalls", parent);
            var roof = Group("RoofAndTrusses", parent); var columns = Group("StructuralColumns", parent);
            for (int x=x0; x<x1; x+=6) for (int z=z0; z<z1; z+=6) {
                string bay = "Bay_" + ((x-x0)/6+1).ToString("00") + "_" + ((z-z0)/6+1).ToString("00");
                Place("Structures/Floor/Floor Tile " + ((x+z)%12==0?"04":"01")+".prefab",bay,V(x,0,z),0,floor);
                Place("Structures/Ceiling/Ceiling Closed.prefab",bay,V(x,9.2f,z),0,roof);
                Place("Structures/Pillars and Beams/Ceiling Support.prefab","Truss_"+bay,V(x,8.9f,z),0,roof);
            }
            for(int x=x0; x<x1; x+=6) {
                Place("Structures/Walls/Wall Plain.prefab","South_Bay_"+(x-x0)/6,V(x+6,0,z0),-90,walls);
                Place("Structures/Walls/Wall "+(x%12==0?"Fan":"Plain")+".prefab","North_Bay_"+(x-x0)/6,V(x,0,z1),90,walls);
                foreach(float z in new[]{z0+.4f,z1-.4f}) Place("Structures/Pillars and Beams/Pillar.prefab","Column_"+x+"_"+z,V(x+.35f,0,z),0,columns);
            }
            for(int z=z0;z<z1;z+=6) {
                if(westEntry) Place("Structures/Walls/Wall "+(z==18?"Corridor":"Plain")+".prefab","West_Bay_"+(z-z0)/6,V(x0,0,z),0,walls);
                Place("Structures/Walls/Wall "+(eastExit&&z==18?"Corridor":"Plain")+".prefab","East_Bay_"+(z-z0)/6,V(x1,0,z+6),180,walls);
            }
        }

        private static void ContainerAssets()
        {
            var colors = new[]{new Color(.36f,.20f,.075f),new Color(.085f,.19f,.24f),new Color(.29f,.095f,.055f)};
            var names = new[]{"Ochre","Blue","Oxide"};
            for(int k=0;k<names.Length;k++) {
                var paint = Mat("Container_"+names[k], colors[k], .25f);
                var faded = Mat("Container_"+names[k]+"_Ribs", colors[k]*1.14f, .25f);
                var go = new GameObject("Container40ft_"+names[k]); var t=go.transform;
                // Real 40-foot proportions. Root is at bottom centre; no non-uniform instance scale.
                Box("SealedHull",V(0,1.3f,0),V(12.19f,2.6f,2.44f),paint,t,true);
                var corrugation = Group("CorrugatedSkin",t);
                for(int i=0;i<49;i++) {
                    float x=-5.94f+i*.2475f;
                    foreach(float z in new[]{-1.255f,1.255f}) Box("PressedRib",V(x,1.32f,z),V(.095f,2.32f,.075f),faded,corrugation,false);
                    Box("RoofRib",V(x,2.64f,0),V(.08f,.065f,2.25f),faded,corrugation,false);
                }
                Merge(corrugation,"Container_"+names[k]+"_Corrugation");
                var frame=Group("CornerCastingsAndEdgeRails",t);
                foreach(float x in new[]{-6.07f,6.07f}) foreach(float z in new[]{-1.22f,1.22f}) {
                    Box("CornerPost",V(x,1.3f,z),V(.16f,2.65f,.16f),steel,frame,false);
                    foreach(float y in new[]{.12f,2.53f}) Box("ISO_Casting",V(x,y,z),V(.22f,.24f,.2f),dark,frame,false);
                }
                foreach(float y in new[]{.12f,2.54f}) foreach(float z in new[]{-1.24f,1.24f}) Box("LongitudinalRail",V(0,y,z),V(12.2f,.10f,.10f),steel,frame,false);
                Merge(frame,"Container_"+names[k]+"_Frame");
                var doors=Group("CargoDoorsAndLockingBars",t);
                foreach(float z in new[]{-.6f,.6f}) {
                    Box("DoorLeaf",V(6.105f,1.3f,z),V(.09f,2.34f,1.14f),paint,doors,false);
                    foreach(float offset in new[]{-.32f,.32f}) {
                        Box("LockingRod",V(6.185f,1.3f,z+offset),V(.065f,2.2f,.065f),steel,doors,false);
                        Box("LatchHandle",V(6.22f,1.06f,z+offset-.08f),V(.08f,.055f,.24f),white,doors,false);
                    }
                    foreach(float y in new[]{.4f,1.3f,2.2f}) Box("DoorHinge",V(6.18f,y,z>0?1.16f:-1.16f),V(.1f,.13f,.17f),steel,doors,false);
                }
                Merge(doors,"Container_"+names[k]+"_Doors");
                var labels=Group("Identification",t);
                Sign("ShippingMark", "BCTU  0"+(k+2)+"871  4", V(-2.8f,1.83f,-1.302f),0,3.3f,.32f,white.color,labels,false);
                Sign("ServiceMark", "BLACKOUT / CARGO TERMINAL", V(1.0f,.66f,-1.302f),0,3.6f,.20f,white.color,labels,false);
                Sign("RearShippingMark", "BCTU  0"+(k+2)+"871  4", V(2.8f,1.83f,1.302f),180,3.3f,.32f,white.color,labels,false);
                Sign("WeightPlate", "MAX. GROSS  30,480 KG\nTARE             3,740 KG", V(6.24f,1.75f,0),-90,1.6f,.35f,white.color,labels);
                foreach(float z in new[]{-1.31f,1.31f}) foreach(float x in new[]{-5.7f,5.7f}) Box("Reflector",V(x,.45f,z),V(.28f,.075f,.012f),ochre,labels,false);
                PrefabUtility.SaveAsPrefabAsset(go,Prefabs+"/Container40ft_"+names[k]+".prefab");
                Object.DestroyImmediate(go);
            }
        }

        private static void Stack(Transform parent,string name,string variant,Vector3 point,float yaw,bool doubleStack)
        {
            var group=Group(name,parent);
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/Container40ft_"+variant+".prefab");
            for(int i=0;i<(doubleStack?2:1);i++) {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene); go.name=i==0?"GroundContainer":"UpperContainer";
                go.transform.SetParent(group,false); go.transform.SetPositionAndRotation(point+Vector3.up*(i*2.7f),Quaternion.Euler(0,yaw,0));
            }
        }

        private static void Gate(Transform parent)
        {
            var gate=Group("SecurityGate_PowerLinked",parent); gate.position=V(82,0,21);
            // Local coordinates are relative to gate plane; x is thickness, z spans the opening.
            var frame=Group("FrameAndDrive",gate);
            foreach(float z in new[]{-3.2f,3.2f}) {
                Box("GuideColumn",V(0,2.4f,z),V(.75f,4.8f,.38f),steel,frame,true);
                Box("SafetyFace",V(-.4f,2.35f,z),V(.04f,4.25f,.22f),ochre,frame,false);
            }
            Box("DriveHousing",V(0,4.65f,0),V(1,.5f,6.8f),dark,frame,true);
            var south=Group("SouthSlidingLeaf",gate); var north=Group("NorthSlidingLeaf",gate);
            foreach(var leaf in new[]{south,north}) {
                float sign=leaf==south?-1:1;
                Box("ArmouredPanel",V(0,2.18f,sign*1.48f),V(.25f,4.35f,2.94f),steel,leaf,true);
                for(int i=0;i<9;i++) Box("PressedHorizontalRib",V(-.17f,.28f+i*.465f,sign*1.48f),V(.1f,.1f,2.8f),dark,leaf,false);
                Box("CentreSealingRail",V(-.18f,2.18f,sign*.06f),V(.1f,4.35f,.10f),ochre,leaf,false);
                Sign("GateLeafStencil",leaf==south?"SECURITY":"S - 03",V(-.245f,2.3f,sign*1.48f),90,2.2f,.50f,white.color,leaf);
            }
            var socket=Group("PowerSocket",gate);
            Box("SocketCabinet",V(-4,1.25f,-4.2f),V(.7f,2.5f,1.3f),dark,socket,true);
            Box("Recess",V(-4.38f,1.45f,-4.2f),V(.05f,.7f,.7f),steel,socket,false);
            Box("CellSlot",V(-4.42f,1.45f,-4.2f),V(.03f,.48f,.23f),cyan,socket,false);
            Sign("SocketInstruction","INSERT 1 CELL\nDOOR + TURRET",V(-4.43f,2.15f,-4.2f),90,1.1f,.42f,white.color,socket);
            var conduit=Group("SharedPowerConduit",gate);
            Box("CabinetRiser",V(-3.9f,3.8f,-4.2f),V(.085f,2.6f,.085f),steel,conduit,false);
            Box("OverheadFeed",V(-1.95f,5.1f,-4.2f),V(3.9f,.085f,.085f),steel,conduit,false);
            Box("DoorAndTurretBus",V(-.55f,5.1f,-1.0f),V(.085f,.085f,6.4f),steel,conduit,false);
            var turret=Group("Turret_VisualMount",gate);
            Box("Mount",V(-.4f,4.9f,3.6f),V(1,.4f,1),steel,turret,false);
            Box("Swivel",V(-.65f,4.65f,3.6f),V(.55f,.55f,.65f),dark,turret,false);
            Box("ArmouredReceiver",V(-1.08f,4.45f,3.6f),V(.9f,.42f,.70f),steel,turret,false);
            foreach(float z in new[]{3.4f,3.8f}) {
                Box("Barrel",V(-1.85f,4.40f,z),V(1,.13f,.13f),dark,turret,false);
                Box("Muzzle",V(-2.35f,4.4f,z),V(.2f,.20f,.20f),steel,turret,false);
            }
            var locked=Group("State_Blackout",gate); var powered=Group("State_Powered",gate); var active=Group("State_TurretArmed",gate);
            Box("RedStatusLens",V(-.56f,4.66f,0),V(.08f,.15f,1.4f),red,locked,false);
            Sign("LockedLabel","OFFLINE / LOCKED",V(-.58f,3.65f,0),90,2.5f,.38f,white.color,locked);
            Box("CyanStatusLens",V(-.56f,4.66f,0),V(.08f,.15f,1.4f),cyan,powered,false);
            Box("TurretOptic",V(-1.58f,4.47f,3.6f),V(.03f,.1f,.14f),red,active,false);
            Point("ArmedWarning",V(-1.8f,4.6f,3.6f),new Color(1,.045f,.015f),2.5f,7,active);
            gate.gameObject.AddComponent<BlackoutSecurityGatePreview>().Configure(south,north,locked.gameObject,powered.gameObject,active.gameObject);
            PrefabUtility.SaveAsPrefabAssetAndConnect(gate.gameObject,Prefabs+"/SecurityGate_PowerLinked.prefab",InteractionMode.AutomatedAction);
        }

        private static void Barrier(string name,Vector3 p,Transform parent)
        {
            var g=Group(name,parent);g.position=p;
            Box("ConcreteBase",V(0,.35f,0),V(2.7f,.7f,.65f),concrete,g,true);
            Box("SteelRail",V(0,.9f,0),V(2.8f,.35f,.25f),ochre,g,true);
            foreach(float x in new[]{-1f,1f}) Box("RailPost",V(x,.8f,0),V(.12f,1.4f,.12f),steel,g,true);
        }
        private static void Bollard(string name,Vector3 p,Transform parent)
        {
            var g=Group(name,parent);g.position=p;
            Box("BasePlate",V(0,.04f,0),V(.45f,.08f,.45f),steel,g,true);
            Box("ImpactPost",V(0,.68f,0),V(.24f,1.3f,.24f),ochre,g,true);
            Box("BlackBand",V(0,.9f,0),V(.245f,.22f,.245f),dark,g,false);
        }
        private static void WorkLight(string name,Vector3 p,Color c,float intensity,float range,Transform parent,bool shadows)
        {
            var g=Group(name,parent);
            Place("Props/Misc Props/Hanging Light.prefab","Fixture",p+Vector3.up*.85f,0,g);
            var light=Point("LightPool",p,c,intensity,range,g);
            light.type=LightType.Spot;light.spotAngle=115;light.innerSpotAngle=85;light.transform.rotation=Quaternion.Euler(90,0,0);
            light.shadows=shadows?LightShadows.Soft:LightShadows.None;light.shadowBias=.035f;light.shadowNormalBias=.15f;
            Box("LuminousLens",p+Vector3.up*.15f,V(.4f,.035f,.4f),glow,g,false);
        }
        private static Light Point(string name,Vector3 p,Color c,float intensity,float range,Transform parent)
        {
            var g=Group(name,parent);g.localPosition=p;var l=g.gameObject.AddComponent<Light>();
            l.type=LightType.Point;l.color=c;l.intensity=intensity;l.range=range;return l;
        }
        private static void Lane(Transform parent,Vector3 a,Vector3 b,bool dashed)
        {
            var g=Group("RouteMark_"+a.x+"_"+a.z,parent);float distance=Vector3.Distance(a,b);var dir=(b-a).normalized;
            for(float d=0;d<distance-.2f;d+=dashed?2f:distance) {
                var mark=Box("PaintSegment",a+dir*(d+.45f),V(.9f,.007f,.065f),ochre,g,false);
                mark.transform.rotation=Quaternion.LookRotation(Vector3.Cross(Vector3.up,dir));
            }
        }
        private static void HazardStrip(Transform parent,Vector3 p,float width,float length)
        {
            var g=Group("GateKeepClearMarking",parent);g.position=p;
            Box("BlackPaint",Vector3.zero,V(width,.006f,length),dark,g,false);
            for(float z=-length/2+.25f;z<length/2;z+=.5f) {
                var b=Box("OchreStripe",V(0,.005f,z),V(width*.85f,.006f,.18f),ochre,g,false);b.transform.localRotation=Quaternion.Euler(0,25,0);
            }
        }
        private static GameObject Place(string relative,string name,Vector3 p,float yaw,Transform parent)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(BlackoutIntroAudit.Kit+relative);
            if(!asset) throw new InvalidOperationException("Missing prefab: "+relative);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);go.name=name;go.transform.SetParent(parent,false);
            go.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
            foreach(var r in go.GetComponentsInChildren<Renderer>(true)) {
                r.sharedMaterials=r.sharedMaterials.Select(KitMaterial).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            foreach(var l in go.GetComponentsInChildren<Light>(true)) l.enabled=false;
            foreach(var a in go.GetComponentsInChildren<AudioSource>(true)) a.enabled=false;
            foreach(var body in go.GetComponentsInChildren<Rigidbody>(true)) {body.isKinematic=true;body.useGravity=false;}
            return go;
        }
        private static GameObject Ground(string relative,string name,Vector3 bottom,float yaw,Transform parent)
        {
            var go=Place(relative,name,Vector3.zero,yaw,parent);var b=BlackoutIntroAudit.BoundsOf(go);
            go.transform.position=bottom-new Vector3(b.center.x,b.min.y,b.center.z);return go;
        }
        private static Material KitMaterial(Material source)
        {
            if(!source) throw new InvalidOperationException("Missing source material.");
            if(kitMaterials.TryGetValue(source,out var found))return found;
            string safe=string.Concat(source.name.Select(c=>char.IsLetterOrDigit(c)?c:'_'));
            string path=Art+"/Materials/Kit_"+safe+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat) {
                mat=new Material(source); mat.name="Cargo_"+source.name;
                mat.SetFloat("_Metallic",Mathf.Min(mat.GetFloat("_Metallic"),.4f));mat.SetFloat("_Smoothness",.3f);
                mat.SetColor("_EmissionColor",Color.black);mat.DisableKeyword("_EMISSION");AssetDatabase.CreateAsset(mat,path);
            }
            kitMaterials[source]=mat;return mat;
        }
        private static Material Mat(string name,Color color,float metallic,float emission=0)
        {
            string path=Art+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) {m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.26f);
            if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}
            return m;
        }
        private static GameObject Box(string name,Vector3 p,Vector3 size,Material material,Transform parent,bool collider)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);
            g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider)Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        private static void Sign(string name,string text,Vector3 p,float yaw,float width,float height,Color color,Transform parent,bool backing=true)
        {
            text=BlackoutCargoKoreanSigns.Translate(text);
            var signFont=BlackoutCargoKoreanSigns.HasKorean(text)?AssetDatabase.LoadAssetAtPath<Font>(BlackoutCargoKoreanSigns.FontPath):font;
            var g=Group(name,parent);g.localPosition=p;g.localRotation=Quaternion.Euler(0,yaw,0);
            if(backing)Box("Plate",Vector3.zero,V(width,height,.03f),dark,g,false);
            var label=new GameObject("Lettering",typeof(TextMesh));label.transform.SetParent(g,false);label.transform.localPosition=V(0,0,-.025f);
            var tm=label.GetComponent<TextMesh>();tm.font=signFont;tm.fontSize=96;tm.characterSize=.055f;tm.text=text;
            tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=color;
            var renderer=label.GetComponent<MeshRenderer>();renderer.sharedMaterial=signFont.material;
            signFont.RequestCharactersInTexture(text,96,FontStyle.Normal);
            var size=renderer.bounds.size;float wide=Mathf.Max(size.x,size.z);
            float fit=Mathf.Min(width*.9f/Mathf.Max(.01f,wide),height*.75f/Mathf.Max(.01f,size.y));label.transform.localScale=Vector3.one*fit;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        // Collapse repeated ribs into a small number of material batches, not hundreds of Hierarchy entries.
        private static void Merge(Transform group,string meshName)
        {
            var renderers=group.GetComponentsInChildren<MeshRenderer>();
            var byMaterial=renderers.GroupBy(r=>r.sharedMaterial).ToArray();
            foreach(var batch in byMaterial) {
                var combines=batch.Select(r=>new CombineInstance {mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=group.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
                var mesh=new Mesh {name=meshName+"_"+batch.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combines,true,true);mesh.RecalculateBounds();
                string path=Art+"/Meshes/"+mesh.name+".asset";AssetDatabase.CreateAsset(mesh,path);
                var child=Group(batch.Key.name,group).gameObject;child.AddComponent<MeshFilter>().sharedMesh=mesh;child.AddComponent<MeshRenderer>().sharedMaterial=batch.Key;
            }
            foreach(var r in renderers)Object.DestroyImmediate(r.gameObject);
        }
        private static Transform Group(string name,Transform parent){var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
        private static void Anchor(string name,Vector3 p,Transform parent){Group(name,parent).localPosition=p;}
        private static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        private static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;var parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
}
