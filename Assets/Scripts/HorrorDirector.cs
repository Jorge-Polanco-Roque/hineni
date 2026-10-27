using System;
using UnityEngine;

namespace HINENI
{
    // The threat obeys one readable rule: light + direct sight hold it in place.
    // Dialogue/pause stop the simulation. Capture costs time, never inventory or puzzle progress.
    public sealed class HorrorDirector : MonoBehaviour
    {
        [Serializable] public sealed class State
        {
            public int phase,breakerProgress,captures;
            public bool chapelOpen,serviceOpen,lightOn,lever;   // the flashlight starts off
            public float witnessX=7.2f,witnessZ=12.1f;
        }
        public State Data { get; private set; }=new State();
        public bool Hidden { get; private set; }
        public bool IsWatched { get; private set; }
        public bool Powered=>Data.phase>=3;
        public bool ActiveThreat=>Data.phase>0&&Data.phase<3&&grace<=0;
        public float ThreatDistance { get; private set; }=99;
        public string Objective=>Data.phase==0?"Capilla · recupera el fusible del altar":Data.phase==1?
            "Sala eléctrica · lleva el fusible al cuadro":Data.phase==2?"Activa los interruptores: la pared conserva el orden":null;
        public string Equipment=>Hidden?Loc.T("OCULTO · E para salir"):
            Loc.T(Data.lightOn?"F · apagar linterna":"F · encender linterna")+(Data.phase==1?"  /  "+Loc.T("FUSIBLE 03"):"");

        DemoGame game;
        HospitalWorld world;
        Light torch;
        AudioSource breath,impact;
        AudioClip knock,shock,burst;
        float grace,blocked,noise,hiddenTime,stepClock,simTime,lightFade;
        Vector3 hidingExit;
        int lastHideFrame=-1;
        // Safe aisle waypoints through the two real openings (no teleporting through walls).
        readonly Vector3[] nodes={new Vector3(7,0,10),new Vector3(3.1f,0,10),new Vector3(0,0,10),
            new Vector3(0,0,14),new Vector3(-3.1f,0,14),new Vector3(-8.3f,0,14),
            new Vector3(0,0,19.2f),new Vector3(0,0,4),new Vector3(0,0,1)};
        readonly int[,] edges={{0,1},{1,2},{2,3},{3,4},{4,5},{3,6},{2,7},{7,8}};

