using System.Collections.Generic;
using UnityEngine;

namespace LosQueQuedan
{
    // Builds the hospital at runtime: architecture is procedural, props and people are CC0 models
    // loaded from Resources (see Tools/fetch_props.py and Tools/build_characters.py).
    // Lighting is designed for the ASCII pass: low ambient, one motivated key per space, contrast kept for people.
    public sealed partial class HospitalWorld : MonoBehaviour
    {
        public Material surfaceTemplate, cutoutTemplate;
        public Shader worldTextShader;
        Material worldFont;
        Material plaster, plasterLow, black, pale, steel, cloth, glow, tube, curtain, door;
        Texture2D tiles;
        Font font;
        public Transform seam;
        public Transform window;
        public GameObject cup;
        public GameObject nurse;
        public GameObject patient;
        public GameObject doctorNpc;
        public GameObject corridorGate;
        public bool anomaly;
        Light flicker, emergency;
        Material blood;
        public Material BloodMaterial => blood;
        readonly List<Material> owned = new List<Material>();

        public void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            font.RequestCharactersInTexture("HOSPITAL DEL UMBRAL RECUPERACIÓN / 03 SALA COMÚN NO DEJAR A NADIE SOLO", 64);
            worldFont = new Material(worldTextShader) { mainTexture = font.material.mainTexture }; owned.Add(worldFont);
            Font.textureRebuilt += RefreshFont;
            plaster = Mat("Cal", new Color(.3f, .3f, .29f));
            plasterLow = Mat("Zocalo pintado", new Color(.16f, .17f, .17f));
            pale = Mat("Porcelana", new Color(.8f, .79f, .74f));
            black = Mat("Tinta", new Color(.03f, .03f, .035f));
            steel = Mat("Acero", new Color(.32f, .34f, .34f), .45f);
            // Floor tiles are a texture, not geometry: tile edges would otherwise become contour strokes.
            tiles = new Texture2D(2, 2, TextureFormat.RGB24, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            Color ta = new Color(.25f, .25f, .24f), tb = new Color(.19f, .19f, .19f);
            tiles.SetPixels(new[] { ta, tb, tb, ta }); tiles.Apply();
            cloth = Mat("Sabanas", new Color(.74f, .73f, .69f));
            curtain = Mat("Cortina", new Color(.52f, .55f, .54f));
            door = Mat("Puerta", new Color(.24f, .25f, .25f), .3f);
            glow = Mat("Luz de papel", new Color(.92f, .9f, .82f), 0, true);
            tube = Mat("Tubo", new Color(.45f, .6f, .45f), 0, true);
            blood = cutoutTemplate ? new Material(cutoutTemplate) { name = "Sangre", color = new Color(.32f, .015f, .02f) } : Mat("Sangre", new Color(.32f, .015f, .02f), .6f);
            if (cutoutTemplate) { blood.SetFloat("_Glossiness", .6f); owned.Add(blood); }
            blood.mainTexture = Blot();
            blood.SetFloat("_Mode", 1); blood.SetFloat("_Cutoff", .5f); blood.EnableKeyword("_ALPHATEST_ON");
            blood.SetOverrideTag("RenderType", "TransparentCutout"); blood.renderQueue = 2450;

            RenderSettings.ambientLight = new Color(.035f, .04f, .042f);
            RenderSettings.fog = true; RenderSettings.fogColor = Color.black;
            RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .065f;
            // Same look on every platform: WebGL defaults to a lower quality level with only two per-pixel lights,
            // which turns the flashlight and the spot keys into flat vertex light.
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
            QualitySettings.pixelLightCount = Application.isMobilePlatform ? 5 : 8;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 30;

            Room("Recuperacion", 0, 0, 10, 10, true, false);
            Room("Corredor", 0, 11, 4, 12, true, true);
            Room("Sala comun", 0, 22, 10, 10, false, true);
            DoorFrame(5); DoorFrame(17);
            RecoveryRoom();
            Corridor();
            CommonRoom();
            BuildHorrorWing();

            Sign("HOSPITAL DEL UMBRAL", new Vector3(0, 3.1f, 4.88f), 180, .105f);
            Sign("RECUPERACIÓN  /  03", new Vector3(-4.88f, 2.35f, -2.6f), 90, .1f);
            Sign("SALA COMÚN", new Vector3(0, 3.1f, 16.88f), 180, .12f);
            var alone = Sign("NO DEJAR A NADIE SOLO", new Vector3(-4.88f, 2.4f, 21.8f), 90, .075f);
            writings["sign-alone"] = (alone, Mathf.Max(alone.GetComponent<Renderer>().bounds.size.z, .5f), alone.text, "NO DEJAR A NADIE\nENTERO");

            // Gate prevents skipping the short demo's opening conversation.
            corridorGate = Cube("Paso pendiente", new Vector3(0, 1.325f, 5), new Vector3(2.5f, 2.65f, .12f), door);   // fills the frame to the lintel: no gap above
            DressDoor(corridorGate, Vector3.forward, 2);
            Target(corridorGate, "gate", "Puerta de recuperación", "Abrir");
            seam = new GameObject("La costura").transform; seam.SetParent(transform);
            for (int i = 0; i < 32; i++)
            {
                float z = 7 + i * .26f, x = Mathf.Sin(i * .65f) * .12f;
                var line = Cube("Sutura", new Vector3(x, .025f, z), new Vector3(.035f, .012f, .3f), black);
                line.transform.SetParent(seam);
                var stitch = Cube("Punto", new Vector3(x, .028f, z), new Vector3(.36f, .015f, .025f), black);
                stitch.transform.rotation = Quaternion.Euler(0, 28, 0); stitch.transform.SetParent(seam);
            }
            seam.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- spaces
        void RecoveryRoom()
        {
            HospitalBed(new Vector3(-2.8f, 0, -1.2f), 0, true);
            Table(new Vector3(-2.8f, 0, -3.6f));
            cup = Cylinder("Vaso", new Vector3(-2.8f, 1.08f, -3.6f), new Vector3(.18f, .15f, .18f), pale);
            Target(cup, "water", "Un vaso de agua", "Tomar");
            var waterSurface = Cube("Agua", new Vector3(-2.8f, 1.226f, -3.6f), new Vector3(.13f, .008f, .13f), steel);
            waterSurface.transform.SetParent(cup.transform, true);
            Destroy(waterSurface.GetComponent<Collider>());
            Curtain(new Vector3(-4.6f, 0, .55f), 3.4f, 0);
            Prop("drawer_cabinet", new Vector3(-4.54f, 0, 3.1f), 90);
            Prop("medical_box", new Vector3(-4.54f, 1.88f, 3.1f), 80, collide: false, rest: true);
            Prop("steel_frame_shelves_01", new Vector3(4.54f, 0, -2.6f), -90);
            Prop("side_table_01", new Vector3(-1.75f, 0, .4f), 0);
            Prop("wall_clock", new Vector3(0, 2.6f, -4.9f), 0, collide: false);
            var phone = Prop("korean_public_payphone_01", new Vector3(4.765f, 1.07f, 2.9f), -90);   // back on the plaster, above the rail
            Target(phone, "phone", "Una línea al exterior", "Llamar");
            var record = Prop("clipboard", new Vector3(-2.6f, .645f, -1.7f), 18, collide: true, rest: true);   // on the made-up sheet
            Target(record, "record", "Tu nombre en una carpeta", "Leer");
            nurse = Character("Ines", new Vector3(.9f, 0, -.7f), -153, u =>
            {
                u.displayName = "INÉS"; u.blinkEvery = 3.5f; u.follow = .85f; u.fearNear = .1f;
                u.repertoire = new[] { Uncanny.Gesture.WringHands, Uncanny.Gesture.TouchFace, Uncanny.Gesture.LookAtHand }; u.talkWithHands = true;
                u.lines = new[] { "Ya casi. Ya casi.", "Te cambié las sábanas aunque no estuvieras dentro.", "La hora también tiene fiebre." };
            });
            Target(nurse, "ines", "Inés · enfermera", "Hablar");
            // Sickly key from the bedside lamp, cold rim from behind: a lit figure in a dark room.
            Aim("Lampara de cabecera", new Vector3(-1.6f, 2.5f, -.2f), new Vector3(.9f, 1.3f, -.7f), 40, 3.4f, 9, new Color(1, .78f, .5f));
            Aim("Recorte Ines", new Vector3(2.2f, 2.9f, 1.3f), new Vector3(.9f, 1.5f, -.7f), 30, 3.5f, 7, new Color(.45f, .85f, 1));
            Aim("Expediente", new Vector3(-1.6f, 3.0f, -3.4f), new Vector3(-2.8f, .8f, -2.25f), 34, 2.2f, 6, new Color(1, .85f, .6f));
            Stain(new Vector3(-2.2f, .012f, -2.9f), Vector3.up, 1.1f, 20);
        }

        void Corridor()
        {
            Prop("wheelchair_01", new Vector3(1.35f, 0, 9.2f), 180);
            var woman = Character("Mujer", new Vector3(1.35f, .13f, 9.12f), 180, u =>
            {
                u.displayName = "LA MUJER DE LA SILLA"; u.blinkEvery = 9; u.follow = 1; u.followLimit = 45; u.tilt = 9;
                u.fear = .55f; u.fearNear = .3f; u.scream = .1f; u.rock = .5f; u.sway = .15f;

                u.lines = new[] { "Mi hijo vuelve a las siete. Son las siete desde ayer.", "No parpadees. Si parpadeas, se acomodan.",
                                  "Tienes la cara de alguien que ya se fue.", "Empújame hacia la ventana. No. Hacia la otra ventana." };
            });
            Target(woman, "mujer", "Una mujer en silla de ruedas", "Hablar");
            Aim("Charco silla", new Vector3(.9f, 3.4f, 8.2f), new Vector3(1.35f, .9f, 9.2f), 34, 3.4f, 6, new Color(.62f, 1, .6f));
            var doctor = doctorNpc = Character("Doctor", new Vector3(-1.58f, 0, 11.7f), -90, u =>
            {
                u.displayName = "EL MÉDICO"; u.blinkEvery = 5; u.knock = .35f; u.tremor = .5f;

                u.fear = .5f; u.scream = .2f; u.whisperRadius = 3.8f;
                u.lines = new[] { "Cuarenta y uno. Cuarenta y uno. Cuarenta y uno.", "La pared está tibia. Alguien la respira desde el otro lado.",
                                  "Ya firmé su alta. Firmé la de todos. También la mía.", "No me mire la nuca. Ahí es donde se me olvida la cara." };
            });
            Target(doctor, "doctor", "Un médico de cara a la pared", "Hablar");
            Stain(new Vector3(-1.89f, 1.62f, 11.7f), Vector3.right, .32f, 70);
            Stain(new Vector3(-1.75f, .012f, 11.6f), Vector3.up, .5f, 10);
            Stain(new Vector3(.3f, .012f, 12.8f), Vector3.up, .9f, 140);
            var scatter = new System.Random(12);
            for (int i = 0; i < 7; i++)   // used syringes on the corridor floor, lying flat
                Syringe(new Vector3(-1.5f + (float)scatter.NextDouble() * 2.6f, .016f, 10.6f + (float)scatter.NextDouble() * 2.6f),
                        Quaternion.Euler(90, (float)scatter.NextDouble() * 360, 0) * Quaternion.Euler(0, (float)scatter.NextDouble() * 360, 0));
            var e = new GameObject("Emergencia").AddComponent<Light>(); e.transform.SetParent(transform);
            e.transform.position = new Vector3(-.6f, 3.0f, 12.2f); e.type = LightType.Point; e.range = 6.5f; e.color = new Color(1, .08f, .06f);
            e.shadows = LightShadows.Soft; emergency = e;
            Prop("WetFloorSign_01", new Vector3(-1.25f, 0, 13.4f), 30);
            // Waiting chairs between the gate and the ward door, a hand apart, clear of the wall.
            for (int i = 0; i < 3; i++) Prop("plastic_monobloc_chair_01", new Vector3(-1.53f, 0, 5.85f + i * .77f), 90);
            Prop("vintage_crutches_01", new Vector3(1.45f, 0, 16.45f), -90, tilt: 9);
            Prop("security_camera_01", new Vector3(1.85f, 3.15f, 5.45f), 200, collide: false, tilt: 18);
            Prop("fire_alarm", new Vector3(-1.9f, 1.45f, 15.3f), 90, collide: false);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 2; i++) SideDoor(s * 1.9f, 8.4f + i * 5 + (s > 0 ? 1.4f : 0), s);
        }

