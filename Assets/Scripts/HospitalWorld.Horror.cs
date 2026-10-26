using System.Collections.Generic;
using UnityEngine;

namespace LosQueQuedan
{
    public sealed partial class HospitalWorld
    {
        public GameObject chapelDoor, serviceDoor, fuseObject, witness;
        public Transform[] breakers=new Transform[3];
        public Material[] breakerLights=new Material[3];
        public bool powerFailed,reducedDisturbance;
        readonly List<Light> wingLights=new List<Light>();
        readonly List<float> initialIntensities=new List<float>();
        Material paint,bloodFont;
        Font hand;
        public const float LockerX=-8.9f;

        void SplitCorridorWall(float x,float opening,float start,float end)
        {
            const float half=1.12f;
            float a=opening-half,b=opening+half;
            Cube("Muro lateral sur",new Vector3(x,1.8f,(start+a)/2),new Vector3(.2f,3.6f,a-start),plaster);
            Cube("Muro lateral norte",new Vector3(x,1.8f,(b+end)/2),new Vector3(.2f,3.6f,end-b),plaster);
            Cube("Dintel lateral",new Vector3(x,3.1f,opening),new Vector3(.2f,1f,half*2),plaster);
        }
        void BuildHorrorWing()
        {
            paint=Mat("Sangre escrita",new Color(.46f,.012f,.008f),.35f);
            paint.EnableKeyword("_EMISSION");paint.SetColor("_EmissionColor",new Color(.18f,.001f,0));
            BuildSideRoom(6,10,8,7,true);
            BuildSideRoom(-6,14,8,6,false);
            chapelDoor=WingDoor("Capilla",new Vector3(2,0,10),"chapel-door");
            serviceDoor=WingDoor("Sala eléctrica",new Vector3(-2,0,14),"service-door");

            // Chapel: a central aisle, empty pews, an altar no longer used as an altar.
            for(int row=0;row<3;row++)
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 p=new Vector3(4+row*1.15f,0,10+side*2.1f);
                    Cube("Banco de capilla",p+Vector3.up*.5f,new Vector3(.55f,.15f,1.55f),door);
                    Cube("Respaldo",p+new Vector3(-.28f,.9f,0),new Vector3(.08f,.85f,1.55f),door);
                    for(int foot=-1;foot<=1;foot+=2)Cube("Pata banco",p+new Vector3(0,.23f,foot*.6f),new Vector3(.35f,.46f,.08f),black);
                }
            Cube("Altar",new Vector3(8.2f,.65f,10),new Vector3(1.2f,1.3f,2.3f),plasterLow);
            Cube("Paño de altar",new Vector3(8.2f,1.32f,10),new Vector3(1.35f,.04f,2.45f),cloth);
            Cube("Cruz vertical vacía",new Vector3(9.75f,2.2f,10),new Vector3(.15f,2.2f,.14f),black);
            Cube("Cruz travesaño",new Vector3(9.75f,2.6f,10),new Vector3(.15f,.13f,1.2f),black);
            var fuse=Cube("Fusible del circuito",new Vector3(7.95f,1.45f,10),new Vector3(.35f,.22f,.22f),pale);
            Target(fuse,"fuse","Fusible 03 · aún está caliente","Recoger");fuseObject=fuse;
            // Seven candles, cold. Lit all at once they are one of the last things a person can do here.
            var flameMat=Mat("Llama",new Color(1,.72f,.32f),0,true);
            for(int i=0;i<7;i++)
            {
                float z=8.9f+i*.36f;
                var candle=Cylinder("Vela",new Vector3(8.55f,1.51f,z),new Vector3(.07f,.18f,.07f),pale);
                Target(candle,"candle-"+i,"Vela "+(i+1)+" de 7","Encender");
                flames[i]=Cube("Llama",new Vector3(8.55f,1.74f,z),new Vector3(.03f,.08f,.03f),flameMat);Destroy(flames[i].GetComponent<Collider>());
                var l=new GameObject("Luz de vela").AddComponent<Light>();l.transform.SetParent(transform);l.transform.position=new Vector3(8.4f,1.85f,z);
                l.type=LightType.Point;l.range=2.2f;l.intensity=0;l.color=new Color(1,.68f,.32f);candleLights[i]=l;
                flames[i].SetActive(false);
            }
            BloodWriting("EL VERBO\nSE HIZO CARNE.\nLA CARNE\nSIGUE GRITANDO.",new Vector3(6.5f,2.2f,6.62f),Vector3.forward,5.4f,"graffiti-word","EL VERBO\nSE HIZO CARNE.\nTÚ LA HICISTE\nPEDAZOS.");
            BloodWriting("PADRE, NO CONTESTES\nCON MI VOZ.",new Vector3(6.5f,2.0f,13.38f),Vector3.back,4.7f,"graffiti-father","CUARENTA Y UNO.\nCUARENTA Y UNO.\nCUARENT");
            BloodWriting("MÍRALO.\nLA LUZ LO SUJETA.",new Vector3(9.84f,1.7f,11.7f),Vector3.left,2.6f,"graffiti-gaze");
            Aim("Luz de altar",new Vector3(6.4f,3.1f,8.5f),new Vector3(8.2f,1.3f,10),45,3.7f,8,new Color(.9f,.65f,.4f));
            Aim("Pared de la palabra",new Vector3(5,2.9f,9),new Vector3(6.5f,2,6.6f),70,1.5f,7,new Color(.8f,.15f,.1f));
            Stain(new Vector3(7.2f,.015f,10),Vector3.up,1.4f,23);