        public void Initialize(DemoGame owner)
        {
            game=owner;world=game.world;
            var lamp=new GameObject("Linterna de Álex");lamp.transform.SetParent(game.player.view.transform,false);
            lamp.transform.localPosition=new Vector3(.12f,-.13f,.08f);
            torch=lamp.AddComponent<Light>();torch.type=LightType.Spot;torch.range=19;torch.spotAngle=48;
            torch.innerSpotAngle=22;torch.intensity=5.3f;torch.color=new Color(.91f,.85f,.68f);torch.shadows=LightShadows.Soft;
            torch.shadowBias=.025f;
            impact=gameObject.AddComponent<AudioSource>();impact.spatialBlend=1;impact.minDistance=2;impact.maxDistance=18;
            knock=MakeSound("Tres golpes",.65f,73);shock=MakeSound("Corte de corriente",1.6f,36);burst=MakeSound("Estallido",1.3f,44);
            breath=world.witness.AddComponent<AudioSource>();breath.spatialBlend=1;breath.minDistance=1;breath.maxDistance=16;
            breath.loop=true;breath.clip=MakeBreath();breath.volume=.28f;
            ApplyState();
        }
        AudioClip MakeSound(string name,float seconds,float hz)
        {
            const int rate=22050;var data=new float[(int)(rate*seconds)];var random=new System.Random(53);
            for(int i=0;i<data.Length;i++)
            {
                float t=(float)i/rate,e=Mathf.Exp(-t*7);
                data[i]=((float)random.NextDouble()*.5f-.25f+Mathf.Sin(t*hz*Mathf.PI*2)*.45f)*e*.5f;
            }
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        AudioClip MakeBreath()
        {
            const int rate=22050;var data=new float[rate*6];var random=new System.Random(91);float filtered=0;
            for(int i=0;i<data.Length;i++)
            {
                float t=(float)i/rate;filtered=Mathf.Lerp(filtered,(float)random.NextDouble()*2-1,.13f);
                float e=Mathf.Pow(Mathf.Sin(Mathf.PI*t/6),2);
                data[i]=(filtered*.2f+Mathf.Sin(t*2*Mathf.PI*47)*.04f+Mathf.Sin(t*2*Mathf.PI*71)*.025f)*e;
            }
            var clip=AudioClip.Create("Respiración detrás de la puerta",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        void Sound(AudioClip clip,Vector3 position,float gain=.6f)
        {impact.transform.position=position;impact.PlayOneShot(clip,gain*game.MasterVolume);}
        public bool TryInteract(string id)
        {
            switch(id)
            {
                case "chapel-door":Data.chapelOpen=!Data.chapelOpen;world.DoorState(world.chapelDoor,Data.chapelOpen);Sound(knock,world.chapelDoor.transform.position,.3f);Persist();return true;
                case "service-door":Data.serviceOpen=!Data.serviceOpen;world.DoorState(world.serviceDoor,Data.serviceOpen);Sound(knock,world.serviceDoor.transform.position,.3f);Persist();return true;
                case "fuse":
                    if(Data.phase!=0)return true;
                    Data.phase=1;Data.chapelOpen=true;grace=5;world.fuseObject.SetActive(false);world.SetPower(true);
                    world.witness.SetActive(true);world.DoorState(world.chapelDoor,true);
                    game.Notify("La luz lo sujeta. F: linterna. Míralo mientras retrocedes hacia el pasillo.",9);
                    Sound(shock,new Vector3(8,2,10),.7f);Persist();return true;
                case "fuse-slot":
                    if(Data.phase==0)game.Notify("El alojamiento está vacío. El fusible está en la capilla.",5);
                    else if(Data.phase==1){Data.phase=2;game.Notify("Fusible colocado. La pintada marca el orden de los tres interruptores.",7);Sound(knock,new Vector3(-9,1.5f,14));Persist();}
                    else game.Notify(Powered?"El circuito ya alimenta la sala común.":"Tres negaciones. Un solo nombre. Dos testigos.",5);
                    return true;
                case "breaker-1":case "breaker-2":case "breaker-3":PressBreaker(id[id.Length-1]-'0');return true;
                case "hide":ToggleHide();return true;
                case "lever-41":
                    if(Data.lever){game.Notify("La palanca sigue abajo. Está tibia, como una mano.",5);return true;}
                    game.Ask("PALANCA 41","Una palanca roja, aparte del cuadro. No alimenta ningún circuito.\n\nEncima, escrito con un dedo: 41.\n\nAl otro lado de la pared, en el pasillo, alguien cuenta en voz baja. Cuarenta y uno. Cuarenta y uno.",
                        new[]{"Bajar la palanca.","Dejarla como está."},new Action[]{PullLever,game.CloseDialog});
                    return true;
                case "graffiti-forehead":game.Notify("AQUÍ APOYABA LA FRENTE. La pared todavía tiene la forma de su cara.",7);return true;
                case "graffiti-word":case "graffiti-father":case "graffiti-seventh":case "graffiti-risen":
                    if(Data.lever){game.Notify(Loc.F("{0} La sangre es nueva.",world.WritingText(id)),7);return true;}
                    break;
            }
            switch(id)
            {
                case "graffiti-order":game.Notify("TRES NEGACIONES / UN SOLO NOMBRE / DOS TESTIGOS. Las palancas están numeradas.",8);return true;
                case "graffiti-gaze":game.Notify("MÍRALO. LA LUZ LO SUJETA. La presencia se detiene si la iluminas y la miras.",8);return true;
                case "graffiti-word":game.Notify("EL VERBO SE HIZO CARNE. LA CARNE SIGUE GRITANDO. Debajo hay marcas de uñas.",7);return true;
                case "graffiti-father":game.Notify("PADRE, NO CONTESTES CON MI VOZ. La última letra todavía escurre.",7);return true;
                case "graffiti-seventh":game.Notify("EL SÉPTIMO DÍA NOS DEJÓ SOLOS. Alguien intentó borrar 'SOLOS'.",7);return true;
                case "graffiti-risen":game.Notify("NO FUE RESURRECCIÓN. ALGO APRENDIÓ A MOVERLOS.",7);return true;
                default:return false;
            }
        }
        void PullLever()
        {
            game.CloseDialog();Data.lever=true;world.SetLever(true);
            var at=world.doctorNpc?world.doctorNpc.transform.position+Vector3.up:new Vector3(-1.6f,1,11.7f);
            world.BurstDoctor(false);Sound(burst,at,1);Sound(knock,new Vector3(-9.7f,1.5f,12.2f),.5f);
            game.Notify("Al otro lado de la pared algo revienta. La cuenta se detiene en cuarenta.",9);Persist();
        }
        public void PressBreaker(int number)
        {
            if(Data.phase!=2){game.Notify(Powered?"El circuito está estable.":"Primero coloca el fusible en el alojamiento inferior.");return;}
            int[] order={3,1,2};
            if(number==order[Data.breakerProgress])
            {
                Data.breakerProgress++;world.SetBreakers(Data.breakerProgress);Sound(knock,new Vector3(-9,1.5f,14),.35f);
                if(Data.breakerProgress==3)
                {
                    Data.phase=3;world.Restore();world.witness.SetActive(false);Hidden=false;
                    game.Notify("La corriente vuelve. La respiración se detiene. Vuelve con el hombre de la sala común.",9);
                }
                else game.Notify(Loc.F("Circuito {0} de 3. Quedan pasos al otro lado de la puerta.",Data.breakerProgress),4);
            }
            else
            {
                Data.breakerProgress=0;world.SetBreakers(0);noise=6;
                game.Notify("El cuadro golpea la pared. La presencia ha oído el error. El orden vuelve a empezar.",7);
                Sound(shock,new Vector3(-9,1.5f,14),.65f);
            }
            Persist();
        }
        void ToggleHide()
        {
            lastHideFrame=Time.frameCount;
            if(Hidden){Hidden=false;game.player.Teleport(hidingExit,180);grace=Mathf.Max(grace,1);game.Notify("Has salido. Escucha antes de encender la luz.");return;}
            Hidden=true;hiddenTime=0;hidingExit=new Vector3(HospitalWorld.LockerX,.06f,15.4f);
            game.player.Teleport(hidingExit,180);game.Notify("Oculto. La luz se apaga. E para salir.",5);
        }
        void Update()
        {
            if(!game||!torch)return;
            torch.enabled=game.Started&&Data.lightOn&&!Hidden;
            if(world.witness.activeInHierarchy&&!breath.isPlaying)breath.Play();
            breath.volume=game.SimulationActive?.5f*game.MasterVolume:0;
            if(!game.SimulationActive)return;
            if(Controls.Down(KeyCode.F)&&!Hidden)Data.lightOn=!Data.lightOn;
            if(Hidden&&Controls.Down(KeyCode.E)&&lastHideFrame!=Time.frameCount){ToggleHide();return;}
            float dt=Time.deltaTime;simTime+=dt;grace=Mathf.Max(0,grace-dt);noise=Mathf.Max(0,noise-dt);
            if(!ActiveThreat){IsWatched=false;ThreatDistance=99;return;}
            Vector3 p=game.player.transform.position;Vector3 w=world.witness.transform.position;
            ThreatDistance=Vector3.Distance(new Vector3(p.x,0,p.z),new Vector3(w.x,0,w.z));
            IsWatched=CanHoldWitness();
            if(Hidden)hiddenTime+=dt;else hiddenTime=0;
            if(!IsWatched)
            {
                // Hidden, you are not safe: it comes to the cabinet and waits at the vents.
                Vector3 destination=Hidden?new Vector3(HospitalWorld.LockerX+.4f,0,15.1f):new Vector3(p.x,0,p.z);
                MoveWitness(destination,(noise>0?2.3f:1.38f)*dt);
                stepClock+=dt;
                if(stepClock>1.15f&&ThreatDistance<14){stepClock=0;Sound(knock,w+Vector3.up,.3f);}
            }
            else blocked=0;
            var direction=game.player.view.transform.position-(world.witness.transform.position+Vector3.up*1.5f);direction.y=0;
            if(direction.sqrMagnitude>.01f)world.witness.transform.rotation=Quaternion.LookRotation(direction);
            if(!Hidden&&ThreatDistance<1.05f)Capture();
        }
        public bool CanHoldWitness()
        {
            if(!Data.lightOn||Hidden||!world.witness.activeSelf)return false;
            Vector3 eye=game.player.view.transform.position,target=world.witness.transform.position+Vector3.up*1.7f;
            var delta=target-eye;
            if(delta.magnitude>17||Vector3.Dot(game.player.view.transform.forward,delta.normalized)<.90f)return false;
            return !Physics.Raycast(eye,delta.normalized,delta.magnitude-.25f,~0,QueryTriggerInteraction.Ignore);
        }
        int Nearest(Vector3 p)
        {
            int best=0;float distance=float.PositiveInfinity;
            for(int i=0;i<nodes.Length;i++){float d=(nodes[i]-p).sqrMagnitude;if(d<distance){distance=d;best=i;}}
            return best;
        }
        Vector3 Route(Vector3 from,Vector3 target)
        {
            Vector3 a=from+Vector3.up*1.15f,b=target+Vector3.up*1.15f;
            bool obstructed=false;
            foreach(var hit in Physics.SphereCastAll(a,.23f,(b-a).normalized,Vector3.Distance(a,b),~0,QueryTriggerInteraction.Ignore))
                if(!hit.collider.transform.IsChildOf(game.player.transform)) {obstructed=true;break;}
            if(!obstructed)return target;
            int start=Nearest(from),end=Nearest(target);
            if(Vector3.Distance(from,nodes[start])>.65f)return nodes[start];
            if(start==end)return target;
            int[] prev=new int[nodes.Length];for(int i=0;i<prev.Length;i++)prev[i]=-1;
            var queue=new System.Collections.Generic.Queue<int>();queue.Enqueue(start);prev[start]=start;
            while(queue.Count>0)
            {
                int n=queue.Dequeue();
                for(int e=0;e<edges.GetLength(0);e++)
                {
                    int v=edges[e,0]==n?edges[e,1]:edges[e,1]==n?edges[e,0]:-1;
                    if(v<0||prev[v]!=-1)continue;prev[v]=n;queue.Enqueue(v);
                }
            }
            if(prev[end]<0)return from;
            int next=end;while(prev[next]!=start&&prev[next]!=next)next=prev[next];
            return nodes[next];
        }
        void MoveWitness(Vector3 destination,float step)
        {
            Vector3 pos=world.witness.transform.position;pos.y=0;
            var waypoint=Route(pos,destination);Vector3 delta=waypoint-pos;delta.y=0;
            float distance=Mathf.Min(step,delta.magnitude);
            if(distance<.002f)return;
            if(Physics.SphereCast(pos+Vector3.up*1.15f,.23f,delta.normalized,out var hit,distance+.03f,~0,QueryTriggerInteraction.Ignore))
            {
                blocked+=Time.deltaTime;
                if(blocked>4.5f)
                {
                    if(hit.collider.gameObject==world.chapelDoor){Data.chapelOpen=true;world.DoorState(world.chapelDoor,true);}
                    if(hit.collider.gameObject==world.serviceDoor){Data.serviceOpen=true;world.DoorState(world.serviceDoor,true);}
                    blocked=0;Sound(knock,pos+Vector3.up,.7f);
                }
                return;
            }
            blocked=0;world.witness.transform.position=pos+delta.normalized*distance;
        }
        public void Capture()
        {
            Data.captures++;game.LoseSeconds(30);grace=9;Hidden=false;
            world.witness.transform.position=new Vector3(7,0,10);
            game.player.Teleport(new Vector3(0,.06f,2),0);Data.lightOn=true;
            Sound(shock,game.player.transform.position,.65f);
            game.Notify("Estás de nuevo en recuperación. Faltan treinta segundos más. Conservas el fusible y el circuito.",9);
            Persist();
        }
        void Persist(){game.Save(false);}
        public State Snapshot()
        {
            if(world.witness){Data.witnessX=world.witness.transform.position.x;Data.witnessZ=world.witness.transform.position.z;}
            return Data;
        }
        public void Restore(State state)
        {
            Data=state??new State();Data.phase=Mathf.Clamp(Data.phase,0,3);Data.breakerProgress=Mathf.Clamp(Data.breakerProgress,0,3);
            Hidden=false;grace=4;ApplyState();
        }
        void ApplyState()
        {
            world.DoorState(world.chapelDoor,Data.chapelOpen);world.DoorState(world.serviceDoor,Data.serviceOpen);
            world.fuseObject.SetActive(Data.phase==0);if(Data.phase>=3)world.Restore();else world.SetPower(Data.phase>0);world.SetBreakers(Data.breakerProgress);
            world.witness.transform.position=new Vector3(Data.witnessX,0,Data.witnessZ);world.witness.SetActive(Data.phase>0&&Data.phase<3);
            world.SetLever(Data.lever);if(Data.lever)world.BurstDoctor(true);
        }
        void OnDestroy()
        {
            if(knock)Destroy(knock);if(shock)Destroy(shock);if(burst)Destroy(burst);
            if(breath&&breath.clip)Destroy(breath.clip);
        }
    }
}
