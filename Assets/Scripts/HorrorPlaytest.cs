using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LosQueQuedan
{
    public sealed class HorrorPlaytest : MonoBehaviour
    {
        readonly List<string> results=new List<string>();
        readonly List<string> errors=new List<string>();
        DemoGame g;HorrorDirector h;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--horror-test")>=0)
            {Application.runInBackground=true;new GameObject("Horror QA").AddComponent<HorrorPlaytest>();}
        }
        void Awake(){Application.logMessageReceived+=Log;}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(message);}
        void Check(bool condition,string title){results.Add((condition?"PASS ":"FAIL ")+title);Debug.Log(results[results.Count-1]);}
        void Set(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
        void Call(string name,params object[] args)=>typeof(DemoGame).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g,args);
        IEnumerator Shot(string name,Vector3 p,float yaw)
        {
            g.player.Teleport(p,yaw);Set(g,"paused",true);DemoGame.HideInterface=true;
            yield return new WaitForSeconds(.7f);yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("/tmp/spooky-horror-qa/"+name+".png");
            yield return new WaitForSeconds(.25f);Set(g,"paused",false);DemoGame.HideInterface=false;
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory("/tmp/spooky-horror-qa");
            yield return new WaitForSeconds(2);
            g=DemoGame.Instance;h=g.horror;
            Check(g&&h&&g.world.witness,"Horror wing initializes with preserved ASCII renderer");
            g.Begin();Call("StartWalking");Call("OpenWard");Set(g,"fade",0f);Set(g,"paused",false);
            Check(Physics.Raycast(new Vector3(0,1.3f,10),Vector3.right,3),"Chapel door is physically closed");
            h.TryInteract("chapel-door");h.TryInteract("service-door");Physics.SyncTransforms();
            Check(!Physics.Raycast(new Vector3(0,1.3f,10),Vector3.right,3.7f),"Real passage into chapel is clear");
            Check(!Physics.Raycast(new Vector3(0,1.3f,14),Vector3.left,3.7f),"Real passage into service room is clear");
            yield return Shot("01-capilla",new Vector3(3.4f,.06f,10),90);
            yield return Shot("02-pintada",new Vector3(6.4f,.06f,10),180);
            h.TryInteract("fuse");
            Check(h.Data.phase==1&&!g.world.fuseObject.activeSelf&&g.world.powerFailed,"Taking fuse causes blackout and starts threat");
            Check(!h.Data.lightOn,"Flashlight starts off");
            Check(!h.CanHoldWitness(),"Without light, gaze alone does not hold the witness");
            h.Data.lightOn=true;   // the player presses F
            g.world.witness.transform.position=new Vector3(7.3f,0,10);
            g.player.Teleport(new Vector3(5,.06f,10),90);Set(h,"grace",0f);Set(g,"paused",false);Physics.SyncTransforms();
            yield return null;
            Check(h.CanHoldWitness(),"Direct illuminated gaze holds witness");
            Vector3 before=g.world.witness.transform.position;yield return new WaitForSeconds(.6f);
            Check(Vector3.Distance(before,g.world.witness.transform.position)<.05f,"Witness remains still under gaze");
            yield return Shot("03-testigo",new Vector3(5,.06f,10),90);
            g.player.Teleport(new Vector3(5,.06f,10),-90);Set(g,"paused",false);
            before=g.world.witness.transform.position;yield return new WaitForSeconds(.5f);
            Check(Vector3.Distance(before,g.world.witness.transform.position)>.15f,"Witness advances when player looks away");
            Set(g,"paused",true);before=g.world.witness.transform.position;yield return new WaitForSeconds(.4f);
            Check(Vector3.Distance(before,g.world.witness.transform.position)<.01f,"Pause freezes threat simulation");
            Set(g,"paused",false);
            float time=g.Remaining;h.Capture();
            Check(h.Data.phase==1&&h.Data.captures==1&&g.Remaining<=time-29.9f,"Capture costs time but preserves fuse progress");
            Check(Vector3.Distance(g.player.transform.position,new Vector3(0,.06f,2))<.2f,"Capture returns player to recovery safely");
            yield return Shot("04-sala-electrica",new Vector3(-3.3f,.06f,14),270);
            yield return Shot("05-orden",new Vector3(-6.3f,.06f,14.5f),180);
            h.TryInteract("hide");Check(h.Hidden,"Hiding cabinet can be entered");
            Set(g,"paused",false);Set(h,"grace",999f);DemoGame.HideInterface=false;
            yield return new WaitForSeconds(.3f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("/tmp/spooky-horror-qa/06-escondite.png");
            h.TryInteract("hide");Check(!h.Hidden,"Hiding cabinet has an exit");
            h.TryInteract("fuse-slot");Check(h.Data.phase==2,"Fuse installs in physical panel");
            h.PressBreaker(1);Check(h.Data.breakerProgress==0,"Incorrect breaker order resets safely");
            h.PressBreaker(3);h.PressBreaker(1);g.Save(false);
            var saved=JsonUtility.FromJson<DemoGame.SaveData>(File.ReadAllText("/tmp/spooky-horror-save.json"));
            Check(saved.horror.phase==2&&saved.horror.breakerProgress==2,"Save includes puzzle and horror state");
            h.Restore(new HorrorDirector.State());g.Load();
            Check(h.Data.phase==2&&h.Data.breakerProgress==2,"Load restores blackout, puzzle and open doors");
            h.PressBreaker(2);
            Check(h.Powered&&!g.world.powerFailed&&!g.world.witness.activeSelf,"Correct sequence restores power and resolves threat");
            yield return Shot("07-circuito-resuelto",new Vector3(-7.5f,.06f,14),270);
            Call("EndVigil",true);Call("Close");Set(g,"paused",false);
            g.Interact("window");Check(g.DialogueOpen,"Narrative ending remains reachable after gameplay");
            Call("Close");
            Check(errors.Count==0,"No runtime errors or exceptions");
            results.AddRange(errors);File.WriteAllLines("/tmp/spooky-horror-qa/results.txt",results);
            Debug.Log("HORROR_QA_DONE");Application.Quit(results.Exists(x=>x.StartsWith("FAIL"))?1:0);
        }
    }
}