        void CommonRoom()
        {
            HospitalBed(new Vector3(3.1f, 0, 22.7f), 0, false);
            HospitalBed(new Vector3(-3.2f, 0, 23.6f), 0, true);
            Curtain(new Vector3(.1f, 0, 25.1f), 2.6f, 90);
            patient = Character("Paciente", new Vector3(2.78f, .07f, 22.7f), -90, u =>
            {
                u.displayName = "EL HOMBRE"; u.blinkEvery = 5; u.follow = .5f; u.rock = 2; u.tremor = .6f; u.fear = .25f; u.fearNear = .2f;

                u.lines = new[] { "Las sábanas cuentan hacia atrás.", "Ella me peinó con la otra mano.", "¿Ya pasó? Huele a que ya pasó." };
            });
            var corner = Character("Esquina", new Vector3(-3.98f, 0, 25.98f), -45, u =>
            {
                u.displayName = "EL HOMBRE DE LA ESQUINA"; u.blinkEvery = 8; u.follow = .6f; u.followLimit = 22; u.tremor = .6f; u.sway = .25f;

                u.fear = .9f; u.scream = .6f; u.whisperRadius = 4.5f;
                u.lines = new[] { "Ya nos ha visto.", "Tiene nuestra letra. Escribe con nuestra letra.", "Si me doy la vuelta, la esquina se queda sola." };
            });
            Target(corner, "esquina", "Un hombre en la esquina", "Acercarse");
            Aim("Esquina", new Vector3(-2.6f, 3.4f, 24.6f), new Vector3(-3.98f, 1.5f, 25.98f), 26, 5.5f, 7, new Color(.6f, 1, .7f));
            Stain(new Vector3(-4.0f, .012f, 25.9f), Vector3.up, .7f, 45);
            Target(patient, "patient", "El hombre de la sala común", "Hablar");
            var chair = Prop("plastic_monobloc_chair_01", new Vector3(1.45f, 0, 22.7f), 90);
            Target(chair, "chair", "La silla junto a la cama", "Sentarse");
            Prop("side_table_01", new Vector3(3.1f, 0, 21.2f), 0);
            Prop("medical_box", new Vector3(3.1f, .55f, 21.2f), 20, collide: false, rest: true);
            Prop("steel_frame_shelves_01", new Vector3(4.54f, 0, 18.6f), -90);
            Table(new Vector3(-3.5f, 0, 19.2f));
            var radio = Prop("vintage_radio_transceiver", new Vector3(-3.5f, .93f, 19.2f), 160, rest: true);
            Target(radio, "radio", "Radio de emergencias", "Escuchar");

            // Window: the city burning. A sky of fire and rising smoke, black buildings with lit windows and flames on the roofs.
            fireSky = new Material(surfaceTemplate) { name = "Cielo en llamas", color = Color.black }; owned.Add(fireSky);
            fireSky.EnableKeyword("_EMISSION"); fireSky.SetTexture("_EmissionMap", FireTexture()); fireSky.SetColor("_EmissionColor", new Color(1.5f, .62f, .26f));
            window = Cube("Ventana final", new Vector3(0, 2.1f, 26.88f), new Vector3(5.5f, 2.15f, .08f), fireSky).transform;
            Target(window.gameObject, "window", "El cielo sigue ahí", "Acercarse");
            Cube("Marco ventana", new Vector3(0, 2.1f, 26.8f), new Vector3(.09f, 2.3f, .11f), black);
            Cube("Marco horizontal", new Vector3(0, 2.05f, 26.78f), new Vector3(5.6f, .09f, .11f), black);
            Cube("Alfeizar", new Vector3(0, 1.0f, 26.7f), new Vector3(5.8f, .06f, .3f), steel);
            var rng = new System.Random(7);
            var lit = Mat("Ventanas encendidas", new Color(1, .7f, .35f), 0, true);
            var flame = Mat("Fuego de la ciudad", new Color(1, .45f, .12f), 0, true);
            for (int i = 0; i < 26; i++)
            {
                float w = .12f + (float)rng.NextDouble() * .22f, h = .15f + (float)rng.NextDouble() * (i % 5 == 0 ? 1.4f : .75f);
                var bx = new Vector3(-2.65f + i * .21f, 1.03f + h / 2, 26.72f);
                Cube("Ciudad en negativo", bx, new Vector3(w, h, .04f), black);
                for (int k = 0; k < (int)(h * 10); k++)
                    if (rng.NextDouble() < .3f)
                    {
                        var win = Cube("Ciudad ventana", bx + new Vector3(((float)rng.NextDouble() - .5f) * w * .7f, -h / 2 + .06f + k * .1f, -.025f), new Vector3(.018f, .025f, .005f), lit);
                        Destroy(win.GetComponent<Collider>());
                    }
                if (rng.NextDouble() < .45f)
                    for (int k = 0; k < 4; k++)   // tongues of fire on the roof, each flickering on its own
                    {
                        var f = Cube("Ciudad fuego", bx + new Vector3((k - 1.5f) * w * .22f, h / 2 + .04f, -.03f), new Vector3(w * .12f, .08f, .01f), flame);
                        f.transform.rotation = Quaternion.Euler(0, 0, ((float)rng.NextDouble() - .5f) * 20);
                        Destroy(f.GetComponent<Collider>()); cityFires.Add(f.transform);
                    }
            }
            foreach (Transform t in transform)
                if (t.name.StartsWith("Ventana") || t.name.StartsWith("Ciudad") || t.name.StartsWith("Marco"))
                    t.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Cold light from the window (inside the room, the window is a lit plane): it carves the man on the bed.
            Aim("Cielo", new Vector3(.6f, 2.6f, 26.3f), new Vector3(2.78f, 1.0f, 22.7f), 40, 4.2f, 10, new Color(1, .45f, .2f));
            fireGlow = new GameObject("Resplandor del incendio").AddComponent<Light>(); fireGlow.transform.SetParent(transform);
            fireGlow.transform.position = new Vector3(0, 2.1f, 26.3f); fireGlow.type = LightType.Point; fireGlow.range = 9; fireGlow.color = new Color(1, .42f, .15f); fireGlow.shadows = LightShadows.Soft;
            // The city is heard, too: screams, shots and laughter far away, louder the closer you stand (Tools/city_sound.py).
            var city = window.gameObject.AddComponent<AudioSource>(); city.clip = Resources.Load<AudioClip>("Sounds/ciudad");
            city.loop = true; city.spatialBlend = 1; city.rolloffMode = AudioRolloffMode.Logarithmic; city.minDistance = 1.2f; city.maxDistance = 18; city.volume = .9f;
            if (city.clip) city.Play();
            Aim("Relleno sala", new Vector3(-1.5f, 3.2f, 20.5f), new Vector3(2.6f, .9f, 22.7f), 36, 1.3f, 9, new Color(1, .4f, .25f));
        }

