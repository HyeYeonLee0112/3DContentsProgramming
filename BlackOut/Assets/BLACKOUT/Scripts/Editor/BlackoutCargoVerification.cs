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
    internal static class BlackoutCargoVerification
    {
        internal static string Output => Path.GetFullPath("../docs/images/production/cargo-station");
        [Serializable] private sealed class Report
        {
            public string scene, checkedUtc;
            public bool routeOpen, blackoutBlocksGate, poweredOpensGate, powerSocketReachable, recoveryPocketReachable;
            public int missingScripts, missingMaterials, unsupportedShaders, activeRenderers, activeColliders, lights, shadowLights;
            public string[] issues;
            public string note = "Environment-only QA. Capsule sweep radius 0.4 m, height 1.9 m. No claim of combat, navigation AI, power inventory or 60fps performance verification.";
        }

        [MenuItem("BLACKOUT/Cargo Station/2 Validate routes and references")]
        public static void Validate()
        {
            RequireScene();
            Directory.CreateDirectory(Output);
            var report=new Report {scene=SceneManager.GetActiveScene().path,checkedUtc=DateTime.UtcNow.ToString("o")};
            var issues=new List<string>();
            var points=new[]{V(27,21),V(32.7f,21),V(32.7f,25),V(49.5f,25),V(49.5f,21),V(77,21),V(79,21)};
            report.routeOpen=Route(points,issues);
            report.powerSocketReachable=Route(new[]{V(77,21),V(77,16.8f)},issues);
            report.recoveryPocketReachable=Route(new[]{V(46.3f,25),V(46.3f,28.5f)},issues);
            var gate=Object.FindFirstObjectByType<BlackoutSecurityGatePreview>();
            if(!gate)throw new InvalidOperationException("Gate preview component missing.");
            bool state=gate.Powered;
            try {
                gate.SetPowered(false);Physics.SyncTransforms();
                var expected=new List<string>();report.blackoutBlocksGate=!Route(new[]{V(79,21),V(86,21)},expected);
                gate.SetPowered(true);Physics.SyncTransforms();
                report.poweredOpensGate=Route(new[]{V(79,21),V(88,21)},issues);
            } finally {gate.SetPowered(state);Physics.SyncTransforms();}
            var roots=SceneManager.GetActiveScene().GetRootGameObjects();
            foreach(var go in roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject))
                report.missingScripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            var renderers=roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).ToArray();
            foreach(var r in renderers)foreach(var m in r.sharedMaterials) {
                if(!m) {report.missingMaterials++;issues.Add("Missing material: "+PathOf(r.transform));}
                else if(!m.shader||!m.shader.isSupported) {report.unsupportedShaders++;issues.Add("Unsupported shader: "+m.name);}
            }
            report.activeRenderers=renderers.Count(r=>r.enabled&&r.gameObject.activeInHierarchy);
            report.activeColliders=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Count(c=>c.enabled);
            var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.enabled).ToArray();
            report.lights=lights.Length;report.shadowLights=lights.Count(l=>l.shadows!=LightShadows.None);
            if(!report.blackoutBlocksGate)issues.Add("Closed gate does not block traversal.");
            if(report.missingScripts>0)issues.Add("Missing scripts: "+report.missingScripts);
            report.issues=issues.Distinct().ToArray();
            File.WriteAllText(Output+"/validation.json",JsonUtility.ToJson(report,true));
            var tree=new StringBuilder();foreach(var root in roots)Tree(root.transform,tree,0,4);
            File.WriteAllText(Output+"/hierarchy.txt",tree.ToString());
            Debug.Log("[CargoStation QA] "+JsonUtility.ToJson(report));
        }

        private static bool Route(Vector3[] points,List<string> issues)
        {
            Physics.SyncTransforms();bool clear=true;
            for(int i=0;i<points.Length-1;i++) {
                Vector3 a=points[i],b=points[i+1],delta=b-a;float distance=delta.magnitude;
                // Bottom is 0.1 m above the floor to tolerate joins, not stairs or missing floors.
                var hits=Physics.CapsuleCastAll(a+Vector3.up*.5f,a+Vector3.up*1.6f,.4f,delta.normalized,distance,~0,QueryTriggerInteraction.Ignore);
                foreach(var hit in hits) {clear=false;issues.Add("Blocked "+a+" -> "+b+": "+PathOf(hit.transform));}
                for(float d=0;d<=distance;d+=.35f) {
                    Vector3 p=a+delta.normalized*d;
                    if(!Physics.Raycast(p+Vector3.up*.35f,Vector3.down,.65f,~0,QueryTriggerInteraction.Ignore)) {clear=false;issues.Add("Missing floor at "+p);}
                    var overlap=Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.6f,.4f,~0,QueryTriggerInteraction.Ignore);
                    foreach(var collider in overlap) {clear=false;issues.Add("Capsule overlap at "+p+": "+PathOf(collider.transform));}
                }
            }
            return clear;
        }

        [MenuItem("BLACKOUT/Cargo Station/3 Capture review cameras")]
        public static void CaptureAll()
        {
            RequireScene();Directory.CreateDirectory(Output);
            var camera=Camera.main;if(!camera)throw new InvalidOperationException("Main Camera missing.");
            var originalP=camera.transform.position;var originalR=camera.transform.rotation;
            var gate=Object.FindFirstObjectByType<BlackoutSecurityGatePreview>();bool powered=gate.Powered;
            try {
                gate.SetPowered(false);
                Shot(camera,"01-container-entry",new Vector3(32,2.0f,20.8f),new Vector3(42,2.3f,25.5f));
                Shot(camera,"02-container-dogleg",new Vector3(35,2.0f,25.5f),new Vector3(55,2.2f,24.5f));
                Shot(camera,"03-security-closed",new Vector3(69,2.1f,22.8f),new Vector3(82,3,20.8f));
                Shot(camera,"04-power-socket",new Vector3(74.5f,1.9f,19.3f),new Vector3(78,1.5f,16.8f));
                gate.SetPowered(true);
                Shot(camera,"05-security-powered",new Vector3(74,2.1f,21.5f),new Vector3(83,3,21));
                gate.SetPowered(false);
                var roofs=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="RoofAndTrusses").ToArray();
                foreach(var t in roofs)t.gameObject.SetActive(false);
                try {
                    camera.orthographic=true;camera.orthographicSize=20;
                    Shot(camera,"06-layout-cutaway",new Vector3(60,60,23.5f),new Vector3(60,0,23.5f));
                } finally {camera.orthographic=false;foreach(var t in roofs)t.gameObject.SetActive(true);}
            } finally {
                gate.SetPowered(powered);camera.transform.SetPositionAndRotation(originalP,originalR);
            }
            Debug.Log("[CargoStation QA] Captured 6 real Unity views: "+Output);
        }
        private static void Shot(Camera camera,string name,Vector3 p,Vector3 target)
        {
            camera.transform.position=p;camera.transform.LookAt(target);
            const int width=1920,height=1080;
            var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;var targetBefore=camera.targetTexture;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
            } finally {camera.targetTexture=targetBefore;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
        }
        private static void RequireScene()
        {
            if(SceneManager.GetActiveScene().path!=BlackoutCargoBuilder.ScenePath)throw new InvalidOperationException("Open CargoStation.unity first.");
        }
        private static Vector3 V(float x,float z)=>new Vector3(x,0,z);
        private static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        private static void Tree(Transform t,StringBuilder output,int depth,int max)
        {
            output.Append(' ',depth*2).Append(t.name).Append(t.gameObject.activeSelf?"":" [inactive]").AppendLine();
            if(depth>=max)return;foreach(Transform child in t)Tree(child,output,depth+1,max);
        }
    }
}
