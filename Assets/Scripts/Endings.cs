using UnityEngine;

namespace LosQueQuedan
{
    // Six ways the last fifteen minutes end. Each one stages the world once more and prints it differently:
    // amber embers, green phosphor, gold leaf, the slits of a cabinet, an engraving full of eyes, a red sky.
    public sealed class Endings : MonoBehaviour
    {
        public sealed class Ending
        {
            public string id, title, kicker; public string[] lines; public string afterLever;
        }
        public static readonly Ending[] All =
        {
            new Ending{id="quedarse",title="QUEDARSE",kicker="FINAL · LA VIGILIA",lines=new[]{
                "Respiraste con él hasta que el aire dejó de hacer falta.",
                "No le dijiste que todo iba a estar bien. Le diste la mano.",
                "Cuando llegó el Juicio, encontró a dos personas que no estaban solas."},
                afterLever="En el pasillo seguía la sangre del médico. Él no preguntó. Tú tampoco."},
            new Ending{id="voz",title="LA VOZ",kicker="FINAL · LA LÍNEA",lines=new[]{
                "La línea aguantó más de lo que nadie esperaba.",
                "Lucía habló de la cocina, de una taza rota, de vuestra madre.",
                "No arreglasteis nada. Pero al final del mundo había alguien al otro lado."},
                afterLever="No le contaste lo de la palanca. Hay cosas que se llevan solas."},
            new Ending{id="velas",title="LAS VELAS",kicker="FINAL · EL SÉPTIMO DÍA",lines=new[]{
                "Siete llamas a la vez, como pedía la pared.",
                "La capilla se llenó de una luz que no venía de las velas.",
                "El séptimo día no os dejó solos. Solo llegó tarde."},
                afterLever="La luz también alumbró tus manos. Seguían rojas."},
            new Ending{id="armario",title="EL ARMARIO",kicker="FINAL · EL ESCONDITE",lines=new[]{
                "Contuviste el aliento hasta que el Testigo se cansó de esperar.",
                "Por las rendijas viste pasar la luz del final, de lado, como bajo una puerta.",
                "Nadie te encontró. Nadie te juzgó. Nadie."},
                afterLever="Ahí dentro olía a hierro. No sabías si era el armario o eras tú."},
            new Ending{id="ojos",title="LOS OJOS",kicker="FINAL · LO QUE ESTABA ESCRITO",lines=new[]{
                "Miraste hacia arriba más tiempo del que se puede mirar.",
                "Rezaban por el mundo. Al final rezaron por ti, con tu nombre mal pronunciado.",
                "No eran ángeles de estampa. Eran exactamente lo que estaba escrito."},
                afterLever="Cuarenta y uno de sus ojos lloraban por el médico."},
            new Ending{id="juicio",title="EL JUICIO",kicker="FINAL · EL RELOJ",lines=new[]{
                "El reloj llegó a cero mientras todavía estabas decidiendo.",
                "No hubo trompetas. Hubo un ruido como de páginas.",
                "Todo lo que no hiciste también fue pesado."},
                afterLever="La palanca 41 pesó más que todo lo demás."},
        };
        public static Ending Find(string id) { foreach (var e in All) if (e.id == id) return e; return All[All.Length - 1]; }
        public static bool Seen(string id) { try { return PlayerPrefs.GetInt("final_" + id, 0) == 1; } catch { return false; } }

        public Ending Current { get; private set; }
        public float Time01 => Mathf.Clamp01(t / 14f);
        public float T => t;
        DemoGame game; Camera cam; float t;
        Vector3 from, to, look; float camHeight;
        Light key;
        AudioSource sound;

        public void Initialize(DemoGame owner) { game = owner; cam = game.player.view; sound = gameObject.AddComponent<AudioSource>(); sound.spatialBlend = 0; }

