using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LosQueQuedan
{
    public sealed class DemoGame : MonoBehaviour
    {
        public static DemoGame Instance { get; private set; }
        public Shader inkShader;
        public Material surfaceTemplate, cutoutTemplate;
        public Shader worldTextShader;
        public FirstPerson player;
        public HospitalWorld world;
        public float Remaining => remaining;
        public bool DialogueOpen => dialogBody != null;
        public bool Started => started;
        public int Stage => stage;
        public bool SimulationActive => started&&!paused&&!DialogueOpen&&!ended&&!prologue;
        // Before Inés speaks: the pale horse and the death that flees (Revelation 6:8, 9:6), quoted, read aloud, on black, until "Continue".
        const string PassageRef="APOCALIPSIS 6:8 · 9:6";
        const string Passage="Y miré, y he aquí un caballo amarillo: y el que estaba sentado sobre él tenía por nombre Muerte; y el infierno le seguía: y le fué dada potestad sobre la cuarta parte de la tierra, para matar con espada, con hambre, con mortandad, y con las bestias de la tierra.\nY en aquellos días buscarán los hombres la muerte, y no la hallarán; y desearán morir, y la muerte huirá de ellos.";
        bool prologue;float prologueStart;AudioSource bells;
        public float MasterVolume => volume;
        public HorrorDirector horror;
        public Heaven heaven;
        public Acts acts;
        public Endings endings;
        public bool Ended=>ended;
        public bool Paused=>paused;
        public bool Sitting=>sat;
        public const float Minutes=15*60;
        static bool openGallery;   // "back to the endings" reloads the scene and reopens the gallery
        bool gallery;
        public bool ReduceEffects=>reduceEffects;
        public void Notify(string text,float duration=5){Toast(text,duration);}
        public void LoseSeconds(float seconds){remaining=Mathf.Max(0,remaining-seconds);}
        bool started, paused, settings, ended, hasWater, gaveWater, called, readRecord, sat, stayed;
        int stage;
        float remaining = 3580, sittingTime, toastTime, fade = 1, volume = .45f;
        string toast, dialogSpeaker, dialogBody;
        string[] choices;
        Action[] callbacks;
        string saveStatus;
        bool reduceEffects, consumed;
        float typeStart;
        InkCamera ink;
        Uncanny speaker;
        Light faceLight;
        public InkCamera Ink=>ink;
        AudioSource atmosphere, atmosphereNext, effects, voice;
        const float MusicCrossfade=5;
        float musicClock;   // our own playhead: browsers cannot report AudioSource.time for compressed audio
        AudioClip foot, tone;
        public static string QA;   // "smoke" / "horror" while an automated test runs (set by the test itself)
        // True when started with --<name>-test, or in a browser with ?qa=<name>.
        public static bool TestRequested(string name)=>Array.IndexOf(Environment.GetCommandLineArgs(),"--"+name+"-test")>=0
            ||(Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("qa="+name));
        string SavePath => QA=="horror" ? "/tmp/spooky-horror-save.json" : QA=="smoke" ? "/tmp/spooky-qa-save.json" : Path.Combine(Application.persistentDataPath,"los-que-quedan-demo.json");

        [Serializable] public class SaveData
        {
            public int version=2,stage;
            public float remaining,x,y,z,yaw,sittingTime;
            public bool hasWater,gaveWater,called,readRecord,sat,stayed;
            public HorrorDirector.State horror;
        }

        void Awake()
        {
            Instance=this; Application.targetFrameRate=60; Loc.Init();
            world=new GameObject("Hospital del Umbral").AddComponent<HospitalWorld>();world.surfaceTemplate=surfaceTemplate;world.cutoutTemplate=cutoutTemplate;world.worldTextShader=worldTextShader;world.Build();
            var p=new GameObject("Alex");p.transform.position=new Vector3(0,.06f,-2.7f);
            var c=p.AddComponent<CharacterController>();c.height=1.72f;c.radius=.25f;c.center=new Vector3(0,.86f,0);c.stepOffset=.24f;
            player=p.AddComponent<FirstPerson>();
            var camObject=new GameObject("Mirada");camObject.tag="MainCamera";camObject.transform.SetParent(p.transform,false);camObject.transform.localPosition=new Vector3(0,1.62f,0);
            player.view=camObject.AddComponent<Camera>();player.view.fieldOfView=67;player.view.nearClipPlane=.06f;player.view.farClipPlane=70;
            player.view.backgroundColor=new Color(.035f,.04f,.045f);player.view.clearFlags=CameraClearFlags.SolidColor;
            camObject.AddComponent<AudioListener>();ink=camObject.AddComponent<InkCamera>();ink.inkShader=inkShader;
            SetupAudio();
            horror=new GameObject("Reglas del Testigo").AddComponent<HorrorDirector>();horror.Initialize(this);
            heaven=new GameObject("Techo").AddComponent<Heaven>();heaven.Initialize(this);
            acts=new GameObject("Lo último que hagas").AddComponent<Acts>();acts.Initialize(this);
            endings=new GameObject("Finales").AddComponent<Endings>();endings.Initialize(this);
            new GameObject("Los que miran").AddComponent<Watchers>().Initialize(this);
            gallery=openGallery;openGallery=false;
            player.Teleport(new Vector3(-.8f,.06f,18.5f),8);
            Unlock();
        }
        void SetupAudio()
        {
            atmosphere=gameObject.AddComponent<AudioSource>();atmosphere.loop=true;atmosphere.spatialBlend=0;
            const int rate=22050;var data=new float[rate*4];var rng=new System.Random(17);
            for(int i=0;i<data.Length;i++)
            {float t=(float)i/rate;data[i]=Mathf.Sin(t*2*Mathf.PI*55)*.07f+Mathf.Sin(t*2*Mathf.PI*110)*.018f+((float)rng.NextDouble()*2-1)*.008f;}
            var hum=AudioClip.Create("Ventilacion original",data.Length,1,rate,false);hum.SetData(data,0);
            // Música en bucle; el zumbido sintético queda de respaldo si falta el archivo.
            var music=Resources.Load<AudioClip>("Music/MechanicalHum");atmosphere.clip=music!=null?music:hum;atmosphere.volume=volume;atmosphere.Play();
            if(music!=null)
            {
                // Dos fuentes alternas: la siguiente entra con fundido mientras la canción hace su propio cierre.
                atmosphere.loop=false;atmosphereNext=gameObject.AddComponent<AudioSource>();atmosphereNext.spatialBlend=0;atmosphereNext.clip=music;
            }
            effects=gameObject.AddComponent<AudioSource>();effects.volume=volume;
            voice=gameObject.AddComponent<AudioSource>();voice.spatialBlend=0;
            foot=Sound("Paso original",.11f,92,.3f);tone=Sound("Resonancia original",1.1f,41,.18f);
        }
        AudioClip Sound(string name,float seconds,float frequency,float gain)
        {
            const int rate=22050;var samples=new float[(int)(rate*seconds)];var rng=new System.Random(31);
            for(int i=0;i<samples.Length;i++)
            {float t=(float)i/rate;float envelope=Mathf.Exp(-t/(seconds*.19f));samples[i]=(Mathf.Sin(2*Mathf.PI*frequency*t)*.6f+((float)rng.NextDouble()*2-1)*.4f)*envelope*gain;}
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        public void Footstep(){if(effects&&foot){effects.pitch=UnityEngine.Random.Range(.9f,1.08f);effects.PlayOneShot(foot);}}
        void Update()
        {
            fade=Mathf.MoveTowards(fade,0,Time.unscaledDeltaTime*.35f);
            if(toastTime>0)toastTime-=Time.unscaledDeltaTime;
            if(Controls.Down(KeyCode.Escape)&&started&&!ended&&!DialogueOpen&&!prologue)
            {paused=!paused;settings=false;if(paused)Unlock();else Lock();}
            player.CanMove=started&&!paused&&!DialogueOpen&&!ended&&!prologue&&!sat&&!(horror&&horror.Hidden)&&!(acts&&acts.Tuning);
            if(started&&!paused&&!DialogueOpen&&!ended&&!prologue)
            {
                remaining=Mathf.Max(0,remaining-Time.deltaTime);
                if(remaining<=0){EndWith("juicio");return;}
                if(sat)
                {
                    sittingTime+=Time.deltaTime;
                    if(sittingTime>8 && sittingTime-Time.deltaTime<=8)Toast("«Una vez dejé ganar a mi hermano para que volviera.»",7);
                    if(sittingTime>17 && sittingTime-Time.deltaTime<=17)Toast("«No sé si aquello era quererlo. Pero volvió.»",7);
                    acts.Breath();if(ended)return;
                    if(Controls.Down(KeyCode.E)){EndVigil(false);return;}
                }
                if(Controls.Down(KeyCode.E)&&player.Focus)Interact(player.Focus.actionId);
                if(Input.GetKeyDown(KeyCode.F5))Save();
            }
            float musicVolume=volume*(paused?.45f:1)*(heaven&&heaven.InSky?.3f:1)*(voice.isPlaying?.6f:1)*(prologue?0:1);effects.volume=volume;voice.volume=volume*1.6f;
            if(atmosphere.isPlaying)musicClock+=Time.unscaledDeltaTime;
            if(atmosphereNext&&atmosphere.clip.loadState==AudioDataLoadState.Loaded&&atmosphere.clip.length>MusicCrossfade*4&&atmosphere.clip.length-musicClock<=MusicCrossfade&&!atmosphereNext.isPlaying)
            {(atmosphere,atmosphereNext)=(atmosphereNext,atmosphere);atmosphere.Play();musicClock=0;}
            // La que entra sube durante el fundido; la que sale conserva volumen porque la pista ya se apaga sola.
            atmosphere.volume=musicVolume*(atmosphereNext&&atmosphereNext.isPlaying?Mathf.Clamp01(musicClock/MusicCrossfade):1);
            if(atmosphereNext)atmosphereNext.volume=musicVolume;
        }
        void Lock(){if(Controls.Touch)return;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        void Unlock(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        // From the menu the game opens on the passage; automated tests go straight to Inés.
        public void Begin(bool withPrologue=false)
        {
            started=true;stage=0;remaining=Minutes;player.Teleport(new Vector3(0,.06f,-2.7f),15);fade=1;
            if(withPrologue){prologue=true;prologueStart=Time.unscaledTime;Unlock();Speak("APOCALIPSIS",Passage);return;}
            WakeUp();
        }
        void WakeUp()
        {
            Say("INÉS", "No intentes levantarte deprisa.\n\nTe llamas Álex. Estás en el Hospital del Umbral. Llevas diecinueve días aquí.\n\nVoy a decirte algo difícil. Puedes tomarte un momento.",
                new[]{"Dime qué está pasando.","Solo necesito un momento."},
                new Action[]{TellTruth,()=>Say("INÉS","Estoy aquí. No tienes que contestar enseguida.",new[]{"Estoy listo. Dímelo."},new Action[]{TellTruth})});
        }
        void TellTruth()
        {
            Say("INÉS","Quedan quince minutos. Para todos.\n\nLo confirmaron hace tres días. La radio lo llama el Juicio. No hay un lugar al que podamos ir.\n\nNo sé qué se hace en el fin del mundo. Sé cambiar estas sábanas.",new[]{"¿Por qué sigues aquí?","Quiero levantarme."},
            new Action[]{()=>Say("INÉS","Porque había alguien aquí.\n\nY porque todavía no he conseguido llamar a mi hermana.",new[]{"Voy a intentarlo. Levantarme, quiero decir."},new Action[]{StartWalking}),StartWalking});
        }
        void StartWalking(){Close();stage=1;Toast("Acércate a Inés. WASD para caminar · ratón para mirar · E para interactuar",9);Save(false);}
        public void Interact(string id)
        {
            if(!started||paused||DialogueOpen||ended)return;
            if(acts&&acts.TryInteract(id))return;
            if(horror&&horror.TryInteract(id))return;
            var who=id=="ines"?world.nurse:id=="patient"?world.patient
                :GameObject.Find(id=="doctor"?"Doctor":id=="mujer"?"Mujer":id=="esquina"?"Esquina":"-");
            speaker=who?who.GetComponent<Uncanny>():null;
            switch(id)
            {
                case "doctor":
                    Say("EL MÉDICO","Su análisis salió perfecto. Le faltan los dientes del martes, pero eso pasa.\n\n[No se da la vuelta. Apoya la frente en la pared y respira contra ella.]\n\nVuelva cuando haya terminado de nacer.",
                        new[]{"¿De qué está hablando?","Alejarme despacio."},
                        new Action[]{()=>Say("EL MÉDICO","De la hora. Siempre hablo de la hora.\n\nLa hora tiene hambre y nosotros somos lo único caliente que queda en el edificio.\n\n[Golpea la pared con la frente. Una vez. Otra.]",new[]{"Retroceder."},new Action[]{Close}),Close});
                    break;
                case "mujer":
                    Say("LA MUJER DE LA SILLA","¿Me traes la sopa? La pedí antes de que existiera la cocina.\n\n[Su cabeza sigue inclinada. Sus ojos no se mueven contigo: te esperan.]\n\nSonríe. Están contando a los que sonríen.",
                        new[]{"No tengo sopa.","Sonreír."},
                        new Action[]{()=>Say("LA MUJER DE LA SILLA","Entonces eres de los otros.\n\nLos otros también traían las manos vacías. Luego ya no traían manos.",new[]{"Seguir caminando."},new Action[]{Close}),
                                     ()=>{Close();Toast("Ella sonríe también. Demasiado tiempo. No deja de sonreír cuando te vas.",7);}});
                    break;
                case "esquina":
                    Say("EL HOMBRE DE LA ESQUINA","No mires el techo.\n\nNo mires el techo.\n\n[Su cabeza ha girado demasiado. Su cuerpo sigue mirando la esquina.]\n\nYa nos ha visto. Escribe con nuestra letra.",
                        new[]{"¿Quién nos ha visto?","Retroceder."},
                        new Action[]{()=>Say("EL HOMBRE DE LA ESQUINA","El que llega a las cero.\n\nLlega tarde a todo, menos a esto.\n\n[Abre la boca más de lo que una boca se abre. No sale ningún sonido.]",new[]{"Apartar la vista."},new Action[]{Close}),Close});
                    break;
                case "ines":
                    if(stage<=1)
                        Say("INÉS","El circuito de la sala común está fallando. Dejaron un fusible en la capilla, a la derecha del pasillo. El cuadro está en mantenimiento, al otro lado.\n\nLleva la linterna. F la enciende.\n\nDespués, si puedes, quédate con el hombre de la sala. Lleva toda la tarde preguntando por alguien.",new[]{"Voy a buscar el fusible.","Primero quiero mirar alrededor."},new Action[]{OpenWard,OpenWard});
                    else Say("INÉS",stayed?"He oído su voz. La de mi hermana.\n\nNo hemos arreglado nada. Pero he oído su voz.\n\nGracias por quedarte.":"La sala común está al final del pasillo.\n\nSi necesitas parar, para. Acabas de volver.",new[]{"Seguir."},new Action[]{Close});
                    break;
                case "water":
                    if(!hasWater&&!gaveWater){hasWater=true;world.cup.SetActive(false);Toast("Llevas un vaso de agua. Puedes ofrecérselo al hombre de la sala común.",6);Save(false);}
                    break;
                case "gate":
                    if(stage<2)Say("ÁLEX","Necesito saber adónde voy antes de salir. Inés está junto a la cama.",new[]{"Volver."},new Action[]{Close});
                    else world.corridorGate.SetActive(false);
                    break;
                case "patient":Patient();break;
                case "chair":
                    if(stage>=3)Say("EL HOMBRE",stayed?"Todavía estás aquí.":"Puedes volver a sentarte si quieres. No tienes que explicarme nada.",new[]{"Quedarme un instante.","Voy a mirar por la ventana."},new Action[]{StartVigil,Close});
                    else Patient();break;
                case "window":
                    if(horror&&!horror.Powered){Toast("La luz de la sala está fallando. Recupera el fusible de la capilla y restablece el circuito.",7);break;}
                    if(stage<3)Say("ÁLEX","La ciudad sigue en su sitio.\n\nEn una ventana alguien sacude un mantel. Me sorprende que todavía haya migas.",new[]{"Volver a la sala."},new Action[]{Close});
                    else Say("ÁLEX","La costura del pasillo también está en el cielo.\n\nNo parece una herida. Parece un intento de cerrar algo.\n\nDetrás de mí, alguien mueve una silla.",new[]{"Mirar hacia arriba. No hace falta ventana.","Volver con ellos."},new Action[]{Close,Close});
                    break;
                case "radio":
                    Say("RADIO DE EMERGENCIAS","…la hora estimada no ha cambiado.\n\nSi puede escuchar esta emisión, no tiene que responder. Seguiremos aquí mientras…\n\n[Una pausa. Se oye a alguien acercar un vaso al micrófono.]\n\n…mientras podamos.",new[]{"Escuchar el silencio un momento."},new Action[]{Close});break;
                case "record":
                    readRecord=true;
                    Say("TU EXPEDIENTE","ÁLEX / RECUPERACIÓN\nIngreso: hace diecinueve días.\nContacto: Lucía.\n\nDentro hay una nota doblada:\n«Si despiertas, no intentes explicármelo todo de golpe.»\n\nReconoces la letra antes de recordar su voz.",new[]{"Guardar la nota."},new Action[]{()=>{Close();Save(false);}});break;
                case "phone":
                    if(called)Say("TELÉFONO","La línea ya no conecta.\n\nLa conversación existió. No puedes repetirla hasta que salga perfecta.",new[]{"Colgar."},new Action[]{Close});
                    else acts.StartTune(()=>Say("LUCÍA","¿Quién…?\n\nNo. No me digas que eres tú si no eres tú.\n\n[Respira.]\n\nPodemos hablar. No sé si podemos arreglarlo. No me hagas prometer las dos cosas.",new[]{"No tienes que prometerme nada.","Solo quería oírte."},new Action[]{EndCall,EndCall}));break;
            }
        }
        void OpenWard(){stage=2;world.corridorGate.SetActive(false);Close();Toast("Capilla a la derecha. Mantenimiento a la izquierda. F: linterna. E: puertas y objetos.",8);Save(false);}
        void EndCall(){called=true;Say("LUCÍA","Estoy sentada en el suelo de la cocina.\n\nNo sé por qué te lo digo.\n\n…Quédate un momento. Quiero que haya alguien al otro lado.",new[]{"Quedarme hasta que la línea se corte."},new Action[]{()=>{Close();EndWith("voz");}});}
        void Patient()
        {
            if(hasWater)
                Say("EL HOMBRE","¿Es para mí?\n\nNo tengo mucha sed. Pero me gusta que me lo preguntes.",new[]{"Ofrecerle el agua.","Sentarme junto a él."},new Action[]{()=>{hasWater=false;gaveWater=true;PatientQuestion();},PatientQuestion});
            else PatientQuestion();
        }
        void PatientQuestion()
        {
            Say("EL HOMBRE","¿Crees que el cielo va a doler?\n\nNo hace falta que inventes una respuesta. Ya me han dado muchas hoy.",new[]{"No lo sé. Puedo quedarme.","Puedo acercar la silla.","Ahora mismo no puedo quedarme."},new Action[]{StartVigil,StartVigil,()=>EndVigil(false)});
        }
        void StartVigil()
        {
            Close();sat=true;sittingTime=0;player.Teleport(new Vector3(1.45f,.06f,22.7f),90);
            player.view.transform.localPosition=new Vector3(0,1.12f,0);Lock();
            Toast("Permanecer también es una acción. Puedes levantarte con E.",6);
        }
        void EndVigil(bool completed)
        {
            sat=false;stayed=stayed||completed;stage=3;world.RevealAnomaly();
            player.view.transform.localPosition=new Vector3(0,1.62f,0);
            effects.pitch=1;effects.PlayOneShot(tone);
            Say(completed?"INÉS":"EL HOMBRE",completed?
                "He podido llamarla.\n\n[Inés deja una mano en el respaldo de la silla.]\n\n¿Esa línea estaba antes en el suelo?\n\n[Él mira el pasillo y después la ventana. «Prefiero mirar allí», dice.]":
                "Está bien.\n\n[No lo dice como un reproche. Tampoco como una absolución.]\n\n[Algo se mueve en el pasillo. Una línea negra atraviesa las baldosas. Él pide que giren la silla hacia la ventana.]",
                new[]{"Mirar el cielo."},new Action[]{()=>{Close();Toast("La ventana está al fondo de la sala. También puedes volver al pasillo.",7);Save(false);}});
        }
        public void Ask(string speaker,string body,string[] options,Action[] actions)=>Say(speaker,body,options,actions);
        public void CloseDialog()=>Close();
        void Say(string speaker,string body,string[] options,Action[] actions)
        {
            var said=Loc.T(body);
            dialogSpeaker=Loc.T(speaker);dialogBody=Spoken(said);choices=Array.ConvertAll(options,Loc.T);callbacks=actions;
            typeStart=Time.unscaledTime;Unlock();Speak(speaker,body);
        }
        // Stage directions in [brackets] stay in the script for the voices tool; the player reads only the words.
        static string Spoken(string body)
        {
            var s=System.Text.RegularExpressions.Regex.Replace(body,@"\[[^\]]*\]","");
            s=System.Text.RegularExpressions.Regex.Replace(s,@"\n\s*\n(\s*\n)+","\n\n");
            return s.Trim();
        }
        // Spoken lines are pre-rendered by Tools/voices.py (Kokoro); narration and Álex's thoughts stay silent.
        public static string VoiceKey(string speaker,string text)
        {
            uint h=2166136261;foreach(byte b in System.Text.Encoding.UTF8.GetBytes(speaker+"\n"+text)){h^=b;h*=16777619;}
            return h.ToString("x8");
        }
        // A Spanish line, spoken in the voice language: Voices/ for Spanish, Voices/<code>/ for the rest, keyed by the
        // line as written in that language. Text and voice languages are independent.
        public static AudioClip Voice(string speaker,string spanish)
        {
            var code=Loc.VoiceLang;
            return Resources.Load<AudioClip>((code=="es"?"Voices/":"Voices/"+code+"/")+VoiceKey(speaker,Loc.In(code,spanish)));
        }
        void Speak(string speaker,string body){voice.Stop();var clip=Voice(speaker,body);if(clip){voice.clip=clip;voice.Play();}}
        void Close(){voice.Stop();dialogBody=null;choices=null;callbacks=null;if(speaker)speaker.Talking=false;speaker=null;if(!paused&&!ended)Lock();}
        // Lines said under the breath arrive as subtitles; the speaker's mouth moves while they last.
        public void Whisper(Uncanny who,string text)
        {
            if(!started||paused||ended||prologue)return;
            who.talkUntil=Time.time+2.6f;
            var clip=Voice(who.displayName,text);
            if(clip)
            {
                if(!who.TryGetComponent(out AudioSource mouth)){mouth=who.gameObject.AddComponent<AudioSource>();mouth.spatialBlend=1;mouth.minDistance=1.5f;mouth.maxDistance=10;mouth.rolloffMode=AudioRolloffMode.Linear;}
                mouth.Stop();mouth.PlayOneShot(clip,volume*1.4f);who.talkUntil=Time.time+clip.length;
            }
            Toast(Loc.F("{0}   «{1}»",Loc.T(who.displayName),Loc.T(text)),5.5f);
        }
        void Toast(string text,float duration=4){toast=Loc.T(text);toastTime=duration;}
        // The last thing you finish decides the ending.
        public void EndWith(string id)
        {
            if(ended)return;
            ended=true;sat=false;Close();Unlock();toastTime=0;
            player.view.transform.localPosition=new Vector3(0,1.62f,0);
            endings.Play(id);
        }
        void Finish(bool timeEnded)
        {
            ended=true;sat=false;Close();Unlock();
            dialogSpeaker=timeEnded?"EL FRENTE":"FIN DEL PRIMER FRAGMENTO";
            saveStatus=timeEnded?"El reloj llegó a cero. El mundo no esperaba una respuesta.":"La última hora acaba de empezar.";
        }
        public void Save(bool notify=true)
        {
            if(!started||ended||stage<1)return;
            var pos=player.transform.position;
            var data=new SaveData{stage=stage,remaining=remaining,x=pos.x,y=pos.y,z=pos.z,yaw=player.transform.eulerAngles.y,
                hasWater=hasWater,gaveWater=gaveWater,called=called,readRecord=readRecord,sat=sat,stayed=stayed,sittingTime=sittingTime,horror=horror?horror.Snapshot():null};
            try{File.WriteAllText(SavePath,JsonUtility.ToJson(data,true));saveStatus="Partida guardada en este equipo.";if(notify)Toast("Partida guardada.");}
            catch(Exception e){Debug.LogWarning("No se pudo guardar: "+e.Message);saveStatus="No se pudo guardar la partida.";Toast(saveStatus);}
        }
        public void Load()
        {
            try
            {
                var s=JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if(s==null||(s.version!=1&&s.version!=2)||s.stage<1||s.stage>3)throw new InvalidDataException("Guardado incompatible");
                started=true;stage=s.stage;remaining=Mathf.Clamp(s.remaining,0,Minutes);hasWater=s.hasWater;gaveWater=s.gaveWater;called=s.called;readRecord=s.readRecord;stayed=s.stayed;sat=s.sat;sittingTime=s.sittingTime;
                world.cup.SetActive(!hasWater&&!gaveWater);world.corridorGate.SetActive(stage<2);if(stage>=3)world.RevealAnomaly();
                if(horror)horror.Restore(s.horror);
                player.Teleport(new Vector3(s.x,s.y,s.z),s.yaw);player.view.transform.localPosition=new Vector3(0,sat?1.12f:1.62f,0);Lock();Toast("Has vuelto. El tiempo no avanzó mientras no estabas.",6);
            }
            catch(Exception e){Debug.LogWarning(e.Message);Toast("No se pudo cargar. Puedes iniciar una nueva partida.",6);}
        }
        void OnApplicationQuit(){Save(false);}
        void OnApplicationFocus(bool focus)
        {if(!focus&&started&&!ended&&!DialogueOpen&&QA==null){paused=true;Unlock();}}   // tests run without focus
        // ---- Interface: every element is text on the same character grid as the world. ----
        static readonly System.Collections.Generic.Dictionary<char,string[]> Letters=new System.Collections.Generic.Dictionary<char,string[]>
        {
            {'L',new[]{"#....","#....","#....","#....","#....","#....","#####"}},
            {'O',new[]{".###.","#...#","#...#","#...#","#...#","#...#",".###."}},
            {'S',new[]{".####","#....","#....",".###.","....#","....#","####."}},
            {'Q',new[]{".###.","#...#","#...#","#...#","#.#.#","#..#.",".##.#"}},
            {'U',new[]{"#...#","#...#","#...#","#...#","#...#","#...#",".###."}},
            {'E',new[]{"#####","#....","#....","####.","#....","#....","#####"}},
            {'D',new[]{"####.","#...#","#...#","#...#","#...#","#...#","####."}},
            {'A',new[]{".###.","#...#","#...#","#####","#...#","#...#","#...#"}},
            {'N',new[]{"#...#","##..#","#.#.#","#..##","#...#","#...#","#...#"}},
            {'H',new[]{"#...#","#...#","#...#","#####","#...#","#...#","#...#"}},
            {'B',new[]{"####.","#...#","#...#","####.","#...#","#...#","####."}},
            {'F',new[]{"#####","#....","#....","####.","#....","#....","#...."}},
            {'G',new[]{".####","#....","#....","#.###","#...#","#...#",".###."}},
            {'K',new[]{"#...#","#..#.","#.#..","##...","#.#..","#..#.","#...#"}},
            {'P',new[]{"####.","#...#","#...#","####.","#....","#....","#...."}},
            {'T',new[]{"#####","..#..","..#..","..#..","..#..","..#..","..#.."}},
            {'W',new[]{"#...#","#...#","#...#","#.#.#","#.#.#","##.##","#...#"}},
            {'X',new[]{"#...#","#...#",".#.#.","..#..",".#.#.","#...#","#...#"}},
            {'Y',new[]{"#...#","#...#",".#.#.","..#..","..#..","..#..","..#.."}},
            {'R',new[]{"####.","#...#","#...#","####.","#.#..","#..#.","#...#"}},
            {'V',new[]{"#...#","#...#","#...#","#...#","#...#",".#.#.","..#.."}},
            {'Z',new[]{"#####","....#","...#.","..#..",".#...","#....","#####"}},
            {'J',new[]{"..###","...#.","...#.","...#.","#..#.","#..#.",".##.."}},
            {'I',new[]{".###.","..#..","..#..","..#..","..#..","..#..",".###."}},
            {'C',new[]{".####","#....","#....","#....","#....","#....",".####"}},
            {'M',new[]{"#...#","##.##","#.#.#","#.#.#","#...#","#...#","#...#"}},
        };
        const int LetterWidth=12; // 5 pixels x 2 columns + 2 spacing

        void LateUpdate()
        {
            // Title screen: the sky is open and tilted so the angels fill the right half, beside the title panel.
            bool title=!started&&!HideInterface;   // automated captures run without the title screen
            if(heaven)heaven.menu=title;
            if(title&&player)player.view.transform.rotation=Quaternion.LookRotation(new Vector3(.47f,.88f,0),Vector3.forward);
            FaceCamera();DrawUI();
        }
        // During a conversation the view turns to the speaker's face and closes in: the face fills the frame.
        void FaceCamera()
        {
            if(!started||!player||!player.view)return;
            var cam=player.view;
            bool close=DialogueOpen&&speaker&&speaker.Head&&!ended;
            if(speaker)speaker.Talking=close&&(voice.isPlaying||Time.unscaledTime-typeStart<dialogBody?.Length/62f);
            if(close)
            {
                var face=speaker.Head.position+speaker.Head.up*.04f;
                // Frame head and shoulders with the face in the upper half, clear of the dialogue panel below.
                float span=2*Mathf.Atan2(.42f,Vector3.Distance(face,cam.transform.position))*Mathf.Rad2Deg;
                span=Mathf.Clamp(span,14,67);
                var aim=Quaternion.LookRotation(face-cam.transform.position)*Quaternion.Euler(span*.22f,0,0);
                cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,aim,1-Mathf.Exp(-Time.unscaledDeltaTime*4));
                cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,span,1-Mathf.Exp(-Time.unscaledDeltaTime*3));
                // Horror key from below, between the player and the face: it carves eyes, nose and mouth.
                if(!faceLight)
                {
                    faceLight=new GameObject("Luz de rostro").AddComponent<Light>();faceLight.type=LightType.Spot;
                    faceLight.spotAngle=34;faceLight.range=3.5f;faceLight.color=new Color(.78f,.95f,.85f);faceLight.shadows=LightShadows.Soft;
                }
                var toward=(cam.transform.position-face);toward.y=0;toward.Normalize();
                faceLight.transform.position=face+toward*.9f+Vector3.down*.55f+Vector3.Cross(Vector3.up,toward)*.25f;
                faceLight.transform.rotation=Quaternion.LookRotation(face-faceLight.transform.position);
                faceLight.intensity=Mathf.MoveTowards(faceLight.intensity,4.5f,Time.unscaledDeltaTime*6);
            }
            else
            {
                cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,67,1-Mathf.Exp(-Time.unscaledDeltaTime*4));
                if(faceLight)faceLight.intensity=Mathf.MoveTowards(faceLight.intensity,0,Time.unscaledDeltaTime*6);
            }
        }
        // Every string drawn is looked up in the current language; widths count wide (CJK) characters as two cells.
        void T(int x,int y,string text,float bright=1,bool accent=false)=>ink.Put(x,y,Loc.T(text),bright,accent);
        void Center(int y,string text,float bright=1,bool accent=false){var s=Loc.T(text);ink.Put((ink.Cols-Loc.W(s))/2,y,s,bright,accent);}
        string ClockText()=>Mathf.FloorToInt(remaining/60).ToString("00")+":"+Mathf.FloorToInt(remaining%60).ToString("00");

        static System.Collections.Generic.List<string> Wrap(string text,int width)
        {
            var lines=new System.Collections.Generic.List<string>();
            foreach(var paragraph in text.Split('\n'))
            {
                // Tokens: Latin words, or single wide characters (Chinese and Japanese break between any two).
                var tokens=new System.Collections.Generic.List<(string s,bool space)>();
                string cur="";bool curSpace=false,pending=false;
                foreach(char c in paragraph)
                {
                    if(c==' '){if(cur.Length>0){tokens.Add((cur,curSpace));cur="";}pending=true;continue;}
                    if(Loc.Wide(c)){if(cur.Length>0){tokens.Add((cur,curSpace));cur="";}tokens.Add((c.ToString(),pending));pending=false;continue;}
                    if(cur.Length==0){curSpace=pending;pending=false;}
                    cur+=c;
                }
                if(cur.Length>0)tokens.Add((cur,curSpace));
                string line="";int lineW=0;
                foreach(var (tok,space) in tokens)
                {
                    int add=Loc.W(tok)+(line.Length>0&&space?1:0);
                    if(line.Length>0&&lineW+add>width){lines.Add(line);line=tok;lineW=Loc.W(tok);}
                    else{line+=(line.Length>0&&space?" ":"")+tok;lineW+=add;}
                }
                lines.Add(line);
            }
            return lines;
        }
        // Block letters: each pixel is two cells wide, with a dim offset shadow.
        void Big(int x,int y,string text,float bright)
        {
            for(int pass=0;pass<2;pass++)
                for(int i=0;i<text.Length;i++)
                {
                    char ch="ÁÀÂÃÉÊÍÓÔÕÚÜÇ".IndexOf(text[i])>=0?"AAAAEEIOOOUUC"["ÁÀÂÃÉÊÍÓÔÕÚÜÇ".IndexOf(text[i])]:text[i];
                    if(ch!=text[i]&&pass==1)T(x+i*LetterWidth+5,y-2,"▄▀",bright);
                    if(!Letters.TryGetValue(ch,out var rows))continue;
                    for(int r=0;r<7;r++)for(int c=0;c<5;c++)
                        if(rows[r][c]=='#')
                        {
                            if(pass==0)T(x+i*LetterWidth+c*2+1,y+r+1,"░░",.28f);
                            else T(x+i*LetterWidth+c*2,y+r,"██",bright);
                        }
                }
        }
        // A rule of box-drawing characters with labels set into it.
        void Rule(int y,int x0,int x1,float bright,params (int x,string text,float b,bool a)[] labels)
        {
            T(x0,y,new string('─',Mathf.Max(0,x1-x0)),bright);
            foreach(var l in labels)T(l.x,y," "+Loc.T(l.text)+" ",l.b,l.a);
        }
        bool KeyPressed(int n)=>n>0&&n<10&&(Input.GetKeyDown(KeyCode.Alpha0+n)||Input.GetKeyDown(KeyCode.Keypad0+n));
        bool Button(int x,int y,int w,int key,string label,string value=null)
        {
            var m=ink.Mouse;bool hover=(m.y==y||(Controls.Touch&&m.y==y+1))&&m.x>=x&&m.x<x+w;   // fingers get the empty row below too
            float b=hover?.95f:.86f;label=Loc.T(label);value=Loc.T(value);
            ink.Fill(x,y,w,1,hover?InkCamera.Inverted:InkCamera.Panel,.92f);
            if(key>0)T(x+2,y,key.ToString(),hover?b:.42f);
            T(x+6,y,label,b);
            if(value!=null)T(x+w-2-Loc.W(value),y,value,hover?b:.6f);
            else if(hover)T(x+w-3,y,"→",b);
            bool fire=!consumed&&((hover&&Input.GetMouseButtonDown(0))||KeyPressed(key));
            if(fire)consumed=true;
            return fire;
        }
        float Slider(int x,int y,int w,string label,float value,float min,float max)
        {
            T(x,y,label,.86f);
            int bar=w-10,bx=x,by=y+1;
            float t=Mathf.InverseLerp(min,max,value);int filled=Mathf.RoundToInt(t*bar);
            var m=ink.Mouse;bool hover=m.y==by&&m.x>=bx&&m.x<bx+bar;
            T(bx,by,new string('█',filled),hover?.95f:.75f);
            T(bx+filled,by,new string('░',bar-filled),.3f);
            T(bx+bar+2,by,value.ToString("0.00"),.6f);
            if(hover&&Input.GetMouseButton(0))value=Mathf.Lerp(min,max,(m.x-bx+.5f)/bar);
            return value;
        }

        public static bool HideInterface; // set by ArtDirector so shots show only the world
        void DrawUI()
        {
            if(!ink)return;
            ink.Clear();consumed=false;
            if(HideInterface){ink.reveal=1;return;}
            ink.reveal=1-fade;
            int C=ink.Cols,R=ink.Rows;
            if(!started)DrawMenu(C,R);
            else if(prologue){DrawPrologue(C,R);return;}
            else if(ended){endings.DrawOverlay(ink,C,R);DrawEnd(C,R);}
            else
            {
                if(!DialogueOpen&&!paused&&!(heaven&&heaven.InSky))DrawHud(C,R);
                if(!DialogueOpen&&!paused&&acts&&!(heaven&&heaven.InSky))acts.Draw(ink,C,R);
                if(DialogueOpen)DrawDialogue(C,R);
                if(paused)DrawPause(C,R);
            }
            if(toastTime>0&&!DialogueOpen&&!paused&&!ended&&started)
            {
                var lines=Wrap(toast,Mathf.Min(84,C-12));int w=0;foreach(var l in lines)w=Mathf.Max(w,Loc.W(l));
                int x=(C-w-6)/2,y=Controls.Touch?5:R-5-lines.Count;   // on phones the bottom belongs to the sticks
                ink.Frame(x,y,w+6,lines.Count+2,.3f);
                for(int i=0;i<lines.Count;i++)T(x+3,y+1+i,lines[i],.85f);
            }
            var tongue=heaven?heaven.Subtitle:null;
            if(tongue!=null&&started&&!ended&&!paused)
            {int x=(C-tongue.Length)/2,y=R-4;ink.Fill(x-3,y-1,tongue.Length+6,3,InkCamera.Panel);T(x,y,tongue,1,true);}
            Controls.Draw(ink,C,R);   // last: on a phone held upright it asks to be turned
        }
        // Black, red words bleeding downwards, the image tearing now and then. Leaves only through "Continue".
        // One centred column: lines of a verse together, a blank row between verses (where the blood runs).
        void DrawPrologue(int C,int R)
        {
            float t=Time.unscaledTime-prologueStart;
            // Funeral bells under the voice, without pause (CC0 recording, Sounds/campanas, looped).
            if(!bells){bells=gameObject.AddComponent<AudioSource>();bells.spatialBlend=0;bells.loop=true;bells.clip=Resources.Load<AudioClip>("Sounds/campanas");}
            if(!bells.isPlaying)bells.Play();
            bells.volume=volume*1.1f;
            ink.Fill(0,0,C,R+1,InkCamera.Panel);
            string head=Loc.T(PassageRef);
            int w=Mathf.Min(72,C-8),x0=Mathf.Max(2,(C-w)/2);
            var rows=new System.Collections.Generic.List<(string text,bool lastOfVerse)>();
            foreach(var verse in Loc.T(Passage).Split('\n'))
            {
                var wrapped=Wrap(verse,w);
                for(int k=0;k<wrapped.Count;k++)rows.Add((wrapped[k],k==wrapped.Count-1));
            }
            int verses=0;foreach(var r in rows)if(r.lastOfVerse)verses++;
            int height=3+rows.Count+verses-1+5,y0=Mathf.Max(1,(R-height)/2);
            float pulse=Mathf.PerlinNoise(t*1.2f,3)>.76f?.18f+.3f*Mathf.PerlinNoise(t*11,1):0;
            if(!reduceEffects)ink.glitch=pulse;
            ink.Put(x0,y0,head,.6f,true);
            ink.Put(x0,y0+1,new string('─',Mathf.Min(w,Loc.W(head))),.22f,true);
            int total=0;foreach(var r in rows)total+=r.text.Length;
            float chars=t*34;bool typing=chars<total;
            if(typing&&(Input.GetMouseButtonDown(0)||Controls.Down(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return))){prologueStart-=999;consumed=true;}
            int y=y0+3,before=0;
            for(int i=0;i<rows.Count;i++)
            {
                var (line,last)=rows[i];
                int n=Mathf.Clamp(Mathf.FloorToInt(chars)-before,0,line.Length);
                if(n>0)
                {
                    int shake=Hash(i,Mathf.FloorToInt(t*14))<pulse*.5f?(Hash(i+9,Mathf.FloorToInt(t*14))<.5f?-1:1):0;
                    ink.Put(x0+shake,y,line.Substring(0,n),.82f+.18f*Mathf.PerlinNoise(t*5,i),true);
                    // Blood runs from a few letters into the gap below a verse; the last verse bleeds longer.
                    if(last)
                        for(int k=0;k<n;k++)
                        {
                            if(line[k]==' '||Hash(i*131+k,3)>(i==rows.Count-1?.12f:.07f))continue;
                            float age=t-(before+k)/34f;
                            int max=i==rows.Count-1?1+Mathf.FloorToInt(Hash(k,i)*3):1;
                            int len=Mathf.Min(max,Mathf.FloorToInt(age*(.3f+Hash(k,i+5)*.4f)));
                            int cx=x0+Loc.W(line.Substring(0,k));
                            for(int d=1;d<=len;d++)ink.Put(cx,y+d,d==len&&d>1?"·":"│",.7f-.35f*d/(max+1f),true);
                        }
                }
                before+=line.Length;y+=last?2:1;
            }
            if(!typing&&t>1.2f)
            {
                string label=Loc.T("Continuar");int bw=Loc.W(label)+12;
                if(Button((C-bw)/2,Mathf.Min(y+2,R-2),bw,1,"Continuar")){prologue=false;voice.Stop();if(bells)bells.Stop();ink.glitch=0;atmosphere.Stop();if(atmosphereNext)atmosphereNext.Stop();atmosphere.Play();musicClock=0;Lock();WakeUp();}
            }
        }
        static float Hash(int a,int b){unchecked{uint h=(uint)(a*73856093^b*19349663);h^=h>>13;h*=0x5bd1e995;h^=h>>15;return (h&0xffff)/65535f;}}
                void DrawHud(int C,int R)
        {
            string clock="T−"+ClockText();
            ink.Fill(0,0,C,2,InkCamera.Panel);
            Rule(1,2,C-2,.22f,(4,"HINENI",.9f,false),(14,"HOSPITAL DEL UMBRAL",.45f,false),(C-6-clock.Length,clock,1,true));
            string objective=Loc.T(stage<2?"01 · Habla con Inés":horror&&!horror.Powered?horror.Objective:stage<3?"Acompaña al hombre de la sala común":"Mira por la ventana");
            ink.Fill(3,3,Loc.W(objective)+4,hasWater?2:1,InkCamera.Panel);
            T(4,3,"▸ ",.9f);T(6,3,objective,.8f);
            if(hasWater){T(4,4,"·",.5f);T(6,4,"En las manos: un vaso de agua",.5f);}
            if(horror)
            {
                ink.Fill(3,6,Mathf.Min(C-6,90),2,InkCamera.Panel);
                T(4,6,horror.Equipment,.7f);
                if(horror.IsWatched)T(4,7,"LO ESTÁS SUJETANDO CON LA LUZ. RETROCEDE.",.95f,true);
                else if(horror.ActiveThreat&&horror.ThreatDistance<8)T(4,7,"LOS PASOS ESTÁN MÁS CERCA.",.8f,true);
                if(horror.Hidden)
                {
                    ink.Fill(0,8,C,Mathf.Max(0,R/2-11),InkCamera.Panel);
                    ink.Fill(0,R/2+3,C,R/2,InkCamera.Panel);
                    Center(R-7,"DENTRO DEL ARMARIO · E PARA SALIR",.8f);
                }
            }
            int cx=C/2,cy=R/2;
            if(player.Focus&&!sat)
            {
                T(cx-2,cy-1,"┌",.8f);T(cx+2,cy-1,"┐",.8f);T(cx-2,cy+1,"└",.8f);T(cx+2,cy+1,"┘",.8f);
                T(cx,cy,"·",1);
                string label=Loc.T(player.Focus.label),prompt="E",action=Loc.T(player.Focus.prompt);
                int w=Mathf.Max(Loc.W(label),Loc.W(action)+4)+6,x=cx-w/2;
                ink.Fill(x,cy+3,w,3,InkCamera.Panel);
                T(x+3,cy+3,label,.95f);
                ink.Fill(x+3,cy+5,3,1,InkCamera.Inverted,.9f);T(x+4,cy+5,prompt,.9f);
                T(x+7,cy+5,action,.6f);
            }
            else T(cx,cy,"+",.75f);
            if(sat){ink.Fill(cx-16,R-4,33,1,InkCamera.Panel);Center(R-4,"ESTÁS AQUÍ   ·   E levantarte",.75f);}
            if(Controls.Touch)return;   // phones get sticks and buttons instead of the key legend
            ink.Fill(0,R-2,C,3,InkCamera.Panel);
            // The key legend is laid out from the translated words, not from fixed columns.
            var legend=new System.Collections.Generic.List<(int,string,float,bool)>();int lx=4;
            foreach(var (key,what) in new[]{("WASD","caminar"),("RATÓN","mirar"),("E","interactuar"),("ESC","pausa"),("F5","guardar")})
            {
                string k=Loc.T(key),d=Loc.T(what);
                legend.Add((lx,k,.75f,false));legend.Add((lx+Loc.W(k)+1,d,.38f,false));lx+=Loc.W(k)+Loc.W(d)+5;
            }
            Rule(R-1,2,C-2,.18f,legend.ToArray());
        }
        void DrawDialogue(int C,int R)
        {
            int w=Mathf.Min(94,C-6),tw=w-12;
            var body=Wrap(dialogBody,tw);
            int n=choices?.Length??0,h=body.Count+n*2+9;
            int x=(C-w)/2,y=speaker?Mathf.Max(1,R-h-1):Mathf.Max(1,(R-h)/2);
            ink.Frame(x,y,w,h,.4f);
            T(x+3,y,"┤ "+dialogSpeaker+" ├",.95f);T(x+3,y,"┤",.4f);T(x+5+Loc.W(dialogSpeaker),y,"├",.4f);
            string clock=" T−"+ClockText()+" · "+Loc.T("detenido")+" ";T(x+w-3-Loc.W(clock),y,clock,.8f,true);
            int shown=Mathf.FloorToInt((Time.unscaledTime-typeStart)*62),total=0;
            foreach(var l in body)total+=l.Length;
            bool typing=shown<total;
            if(typing&&(Input.GetMouseButtonDown(0)||Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return)))
            {typeStart=-999;consumed=true;typing=false;shown=total;}
            int left=shown;
            for(int i=0;i<body.Count;i++)
            {
                string line=body[i].Substring(0,Mathf.Clamp(left,0,body[i].Length));
                T(x+6,y+3+i,line,.92f);
                if(typing&&left>=0&&left<=body[i].Length&&Mathf.FloorToInt(Time.unscaledTime*3)%2==0)T(x+6+line.Length,y+3+i,"▌",.7f);
                left-=body[i].Length;
            }
            int cy=y+4+body.Count;
            if(typing)T(x+6,cy+1,"espacio · clic para continuar",.25f);
            else for(int i=0;i<n;i++)
                if(Button(x+3,cy+1+i*2,w-6,i+1,choices[i])){var callback=callbacks[i];callback();break;}
            T(x+6,y+h-2,"El reloj se detiene mientras lees y eliges.",.3f);
        }
        void DrawMenu(int C,int R)
        {
            int L=Mathf.Min(C,Mathf.Min(98,Mathf.Max(84,C/2)));   // the right side belongs to the angels
            ink.Fill(0,0,L,R+1,InkCamera.Panel);
            if(L<C)for(int y=0;y<=R;y++)T(L-1,y,"│",.16f);
            // Vertical rhythm adapts to the rows available (a phone in landscape has about 48).
            int top=Mathf.Clamp((R-48)/3,1,6);
            T(6,top+1,"LOS ÚLTIMOS QUINCE MINUTOS",.5f);
            T(6,top+2,new string('─',L-12),.2f);
            Big(6,top+5,"HINENI",.95f);
            T(6,top+14,"«Heme aquí.» Lo dijo Isaías cuando vio a los serafines. Isaías 6:8",.45f);
            T(6,top+17,"Vuelves a abrir los ojos.",.88f);
            T(6,top+18,"Al mundo le quedan quince minutos. Lo último que hagas decide cómo terminan.",.88f);
            int key=1,y0=top+21,bw=Mathf.Min(52,L-10);
            if(gallery){DrawGallery(L,y0);return;}
            if(Button(6,y0,bw,key++,"Despertar"))Begin(true);
            if(File.Exists(SavePath)&&Button(6,y0+2,bw,key++,"Continuar el último despertar"))Load();
            int seen=0;foreach(var e in Endings.All)if(Endings.Seen(e.id))seen++;
            if(Button(6,y0+4,bw,key++,"Finales",Loc.F("{0} de {1} vistos",seen,Endings.All.Length)))gallery=true;
            // Language: cycles through the five; the hospital is rebuilt in the new language.
            // Text and voices are chosen separately; changing the text rebuilds the hospital in that language.
            int li=Array.IndexOf(Loc.Codes,Loc.Lang),vi=Array.IndexOf(Loc.Codes,Loc.VoiceLang);
            if(Button(6,y0+6,bw,key++,"Texto",Loc.Names[li]+"  ▸"))
            {Loc.Set(Loc.Codes[(li+1)%Loc.Codes.Length],true);SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
            if(Button(6,y0+8,bw,key++,"Voces",Loc.Names[vi]+"  ▸"))Loc.SetVoice(Loc.Codes[(vi+1)%Loc.Codes.Length],true);
#if !UNITY_WEBGL
            if(Button(6,y0+10,bw,key,"Salir"))Application.Quit();
#endif
            if(R-3>y0+13)T(6,R-3,"v0.3",.4f);
            if(L+30<C)
            {
                ink.Frame(C-30,R-7,26,5,.3f);
                T(C-27,R-6,"T−15:00",1,true);
                T(C-27,R-4,"ANTES DEL JUICIO",.55f);
            }
        }
        void DrawPause(int C,int R)
        {
            ink.Fill(0,0,C,R+1,InkCamera.Panel);
            int x=Mathf.Max(4,(C-70)/2),w=Mathf.Min(70,C-8);
            T(x,5,settings?"A TU RITMO":"EL TIEMPO ESTÁ DETENIDO.",.95f);
            T(x,6,new string('─',w),.2f);
            T(x,7,"T−"+ClockText(),1,true);T(x+9,7,"· tu ausencia no consume esta hora.",.45f);
            int y=11;
            if(settings)
            {
                player.sensitivity=Slider(x,y,w,"Sensibilidad del ratón",player.sensitivity,.3f,4f);
                volume=Slider(x,y+3,w,"Volumen",volume,0,1);
                y+=7;
                if(Button(x,y,w,1,"Texto grande",ink.largeText?"sí":"no"))ink.largeText=!ink.largeText;
                if(Button(x,y+2,w,2,"Mundo en ASCII",ink.enabledEffect?"sí":"no"))ink.enabledEffect=!ink.enabledEffect;
                if(Button(x,y+4,w,3,"Invertir mirada vertical",player.invertY?"sí":"no"))player.invertY=!player.invertY;
                if(Button(x,y+6,w,4,"Movimiento y parpadeos",reduceEffects?"reducidos":"suaves")){reduceEffects=!reduceEffects;world.reducedDisturbance=reduceEffects;world.anomaly=!reduceEffects&&stage>=3;}
                if(Button(x,y+9,w,5,"Volver"))settings=false;
            }
            else
            {
                if(Button(x,y,w,1,"Continuar")){paused=false;Lock();}
                if(Button(x,y+2,w,2,"Opciones"))settings=true;
                if(Button(x,y+4,w,3,"Guardar partida"))Save();
                if(Button(x,y+6,w,4,"Volver al inicio")){Save(false);SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
                if(Button(x,y+8,w,5,"Guardar y salir")){Save(false);Application.Quit();}
                T(x,y+12,saveStatus??"No hay una respuesta perfecta que desbloquear.",.35f);
            }
        }
        // The menu's gallery: every ending can be watched again; the ones you reached are marked.
        void DrawGallery(int L,int y0)
        {
            T(6,y0-1,"FINALES",.55f);
            for(int i=0;i<Endings.All.Length;i++)
            {
                var e=Endings.All[i];
                var kicker=Loc.T(e.kicker);kicker=kicker.Substring(kicker.LastIndexOf('·')+1).Trim();
                if(Button(6,y0+1+i*2,L-14,i+1,Loc.T(e.title)+"  ·  "+(Loc.CJK?kicker:kicker.ToLowerInvariant()),Endings.Seen(e.id)?"● "+Loc.T("visto"):"○"))
                {started=true;ended=true;gallery=false;fade=1;Unlock();endings.Play(e.id);return;}
            }
            if(Button(6,y0+3+Endings.All.Length*2,L-14,9,"Volver"))gallery=false;
        }
        void DrawEnd(int C,int R)
        {
            var e=endings.Current;if(e==null)return;
            float t=endings.T;
            int w=Mathf.Min(84,C-8),x=6,y=R-20;
            T(x,3,e.kicker,.7f,true);
            if(Loc.CJK)T(x,7,e.title,Mathf.Clamp01(t/2),true);   // the block letters are Latin only
            else Big(x,5,Loc.T(e.title),Mathf.Clamp01(t/2));
            // The epilogue arrives a line at a time; it bends to what you did on the way.
            var text=new System.Collections.Generic.List<string>(Array.ConvertAll(e.lines,Loc.T));
            if(horror&&horror.Data.lever)text.Add(Loc.T(e.afterLever));
            if(gaveWater)text.Add(Loc.T("Alguien recibió un vaso de agua."));
            if(called&&e.id!="voz")text.Add(Loc.T("Lucía supo que estabas al otro lado."));
            ink.Fill(x-2,y-1,w+4,text.Count*2+2,InkCamera.Panel);
            for(int i=0;i<text.Count;i++)
            {
                float appear=2.5f+i*3.2f;if(t<appear)break;
                string line=text[i];int shown=Mathf.Clamp(Mathf.FloorToInt((t-appear)*40),0,line.Length);
                T(x,y+i*2,line.Substring(0,shown),i<3?.92f:.6f,i>=3);
            }
            if(t<2.5f+text.Count*3.2f)return;
            bool fromMenu=!horror||horror.Data.phase==0&&stage==0;
            if(Button(x,R-5,40,1,"Volver a los finales")){openGallery=true;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
            if(Button(x+44,R-5,30,2,"Volver al inicio"))SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            if(Button(x+78,R-5,16,3,"Salir"))Application.Quit();
        }
    }
}