            // Service room: three physical breakers, a fuse slot and a hiding cabinet.
            Prop("steel_frame_shelves_01",new Vector3(-3.05f,0,16.45f),180);
            Cube("Cuadro eléctrico",new Vector3(-9.72f,1.55f,14),new Vector3(.22f,1.8f,2.3f),steel);
            var slot=Cube("Alojamiento del fusible",new Vector3(-9.48f,1.1f,14),new Vector3(.2f,.32f,.5f),black);
            Target(slot,"fuse-slot","Circuito de la sala común","Colocar fusible");
            for(int i=0;i<3;i++)
            {
                float z=13.3f+i*.7f;
                var handle=Cube("Interruptor "+(i+1),new Vector3(-9.41f,1.7f,z),new Vector3(.18f,.42f,.22f),pale);
                Target(handle,"breaker-"+(i+1),"Interruptor "+(i+1),"Accionar");breakers[i]=handle.transform;
                breakerLights[i]=Mat("Indicador "+i,new Color(.12f,.015f,.008f),0,true);
                Cube("Indicador",new Vector3(-9.47f,2.12f,z),new Vector3(.07f,.09f,.13f),breakerLights[i]);
                Bleed(SmallLabel((i+1).ToString(),new Vector3(-9.605f,2.3f,z),Vector3.right,.045f),Vector3.right,.1f,1);
            }
            BloodWriting("TRES NEGACIONES.\nUN SOLO NOMBRE.\nDOS TESTIGOS.",new Vector3(-6.3f,2.1f,11.12f),Vector3.forward,4.8f,"graffiti-order");
            BloodWriting("EL SÉPTIMO DÍA\nNOS DEJÓ SOLOS.",new Vector3(-5.95f,2.1f,16.88f),Vector3.back,3.6f,"graffiti-seventh","EL SÉPTIMO DÍA\nTE DEJÓ LA PALANCA.");
            var locker=Cube("Armario de mantenimiento",new Vector3(LockerX,1.2f,16.4f),new Vector3(1.1f,2.4f,.8f),plasterLow);
            Target(locker,"hide","Armario de mantenimiento","Ocultarte / E para salir");
            for(int i=0;i<7;i++)Cube("Respiradero",new Vector3(LockerX,1.6f+i*.065f,15.98f),new Vector3(.55f,.016f,.015f),black);
            Aim("Luz de cuadro",new Vector3(-6.5f,3.2f,12),new Vector3(-9.4f,1.5f,14),55,2.5f,7,new Color(.3f,.7f,.6f));
            BloodWriting("NO FUE RESURRECCIÓN.\nALGO APRENDIÓ A MOVERLOS.",new Vector3(1.87f,2.1f,12.7f),Vector3.left,2.3f,"graffiti-risen","NO FUE RESURRECCIÓN.\nFUISTE TÚ.");
            BuildLever();