        // ---------------------------------------------------------------- architecture
        void Room(string name, float x, float z, float width, float length, bool northDoor, bool southDoor)
        {
            var floor = Mat(name + " baldosas", Color.white, .35f);
            floor.mainTexture = tiles; floor.mainTextureScale = new Vector2(width / 2, length / 2);
            Cube(name + " suelo", new Vector3(x, -.12f, z), new Vector3(width, .24f, length), floor);
            Cube(name + " techo", new Vector3(x, 3.7f, z), new Vector3(width, .2f, length), plasterLow);
            if(name=="Corredor")
            {
                SplitCorridorWall(-2,14,5,17);
                SplitCorridorWall(2,10,5,17);
            }
            else
            {
                Cube("Pared izquierda", new Vector3(x - width / 2, 1.8f, z), new Vector3(.2f, 3.6f, length), plaster);
                Cube("Pared derecha", new Vector3(x + width / 2, 1.8f, z), new Vector3(.2f, 3.6f, length), plaster);
            }
            EndWall(z + length / 2, width, northDoor); EndWall(z - length / 2, width, southDoor);
            for (int side = -1; side <= 1; side += 2)
            {
                if(name=="Corredor")continue; // The side passages must remain physically open.
                // Two-tone wall: darker painted lower band, steel bumper rail, black skirting.
                Cube("Zocalo pintado", new Vector3(x + side * (width / 2 - .11f), .6f, z), new Vector3(.02f, 1.2f, length), plasterLow);
                Cube("Rodapie", new Vector3(x + side * (width / 2 - .125f), .06f, z), new Vector3(.05f, .12f, length), black);
                Cube("Guardacamillas", new Vector3(x + side * (width / 2 - .16f), 1.0f, z), new Vector3(.08f, .1f, length), steel);
            }
            for (float p = z - length / 2 + 2; p < z + length / 2; p += 4)
            {
                Prop("mounted_fluorescent_lights", new Vector3(x, 3.6f, p), 0, collide: false);
                var lamp = Cube("Tubo", new Vector3(x, 3.555f, p), new Vector3(.8f, .02f, .5f), tube);
                lamp.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Destroy(lamp.GetComponent<Collider>());
                var l = new GameObject("Luz fluorescente"); l.transform.SetParent(transform); l.transform.position = new Vector3(x, 3.2f, p);
                var light = l.AddComponent<Light>(); light.type = LightType.Point; light.range = 5.2f; light.intensity = .85f;
                light.color = new Color(.68f, .95f, .7f); light.shadows = LightShadows.None;
                if (name == "Corredor" && p > z) flicker = light;
            }
        }