        public void Play(string id)
        {
            Current = Find(id); t = 0;
            try { PlayerPrefs.SetInt("final_" + id, 1); PlayerPrefs.Save(); } catch { }
            var w = game.world; var ink = game.Ink;
            ink.enabledEffect = true; ink.heaven = 0; ink.glitch = 0; ink.colorAmount = 0; ink.saturation = 1;
            key = new GameObject("Luz del final").AddComponent<Light>(); key.type = LightType.Point; key.shadows = LightShadows.Soft;
            if (w.witness) w.witness.SetActive(false);
            switch (id)
            {
                case "quedarse":   // amber embers on black: two people and a chair
                    from = new Vector3(1.45f, 1.15f, 22.7f); to = new Vector3(-.6f, 2.3f, 21.2f); look = new Vector3(2.7f, .9f, 22.7f);
                    ink.ink = new Color(1, .72f, .38f); ink.shadowTint = new Color(.45f, .2f, .05f); ink.paper = new Color(.02f, .01f, 0); ink.ramp = " .·:-=+*%@";
                    key.transform.position = new Vector3(2, 2.2f, 21.5f); key.color = new Color(1, .6f, .3f); key.range = 6; key.intensity = 3;
                    break;
                case "voz":        // green phosphor: the world is gone, only her voice drawn as a trace
                    from = to = new Vector3(3.9f, 1.6f, 2.9f); look = new Vector3(4.9f, 1.5f, 2.9f);
                    ink.ink = new Color(.45f, 1, .55f); ink.shadowTint = new Color(.1f, .35f, .15f); ink.paper = new Color(0, .02f, .01f); ink.accent = new Color(.6f, 1, .7f);
                    PlayVoice("LUCÍA", "Estoy sentada en el suelo de la cocina.\n\nNo sé por qué te lo digo.\n\n…Quédate un momento. Quiero que haya alguien al otro lado.");
                    break;
                case "velas":      // gold leaf: the chapel engraved in light
                    from = new Vector3(4.2f, 1.5f, 10); to = new Vector3(6.6f, 1.6f, 10); look = new Vector3(9.75f, 2.2f, 10);
                    for (int i = 0; i < 7; i++) { w.flames[i].SetActive(true); w.candleLights[i].intensity = 1.6f; }
                    ink.heaven = 1; ink.engravePaper = new Color(.98f, .93f, .78f); ink.engraveInk = new Color(.3f, .17f, .02f); ink.engraveBlood = new Color(.75f, .45f, .05f);
                    key.transform.position = new Vector3(9, 3, 10); key.color = new Color(1, .85f, .5f); key.range = 12; key.intensity = 6;
                    break;
                case "armario":    // black: seven slits of light and something passing behind them
                    from = to = new Vector3(HospitalWorld.LockerX, 1.65f, 16.4f); look = new Vector3(HospitalWorld.LockerX, 1.65f, 14);
                    ink.ink = new Color(.85f, .85f, .8f); ink.paper = Color.black; ink.shadowTint = new Color(.2f, .2f, .2f);
                    break;
                case "ojos":       // the engraving: they come down
                    from = to = new Vector3(0, 1.6f, 21); look = Vector3.zero;
                    game.heaven.forced = true;
                    break;
                default:           // the red sky and the sound of pages
                    from = to = new Vector3(0, 1.6f, 1); look = Vector3.zero;
                    game.heaven.forced = true;
                    ink.engravePaper = new Color(.42f, .02f, .02f); ink.engraveInk = new Color(.02f, 0, 0); ink.engraveBlood = new Color(1, .75f, .55f);
                    break;
            }
            if (id == "armario" || id == "voz") key.intensity = 0;
            game.player.Teleport(new Vector3(from.x, .06f, from.z), 0);
        }

        void PlayVoice(string speaker, string text)
        {
            var clip = DemoGame.Voice(speaker, text);
            if (clip) { sound.clip = clip; sound.volume = game.MasterVolume * 1.6f; sound.Play(); }
        }

        void LateUpdate()
        {
            if (Current == null) return;
            t += Time.unscaledDeltaTime;
            var ink = game.Ink; float ease = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 22));
            cam.fieldOfView = 60;
            var p = Vector3.Lerp(from, to, ease); cam.transform.position = p;
            switch (Current.id)
            {
                case "ojos":
                case "juicio":
                    cam.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward) * Quaternion.Euler(0, 0, t * 3);
                    var root = game.heaven.Root;
                    if (root) root.position = new Vector3(p.x, -ease * (Current.id == "ojos" ? 9 : 4), p.z);   // they come closer
                    ink.heaven = 1;
                    ink.glitch = Current.id == "juicio" ? (Mathf.Repeat(t, 3) < .35f ? 1 : .08f) : (Mathf.PerlinNoise(t * 1.3f, 2) > .72f ? .5f : .03f);
                    if (Current.id == "juicio") cam.transform.position += Random.insideUnitSphere * .03f;
                    break;
                case "armario":
                    cam.transform.rotation = Quaternion.LookRotation(look - p);
                    break;
                default:
                    cam.transform.rotation = Quaternion.LookRotation(look - p);
                    if (Current.id == "velas") ink.glitch = 0;
                    break;
            }
        }

        // Overlays drawn into the text grid on top of the staged world.
        public void DrawOverlay(InkCamera ink, int C, int R)
        {
            if (Current == null) return;
            if (Current.id == "voz")
            {
                ink.Fill(0, 0, C, R + 1, InkCamera.Panel);
                var data = new float[512]; if (sound.isPlaying) sound.GetOutputData(data, 0);
                int mid = R / 2 - 2;
                for (int x = 0; x < C; x++)
                {
                    float v = data[x * data.Length / C] * 14;   // when her voice ends, the trace goes flat
#if UNITY_WEBGL
                if (sound.isPlaying) v = (Mathf.PerlinNoise(x * .21f, t * 9) - .5f) * 2.2f * Mathf.PerlinNoise(t * 3, 1);   // no output data in browsers
#endif
                    int h = Mathf.Clamp(Mathf.RoundToInt(v * 6), -12, 12);
                    for (int k = 0; k <= Mathf.Abs(h); k++) ink.Put(x, mid - (h > 0 ? k : -k), k == Mathf.Abs(h) ? "█" : "│", .5f + .5f * k / 12f, true);
                }
            }
            else if (Current.id == "armario")
            {
                ink.Fill(0, 0, C, R + 1, InkCamera.Panel);
                // Seven vents. Every few seconds something crosses the light from left to right.
                float pass = Mathf.Repeat(t * .22f, 1.6f) * C;
                for (int i = 0; i < 7; i++)
                {
                    int y = R / 2 - 7 + i * 2;
                    for (int x = C / 4; x < C * 3 / 4; x++)
                    {
                        bool shadow = Mathf.Abs(x - pass) < C * .07f;
                        float b = shadow ? .05f : .45f + Mathf.PerlinNoise(x * .05f, t * .7f + i) * .4f;
                        ink.Put(x, y, shadow ? " " : "═", b);
                    }
                }
            }
        }
        void OnDestroy() { if (key) Destroy(key.gameObject); }
    }
}