            witness=Character("Esquina",new Vector3(7.2f,0,12.1f),-90,u=>
            {
                u.displayName="EL TESTIGO";u.blinkEvery=0;u.follow=1;u.followLimit=85;
                u.tilt=24;u.fear=.9f;u.scream=.85f;u.tremor=.25f;u.lines=new string[0];
            });
            witness.name="El Testigo";witness.transform.localScale=Vector3.Scale(witness.transform.localScale,new Vector3(.78f,1.48f,.78f));
            foreach(var col in witness.GetComponentsInChildren<Collider>())col.enabled=false;
            witness.SetActive(false);
            foreach(var l in GetComponentsInChildren<Light>())
            {wingLights.Add(l);initialIntensities.Add(l.intensity);}
        }
        void BuildSideRoom(float x,float z,float width,float length,bool east)
        {
            Cube("Ala suelo",new Vector3(x,-.12f,z),new Vector3(width,.24f,length),plasterLow);
            Cube("Ala techo",new Vector3(x,3.7f,z),new Vector3(width,.2f,length),black);
            Cube("Ala muro sur",new Vector3(x,1.8f,z-length/2),new Vector3(width,3.6f,.2f),plaster);
            Cube("Ala muro norte",new Vector3(x,1.8f,z+length/2),new Vector3(width,3.6f,.2f),plaster);
            Cube("Ala muro exterior",new Vector3(east?x+width/2:x-width/2,1.8f,z),new Vector3(.2f,3.6f,length),plaster);
            // The shared corridor wall is already split; no invisible wall across the doorway.
        }
        GameObject WingDoor(string label,Vector3 p,string id)
        {
            var leaf=Cube(label,p+Vector3.up*1.25f,new Vector3(.12f,2.5f,2.04f),door);
            DressDoor(leaf,Vector3.right,2);
            Target(leaf,id,label,"Abrir / cerrar");
            var handle=Cube("Pulsador "+label,p+new Vector3(p.x>0?-.18f:.18f,1.15f,1.35f),new Vector3(.12f,.22f,.16f),pale);
            Target(handle,id,label,"Abrir / cerrar");
            var inside=Cube("Pulsador interior "+label,p+new Vector3(p.x>0?.18f:-.18f,1.15f,1.35f),new Vector3(.12f,.22f,.16f),pale);
            Target(inside,id,label,"Abrir / cerrar");
            var plaque=SmallLabel(label.ToUpperInvariant(),p+new Vector3(p.x>0?-.11f:.11f,3.05f,0),p.x>0?Vector3.left:Vector3.right,.08f);Bleed(plaque,p.x>0?Vector3.left:Vector3.right,1.6f,4);
            return leaf;
        }
        Material glass;
        // Hardware on both faces of a door slab; the pieces are children, so they slide with it.
        public void DressDoor(GameObject leaf,Vector3 normal,int leaves)
        {
            if(!glass){glass=Mat("Vidrio armado",new Color(.06f,.08f,.08f),.9f);}
            var b=leaf.GetComponent<Renderer>().bounds;var c=b.center;
            var right=Vector3.Cross(Vector3.up,normal).normalized;
            float width=Mathf.Abs(Vector3.Dot(b.size,right)),thick=Mathf.Abs(Vector3.Dot(b.size,normal)),bottom=b.min.y;
            Vector3 Size(float along,float h,float t)=>normal.x!=0?new Vector3(t,h,along):new Vector3(along,h,t);
            void Piece(string n,Vector3 at,Vector3 size,Material m){var o=Cube(n,at,size,m);Destroy(o.GetComponent<Collider>());o.transform.SetParent(leaf.transform,true);}
            float lw=width/leaves;
            for(int side=-1;side<=1;side+=2)
            {
                var face=c+normal*side*(thick/2+.004f);
                for(int i=0;i<leaves;i++)
                {
                    var mid=face+right*(-width/2+lw*(i+.5f));
                    var win=new Vector3(mid.x,bottom+1.62f,mid.z);
                    Piece("Mirilla",win,Size(lw*.36f,.62f,.012f),glass);
                    for(int k=-1;k<=1;k+=2)
                    {
                        Piece("Marco mirilla",win+Vector3.up*k*.32f,Size(lw*.4f,.035f,.02f),steel);
                        Piece("Marco mirilla",win+right*k*lw*.19f,Size(.035f,.66f,.02f),steel);
                    }
                    Piece("Alambre",win,Size(lw*.36f,.008f,.016f),black);
                    Piece("Alambre",win,Size(.008f,.62f,.016f),black);
                    Piece("Barra",new Vector3(mid.x,bottom+1.02f,mid.z)+normal*side*.05f,Size(lw*.62f,.045f,.045f),steel);
                    for(int k=-1;k<=1;k+=2)Piece("Soporte",new Vector3(mid.x,bottom+1.02f,mid.z)+normal*side*.025f+right*k*lw*.3f,Size(.04f,.07f,.05f),steel);
                    Piece("Zocalo de puerta",new Vector3(mid.x,bottom+.14f,mid.z),Size(lw*.9f,.26f,.012f),steel);
                }
                if(leaves>1)Piece("Junta",new Vector3(c.x,c.y,c.z)+normal*side*(thick/2+.006f),Size(.02f,b.size.y,.012f),black);
            }
        }
        GameObject SmallLabel(string text,Vector3 pos,Vector3 normal,float scale)
        {
            var o=new GameObject(text);o.transform.SetParent(transform);o.transform.position=pos;o.transform.rotation=Quaternion.LookRotation(-normal);
            var mesh=o.AddComponent<TextMesh>();mesh.text=text;mesh.font=font;mesh.fontSize=64;mesh.characterSize=scale;
            mesh.anchor=TextAnchor.MiddleCenter;mesh.color=new Color(.65f,.63f,.54f);
            o.GetComponent<MeshRenderer>().sharedMaterial=worldFont;
            return o;
        }
        // Every word on a wall is written in blood: a rough hand, wet red, with runs and drops under the last line.
        void Bleed(GameObject o,Vector3 normal,float width,int drips){StyleBlood(o);Drips(o,normal,width,drips);}
        void StyleBlood(GameObject o)
        {
            if(!hand)
            {
#if UNITY_WEBGL
                hand=Resources.Load<Font>("Fonts/RubikWetPaint-Regular");   // browsers expose no system fonts: a bundled OFL wet-paint face
                if(!hand)hand=font;
#else
                hand=Font.CreateDynamicFontFromOSFont(new[]{"Chalkduster","Marker Felt","Arial"},64);
#endif
                bloodFont=new Material(worldTextShader){mainTexture=hand.material.mainTexture};owned.Add(bloodFont);
            }
            var t=o.GetComponent<TextMesh>();t.font=hand;t.fontStyle=FontStyle.Normal;t.color=new Color(1f,.2f,.13f);
            hand.RequestCharactersInTexture(t.text,64);
            o.GetComponent<MeshRenderer>().sharedMaterial=bloodFont;
        }
        void Drips(GameObject o,Vector3 normal,float width,int count)
        {
            if(!paint){paint=Mat("Sangre escrita",new Color(.46f,.012f,.008f),.35f);paint.EnableKeyword("_EMISSION");paint.SetColor("_EmissionColor",new Color(.18f,.001f,0));}
            var b=o.GetComponent<Renderer>().bounds;var right=Vector3.Cross(Vector3.up,normal).normalized;
            float span=Mathf.Min(width,Mathf.Max(b.size.x,b.size.z)),thick=Mathf.Lerp(.004f,.012f,span/2);
            var random=new System.Random(o.GetComponent<TextMesh>().text.Length*7919+(int)(b.center.x*100+b.center.z*10));
            for(int i=0;i<count;i++)
            {
                float x=((float)random.NextDouble()-.5f)*span*.9f;
                float length=(.04f+(float)random.NextDouble()*.5f)*Mathf.Clamp(span,.15f,1);
                float top=b.min.y+b.size.y*.12f;
                var at=new Vector3(b.center.x,0,b.center.z)+right*x+normal*.012f;
                var drip=Cube("Escurrimiento",at+Vector3.up*(top-length/2),new Vector3(thick,length,thick),paint);
                drip.transform.rotation=Quaternion.LookRotation(-normal);
                Destroy(drip.GetComponent<Collider>());
                Stain(at+Vector3.up*(top-length),normal,thick*4+(float)random.NextDouble()*thick*5,i*23);
            }
        }
        // Wall messages that change after the player's choices: id -> (text, width, normal, alternate text).
        readonly Dictionary<string,(TextMesh text,float width,string before,string after)> writings=new Dictionary<string,(TextMesh,float,string,string)>();
        GameObject BloodWriting(string words,Vector3 p,Vector3 normal,float maxWidth,string id,string after=null)
        {
            var o=SmallLabel(words,p,normal,.12f);o.name="Pintada · "+id;
            var text=o.GetComponent<TextMesh>();text.lineSpacing=.94f;
            StyleBlood(o);Fit(text,maxWidth);
            var hit=o.AddComponent<BoxCollider>();hit.center=Vector3.zero;
            float inv=1/o.transform.localScale.x;hit.size=new Vector3(maxWidth*inv,1.25f*inv,.06f*inv);
            Target(o,id,words.Replace('\n',' '),"Leer la pintada");
            Drips(o,normal,maxWidth,17);
            if(after!=null)writings[id]=(text,maxWidth,words,after);
            return o;
        }
        static void Fit(TextMesh text,float maxWidth)
        {
            text.transform.localScale=Vector3.one;
            var bounds=text.GetComponent<Renderer>().bounds;
            float span=Mathf.Max(bounds.size.x,bounds.size.z);
            if(span>.01f)text.transform.localScale=Vector3.one*Mathf.Min(1.6f,maxWidth/span);
        }
        public string WritingText(string id)=>writings.TryGetValue(id,out var w)?w.text.text.Replace('\n',' '):null;
        public void Rewrite(bool after)
        {
            foreach(var w in writings.Values)
            {
                w.text.text=after?w.after:w.before;hand.RequestCharactersInTexture(w.text.text,64);
                Fit(w.text,w.width);
                var target=w.text.GetComponent<Interactable>();if(target)target.label=w.text.text.Replace('\n',' ');
            }
            if(lastWords)lastWords.SetActive(after);
        }
        // Lever 41 is wired to nothing but the doctor. Pulling it bursts him against his wall.
        public void BurstDoctor(bool silent)
        {
            if(!doctorNpc||!doctorNpc.activeSelf)return;
            var centre=doctorNpc.transform.position+Vector3.up*1.1f;doctorNpc.SetActive(false);
            var flesh=Mat("Carne",new Color(.42f,.03f,.03f),.55f);var coat=Mat("Bata rota",new Color(.78f,.77f,.72f));var bone=Mat("Hueso",new Color(.86f,.83f,.74f),.2f);
            var random=new System.Random(41);
            for(int i=0;i<46;i++)
            {
                float size=.04f+(float)random.NextDouble()*(i<6?.24f:.12f);
                var piece=GameObject.CreatePrimitive(i%3==0?PrimitiveType.Sphere:PrimitiveType.Cube);piece.name="Resto";piece.transform.SetParent(transform);
                piece.transform.position=centre+Random.insideUnitSphere*.3f;piece.transform.rotation=Random.rotation;
                piece.transform.localScale=new Vector3(size,size*(.4f+(float)random.NextDouble()),size*(.6f+(float)random.NextDouble()*.8f));
                piece.GetComponent<Renderer>().sharedMaterial=i%5==0?coat:i%7==0?bone:flesh;
                var body=piece.AddComponent<Rigidbody>();body.mass=size*8;
                var push=(piece.transform.position-centre).normalized;push.x=Mathf.Abs(push.x)*1.4f;   // away from the wall he leaned on
                body.linearVelocity=silent?Vector3.zero:push*(3+(float)random.NextDouble()*5)+Vector3.up*(float)random.NextDouble()*3;
                body.angularVelocity=Random.insideUnitSphere*12;
            }
            Stain(new Vector3(-1.89f,1.55f,11.7f),Vector3.right,1.9f,15);
            Stain(new Vector3(-1.89f,2.4f,12.3f),Vector3.right,.9f,95);
            Stain(new Vector3(-1.89f,.9f,11.1f),Vector3.right,.7f,200);
            Stain(new Vector3(-.4f,3.59f,11.8f),Vector3.down,1.5f,60);
            Stain(new Vector3(1.89f,1.4f,12.1f),Vector3.left,.8f,130);
            for(int i=0;i<7;i++)Stain(new Vector3(-1.4f+i*.42f,.014f,11.2f+(float)random.NextDouble()*1.4f),Vector3.up,.4f+(float)random.NextDouble()*.9f,i*51);
            var corner=GameObject.Find("Esquina");
            if(corner&&corner.TryGetComponent(out Uncanny u))u.lines=new[]{"Lo oíste reventar. Contaba hasta cuarenta y uno.","Ahora nos mira con sus pedazos.","La palanca era para ver si bajabas la mano."};
            Rewrite(true);
        }
        GameObject lastWords;
        void BuildLever()
        {
            Cube("Placa de palanca",new Vector3(-9.86f,1.45f,12.2f),new Vector3(.06f,.62f,.34f),black);
            var red=Mat("Palanca roja",new Color(.55f,.03f,.02f),.5f,true);
            var lever=Cube("Palanca 41",new Vector3(-9.74f,1.6f,12.2f),new Vector3(.22f,.08f,.08f),red);
            Target(lever,"lever-41","Una palanca roja, aparte del cuadro","Examinar");leverHandle=lever.transform;
            Bleed(SmallLabel("41",new Vector3(-9.82f,1.95f,12.2f),Vector3.right,.06f),Vector3.right,.2f,3);
            lastWords=BloodWriting("AQUÍ APOYABA\nLA FRENTE.",new Vector3(-1.88f,2.55f,11.7f),Vector3.right,1.5f,"graffiti-forehead");
            lastWords.SetActive(false);
        }
        public Transform leverHandle;
        public readonly GameObject[] flames=new GameObject[7];
        public readonly Light[] candleLights=new Light[7];
        public void SetLever(bool down){if(leverHandle)leverHandle.localRotation=Quaternion.Euler(0,0,down?-60:0);}
        public void DoorState(GameObject leaf,bool open)
        {
            // Slide the slab into the wall; the opening remains 2.24 metres wide.
            var p=leaf.transform.position;
            float centre=leaf==chapelDoor?10:14;
            leaf.transform.position=new Vector3(p.x,p.y,centre+(open?2.2f:0));
            leaf.GetComponent<Collider>().enabled=!open;
            leaf.GetComponent<Interactable>().prompt=open?"Cerrar":"Abrir";
        }
        public void SetPower(bool failed)
        {
            powerFailed=failed;
            for(int i=0;i<wingLights.Count;i++)if(wingLights[i])wingLights[i].intensity=initialIntensities[i]*(failed?.08f:1);
        }
        public void SetBreakers(int progress)
        {
            int[] order={3,1,2};
            for(int i=0;i<3;i++)
            {
                bool on=false;for(int n=0;n<progress;n++)if(order[n]==i+1)on=true;
                breakers[i].localRotation=Quaternion.Euler(on?55:0,0,0);
                breakerLights[i].SetColor("_EmissionColor",on?new Color(.4f,.8f,.55f):new Color(.14f,.006f,.003f));
            }
        }
    }
}
