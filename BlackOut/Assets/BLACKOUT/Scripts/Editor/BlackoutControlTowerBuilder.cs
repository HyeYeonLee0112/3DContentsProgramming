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
    public static partial class BlackoutControlTowerBuilder
    {
        public const string Art="Assets/BLACKOUT/Art/ControlTower";
        public const string Prefabs="Assets/BLACKOUT/Prefabs/Environment/ControlTower";
        public const string Materials="Assets/BLACKOUT/Art/Materials/CargoStation";
        public const string TowerMaterials="Assets/BLACKOUT/Art/Materials/ControlTower";
        public static string Output=>Path.GetFullPath("../docs/images/production/control-tower");
        private static Material steel,dark,paint,ochre,concrete,rubber,ivory,cyan,red,lamp,screen,city;
        private static Font font;
        private static Material lettering;
        private static Transform arena;
        private static GameObject leftOn,rightOn,leftAlert,rightAlert,coolOn,coolOff,restoreOn,restoreOff;

        [MenuItem("BLACKOUT/Control Tower/1 Add arena (new only)")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scenes first.");
            if(SceneManager.GetActiveScene().path!=BlackoutCargoBuilder.ScenePath)EditorSceneManager.OpenScene(BlackoutCargoBuilder.ScenePath);
            var environment=GameObject.Find("BLACKOUT_CargoStation/Environment").transform;
            if(environment.Find("05_ControlTower")||environment.Find("04_FreightLiftArrival"))throw new InvalidOperationException("Arena exists; refusing to overwrite.");
            Folder(Art+"/Meshes");Folder(TowerMaterials);Folder(Art+"/Reflections");Folder(Prefabs);Directory.CreateDirectory(Output);
            LoadMaterials();
            var checkpoint=environment.Find("03_SecurityGate");
            var endcap=checkpoint.Find("Architecture/PerimeterWalls/East_Bay_1");
            if(!endcap||Vector3.Distance(endcap.position,V(90,0,24))>.1f)throw new InvalidOperationException("Expected checkpoint endcap changed; inspect before connecting.");
            // Retain the original wall for recovery, and open only this exact six-metre bay.
            endcap.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(endcap.gameObject);
            var oldNotice=checkpoint.Find("Wayfinding/NextZoneNotice");if(oldNotice)oldNotice.gameObject.SetActive(false);
            var oldAnchor=checkpoint.Find("GameplayAnchors/Exit_ToFreightLift_Unbuilt");if(oldAnchor)oldAnchor.name="Exit_ToFreightLiftArrival";
            Arrival(G("04_FreightLiftArrival",environment));
            arena=G("05_ControlTower",environment);
            Architecture(); CombatLayout(); Facilities(); LightingAndVista(); WayfindingAndAnchors();
            var preview=arena.gameObject.AddComponent<BlackoutControlTowerPreview>();
            preview.Configure(leftOn,rightOn,leftAlert,rightAlert,coolOn,coolOff,restoreOn,restoreOff);
            preview.PreviewBlackout();
            var probe=G("ControlTower_Reflection",arena.Find("Lighting")).gameObject.AddComponent<ReflectionProbe>();
            probe.transform.position=V(126,10,24);probe.size=V(36,12,36);probe.center=Vector3.zero;
            probe.resolution=128;probe.hdr=true;probe.boxProjection=true;probe.blendDistance=2;probe.farClipPlane=75;
            var cam=Camera.main;cam.transform.position=V(111,8.1f,26.7f);cam.transform.LookAt(V(130,8.4f,24));cam.fieldOfView=65;
            Save(); Selection.activeGameObject=arena.gameObject;
            File.WriteAllText(Output+"/build-status.txt","completed");
        }

        private static void LoadMaterials()
        {
            steel=Existing("Steel_Brushed");dark=Existing("Steel_Gunmetal");paint=Existing("Paint_Gate");ochre=Existing("Safety_Ochre");
            concrete=Existing("Concrete_Dry");rubber=Existing("Rubber_Cable");
            ivory=Flat("Stencil_Ivory",new Color(.72f,.76f,.71f));
            cyan=Flat("Power_Cyan",new Color(.16f,.57f,.66f),2);
            red=Flat("Danger_Red",new Color(.62f,.035f,.018f),2);
            lamp=Flat("Worklight_White",new Color(.88f,.94f,1),3);
            screen=Flat("Display_Standby",new Color(.025f,.08f,.09f),.6f);
            city=Flat("City_Window",new Color(.52f,.63f,.64f),1.5f);
            font=AssetDatabase.LoadAssetAtPath<Font>(BlackoutCargoKoreanSigns.FontPath);
            lettering=AssetDatabase.LoadAssetAtPath<Material>("Assets/BLACKOUT/Art/CargoStation/Materials/Sign_Pretendard-SemiBold.mat");
            if(!font||!lettering)throw new InvalidOperationException("Korean font/sign material missing.");
        }
        private static void Arrival(Transform root)
        {
            var structure=G("Architecture",root);var floor=G("LowerFloor",structure);var envelope=G("Envelope",structure);
            for(int x=90;x<108;x+=6)for(int z=12;z<36;z+=6)Kit("Structures/Floor/Floor Tile 01.prefab","Deck_"+x+"_"+z,V(x,0,z),0,floor,true);
            Box("NorthWall",V(99,7,36),V(18,14,.4f),paint,envelope,true);
            Box("SouthWall",V(99,7,12),V(18,14,.4f),paint,envelope,true);
            Box("UpperWestWall",V(90,11.5f,24),V(.4f,5,24),paint,envelope,true);
            Box("Ceiling",V(99,14,24),V(18,.3f,24),dark,envelope,true);
            var stairs=G("MaintenanceStair",structure);
            for(int i=0;i<36;i++) {
                float x=94+(i+.5f)/3f,y=(i+1)/6f;
                Box("Tread_"+(i+1).ToString("00"),V(x,y-.09f,27),V(.334f,.18f,3.6f),steel,stairs,false);
                Box("SafetyNosing_"+(i+1).ToString("00"),V(x-.15f,y+.008f,27),V(.04f,.016f,3.6f),ochre,stairs,false);
            }
            var ramp=new Mesh{name="MaintenanceStair_WalkSurface"};ramp.vertices=new[]{V(94,0,25.2f),V(94,0,28.8f),V(106,6,25.2f),V(106,6,28.8f)};ramp.triangles=new[]{0,1,2,2,1,3};ramp.RecalculateNormals();ramp.RecalculateBounds();
            AssetDatabase.CreateAsset(ramp,Art+"/Meshes/MaintenanceStair_WalkSurface.asset");G("SmoothWalkCollision",stairs).gameObject.AddComponent<MeshCollider>().sharedMesh=ramp;
            foreach(float z in new[]{25.15f,28.85f}) {
                Beam("Stringer",V(94,-.2f,z),V(106,5.8f,z),.16f,.35f,dark,stairs,false);
                Beam("Handrail",V(94,1.1f,z),V(106,7.1f,z),.07f,.07f,ochre,stairs,true);
                for(int i=0;i<=6;i++)Box("GuardPost",V(94+i*2,i+.55f,z),V(.07f,1.1f,.07f),steel,stairs,false);
            }
            Box("UpperLanding",V(107,5.85f,27),V(2,.3f,4),steel,structure,true);
            foreach(float z in new[]{25f,29f})Box("LandingGuard",V(107,6.65f,z),V(2,1.3f,.08f),dark,structure,true);
            var lift=G("FreightLift_VisualOnly",root);
            Box("LiftBase",V(99,.08f,18.5f),V(7,.16f,7),steel,lift,true);
            foreach(float x in new[]{95.6f,102.4f})foreach(float z in new[]{15.1f,21.9f}){
                Box("LiftGuide",V(x,6.2f,z),V(.3f,12.4f,.3f),ochre,lift,true);
                Box("GuideFoot",V(x,.3f,z),V(.7f,.6f,.7f),dark,lift,true);
            }
            Box("LiftDriveCrosshead",V(99,12.2f,18.5f),V(7.3f,.7f,7.3f),dark,lift,false);
            foreach(float x in new[]{96.4f,101.6f})Beam("Suspension",V(x,.25f,18.5f),V(x,12,18.5f),.06f,.06f,rubber,lift,false);
            Box("SafetyRearFence",V(102.5f,1.2f,18.5f),V(.12f,2.4f,7),dark,lift,true);
            var signs=G("Wayfinding",root);
            Sign("LiftNotice","04  화물 승강장",V(90.35f,3.7f,21),90,4.8f,.75f,signs);
            Sign("MaintenanceRoute","관제탑 ↑\n점검 계단 이용",V(94,2.8f,27),90,3.1f,1.1f,signs);
            Sign("LiftOutOfService","승강기 점검 중",V(95.45f,2.7f,18.5f),90,3.6f,.65f,signs);
            var preparation=G("BossPreparation",root);
            Box("CheckpointCabinet",V(92,1,31),V(1.1f,2,1.2f),paint,preparation,true);
            Box("StationStatus",V(92.62f,1.3f,31),V(.08f,.35f,.65f),cyan,preparation,false);
            Sign("PreparationNotice","최종 관제실\n정비 · 저장 지점",V(92.7f,2.8f,31),-90,3,1,preparation);
            var anchors=G("GameplayAnchors",root);
            Anchor("Checkpoint_BeforeBoss",V(93.8f,0,30),anchors);Anchor("RecoveryStation_Interact",V(93.8f,0,31),anchors);
            var lights=G("Lighting",root);
            Point("ArrivalPool",V(94,6,24),new Color(.92f,.69f,.4f),16,15,lights);
            Point("UpperLandingPool",V(105,10,28),new Color(.53f,.69f,.8f),22,16,lights);
        }

        private static void Architecture()
        {
            var architecture=G("Architecture",arena);var floor=G("ArenaFloor",architecture);var walls=G("Envelope",architecture);var roof=G("RoofAndTrusses",architecture);var columns=G("StructuralColumns",architecture);
            for(int x=108;x<144;x+=6)for(int z=6;z<42;z+=6){
                Kit("Structures/Floor/Floor Tile 01.prefab","Slab_"+x+"_"+z,V(x,6,z),0,floor,true);
                Kit("Structures/Ceiling/Ceiling Closed.prefab","RoofBay_"+x+"_"+z,V(x,18,z),0,roof);
            }
            for(int x=108;x<144;x+=6){
                Kit("Structures/Walls/Wall "+(x%12==0?"Fan":"Plain")+".prefab","SouthServiceWall_"+x,V(x+6,6,6),-90,walls);
                Kit("Structures/Walls/Wall Plain.prefab","NorthServiceWall_"+x,V(x,6,42),90,walls);
                Box("SouthClerestory",V(x+3,16.4f,6),V(6,3,.4f),paint,walls,true);
                Box("NorthClerestory",V(x+3,16.4f,42),V(6,3,.4f),paint,walls,true);
            }
            // West arrival opening remains 4 m wide, 4.4 m tall; no hidden solid-wall collider.
            Box("ArrivalSouthBulkhead",V(108,12,15.5f),V(.45f,12,19),paint,walls,true);
            Box("ArrivalNorthBulkhead",V(108,12,35.5f),V(.45f,12,13),paint,walls,true);
            Box("ArrivalHeader",V(108,14.2f,27),V(.65f,7.6f,4),dark,walls,true);
            foreach(float z in new[]{25f,29f})Box("ArrivalDoorJamb",V(108.3f,8.1f,z),V(.8f,4.2f,.23f),ochre,walls,true);
            var glazing=G("ObservationFacade",architecture);
            Box("WindowSill",V(144,7.1f,24),V(.5f,2.2f,36),paint,glazing,true);
            Box("WindowHeader",V(144,16.5f,24),V(.5f,3,36),dark,glazing,true);
            for(int z=6;z<=42;z+=6)Box("FacadeMullion_"+z,V(144,11.6f,z),V(.45f,7,.22f),steel,glazing,true);
            Box("FacadeTransom",V(144,12.2f,24),V(.4f,.13f,36),steel,glazing,false);
            var glass=Flat("Observation_Glass",new Color(.09f,.18f,.21f,.09f));
            glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Smoothness",.8f);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
            for(int z=6;z<42;z+=6)Box("GlassPane_"+z,V(144.06f,11.6f,z+3),V(.045f,6.8f,5.76f),glass,glazing,true);
            foreach(float x in new[]{113.5f,125.5f,137.5f})foreach(float z in new[]{6.65f,41.35f}){
                Box("Column",V(x,12,z),V(.5f,12,.7f),dark,columns,true);
                Box("ColumnFoot",V(x,6.7f,z),V(.75f,1.4f,.9f),ochre,columns,true);
            }
            for(int x=114;x<=138;x+=12){
                Box("RoofTie_"+x,V(x,17.1f,24),V(.35f,.65f,35),steel,roof,false);
                for(int z=8;z<40;z+=4)Beam("TrussDiagonal",V(x,17,z),V(x,18,z+4),.09f,.09f,steel,roof,false);
            }
            var galleries=G("ServiceGalleries",architecture);
            foreach(float z in new[]{8f,40f}){
                Box("ServiceDeck",V(126,10.8f,z),V(34,.22f,2.7f),steel,galleries,true);
                float inner=z<24?z+1.35f:z-1.35f;
                Box("GuardTop",V(126,12,inner),V(34,.08f,.08f),ochre,galleries,false);
                Box("GuardMid",V(126,11.5f,inner),V(34,.055f,.055f),steel,galleries,false);
                for(int x=110;x<=142;x+=4)Box("GalleryPost",V(x,11.45f,inner),V(.07f,1.25f,.07f),steel,galleries,false);
            }
            var services=G("OverheadServices",architecture);
            foreach(float z in new[]{11f,37f}){
                Pipe("MainCoolingReturn",V(110,15.7f,z),V(143,15.7f,z),.32f,steel,services);
                for(int x=112;x<144;x+=4){Box("PipeHanger",V(x,16.7f,z),V(.08f,2,.08f),dark,services,false);}
            }
        }

        private static void CombatLayout()
        {
            var cover=G("CombatLayout",arena);
            foreach(float x in new[]{118f,134f})foreach(float z in new[]{16f,32f}){
                var bank=G((z<24?"South":"North")+(x<126?"Entry":"Far")+"CoverBank",cover);
                bank.localPosition=V(x,6,z);
                Box("ConcreteFooting",V(0,.24f,0),V(4,.48f,1.4f),concrete,bank,true);
                Box("ArmouredCover",V(0,.85f,0),V(3.8f,1.1f,1.15f),paint,bank,true);
                Box("TopCap",V(0,1.42f,0),V(4,.12f,1.4f),steel,bank,false);
                foreach(float side in new[]{-1f,1f})Box("SafetyEnd",V(side*1.85f,.83f,0),V(.1f,1.05f,1.18f),ochre,bank,false);
                foreach(float a in new[]{-1.1f,0,1.1f})Box("FrontVent",V(a,.75f,z<24?.59f:-.59f),V(.7f,.44f,.025f),dark,bank,false);
            }
            var markings=G("FloorMarkings",cover);
            Ring("CoolingServiceBoundary",V(126,6.018f,24),3.8f,4.0f,ochre,markings,48);
            foreach(float z in new[]{12f,36f}){
                Box("OuterLaneEdge",V(126,6.018f,z),V(29,.018f,.07f),ivory,markings,false);
                for(int x=111;x<=141;x+=3)Box("CautionLaneDash",V(x,6.025f,z+(z<24?-.22f:.22f)),V(1.4f,.02f,.13f),ochre,markings,false);
            }
            foreach(float x in new[]{114f,138f})Box("CrossLaneEdge",V(x,6.018f,24),V(.07f,.018f,20),ivory,markings,false);
            Ring("BossStandbyMark",V(134,6.02f,24),2.3f,2.36f,ivory,markings,32);
        }

        private static void Facilities()
        {
            var facilities=G("Facilities",arena);
            Circuit(facilities,"South_WorklightCircuit",V(122,6,9.6f),0,out leftOn,out leftAlert);
            Circuit(facilities,"North_WorklightCircuit",V(122,6,38.4f),180,out rightOn,out rightAlert);
            Cooling(facilities); Restoration(facilities);
            var desks=G("OperatorConsoleBanks",facilities);
            foreach(float z in new[]{12f,16f,32f,36f}){
                var desk=G("ConsoleBank_"+z,desks);desk.localPosition=V(141,6,z);desk.localRotation=Quaternion.Euler(0,90,0);
                Box("EquipmentBase",V(0,.5f,0),V(2.8f,1,.9f),paint,desk,true);
                var top=Box("SlopedControlDesk",V(0,1.15f,0),V(2.9f,.12f,1.15f),dark,desk,true);top.transform.localRotation=Quaternion.Euler(-12,0,0);
                foreach(float x in new[]{-.73f,.73f}){
                    Box("ScreenFrame",V(x,1.7f,.27f),V(1.25f,.8f,.14f),dark,desk,false);
                    Box("StandbyDisplay",V(x,1.7f,.185f),V(1.1f,.65f,.025f),screen,desk,false);
                    for(int i=0;i<3;i++)Box("DisplayTrace",V(x-.2f,1.85f-i*.16f,.165f),V(.6f-i*.13f,.025f,.008f),cyan,desk,false);
                }
                Box("Keyboard",V(0,1.25f,-.33f),V(1.5f,.04f,.25f),steel,desk,false);
            }
        }

        private static void Circuit(Transform parent,string name,Vector3 p,float yaw,out GameObject active,out GameObject alert)
        {
            var root=G(name,parent);
            Box("CabinetBody",V(0,1.05f,0),V(1.45f,2.1f,.8f),paint,root,true);
            Box("DoorPanel",V(0,1.1f,-.43f),V(1.26f,1.8f,.08f),dark,root,false);
            Socket(root,V(0,1.1f,-.52f),1);
            Sign("CircuitLabel","작업등\n전력 1칸",V(0,2.65f,-.05f),0,2.7f,1,root);
            Sign("TurretWarning","주의 · 포탑 동시 작동",V(0,.35f,-.5f),0,1.25f,.28f,root);
            // Panel fronts face the arena rather than the outer wall.
            root.localRotation=Quaternion.Euler(0,yaw+180,0);root.localPosition=p;
            var on=G("Powered_Worklights",root);active=on.gameObject;
            Box("SocketLive",V(0,1.1f,-.65f),V(.24f,.42f,.04f),cyan,on,false);
            float side=p.z<24?1:-1;
            var assembly=G(name+"_LightAndTurretRig",parent);
            foreach(float x in new[]{117f,133f}){
                var housing=G("FloodlightHousing_"+x,assembly);housing.localPosition=V(x,12.7f,p.z+side*.2f);housing.LookAt(V(126,6,24));
                Box("CastHousing",Vector3.zero,V(1.5f,.65f,.35f),dark,housing,false);
                var lens=Box("PoweredLens",V(0,0,.19f),V(1.28f,.44f,.05f),lamp,housing,false);
                lens.transform.SetParent(on,true);
                var light=G("ArenaFlood_"+x,on).gameObject.AddComponent<Light>();light.transform.position=housing.position+housing.forward*.4f;light.transform.rotation=housing.rotation;
                light.type=LightType.Spot;light.color=new Color(.76f,.87f,1);light.intensity=95;light.range=33;light.spotAngle=85;light.innerSpotAngle=48;light.shadows=LightShadows.None;
            }
            var turret=G("LinkedTurret_VisualOnly",assembly);turret.localPosition=V(135,11.1f,p.z+side*.9f);turret.LookAt(V(126,8,24));
            Cylinder("RotationBase",V(0,0,0),V(.95f,.2f,.95f),steel,turret,false);
            Box("TurretArmour",V(0,.35f,0),V(.95f,.65f,1.3f),paint,turret,false);
            Box("Barrel",V(0,.38f,1),V(.22f,.22f,1.2f),dark,turret,false);
            Box("Muzzle",V(0,.38f,1.6f),V(.34f,.34f,.18f),steel,turret,false);
            var warning=G("Powered_TurretAlert",turret);alert=warning.gameObject;
            Box("DangerSensor",V(0,.64f,.7f),V(.36f,.1f,.05f),red,warning,false);
            Point("WarningSpill",V(0,.6f,1),new Color(1,.09f,.025f),2,3,warning);
            Pipe("SharedCircuitConduit",V(p.x,8,p.z),V(p.x,12,p.z),.075f,rubber,assembly);
            Pipe("SharedOverheadFeed",V(p.x,12,p.z),V(135,12,p.z),.075f,rubber,assembly);
        }

        private static void Cooling(Transform parent)
        {
            var root=G("CentralCooling_TwoCell",parent);
            Cylinder("Foundation",V(0,.12f,0),V(4.4f,.12f,4.4f),dark,root,true);
            Cylinder("HeatExchanger",V(0,.63f,0),V(2.8f,.43f,2.8f),steel,root,true);
            for(int i=0;i<12;i++){
                float a=i*Mathf.PI/6;var rib=Box("CoolingFin",V(Mathf.Cos(a)*1.47f,.65f,Mathf.Sin(a)*1.47f),V(.13f,.8f,.55f),paint,root,false);rib.transform.localRotation=Quaternion.Euler(0,-i*30,0);
            }
            Ring("SafetyCollar",V(0,1.09f,0),1.2f,1.55f,ochre,root,32);
            var off=G("Idle_ClosedShutter",root);coolOff=off.gameObject;
            Cylinder("ArmouredLid",V(0,1.12f,0),V(2.4f,.07f,2.4f),dark,off,false);
            var on=G("Active_CoolingCore",root);coolOn=on.gameObject;
            Cylinder("CoolantCore",V(0,1.12f,0),V(1.9f,.1f,1.9f),cyan,on,false);
            Ring("CoreRim",V(0,1.34f,0),.85f,1,steel,on,32);
            Point("CoolantBounce",V(0,1.6f,0),new Color(.25f,.78f,1),9,7,on);
            foreach(float z in new[]{-2.35f,2.35f}){
                var station=G(z<0?"SouthInteractionPanel":"NorthInteractionPanel",root);station.localPosition=V(0,0,z);station.localRotation=Quaternion.Euler(0,z<0?0:180,0);
                Box("PanelHousing",V(0,.6f,0),V(1.9f,1.2f,.5f),paint,station,true);Socket(station,V(0,.75f,-.3f),2);
                Sign("RequiredCharge","중앙 냉각기\n전력 2칸",V(0,1.6f,0),0,2.5f,.8f,station);
                Sign("CoolingWindow","냉각 중 약점 노출 · 8초",V(0,.3f,-.31f),0,1.7f,.22f,station);
            }
            root.localPosition=V(126,6,24);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Prefabs+"/CoolingUnit_TwoCell.prefab");
        }

        private static void Restoration(Transform parent)
        {
            var root=G("GridRestorationConsole",parent);root.localPosition=V(142,6,24);root.localRotation=Quaternion.Euler(0,90,0);
            Box("CabinetBase",V(0,.75f,0),V(4.2f,1.5f,1.2f),paint,root,true);
            Box("ControlDesk",V(0,1.5f,-.35f),V(4.4f,.2f,1.1f),steel,root,false);
            Box("DisplayFrame",V(0,3.4f,.3f),V(6.4f,3.2f,.35f),dark,root,true);
            Box("DisplayBack",V(0,3.4f,.09f),V(6,2.8f,.06f),screen,root,false);
            var idle=G("Offline_GridStatus",root);restoreOff=idle.gameObject;
            Sign("OfflineNotice","도시 전력망\n송전 중단",V(0,3.7f,.04f),0,5,1.3f,idle);
            var on=G("Online_GridStatus",root);restoreOn=on.gameObject;
            Sign("RestoredNotice","도시 전력망\n송전 복구 완료",V(0,3.7f,.035f),0,5,1.3f,on);
            for(int i=0;i<7;i++)Box("GridLiveBar",V(-2.4f+i*.8f,2.5f,0),V(.5f,.18f,.04f),cyan,on,false);
            Socket(root,V(0,1.2f,-.97f),1);
            Sign("FinalObjective","최종 송전 장치",V(0,5.5f,0),0,6.7f,.8f,root);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Prefabs+"/GridRestorationConsole.prefab");
        }

        private static void LightingAndVista()
        {
            var lighting=G("Lighting",arena);
            // A readable blackout base, with one shadow-casting light for this new arena.
            var key=Point("HighWindowAmbient",V(137,15,24),new Color(.51f,.68f,.82f),52,32,lighting);key.shadows=LightShadows.Soft;key.shadowStrength=.75f;
            Point("EntryEmergencyPool",V(112,11,27),new Color(.9f,.62f,.32f),18,18,lighting);
            Point("CoolingReadability",V(126,12,24),new Color(.57f,.7f,.77f),22,19,lighting);
            foreach(float z in new[]{9f,39f}){
                for(int x=113;x<=137;x+=12){
                    Box("EmergencyFixture",V(x,8.8f,z),V(1.1f,.16f,.25f),lamp,lighting,false);
                    Point("EmergencyPool_"+x+"_"+z,V(x,8.6f,z),new Color(.95f,.68f,.38f),3.5f,8,lighting);
                }
            }
            var vista=G("ExteriorVista_NonPlayable",arena);var buildings=G("DistantCargoDistrict",vista);var restoredWindows=G("RestoredCityWindows",restoreOn.transform);
            for(int i=0;i<18;i++){
                float x=161+(i%3)*18,z=-18+(i/3)*15,h=9+(i*7%18);var block=G("DistrictBlock_"+i,buildings);
                Box("TowerMass",V(x,h*.5f,z),V(8+(i%3)*2,h,9),dark,block,false);
                Box("RoofPlant",V(x,h+1,z),V(5,2,5),paint,block,false);
                for(int row=1;row<h/3;row++)for(int col=0;col<3;col++){
                    var window=Box("LitWindow",V(x-5.08f,row*3,z-3+col*3),V(.06f,.85f,1.3f),city,vista,false);
                    window.transform.SetParent(restoredWindows,true);
                }
                if(i%4==0)Box("AviationBeacon",V(x,h+2.1f,z),V(.18f,.18f,.18f),red,block,false);
            }
            Box("DistrictGround",V(185,-1,24),V(80,1,130),dark,buildings,false);
        }

        private static void WayfindingAndAnchors()
        {
            var signs=G("Wayfinding",arena);
            Sign("TowerIdentity","05  중앙 관제탑",V(143.5f,14.6f,24),90,13,1.4f,signs);
            Sign("EntryWarning","중앙 관제실\n자동 방어 장치 작동 구역",V(108.65f,11.1f,27),-90,7.2f,1.2f,signs);
            Sign("SouthCircuitDirection","작업등  가",V(122,10.1f,7),180,5,.75f,signs);
            Sign("NorthCircuitDirection","작업등  나",V(122,10.1f,41),0,5,.75f,signs);
            var anchors=G("GameplayAnchors",arena);
            Anchor("PlayerEntry",V(111,6,27),anchors);Anchor("BossSpawn_HeavyRobot",V(134,6,24),anchors);
            Anchor("BossArenaCentre",V(126,6,24),anchors);Anchor("LeftCircuit_Interact",V(122,6,11.5f),anchors);Anchor("RightCircuit_Interact",V(122,6,36.5f),anchors);
            Anchor("Cooling_SouthInteract",V(126,6,20.5f),anchors);Anchor("Cooling_NorthInteract",V(126,6,27.5f),anchors);
            Anchor("RestoreGrid_Interact",V(139.5f,6,24),anchors);Anchor("EntryCombatGate_Future",V(108.5f,6,27),anchors);
        }

        private static void Socket(Transform parent,Vector3 p,int count)
        {
            var root=G("EnergySocket_"+count+"Cell",parent);root.localPosition=p;
            for(int i=0;i<count;i++){
                float x=(i-(count-1)*.5f)*.58f;
                Box("SocketRim",V(x,0,0),V(.46f,.65f,.16f),ochre,root,false);
                Box("SocketRecess",V(x,0,-.09f),V(.30f,.48f,.035f),rubber,root,false);
                Box("SocketContact",V(x,-.15f,-.115f),V(.21f,.055f,.015f),steel,root,false);
            }
        }
        private static void Kit(string relative,string name,Vector3 position,float yaw,Transform parent,bool floor=false)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(BlackoutIntroAudit.Kit+relative);if(!source)throw new IOException(relative);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>(true)){
                if(floor){renderer.sharedMaterial=concrete;MetricUV(renderer.GetComponent<MeshFilter>());}
                else renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
                    string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m));
                    var copy=AssetDatabase.LoadAssetAtPath<Material>(Materials+"/KitFinish_"+m.name+"_"+guid.Substring(0,6)+".mat");return copy?copy:m;
                }).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
        internal static Transform G(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        internal static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        private static void Anchor(string name,Vector3 p,Transform parent){G(name,parent).localPosition=p;}
        private static GameObject Box(string name,Vector3 p,Vector3 size,Material material,Transform parent,bool collision)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());MetricUV(go.GetComponent<MeshFilter>());return go;
        }
        private static GameObject Cylinder(string name,Vector3 p,Vector3 size,Material material,Transform parent,bool collision)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if(collision){var c=go.AddComponent<BoxCollider>();c.size=V(1,2,1);}
            MetricUV(go.GetComponent<MeshFilter>());return go;
        }
        private static void Beam(string name,Vector3 a,Vector3 b,float width,float height,Material material,Transform parent,bool collision)
        {
            var go=Box(name,(a+b)*.5f,V(width,height,Vector3.Distance(a,b)),material,parent,collision);go.transform.localRotation=Quaternion.LookRotation(b-a);
        }
        private static void Pipe(string name,Vector3 a,Vector3 b,float radius,Material material,Transform parent)
        {
            var go=Cylinder(name,(a+b)*.5f,V(radius*2,Vector3.Distance(a,b)*.5f,radius*2),material,parent,false);go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
        private static void Ring(string name,Vector3 position,float inner,float outer,Material mat,Transform parent,int segments)
        {
            var mesh=new Mesh{name=name};var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int i=0;i<=segments;i++){float a=i*2*Mathf.PI/segments;foreach(float r in new[]{inner,outer}){var p=V(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);vertices.Add(p);uv.Add(new Vector2(p.x,p.z));}}
            for(int i=0;i<segments;i++){int k=i*2;triangles.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            string path=Art+"/Meshes/"+name+".asset";AssetDatabase.CreateAsset(mesh,path);
            var go=G(name,parent);go.localPosition=position;go.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;go.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        private static Light Point(string name,Vector3 p,Color color,float intensity,float range,Transform parent)
        {
            var l=G(name,parent).gameObject.AddComponent<Light>();l.transform.localPosition=p;l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;return l;
        }
        private static void Sign(string name,string text,Vector3 p,float yaw,float width,float height,Transform parent)
        {
            var root=G(name,parent);Box("Plate",Vector3.zero,V(width,height,.04f),dark,root,false);
            var tm=G("Lettering",root).gameObject.AddComponent<TextMesh>();tm.transform.localPosition=V(0,0,-.031f);tm.font=font;tm.fontSize=96;tm.characterSize=.055f;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.text=text;tm.color=new Color(.82f,.86f,.82f);
            font.RequestCharactersInTexture(text,96,FontStyle.Normal);var renderer=tm.GetComponent<MeshRenderer>();renderer.sharedMaterial=lettering;lettering.mainTexture=font.material.mainTexture;
            var size=renderer.bounds.size;float fit=Mathf.Min(width*.9f/Mathf.Max(.01f,size.x),height*.78f/Mathf.Max(.01f,size.y));tm.transform.localScale=Vector3.one*fit;renderer.shadowCastingMode=ShadowCastingMode.Off;
            root.localPosition=p;root.localRotation=Quaternion.Euler(0,yaw,0);
        }
        private static void MetricUV(MeshFilter filter)
        {
            if(!filter||!filter.sharedMesh)return;var source=filter.sharedMesh;var scale=filter.transform.lossyScale;
            string path=Art+"/Meshes/Surface_"+Hash128.Compute(source.name+":"+scale.ToString("F4"))+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=Object.Instantiate(source);mesh.name="Metric_"+source.name;var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
                for(int i=0;i<uv.Length;i++){var p=Vector3.Scale(vertices[i],scale);var n=normals[i];var a=V(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z));uv[i]=a.y>=a.x&&a.y>=a.z?new Vector2(p.x,-p.z*Mathf.Sign(n.y)):a.x>=a.z?new Vector2(-p.z*Mathf.Sign(n.x),p.y):new Vector2(p.x*Mathf.Sign(n.z),p.y);}
                mesh.uv=uv;mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);
            }filter.sharedMesh=mesh;if(PrefabUtility.IsPartOfPrefabInstance(filter))PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
        }
        private static Material Existing(string name){var m=AssetDatabase.LoadAssetAtPath<Material>(Materials+"/"+name+".mat");if(!m)throw new IOException(name);return m;}
        private static Material Flat(string name,Color color,float emission=0)
        {
            string path=TowerMaterials+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.25f);mat.enableInstancing=true;
            if(emission>0){mat.SetColor("_EmissionColor",color*emission);mat.EnableKeyword("_EMISSION");}EditorUtility.SetDirty(mat);return mat;
        }
        internal static void Save(){AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new IOException("Scene save failed.");}
        private static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;var parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
    }
}
