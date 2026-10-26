using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace LosQueQuedan {
public sealed class VisualAudit:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--visual-qa")>=0){Application.runInBackground=true;new GameObject("Visual audit").AddComponent<VisualAudit>();}}
 readonly List<string> report=new List<string>();
 IEnumerator Start(){
 yield return new WaitForSeconds(3);var g=DemoGame.Instance;DemoGame.HideInterface=true;
 Directory.CreateDirectory("/tmp/spooky-polish/qa");
 var songs=Array.FindAll(FindObjectsByType<AudioSource>(FindObjectsSortMode.None),a=>a.clip&&a.clip.name=="MechanicalHum");
 foreach(var a in songs)if(a.isPlaying)a.time=a.clip.length-6;
 for(int k=0;k<4;k++){yield return new WaitForSeconds(k==3?5:1.5f);foreach(var a in songs)report.Add("AUDIO t+"+k+" "+a.GetInstanceID()+" playing="+a.isPlaying+" time="+a.time.ToString("F1")+" vol="+a.volume.ToString("F2"));}
 var fill=new GameObject("Inspection fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.8f;fill.color=Color.white;
 foreach(string name in new[]{"Ines","Mujer","Doctor","Paciente","Esquina","plastic_monobloc_chair_01","wheelchair_01"}){
 var o=GameObject.Find(name);if(!o)continue;
 var bounds=GeometryBounds(o);report.Add(name+" min="+bounds.min.ToString("F3")+" max="+bounds.max.ToString("F3"));
 foreach(var t in o.GetComponentsInChildren<Transform>())if(t.name=="pelvis"||t.name=="Head"||t.name=="foot_l"||t.name=="foot_r")report.Add("  "+t.name+" "+t.position.ToString("F3"));
 foreach(var r in o.GetComponentsInChildren<Renderer>())if(r.name.Contains("Hair"))report.Add("  "+r.name+" "+r.bounds.ToString());
 for(int side=0;side<2;side++){
 var target=bounds.center; var dir=side==0?o.transform.forward:-o.transform.forward;
 Vector3 eye=target+dir*2.3f+o.transform.right*.35f;
 // Camera is detached for inspection; it does not move the live player or alter saves.
 g.player.view.transform.position=eye;g.player.view.transform.rotation=Quaternion.LookRotation(target-eye);g.player.view.fieldOfView=50;
 fill.transform.rotation=g.player.view.transform.rotation;
 for(int raw=0;raw<2;raw++){
 fill.enabled=raw==1;g.Ink.enabledEffect=raw==0;
 yield return null;yield return new WaitForEndOfFrame();
 var img=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("/tmp/spooky-polish/qa/"+name+"-"+side+(raw==1?"-raw":"-ascii")+".png",img.EncodeToPNG());Destroy(img);
 }
 }
 }
 File.WriteAllLines("/tmp/spooky-polish/qa/geometry.txt",report);Debug.Log("VISUAL_AUDIT_DONE");Application.Quit();
 }
 public static Bounds GeometryBounds(GameObject o){
 bool first=true;Bounds b=new Bounds();
 foreach(var r in o.GetComponentsInChildren<Renderer>()){
 Mesh m=null;bool temp=false;
 if(r is SkinnedMeshRenderer skin){m=new Mesh();skin.BakeMesh(m);temp=true;}
 else {var mf=r.GetComponent<MeshFilter>();if(mf)m=mf.sharedMesh;}
 if(m&&m.isReadable){foreach(var v in m.vertices){var p=r.transform.TransformPoint(v);if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}
 else {if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}
 if(temp)Destroy(m);
 }return b;
 }
}}