        void EndWall(float z, float width, bool door)
        {
            if (!door) { Cube("Muro", new Vector3(0, 1.8f, z), new Vector3(width, 3.6f, .2f), plaster); return; }
            float segment = (width - 2.5f) / 2;
            Cube("Muro puerta L", new Vector3(-1.25f - segment / 2, 1.8f, z), new Vector3(segment, 3.6f, .2f), plaster);
            Cube("Muro puerta R", new Vector3(1.25f + segment / 2, 1.8f, z), new Vector3(segment, 3.6f, .2f), plaster);
            Cube("Dintel", new Vector3(0, 3.15f, z), new Vector3(2.5f, .9f, .2f), plaster);
        }

        void DoorFrame(float z)
        {
            for (int s = -1; s <= 1; s += 2) Cube("Marco", new Vector3(s * 1.27f, 1.35f, z), new Vector3(.1f, 2.7f, .3f), steel);
            Cube("Marco superior", new Vector3(0, 2.7f, z), new Vector3(2.64f, .1f, .3f), steel);
        }

        // A closed ward door set into a corridor wall: frame, leaf, wired glass, push plate.
        void SideDoor(float x, float z, int side)
        {
            if((side>0&&Mathf.Abs(z-9.8f)<.2f)||(side<0&&Mathf.Abs(z-13.4f)<.2f))return;
            float face = x - side * .1f;
            Cube("Marco puerta", new Vector3(face, 1.08f, z), new Vector3(.06f, 2.2f, 1.12f), black);
            Cube("Hoja", new Vector3(face - side * .02f, 1.04f, z), new Vector3(.05f, 2.06f, .98f), door);
            var glass = Cube("Mirilla", new Vector3(face - side * .05f, 1.55f, z + .18f), new Vector3(.01f, .5f, .22f), plasterLow);
            Destroy(glass.GetComponent<Collider>());
            Cube("Placa", new Vector3(face - side * .05f, 1.05f, z - .32f), new Vector3(.012f, .28f, .09f), steel);
        }

