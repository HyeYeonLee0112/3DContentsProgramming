using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Blackout.EditorTools
{
    public static partial class BlackoutControlTowerBuilder
    {
        [MenuItem("BLACKOUT/Control Tower/5 Apply reviewed detail pass (once)")]
        public static void Refine()
        {
            LoadMaterials();arena=GameObject.Find("BLACKOUT_CargoStation/Environment/05_ControlTower").transform;
            if(arena.Find("Architecture/DetailPass"))throw new InvalidOperationException("Detail pass already present; do not duplicate.");
            var details=G("DetailPass",arena.Find("Architecture"));
            var arrival=GameObject.Find("BLACKOUT_CargoStation/Environment/04_FreightLiftArrival").transform;
            var central=arena.Find("Facilities/CentralCooling_TwoCell");
            central.Find("SouthInteractionPanel").localPosition=V(0,0,-2);
            central.Find("NorthInteractionPanel").localPosition=V(0,0,2);
            Box("TowerFoundation",V(108,2.9f,24),V(.6f,5.8f,36),concrete,details,true);
            // Close the lower stair-hall silhouette; keep the upper portal untouched.
            var approach=G("IndustrialCladding",arrival.Find("Architecture"));
            foreach(float z in new[]{12.25f,35.75f})for(int x=93;x<=105;x+=6){
                Box("WallReinforcement",V(x,6.5f,z),V(.2f,13,.2f),steel,approach,false);
                Box("LowerBumper",V(x,1.2f,z),V(.45f,2.4f,.3f),ochre,approach,false);
            }
            for(int z=15;z<=33;z+=6)Box("EastFoundationPanel",V(107.64f,2.8f,z),V(.06f,4.8f,5.7f),paint,approach,false);
            for(int x=93;x<=105;x+=6)Box("CeilingBeam",V(x,13.6f,24),V(.3f,.6f,24),steel,approach,false);
            foreach(float z in new[]{13f,35f})Pipe("LiftServiceMain",V(91,11,z),V(107,11,z),.18f,steel,approach);
            var notice=arrival.Find("Wayfinding/MaintenanceRoute");notice.position=V(93,2.8f,24.3f);

            var coolingDetail=G("MechanicalDetails",central);
            foreach(float side in new[]{-1f,1f}){
                Pipe("CoolantManifold",V(side*1.25f,.55f,-1.1f),V(side*1.25f,.55f,1.1f),.12f,steel,coolingDetail);
                foreach(float z in new[]{-.75f,.75f}){
                    var flange=Cylinder("PipeFlange",V(side*1.25f,.55f,z),V(.34f,.05f,.34f),dark,coolingDetail,false);flange.transform.localRotation=Quaternion.Euler(90,0,0);
                }
            }
            for(int i=0;i<16;i++){
                float a=i*Mathf.PI/8;Cylinder("CollarBolt",V(Mathf.Cos(a)*1.4f,1.12f,Mathf.Sin(a)*1.4f),V(.075f,.025f,.075f),steel,coolingDetail,false);
            }
            foreach(string panelName in new[]{"SouthInteractionPanel","NorthInteractionPanel"}){
                var panel=central.Find(panelName);var trim=G("PanelHardware",panel);
                foreach(float x in new[]{-.82f,.82f})foreach(float y in new[]{.15f,1.05f})Box("Fastener",V(x,y,-.264f),V(.045f,.045f,.025f),steel,trim,false);
                for(int i=0;i<6;i++)Box("VentSlot",V(-.55f+i*.22f,.05f,-.27f),V(.1f,.035f,.02f),dark,trim,false);
                Box("StatusBar",V(0,1.1f,-.28f),V(1.2f,.05f,.015f),screen,trim,false);
            }
            foreach(string circuitName in new[]{"South_WorklightCircuit","North_WorklightCircuit"}){
                var circuit=arena.Find("Facilities/"+circuitName);var trim=G("CabinetHardware",circuit);
                for(int i=0;i<5;i++)Box("VentSlot",V(-.43f+i*.21f,1.72f,-.48f),V(.1f,.18f,.025f),steel,trim,false);
                Box("Latch",V(.51f,1.06f,-.5f),V(.055f,.32f,.06f),steel,trim,false);
                foreach(float y in new[]{.45f,1.6f})Box("Hinge",V(-.6f,y,-.48f),V(.085f,.16f,.06f),steel,trim,false);
                foreach(float x in new[]{-.55f,.55f})Box("SignBracket",V(x,2.25f,0),V(.07f,.7f,.08f),steel,trim,false);
                var live=circuit.Find("Powered_Worklights/SocketLive");live.localPosition=V(0,1.1f,-.66f);
            }
            var final=arena.Find("Facilities/GridRestorationConsole");var hardware=G("ConsoleHardware",final);
            foreach(float x in new[]{-1.8f,1.8f}){
                Box("CabinetSeam",V(x,.7f,-.61f),V(.025f,1.1f,.02f),dark,hardware,false);
                Box("AccessHandle",V(x,.9f,-.64f),V(.06f,.24f,.05f),steel,hardware,false);
            }
            foreach(float x in new[]{-1.5f,-.8f,.8f,1.5f})Box("ControlButton",V(x,1.64f,-.57f),V(.2f,.06f,.18f),ochre,hardware,false);
            for(int i=0;i<9;i++)Box("CabinetVent",V(-1.5f+i*.375f,.2f,-.62f),V(.21f,.06f,.025f),dark,hardware,false);
            // Industrial warning ticks define the service radius without a decorative glowing ring.
            var floorDetail=G("CoolingSafetyTicks",arena.Find("CombatLayout"));
            for(int i=0;i<24;i++){
                float a=i*Mathf.PI/12;var tick=Box("CautionTick",V(126+Mathf.Cos(a)*4.17f,6.026f,24+Mathf.Sin(a)*4.17f),V(.35f,.015f,.13f),ochre,floorDetail,false);tick.transform.localRotation=Quaternion.Euler(0,-i*15,0);
            }
            var pipes=arena.Find("Architecture/OverheadServices");
            foreach(float z in new[]{11f,37f})for(int x=111;x<=143;x+=4){var flange=Cylinder("PipeCoupling",V(x,15.7f,z),V(.78f,.065f,.78f),dark,pipes,false);flange.transform.localRotation=Quaternion.Euler(0,0,90);}
            foreach(float z in new[]{8f,40f})for(int x=110;x<=142;x+=4)Beam("GalleryBracket",V(x,10.6f,z),V(x,9.2f,z<24?6.5f:41.5f),.16f,.16f,steel,details,false);

            arena.Find("Lighting/HighWindowAmbient").GetComponent<Light>().intensity=28;
            arena.Find("Lighting/CoolingReadability").GetComponent<Light>().intensity=12;
            foreach(var light in arena.GetComponentsInChildren<Light>(true))if(light.type==LightType.Spot)light.intensity=135;
            var glass=AssetDatabase.LoadAssetAtPath<Material>(TowerMaterials+"/Observation_Glass.mat");glass.SetFloat("_SpecularHighlights",0);glass.SetFloat("_EnvironmentReflections",0);glass.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");glass.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");EditorUtility.SetDirty(glass);
            // URP material keyword setup is explicit, including emission GI flags.
            foreach(var mat in new[]{cyan,red,lamp,city}){mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;EditorUtility.SetDirty(mat);}
            foreach(var w in final.Find("Online_GridStatus/RestoredCityWindows").GetComponentsInChildren<MeshRenderer>()){
                var p=w.transform.position;int col=Mathf.RoundToInt((p.x+5.08f-161)/18);p.x=161+col*18-(8+col*2)*.5f-.04f;w.transform.position=p;
            }
            // Chamfer hero props so light catches real edges rather than razor-sharp cubes.
            foreach(var renderer in arena.GetComponentsInChildren<MeshRenderer>(true)){
                string n=renderer.name;
                if(new[]{"ArmouredCover","TopCap","EquipmentBase","SlopedControlDesk","ScreenFrame","CabinetBody","PanelHousing","CabinetBase","ControlDesk","DisplayFrame","TurretArmour","Muzzle","CastHousing"}.Contains(n))Bevel(renderer,Mathf.Min(.065f,renderer.transform.localScale.y*.24f));
            }
            foreach(var root in new[]{arrival.Find("Architecture/MaintenanceStair"),approach,pipes,details,arena.Find("Architecture/ServiceGalleries"),arena.Find("Architecture/StructuralColumns"),arena.Find("CombatLayout/FloorMarkings"),floorDetail,arena.Find("Facilities/OperatorConsoleBanks"),coolingDetail,hardware,arena.Find("ExteriorVista_NonPlayable/DistantCargoDistrict"),final.Find("Online_GridStatus/RestoredCityWindows")})BatchDecoration(root);
            foreach(string circuitName in new[]{"South_WorklightCircuit","North_WorklightCircuit"})BatchDecoration(arena.Find("Facilities/"+circuitName+"/CabinetHardware"));
            foreach(string panelName in new[]{"SouthInteractionPanel","NorthInteractionPanel"})BatchDecoration(central.Find(panelName+"/PanelHardware"));
            SaveLocalPrefab(central,"CoolingUnit_TwoCell");
            // The restoration prefab must not drag the far-away city vista into a reusable prop.
            var cityWindows=final.Find("Online_GridStatus/RestoredCityWindows");var previous=cityWindows.parent;cityWindows.SetParent(arena,true);
            SaveLocalPrefab(final,"GridRestorationConsole");cityWindows.SetParent(previous,true);
            Save();
        }
        private static void SaveLocalPrefab(Transform root,string name)
        {
            var position=root.localPosition;var rotation=root.localRotation;root.localPosition=Vector3.zero;root.localRotation=Quaternion.identity;
            try{PrefabUtility.SaveAsPrefabAsset(root.gameObject,Prefabs+"/"+name+".prefab");}finally{root.localPosition=position;root.localRotation=rotation;}
        }
        private static void BatchDecoration(Transform root)
        {
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>!r.GetComponent<TextMesh>()&&!r.GetComponent<Collider>()&&r.transform.childCount==0&&r.GetComponent<MeshFilter>()&&r.gameObject.activeSelf).ToArray();
            foreach(var batch in renderers.GroupBy(r=>r.sharedMaterial)){
                if(batch.Count()<3)continue;
                var mesh=new Mesh{name=root.name+"_"+batch.Key.name,indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(batch.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray(),true,true);mesh.RecalculateBounds();
                string path=Art+"/Meshes/Batch_"+Hash128.Compute(PathOf(root)+batch.Key.name)+".asset";AssetDatabase.CreateAsset(mesh,path);
                var go=G("DetailBatch_"+batch.Key.name,root);go.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;go.gameObject.AddComponent<MeshRenderer>().sharedMaterial=batch.Key;
                foreach(var r in batch)Object.DestroyImmediate(r.gameObject);
            }
        }
        private static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        private static void Bevel(MeshRenderer renderer,float radius)
        {
            var t=renderer.transform;Vector3 size=t.localScale;string path=Art+"/Meshes/Bevel_"+Hash128.Compute(size.ToString("F4")+radius)+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var indices=new List<int>();Vector3 h=size*.5f,s=h-Vector3.one*radius;
                Action<Vector3[]> face=points=>{
                    Vector3 normal=Vector3.Cross(points[1]-points[0],points[2]-points[0]).normalized;var centre=points.Aggregate(Vector3.zero,(sum,p)=>sum+p)/points.Length;
                    if(Vector3.Dot(normal,centre)<0){Array.Reverse(points);normal=-normal;}
                    int start=vertices.Count;foreach(var p in points){vertices.Add(p);normals.Add(normal);Vector3 a=V(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z));uvs.Add(a.y>a.x&&a.y>a.z?new Vector2(p.x,p.z):a.x>a.z?new Vector2(p.z,p.y):new Vector2(p.x,p.y));}
                    for(int i=1;i<points.Length-1;i++)indices.AddRange(new[]{start,start+i,start+i+1});
                };
                for(int axis=0;axis<3;axis++)foreach(float sign in new[]{-1f,1f}){
                    int b=(axis+1)%3,c=(axis+2)%3;var pts=new Vector3[4];int k=0;foreach(var pair in new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)}){var p=Vector3.zero;p[axis]=h[axis]*sign;p[b]=s[b]*pair.x;p[c]=s[c]*pair.y;pts[k++]=p;}face(pts);
                }
                for(int axis=0;axis<3;axis++)foreach(float sb in new[]{-1f,1f})foreach(float sc in new[]{-1f,1f}){
                    int b=(axis+1)%3,c=(axis+2)%3;var pts=new Vector3[4];for(int i=0;i<4;i++){var p=Vector3.zero;p[axis]=(i<2?-1:1)*s[axis];bool outer=i==0||i==3;p[b]=sb*(outer?h[b]:s[b]);p[c]=sc*(outer?s[c]:h[c]);pts[i]=p;}face(pts);
                }
                foreach(float sx in new[]{-1f,1f})foreach(float sy in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})face(new[]{V(sx*h.x,sy*s.y,sz*s.z),V(sx*s.x,sy*h.y,sz*s.z),V(sx*s.x,sy*s.y,sz*h.z)});
                mesh=new Mesh{name="BevelledIndustrialBox"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.SetTriangles(indices,0);mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            renderer.GetComponent<MeshFilter>().sharedMesh=mesh;var collider=renderer.GetComponent<BoxCollider>();if(collider)collider.size=size;t.localScale=Vector3.one;
        }
    }
}
