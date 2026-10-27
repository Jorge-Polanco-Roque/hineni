using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace HINENI
{
    // Explicit opt-in diagnostic. Never runs during an ordinary play session.
    public sealed class DemoSmokeTest : MonoBehaviour
    {
        readonly List<string> results=new List<string>();
        readonly List<string> errors=new List<string>();
        DemoGame game;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(DemoGame.TestRequested("smoke"))
            {DemoGame.QA="smoke";new GameObject("Opt-in QA").AddComponent<DemoSmokeTest>();}
        }
        void Awake(){Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Log(string message,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
        void Check(bool ok,string name){results.Add((ok?"PASS ":"FAIL ")+name);Debug.Log(results[results.Count-1]);}
        void Invoke(string name,params object[] args)
        {typeof(DemoGame).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,args);}
        void Field(string name,object value)
        {typeof(DemoGame).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,value);}
        IEnumerator Capture(string file)
        {
            yield return new WaitForSeconds(.3f);yield return new WaitForEndOfFrame();
            if(Application.platform!=RuntimePlatform.WebGLPlayer)ScreenCapture.CaptureScreenshot("/tmp/spooky-qa/"+file+".png");
            yield return new WaitForSeconds(.3f);
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory("/tmp/spooky-qa");
            yield return new WaitForSeconds(3.5f);
            game=DemoGame.Instance;
            Check(game!=null,"Runtime scene initialized");
            Debug.Log("SPOOKY_SCREEN "+Screen.width+"x"+Screen.height+" dpi "+Screen.dpi);
            if(!game){Application.Quit(1);yield break;}
            Check(game.inkShader&&game.inkShader.isSupported,"Ink shader supported on actual graphics device");
            yield return Capture("01-menu");
            game.Begin();
            Field("fade",0f);
            yield return Capture("02-despertar");
            float frozen=game.Remaining;yield return new WaitForSeconds(.5f);
            Check(Mathf.Abs(game.Remaining-frozen)<.01f,"Dialogue freezes world clock");
            Invoke("StartWalking");
            Field("paused",false);
            yield return new WaitForSeconds(.5f);
            Check(game.Remaining<frozen,"Clock advances during active play");
            Vector3 eye=new Vector3(-2.8f,1.68f,-4.5f);
            Vector3 water=new Vector3(-2.8f,1.1f,-3.6f);
            bool waterHit=Physics.Raycast(eye,(water-eye).normalized,out RaycastHit pickup,3.2f);
            Check(waterHit&&pickup.collider.GetComponentInParent<Interactable>()?.actionId=="water","Water can be targeted from a reachable standing position");
            game.Interact("water");
            Check(!game.world.cup.activeSelf,"Water pickup changes world");
            Invoke("OpenWard");
            Check(!game.world.corridorGate.activeSelf&&game.Stage==2,"Conversation opens ward");
            game.player.Teleport(new Vector3(0,.06f,6),0);
            Physics.SyncTransforms();
            Check(!Physics.Raycast(new Vector3(0,1.5f,6),Vector3.forward,10f),"Central corridor has a clear traversal path");
            yield return Capture("03-corredor");
            game.player.Teleport(new Vector3(0,.06f,19),24);
            yield return Capture("04-sala");
            game.Interact("patient");
            yield return new WaitForSecondsRealtime(2.5f);
            var face=game.world.patient.GetComponent<Uncanny>().Head;
            float off=face?Vector3.Angle(game.player.view.transform.forward,face.position-game.player.view.transform.position):180;
            Check(off<9&&game.player.view.fieldOfView<40,"Dialogue camera frames the speaker's face (off "+off.ToString("0")+"°, fov "+game.player.view.fieldOfView.ToString("0")+")");
            yield return Capture("05-conversacion");
            Invoke("Close");
            Invoke("StartVigil");
            Field("paused",false);
            yield return new WaitForSeconds(.3f);
            Check(game.player.view.transform.localPosition.y<1.3f,"Sitting changes viewpoint");
            Invoke("EndVigil",true);Invoke("Close");
            Check(game.Stage==3&&game.world.seam.gameObject.activeSelf,"Vigil reveals anomaly and progresses story");
            game.player.Teleport(new Vector3(0,.06f,15.8f),180);
            yield return Capture("06-costura");
            game.Save(false);var saved=JsonUtility.FromJson<DemoGame.SaveData>(File.ReadAllText("/tmp/spooky-qa-save.json"));
            game.player.Teleport(new Vector3(0,.06f,0),0);game.Load();
            Check(Vector3.Distance(game.player.transform.position,new Vector3(saved.x,saved.y,saved.z))<.1f&&game.Stage==3,"Save/load restores location and narrative phase");
            Check(Mathf.Abs(game.Remaining-saved.remaining)<.1f,"Save/load restores clock without offline advancement");
            game.player.Teleport(new Vector3(0,.06f,24.9f),0);
            Field("paused",false);
            yield return Capture("07-ventana");
            game.EndWith("quedarse");Check(game.Ended&&game.endings.Current.id=="quedarse","An act ends the game with its own ending");
            yield return Capture("08-final");
            Field("ended",false);Field("remaining",.05f);Field("paused",false);
            yield return new WaitForSeconds(.2f);
            Check(game.Remaining==0&&(bool)typeof(DemoGame).GetField("ended",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game),"Clock expiration reaches an ending without softlock");
            Check(errors.Count==0,"No runtime error or exception logged");
            results.AddRange(errors);
            File.WriteAllLines("/tmp/spooky-qa/results.txt",results);
            Debug.Log("SPOOKY_SMOKE_DONE");
            Application.Quit(results.Exists(x=>x.StartsWith("FAIL"))?1:0);
        }
    }
}