        void HospitalBed(Vector3 p, float yaw, bool madeUp)
        {
            Prop("old_bed_frame", p, yaw);
            var r = Quaternion.Euler(0, yaw, 0);
            var mattress = Cube("Colchon", p + r * new Vector3(0, .53f, -.05f), new Vector3(.84f, .18f, 1.86f), cloth);
            mattress.transform.rotation = r;
            var pillow = Cube("Almohada", p + r * new Vector3(0, .67f, .66f), new Vector3(.62f, .11f, .38f), pale);
            pillow.transform.rotation = r * Quaternion.Euler(-8, 0, 0);
            if (madeUp)
            {
                var sheet = Cube("Sabana doblada", p + r * new Vector3(0, .63f, -.35f), new Vector3(.88f, .03f, 1.1f), pale);
                sheet.transform.rotation = r;
                Cube("Embozo", p + r * new Vector3(0, .65f, .2f), new Vector3(.88f, .06f, .14f), cloth).transform.rotation = r;
            }
        }

        // Pleated privacy curtain on a ceiling rail; the mesh is generated so folds catch the light.
        void Curtain(Vector3 start, float length, float yaw)
        {
            var go = new GameObject("Cortina"); go.transform.SetParent(transform);
            go.transform.position = start; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            const int columns = 64; const float top = 3.0f, bottom = .32f;
            var v = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i <= columns; i++)
            {
                float t = i / (float)columns, x = t * length;
                float fold = Mathf.Sin(t * length * 10.5f) * .07f + Mathf.Sin(t * length * 3.1f) * .03f;
                v.Add(new Vector3(fold * .6f, top, x)); v.Add(new Vector3(fold, bottom + Mathf.Sin(t * 19) * .02f, x));
            }
            for (int i = 0; i < columns; i++)
            {
                int a = i * 2;
                tris.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3, a + 1, a + 2, a, a + 3, a + 2, a + 1 });
            }
            var mesh = new Mesh { name = "Cortina" }; mesh.SetVertices(v); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = curtain;
            var rail = Cube("Riel", Vector3.zero, new Vector3(.04f, .04f, length + .1f), steel);
            rail.transform.SetParent(go.transform, false); rail.transform.localPosition = new Vector3(0, 3.05f, length / 2);
            Destroy(rail.GetComponent<Collider>());
        }

        void Table(Vector3 p)
        {
            Cube("Mesa", p + Vector3.up * .9f, new Vector3(1.2f, .06f, .75f), steel);
            Cube("Faldon", p + Vector3.up * .84f, new Vector3(1.12f, .06f, .67f), black);
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
                Cube("Pata mesa", p + new Vector3(x * .52f, .42f, z * .3f), new Vector3(.04f, .84f, .04f), steel);
        }

        // ---------------------------------------------------------------- models
        GameObject Prop(string id, Vector3 position, float yaw, bool collide = true, float tilt = 0, bool rest = false)
        {
            var src = Resources.Load<GameObject>($"Props/{id}/{id}");
            if (!src) { Debug.LogWarning("Falta el modelo " + id); return Cube(id, position, Vector3.one * .3f, steel); }
            var o = Instantiate(src, position, Quaternion.Euler(tilt, yaw, 0) * src.transform.localRotation, transform); o.name = id;
            // FBX root rotation contains its Z-up to Y-up conversion. Keep it before laying out the prop.
            // On the floor, or resting on a surface whose height is position.y: the lowest vertex touches it.
            if (Mathf.Abs(position.y)<.001f || rest) {
                var bounds=VisualAudit.GeometryBounds(o);
                o.transform.position+=Vector3.up*(position.y+.004f-bounds.min.y);
            }
            if (collide) AddBounds(o);
            return o;
        }

        GameObject Character(string id, Vector3 position, float yaw, System.Action<Uncanny> mind)
        {
            var o = Instantiate(Resources.Load<GameObject>($"Characters/{id}"), position, Quaternion.Euler(0, yaw, 0), transform);
            o.name = id;
            // Skin carries a faint light of its own: faces stay the focal point in the ASCII image.
            foreach (var r in o.GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials)
                    if (m.name.StartsWith("Piel")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.07f, .06f, .05f)); owned.Add(m); }
                    else owned.Add(m);
            AddBounds(o);
            mind(o.AddComponent<Uncanny>());
            return o;
        }

        // An irregular stain: a noisy blob texture on a quad, cut out by alpha.
        static Texture2D Blot()
        {
            const int n = 96; var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = x / (float)n - .5f, dy = y / (float)n - .5f, a = Mathf.Atan2(dy, dx);
                float edge = .32f + Mathf.PerlinNoise(Mathf.Cos(a) * 2 + 3, Mathf.Sin(a) * 2 + 3) * .18f + Mathf.PerlinNoise(x * .15f, y * .15f) * .06f;
                float splat = Mathf.PerlinNoise(x * .21f + 9, y * .21f) > .72f && Mathf.Sqrt(dx * dx + dy * dy) < .48f ? 1 : 0;
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Max(Mathf.Sqrt(dx * dx + dy * dy) < edge ? 1 : 0, splat)));
            }
            t.Apply(); return t;
        }

        void Stain(Vector3 p, Vector3 normal, float size, float spin)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Mancha"; q.transform.SetParent(transform);
            q.transform.position = p; q.transform.rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0, 0, spin);
            q.transform.localScale = Vector3.one * size;
            q.GetComponent<Renderer>().sharedMaterial = blood; q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(q.GetComponent<Collider>());
        }

        static void AddBounds(GameObject o)
        {
            var rs=o.GetComponentsInChildren<Renderer>(); if(rs.Length==0)return;
            bool first=true;Bounds local=new Bounds();
            foreach(var r in rs) {
                Mesh mesh=null;bool baked=false;
                if(r is SkinnedMeshRenderer skin){mesh=new Mesh();skin.BakeMesh(mesh);baked=true;}
                else {var mf=r.GetComponent<MeshFilter>();if(mf)mesh=mf.sharedMesh;}
                if(mesh&&mesh.isReadable)foreach(var v in mesh.vertices){
                    var p=o.transform.InverseTransformPoint(r.transform.TransformPoint(v));
                    if(first){local=new Bounds(p,Vector3.zero);first=false;}else local.Encapsulate(p);
                }
                else {var wb=r.bounds;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){
                    var p=o.transform.InverseTransformPoint(wb.center+Vector3.Scale(wb.extents,new Vector3(x,y,z)));
                    if(first){local=new Bounds(p,Vector3.zero);first=false;}else local.Encapsulate(p);
                }}
                if(baked)Destroy(mesh);
            }
            var c=o.AddComponent<BoxCollider>();c.center=local.center;c.size=local.size;
        }

        void Aim(string name, Vector3 p, Vector3 target, float angle, float intensity, float range, Color color)
            => Spot(name, p, target - p, angle, intensity, range, color);

        void Spot(string name, Vector3 p, Vector3 dir, float angle, float intensity, float range, Color color)
        {
            var l = new GameObject(name).AddComponent<Light>(); l.transform.SetParent(transform);
            l.transform.position = p; l.transform.rotation = Quaternion.LookRotation(dir);
            l.type = LightType.Spot; l.spotAngle = angle; l.intensity = intensity; l.range = range; l.color = color;
            l.shadows = LightShadows.Soft; l.shadowStrength = .9f; l.shadowBias = .03f;
        }

        // ---------------------------------------------------------------- primitives
        Material Mat(string name, Color color, float gloss = .05f, bool emission = false)
        {
            var m = new Material(surfaceTemplate) { name = name, color = color };
            m.SetFloat("_Glossiness", gloss);
            if (emission) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * .8f); }
            owned.Add(m);
            return m;
        }
        GameObject Cube(string name, Vector3 position, Vector3 scale, Material mat)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = name; o.transform.SetParent(transform);
            o.transform.position = position; o.transform.localScale = scale;
            if (mat) o.GetComponent<Renderer>().sharedMaterial = mat; else o.GetComponent<Renderer>().enabled = false;
            return o;
        }
        GameObject Cylinder(string name, Vector3 position, Vector3 scale, Material mat)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cylinder); o.name = name; o.transform.SetParent(transform);
            o.transform.position = position; o.transform.localScale = scale; o.GetComponent<Renderer>().sharedMaterial = mat;
            return o;
        }
        void Target(GameObject obj, string id, string label, string prompt)
        { var t = obj.AddComponent<Interactable>(); t.actionId = id; t.label = label; t.prompt = prompt; }
        TextMesh Sign(string content, Vector3 p, float yaw, float size)
        {
            var o = new GameObject(content); o.transform.SetParent(transform); o.transform.position = p; o.transform.rotation = Quaternion.Euler(0, yaw + 180, 0);
            var t = o.AddComponent<TextMesh>(); t.text = content; t.fontSize = 64; t.characterSize = size * .45f;
            t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center;
            Bleed(o, -o.transform.forward, size * 8, 5);
            return t;
        }

        void Update()
        {
            AnimateCity();
            if (anomaly && seam) seam.localScale = new Vector3(1 + Mathf.Sin(Time.time * .65f) * .18f, 1, 1);
            // One tired tube in the corridor: mostly steady, with short irregular drop-outs.
            float power=powerFailed?.08f:restored?1.6f:1f;
            if (emergency) emergency.intensity = power*(reducedDisturbance?.8f:2.2f * Mathf.Pow(Mathf.Max(0, Mathf.Sin(Time.time * 2.4f)), 3));
            if (flicker)
            {
                float n = Mathf.PerlinNoise(Time.time * 2.3f, 0);
                flicker.intensity = power*(reducedDisturbance||restored?.6f:n > .72f ? .1f + Mathf.PerlinNoise(Time.time * 40, 1) * .3f : .6f);
            }
        }
        Material fireSky; Light fireGlow; float nextBlast, blast;
        readonly List<Transform> cityFires = new List<Transform>();
        // The fire sky: dark at the top, glowing at the horizon, with smoke. It scrolls upward so the smoke rises.
        Texture2D FireTexture()
        {
            const int w = 256, h = 128; var t = new Texture2D(w, h, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = y / (float)h, n = Mathf.PerlinNoise(x * .035f, y * .05f) * .7f + Mathf.PerlinNoise(x * .12f, y * .15f) * .3f;
                    float glow = Mathf.Pow(1 - v, 2.2f), smoke = Mathf.SmoothStep(.35f, .75f, n);
                    t.SetPixel(x, y, Color.Lerp(new Color(.9f, .35f, .08f), new Color(.08f, .02f, .02f), Mathf.Clamp01(v * 1.2f + smoke * .5f)) * (.25f + glow * .9f));
                }
            t.Apply(); return t;
        }
        void AnimateCity()
        {
            if (!fireSky) return;
            fireSky.SetTextureOffset("_EmissionMap", new Vector2(Time.time * .004f, -Time.time * .02f));
            for (int i = 0; i < cityFires.Count; i++)
            {
                var f = cityFires[i]; var s = f.localScale; s.y = .03f + Mathf.PerlinNoise(Time.time * 4.5f, i * 1.7f) * .16f; f.localScale = s;
            }
            // A distant blast now and then lights the whole room orange for a moment.
            if (Time.time > nextBlast) { blast = 1; nextBlast = Time.time + Random.Range(6f, 16f); }
            blast = Mathf.MoveTowards(blast, 0, Time.deltaTime * 1.8f);
            if (fireGlow) fireGlow.intensity = (powerFailed ? .5f : 1) * (.7f + Mathf.PerlinNoise(Time.time * 2.2f, 5) * .9f + blast * 4);
        }
        public void RevealAnomaly() { anomaly = true; seam.gameObject.SetActive(true); }
        void OnDestroy()
        {
            Font.textureRebuilt -= RefreshFont;
            foreach (var m in owned) if (m) Destroy(m);
            if (tiles) Destroy(tiles);
        }
        void RefreshFont(Font changed)
        {
            if (changed == font && worldFont) worldFont.mainTexture = font.material.mainTexture;
            if (changed == hand && bloodFont) bloodFont.mainTexture = hand.material.mainTexture;
        }
    }
}
