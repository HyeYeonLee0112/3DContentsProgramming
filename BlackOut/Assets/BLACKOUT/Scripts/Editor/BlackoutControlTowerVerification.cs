using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Blackout.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Blackout.EditorTools
{
    public static class BlackoutControlTowerVerification
    {
        private static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        private static string Output=>BlackoutControlTowerBuilder.Output;
        private static Transform Arena=>GameObject.Find("BLACKOUT_CargoStation/Environment/05_ControlTower").transform;
        [Serializable] private sealed class Report
        {
            public string checkedUtc,scene;
            public bool arrivalRoute,perimeterLoop,southCircuit,northCircuit,coolingSouth,coolingNorth,restoration,checkpoint,stateLinks;
            public bool legacyCargoRoute,legacyClosedGate,legacyPoweredGate;
            public int missingScripts,missingMaterials,unsupportedShaders,koreanLabels,missingGlyphs,activeRenderers,triangles,colliders,lights,shadowLights;
            public string[] issues;
            public string scope="Environment-only: 0.4 m radius / 1.9 m capsule, sampled floors and routes, state links and references. No boss AI, actual player controller, timed combat, lift movement or 60 fps claim.";
        }
        [MenuItem("BLACKOUT/Control Tower/2 Validate environment")]
        public static string Validate()
        {
            Directory.CreateDirectory(Output);var problems=new List<string>();var r=new Report{checkedUtc=DateTime.UtcNow.ToString("o"),scene=SceneManager.GetActiveScene().path};
            r.arrivalRoute=Route("arrival",new[]{V(87,0,21),V(93,0,21),V(93,0,27),V(94,0,27),V(106,6,27),V(112,6,27)},problems);
            r.perimeterLoop=Route("combat loop",new[]{V(112,6,27),V(112,6,13.5f),V(139,6,13.5f),V(139,6,34.5f),V(112,6,34.5f),V(112,6,27)},problems);
            r.southCircuit=Route("south worklight",new[]{V(112,6,13.5f),V(122,6,13.5f),V(122,6,11.5f)},problems);
            r.northCircuit=Route("north worklight",new[]{V(112,6,34.5f),V(122,6,34.5f),V(122,6,36.5f)},problems);
            r.coolingSouth=Route("south cooling",new[]{V(112,6,21),V(126,6,21),V(126,6,20.5f)},problems);
            r.coolingNorth=Route("north cooling",new[]{V(112,6,27.5f),V(126,6,27.5f)},problems);
            r.restoration=Route("restore",new[]{V(130,6,24),V(139.5f,6,24)},problems);
            r.checkpoint=Route("checkpoint",new[]{V(93,0,27),V(93.8f,0,30),V(93.8f,0,31)},problems);
            r.legacyCargoRoute=Route("existing cargo route",new[]{V(27,0,21),V(32.7f,0,21),V(32.7f,0,25),V(49.5f,0,25),V(49.5f,0,21),V(77,0,21),V(79,0,21)},problems);
            var gate=Object.FindFirstObjectByType<BlackoutSecurityGatePreview>();bool gateState=gate.Powered;
            try{gate.SetPowered(false);r.legacyClosedGate=!Route("expected closed gate",new[]{V(79,0,21),V(87,0,21)},new List<string>());gate.SetPowered(true);r.legacyPoweredGate=Route("existing powered gate",new[]{V(79,0,21),V(93,0,21)},problems);}finally{gate.SetPowered(gateState);}
            if(!r.legacyClosedGate)problems.Add("Existing closed security gate no longer blocks passage.");
            var preview=Arena.GetComponent<BlackoutControlTowerPreview>();
            var original=new[]{preview.LeftPowered,preview.RightPowered,preview.Cooling,preview.Restored};
            try{
                preview.PreviewBlackout();bool off=!Active("Facilities/South_WorklightCircuit/Powered_Worklights")&&!Active("Facilities/North_WorklightCircuit/Powered_Worklights")&&Active("Facilities/CentralCooling_TwoCell/Idle_ClosedShutter");
                preview.PreviewLeft();bool left=Active("Facilities/South_WorklightCircuit/Powered_Worklights")&&!Active("Facilities/North_WorklightCircuit/Powered_Worklights")&&Active("Facilities/South_WorklightCircuit_LightAndTurretRig/LinkedTurret_VisualOnly/Powered_TurretAlert");
                preview.PreviewCooling();bool cooling=Active("Facilities/CentralCooling_TwoCell/Active_CoolingCore")&&!Active("Facilities/CentralCooling_TwoCell/Idle_ClosedShutter")&&Active("Facilities/North_WorklightCircuit_LightAndTurretRig/LinkedTurret_VisualOnly/Powered_TurretAlert");
                preview.PreviewRestored();bool restored=Active("Facilities/GridRestorationConsole/Online_GridStatus")&&!Active("Facilities/GridRestorationConsole/Offline_GridStatus");
                r.stateLinks=off&&left&&cooling&&restored;if(!r.stateLinks)problems.Add("Visual state linkage failed.");
            }finally{preview.SetState(original[0],original[1],original[2],original[3]);}
            var roots=new[]{Arena,GameObject.Find("BLACKOUT_CargoStation/Environment/04_FreightLiftArrival").transform};
            foreach(var t in roots.SelectMany(root=>root.GetComponentsInChildren<Transform>(true)))r.missingScripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            foreach(var renderer in roots.SelectMany(root=>root.GetComponentsInChildren<Renderer>(true))){
                foreach(var m in renderer.sharedMaterials){if(!m){r.missingMaterials++;problems.Add("Missing material: "+PathOf(renderer.transform));}else if(!m.shader||!m.shader.isSupported){r.unsupportedShaders++;problems.Add("Unsupported shader: "+m.name);}}
                if(renderer.enabled&&renderer.gameObject.activeInHierarchy)r.activeRenderers++;
                var filter=renderer.GetComponent<MeshFilter>();if(filter&&filter.sharedMesh)r.triangles+=filter.sharedMesh.triangles.Length/3;
            }
            foreach(var label in roots.SelectMany(root=>root.GetComponentsInChildren<TextMesh>(true))){r.koreanLabels++;foreach(char c in label.text.Distinct())if(!char.IsWhiteSpace(c)&&!label.font.HasCharacter(c)){r.missingGlyphs++;problems.Add("Missing glyph: "+c);}}
            r.colliders=roots.Sum(root=>root.GetComponentsInChildren<Collider>().Length);
            var lights=roots.SelectMany(root=>root.GetComponentsInChildren<Light>()).Where(l=>l.enabled).ToArray();r.lights=lights.Length;r.shadowLights=lights.Count(l=>l.shadows!=LightShadows.None);
            if(r.missingScripts>0)problems.Add("Missing scripts: "+r.missingScripts);
            r.issues=problems.Distinct().ToArray();string json=JsonUtility.ToJson(r,true);File.WriteAllText(Output+"/validation.json",json);
            var tree=new StringBuilder();foreach(var root in roots)Tree(root,tree,0,4);File.WriteAllText(Output+"/hierarchy.txt",tree.ToString());return json;
        }
        private static bool Active(string path){var target=Arena.Find(path);return target&&target.gameObject.activeInHierarchy;}
        private static bool Route(string name,Vector3[] points,List<string> issues)
        {
            Physics.SyncTransforms();bool clear=true;
            for(int i=0;i<points.Length-1;i++){
                var a=points[i];var b=points[i+1];float length=Vector3.Distance(a,b);int steps=Mathf.CeilToInt(length/.25f);
                for(int j=0;j<=steps;j++){
                    var p=Vector3.Lerp(a,b,(float)j/steps);
                    var overlaps=Physics.OverlapCapsule(p+Vector3.up*.55f,p+Vector3.up*1.65f,.4f,~0,QueryTriggerInteraction.Ignore);
                    foreach(var c in overlaps){clear=false;issues.Add(name+" blocked at "+p+": "+PathOf(c.transform));}
                    if(!Physics.Raycast(p+Vector3.up*.4f,Vector3.down,.8f,~0,QueryTriggerInteraction.Ignore)){clear=false;issues.Add(name+" missing floor at "+p);}
                }
            }return clear;
        }
        [MenuItem("BLACKOUT/Control Tower/3 Capture review views")]
        public static void Capture()
        {
            Directory.CreateDirectory(Output);WarmFonts();var c=Camera.main;var pos=c.transform.position;var rot=c.transform.rotation;float fov=c.fieldOfView;bool ortho=c.orthographic;
            var preview=Arena.GetComponent<BlackoutControlTowerPreview>();var state=new[]{preview.LeftPowered,preview.RightPowered,preview.Cooling,preview.Restored};
            try{
                c.orthographic=false;c.fieldOfView=65;
                preview.PreviewBlackout();Shot(c,"01-entry-blackout",V(111,8.1f,26.7f),V(131,8.7f,24));
                preview.PreviewWorklights();Shot(c,"02-arena-powered",V(113,9.1f,33.8f),V(129,8.5f,22));
                preview.PreviewCooling();Shot(c,"03-central-cooling",V(119,8.2f,18.6f),V(126,7.2f,24));
                Shot(c,"04-worklight-circuit",V(118.7f,8,14.5f),V(123.5f,9.6f,9.8f));
                preview.PreviewRestored();Shot(c,"05-grid-restored",V(135,8.3f,24),V(143,9.6f,24));
                Shot(c,"06-arrival-stair",V(92,2,24),V(103,6.4f,27));
                var roof=Arena.Find("Architecture/RoofAndTrusses");roof.gameObject.SetActive(false);
                try{c.orthographic=true;c.orthographicSize=20;Shot(c,"07-layout-cutaway",V(126,60,24),V(126,6,24));}
                finally{roof.gameObject.SetActive(true);}
            }finally{preview.SetState(state[0],state[1],state[2],state[3]);c.transform.SetPositionAndRotation(pos,rot);c.fieldOfView=fov;c.orthographic=ortho;}
        }
        internal static void WarmFonts()
        {
            foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(!label.font)continue;label.font.RequestCharactersInTexture(label.text,label.fontSize,label.fontStyle);label.GetComponent<Renderer>().sharedMaterial.mainTexture=label.font.material.mainTexture;
            }
        }
        internal static void Shot(Camera camera,string name,Vector3 p,Vector3 target)
        {
            camera.transform.position=p;camera.transform.LookAt(target);var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);
            var active=RenderTexture.active;var old=camera.targetTexture;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
        }
        [MenuItem("BLACKOUT/Control Tower/4 Bake local reflection")]
        public static void BakeReflection()
        {
            var probe=Arena.GetComponentInChildren<ReflectionProbe>();string path=BlackoutControlTowerBuilder.Art+"/Reflections/ControlTower.exr";
            if(!Lightmapping.BakeReflectionProbe(probe,path))throw new InvalidOperationException("Reflection bake failed.");
            AssetDatabase.ImportAsset(path);probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Texture>(path);EditorUtility.SetDirty(probe);BlackoutControlTowerBuilder.Save();
        }
        public static void FinalizeLighting()
        {
            var key=Arena.Find("Lighting/HighWindowAmbient").GetComponent<Light>();
            key.type=LightType.Spot;key.spotAngle=130;key.innerSpotAngle=80;key.intensity=75;key.range=34;
            key.transform.LookAt(V(126,6,24));
            // One spot needs one shadow map; the previous point needed six.
            var camera=Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if(camera)camera.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing;
            var parent=Arena.Find("Lighting");var existing=parent.Find("ArenaLocalGrade");
            if(!parent.Find("RestorationDesk_TaskLight")){
                var taskLight=new GameObject("RestorationDesk_TaskLight");taskLight.transform.SetParent(parent,false);taskLight.transform.position=V(139,9.8f,24);
                var light=taskLight.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.68f,.8f,.88f);light.intensity=18;light.range=10;light.shadows=LightShadows.None;
            }
            string mattePath=BlackoutControlTowerBuilder.TowerMaterials+"/Sign_Plate_Matte.mat";
            var matte=AssetDatabase.LoadAssetAtPath<Material>(mattePath);
            if(!matte){matte=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(matte,mattePath);}
            matte.SetColor("_BaseColor",new Color(.025f,.034f,.04f));matte.SetFloat("_Metallic",0);matte.SetFloat("_Smoothness",0);matte.SetFloat("_SpecularHighlights",0);matte.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");matte.enableInstancing=true;EditorUtility.SetDirty(matte);
            foreach(var root in new[]{Arena,GameObject.Find("BLACKOUT_CargoStation/Environment/04_FreightLiftArrival").transform})
                foreach(var label in root.GetComponentsInChildren<TextMesh>(true)){
                    var plate=label.transform.parent.Find("Plate");if(plate)plate.GetComponent<MeshRenderer>().sharedMaterial=matte;
                }
            // Keep the reusable hero prefabs consistent without changing their local origins.
            foreach(string prefabPath in new[]{BlackoutControlTowerBuilder.Prefabs+"/CoolingUnit_TwoCell.prefab",BlackoutControlTowerBuilder.Prefabs+"/GridRestorationConsole.prefab"}){
                var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
                try{foreach(var label in prefab.GetComponentsInChildren<TextMesh>(true)){var plate=label.transform.parent.Find("Plate");if(plate)plate.GetComponent<MeshRenderer>().sharedMaterial=matte;}PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
            if(!existing){
                var go=new GameObject("ArenaLocalGrade");go.transform.SetParent(parent,false);go.transform.position=V(126,12,24);
                var bounds=go.AddComponent<BoxCollider>();bounds.isTrigger=true;bounds.size=V(37,14,37);
                var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,BlackoutControlTowerBuilder.Art+"/ControlTowerVolume.asset");
                var tone=profile.Add<UnityEngine.Rendering.Universal.Tonemapping>();tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);
                var color=profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>();color.postExposure.Override(.3f);color.contrast.Override(7);color.saturation.Override(-5);
                var bloom=profile.Add<UnityEngine.Rendering.Universal.Bloom>();bloom.threshold.Override(1.1f);bloom.intensity.Override(.12f);bloom.scatter.Override(.55f);
                foreach(var component in profile.components){AssetDatabase.AddObjectToAsset(component,profile);EditorUtility.SetDirty(component);}
                EditorUtility.SetDirty(profile);
                var volume=go.AddComponent<Volume>();volume.isGlobal=false;volume.priority=20;volume.blendDistance=2;volume.sharedProfile=profile;
            }
            BlackoutControlTowerBuilder.Save();
        }
        [Serializable] private sealed class PlayReport
        {
            public bool isPlaying,controllerClimbedStair,controllerCompletedArenaLoop,reflectionPersisted,stateLinks;
            public Vector3 arrivalEnd,loopEnd;
            public int missingMaterials;
            public string[] issues;
            public string note="Temporary native CharacterController smoke test, not the project's playable character or boss combat.";
        }
        public static string PlaySmoke()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var report=new PlayReport{isPlaying=true};var issues=new List<string>();
            var actor=new GameObject("QA_TemporaryTraversalProbe");var cc=actor.AddComponent<CharacterController>();cc.height=1.9f;cc.radius=.4f;cc.center=Vector3.up*.95f;cc.stepOffset=.28f;cc.slopeLimit=45;cc.skinWidth=.035f;cc.minMoveDistance=0;
            try{
                cc.enabled=false;actor.transform.position=V(87,.1f,21);cc.enabled=true;Physics.SyncTransforms();
                report.controllerClimbedStair=Walk(cc,new[]{V(93,0,21),V(93,0,27),V(94,0,27),V(106,6,27),V(112,6,27)},issues);report.arrivalEnd=actor.transform.position;
                report.controllerCompletedArenaLoop=Walk(cc,new[]{V(112,6,13.5f),V(139,6,13.5f),V(139,6,34.5f),V(112,6,34.5f),V(112,6,27)},issues);report.loopEnd=actor.transform.position;
                var probe=Arena.GetComponentInChildren<ReflectionProbe>();report.reflectionPersisted=probe&&probe.mode==ReflectionProbeMode.Custom&&probe.customBakedTexture;
                var preview=Arena.GetComponent<BlackoutControlTowerPreview>();preview.PreviewCooling();report.stateLinks=preview.Cooling&&Active("Facilities/CentralCooling_TwoCell/Active_CoolingCore");preview.PreviewBlackout();
                report.missingMaterials=Arena.GetComponentsInChildren<Renderer>(true).Sum(r=>r.sharedMaterials.Count(m=>!m||!m.shader||!m.shader.isSupported));
                WarmFonts();var camera=Camera.main;var p=camera.transform.position;var q=camera.transform.rotation;
                Shot(camera,"08-playmode-blackout",V(111,8.1f,26.7f),V(131,8.7f,24));camera.transform.SetPositionAndRotation(p,q);
            }finally{Object.Destroy(actor);}
            report.issues=issues.ToArray();string json=JsonUtility.ToJson(report,true);File.WriteAllText(Output+"/playmode-validation.json",json);return json;
        }
        private static bool Walk(CharacterController cc,Vector3[] targets,List<string> issues)
        {
            foreach(var target in targets){
                int guard=0;while(guard++<700){Vector3 delta=target-cc.transform.position;delta.y=0;if(delta.magnitude<.07f)break;cc.Move(Vector3.ClampMagnitude(delta,.075f)+Vector3.down*.025f);}
                for(int i=0;i<5;i++)cc.Move(Vector3.down*.025f);
                var end=cc.transform.position;if(Mathf.Abs(end.y-target.y)>.25f||Vector2.Distance(new Vector2(end.x,end.z),new Vector2(target.x,target.z))>.15f){issues.Add("Traversal failed at "+target+", actual "+end);return false;}
            }return true;
        }
        private static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        private static void Tree(Transform t,StringBuilder text,int depth,int max){text.Append(' ',depth*2).Append(t.name).Append(t.gameObject.activeSelf?"":" [inactive]").AppendLine();if(depth>=max)return;foreach(Transform child in t)Tree(child,text,depth+1,max);}
    }
}
