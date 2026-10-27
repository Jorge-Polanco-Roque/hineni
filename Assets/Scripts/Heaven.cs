using System.Collections.Generic;
using UnityEngine;

namespace HINENI
{
    // Looking straight up tears the image. For as long as the player keeps looking, there is no ceiling: the sky holds
    // what the old texts actually describe. Wheels within wheels, their rims full of eyes. Bodies with six wings and four
    // faces (a man, a lion, an ox, an eagle), eyes all over them. Every eye looks at the player.
    // The picture changes with it: dark ink on bone paper, like an engraving.
    public sealed class Heaven : MonoBehaviour
    {
        const float Recede = 1, MenuRecede = 14, UpEnter = .85f, UpLeave = .7f, TearInTime = .35f, TearOutTime = .22f, Height = 15;
        enum Phase { Earth, TearIn, Sky, TearOut }
        Phase phase;
        float clock, gazeUp;
        DemoGame game;
        Camera cam;
        Transform root;
        bool showing, fogWas;
        float farWas, contourWas;
        Color ambientWas;
        readonly List<Renderer> ceiling = new List<Renderer>();
        readonly List<Transform> eyes = new List<Transform>();
        readonly List<(Transform t, Vector3 axis, float speed)> spins = new List<(Transform, Vector3, float)>();
        readonly List<(Transform t, Quaternion rest, float phase)> wings = new List<(Transform, Quaternion, float)>();
        readonly List<Object> owned = new List<Object>();
        Material sclera, iris, pupil, bone, flesh, horn, feather;
        AudioSource choir, tear, tongue, bell;
        // Near the end the sky does not wait to be looked at: it flashes in front of you, announced by a bell.
        float flashUntil = -1, nextFlash = -1, nextToll;
        bool bellRung, facing;
        readonly List<Renderer> world = new List<Renderer>();
        readonly List<bool> worldWas = new List<bool>();
        AudioClip[] tongues;
        int verse = -1;
        float silence = .8f, skyTime, verseClock;   // verseClock: the voice's playhead, kept by hand (no AudioSource.time in browsers)
        bool choirStarted;   // the sky keeps its own time: it stops while nobody looks at it
        readonly List<(Transform t, Vector3 from, float speed, float phase)> drops = new List<(Transform, Vector3, float, float)>();
        Material blood;
        public bool InSky => phase == Phase.Sky;
        public bool forced;   // an ending holds the sky open
        public bool menu;     // the title screen shows them too
        public Transform Root => root;
        const string Script = "╔╗╚╝║═┼├┤┬┴○●▸▓▒░¡¿Ñ·";
        // What they say, as far as anyone can write it down: revealed as the voice advances.
        public string Subtitle
        {
            get
            {
                if (!showing || verse < 0 || !tongue.clip || !tongue.isPlaying) return null;
                var rng = new System.Random(verse * 7919 + 11); var sb = new System.Text.StringBuilder();
                int words = 5 + verse % 4;
                for (int w = 0; w < words; w++) { if (w > 0) sb.Append(' '); int n = 2 + rng.Next(4); for (int i = 0; i < n; i++) sb.Append(Script[rng.Next(Script.Length)]); }
                int shown = Mathf.CeilToInt(sb.Length * Mathf.Clamp01(verseClock / tongue.clip.length * 1.15f));
                return sb.ToString(0, Mathf.Clamp(shown, 1, sb.Length));
            }
        }

        public void Initialize(DemoGame owner)
        {
            game = owner; cam = game.player.view;
            foreach (var r in game.world.GetComponentsInChildren<Renderer>(true))
            {
                var t = r.transform; while (t.parent && t.parent != game.world.transform) t = t.parent;
                if (t.name.Contains("techo") || t.name.StartsWith("mounted_fluorescent") || t.name == "Tubo") ceiling.Add(r);
            }
            // Lit from within, against a dark sky: on bone paper they print as dense ink.
            sclera = Mat(new Color(.95f, .92f, .85f), .6f, 1.1f); iris = Mat(new Color(.6f, .06f, .03f), .4f, .7f);
            pupil = Mat(new Color(.01f, .01f, .01f), .9f, 0); bone = Mat(new Color(.85f, .8f, .7f), .1f, .6f);
            flesh = Mat(new Color(.55f, .22f, .18f), .3f, .35f); horn = Mat(new Color(.22f, .18f, .14f), .3f, .1f);
            feather = Mat(new Color(.9f, .88f, .82f), .05f, .65f);
            blood = Mat(new Color(.75f, .02f, .01f), .8f, .9f);

            root = new GameObject("Lo que hay encima del techo").transform; root.SetParent(transform);
            Sky();
            var light = new GameObject("Luz desde abajo").AddComponent<Light>(); light.transform.SetParent(root, false);
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(-70, 30, 0); light.intensity = 2.2f;
            light.color = new Color(1, .9f, .75f);
            Wheels(new Vector3(0, Height + 13, 0));
            string[] bodies = { "Esquina", "Ines", "Paciente" };
            for (int i = 0; i < 3; i++) Creature(bodies[i], i * 120 + 15, i);
            root.gameObject.SetActive(false);

            choir = gameObject.AddComponent<AudioSource>(); choir.loop = true; choir.clip = Choir(); choir.volume = 0; choir.spatialBlend = 0;
            tear = gameObject.AddComponent<AudioSource>(); tear.clip = Tear(); tear.spatialBlend = 0;
            tongue = gameObject.AddComponent<AudioSource>(); tongue.spatialBlend = 0;
            bell = gameObject.AddComponent<AudioSource>(); bell.spatialBlend = 0; bell.clip = Bell(); owned.Add(bell.clip);
            foreach (var r in game.world.GetComponentsInChildren<Renderer>(true)) world.Add(r);
            var found = new List<AudioClip>();
            for (int i = 0; ; i++) { var c = Resources.Load<AudioClip>("Voices/lengua_" + i); if (!c) break; found.Add(c); }
            tongues = found.ToArray();
            owned.Add(choir.clip); owned.Add(tear.clip);
        }

        // ------------------------------------------------------------ state
        void Update()
        {
            if (!game || !cam) return;
            float dt = Time.unscaledDeltaTime; clock += dt;
            bool allowed = forced || menu || game.SimulationActive && !(game.horror && game.horror.Hidden);
            float up = forced || menu ? 1 : cam.transform.forward.y;
            if ((forced || menu) && phase != Phase.Sky) Go(Phase.Sky);
            switch (phase)
            {
                case Phase.Earth:
                    gazeUp = allowed && up > UpEnter ? gazeUp + dt : 0;
                    if (gazeUp > .1f) Go(Phase.TearIn);
                    break;
                case Phase.TearIn:
                    if (!allowed || up < UpLeave) Go(Phase.TearOut);
                    else if (clock > TearInTime) Go(Phase.Sky);
                    break;
                case Phase.Sky:
                    if (!allowed || up < UpLeave) Go(Phase.TearOut);
                    break;
                case Phase.TearOut:
                    if (clock > TearOutTime) Go(Phase.Earth);
                    break;
            }
            // The last five minutes: brief visions, more and more often, each announced by a bell; the last fifteen
            // seconds: bells, faster and faster.
            float now = Time.unscaledTime;
            bool live = game.SimulationActive && !(game.horror && game.horror.Hidden) && !forced && !menu;
            if (live && game.Remaining < 300 && phase == Phase.Earth)
            {
                if (nextFlash < 0) nextFlash = now + FlashInterval();
                if (!bellRung && now > nextFlash - 1.4f) { Toll(1); bellRung = true; }
                if (now > nextFlash) { flashUntil = now + Random.Range(.9f, 1.6f); nextFlash = now + FlashInterval(); bellRung = false; }
            }
            if (live && game.Remaining < 15 && now > nextToll) { Toll(Random.Range(.94f, 1.08f)); nextToll = now + Mathf.Lerp(.12f, 1.2f, game.Remaining / 15); }
            bool flash = now < flashUntil && phase == Phase.Earth;
            // The tear flickers between the two worlds before it settles on the sky.
            float flick = Mathf.PerlinNoise(Time.unscaledTime * 31, 3);
            bool sky = flash || phase == Phase.Sky || (phase == Phase.TearIn && clock > .06f && flick > .3f) || (phase == Phase.TearOut && clock < .1f);
            float glitch = flash ? .55f : phase == Phase.TearIn ? 1 : phase == Phase.TearOut ? 1 - clock / TearOutTime
                         : phase == Phase.Sky ? (Mathf.PerlinNoise(Time.unscaledTime * 2.3f, 9) > .78f ? .35f : .04f) : 0;
            if (sky && flash != facing && showing) Show(false);   // switching between a vision and looking up: reopen
            facing = sky && flash;
            Show(sky);
            if (!game.Ended) { game.Ink.glitch = game.ReduceEffects ? glitch * .3f : glitch; game.Ink.heaven = sky ? 1 : 0; }
            choir.volume = .75f * game.MasterVolume; tongue.volume = 1.3f * game.MasterVolume;
            if (showing) { skyTime += Time.deltaTime; Animate(Time.deltaTime); Pray(Time.deltaTime); }
        }
        void Go(Phase next)
        {
            phase = next; clock = 0;
            if (next == Phase.TearIn || next == Phase.TearOut) { tear.pitch = next == Phase.TearIn ? 1 : 1.4f; tear.PlayOneShot(tear.clip, game.MasterVolume); }
        }
        // They pray aloud, verse after verse, in no language anyone speaks.
        void Pray(float dt)
        {
            if (tongue.isPlaying) verseClock += dt;
            if (tongues.Length == 0 || tongue.isPlaying) return;
            silence -= dt; if (silence > 0) return;
            verse = (verse + 1) % tongues.Length; tongue.clip = tongues[verse]; tongue.Play(); verseClock = 0; silence = 1.1f + verse % 3 * .5f;
        }
        void Show(bool on)
        {
            if (on == showing) return;
            showing = on;
            if (on)
            {
                // The sky opens toward wherever you are looking, so they are always in front of you.
                var c = cam.transform; var axis = menu ? Vector3.Slerp(Vector3.up, new Vector3(.47f, .88f, 0).normalized, .2f) : c.forward;   // title: centred in the right half (DemoGame tilts the view to .47, .88)
                root.SetPositionAndRotation(c.position + axis * (menu ? MenuRecede : Recede), Quaternion.FromToRotation(Vector3.up, axis));   // set back so the whole host fits the frame
                if (facing)
                {
                    // A vision: the hospital gone for an instant.
                    worldWas.Clear(); foreach (var r in world) { worldWas.Add(r && r.enabled); if (r) r.enabled = false; }
                }
                fogWas = RenderSettings.fog; ambientWas = RenderSettings.ambientLight; farWas = cam.farClipPlane; contourWas = game.Ink.contourFade;
                RenderSettings.fog = false; RenderSettings.ambientLight = new Color(.4f, .36f, .32f); cam.farClipPlane = 400; game.Ink.contourFade = 90;
            }
            else
            {
                RenderSettings.fog = fogWas; RenderSettings.ambientLight = ambientWas; cam.farClipPlane = farWas; game.Ink.contourFade = contourWas;
                if (worldWas.Count > 0) { for (int i = 0; i < world.Count; i++) if (world[i]) world[i].enabled = worldWas[i]; worldWas.Clear(); }
                root.rotation = Quaternion.identity; root.position = new Vector3(root.position.x, 0, root.position.z);
            }
            root.gameObject.SetActive(on);
            foreach (var r in ceiling) if (r) r.enabled = !on;
            // Looking away does not end anything: voice and choir wait exactly where they were.
            if (on) { if (choirStarted) choir.UnPause(); else { choir.Play(); choirStarted = true; } tongue.UnPause(); }
            else { choir.Pause(); tongue.Pause(); }
        }
        void Animate(float dt)
        {
            float t = skyTime;
            foreach (var s in spins) s.t.Rotate(s.axis, s.speed * dt, Space.Self);
            // Blood falls from their eyes toward whoever is looking up.
            foreach (var d in drops)
            {
                float travel = Mathf.Repeat(t * d.speed + d.phase, 1) * 14;
                d.t.position = d.t.parent.TransformPoint(d.from) + Vector3.down * travel;
            }
            foreach (var w in wings) w.t.localRotation = w.rest * Quaternion.Euler(Mathf.Sin(t * .8f + w.phase) * 6, Mathf.Sin(t * .8f + w.phase) * 14, 0);
            root.Rotate(0, 2.5f * dt, 0);
            var eye = cam.transform.position;
            for (int i = 0; i < eyes.Count; i++)
            {
                var e = eyes[i];
                // A shared, slightly late gaze with small saccades: they look at you, not at the camera.
                var jitter = new Vector3(Mathf.PerlinNoise(i, t * 1.7f) - .5f, Mathf.PerlinNoise(t * 1.7f, i) - .5f, 0) * .6f;
                e.rotation = Quaternion.LookRotation(eye + jitter - e.position);
            }
        }

        // ------------------------------------------------------------ the sky itself
        void Sky()
        {
            const int w = 512, h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat }; owned.Add(tex);
            var px = new Color[w * h];
            Color dark = new Color(.03f, .006f, .006f), blood = new Color(.16f, .02f, .015f), paper = new Color(.5f, .42f, .32f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = y / (float)h, r = Mathf.Clamp01((1 - v) * 2), a = x / (float)w * Mathf.PI * 2;
                    float s = a + r * 7;   // a vortex wound around the zenith
                    float n = Mathf.PerlinNoise(Mathf.Cos(s) * r * 5 + 20, Mathf.Sin(s) * r * 5 + 20) * .7f + Mathf.PerlinNoise(r * 22, s * 2) * .3f;
                    var c = Color.Lerp(dark, blood, Mathf.SmoothStep(.35f, .55f, n));
                    c = Color.Lerp(c, paper, Mathf.SmoothStep(.6f, .78f, n) * (1 - r * .5f));
                    c = Color.Lerp(c, paper, Mathf.SmoothStep(.1f, .02f, r));   // the light at the centre
                    if (v < .5f) c = dark;
                    px[y * w + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            var m = new Material(game.surfaceTemplate) { color = Color.black }; owned.Add(m);
            m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", tex); m.SetColor("_EmissionColor", Color.white * 1.1f);
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere); dome.name = "Cielo"; Destroy(dome.GetComponent<Collider>());
            var mesh = Instantiate(dome.GetComponent<MeshFilter>().sharedMesh); owned.Add(mesh);
            var tris = mesh.triangles; for (int i = 0; i < tris.Length; i += 3) (tris[i], tris[i + 1]) = (tris[i + 1], tris[i]);
            mesh.triangles = tris; dome.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r2 = dome.GetComponent<Renderer>(); r2.sharedMaterial = m; r2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r2.receiveShadows = false;
            dome.transform.SetParent(root, false); dome.transform.localScale = Vector3.one * 300;
        }

        // Ophanim: three rims turning on three axes, one inside the other, and an eye where the hub would be.
        void Wheels(Vector3 centre)
        {
            var hub = new GameObject("Ruedas").transform; hub.SetParent(root, false); hub.localPosition = centre;
            Eye(hub, Vector3.zero, 5.5f);
            float[] radius = { 15, 11, 7.5f }; int[] count = { 30, 22, 15 };
            Vector3[] axis = { Vector3.up, Vector3.right, Vector3.forward };
            for (int k = 0; k < 3; k++)
            {
                var ring = new GameObject("Rueda").transform; ring.SetParent(hub, false);
                ring.localRotation = Quaternion.Euler(k * 55, k * 30, k * 20);
                var mf = ring.gameObject.AddComponent<MeshFilter>(); mf.sharedMesh = Torus(radius[k], .55f + k * .1f); owned.Add(mf.sharedMesh);
                ring.gameObject.AddComponent<MeshRenderer>().sharedMaterial = bone;
                for (int i = 0; i < count[k]; i++)
                {
                    float a = i * Mathf.PI * 2 / count[k];
                    Eye(ring, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius[k], 1.5f - k * .25f);
                }
                spins.Add((ring, axis[k], (k % 2 == 0 ? 1 : -1) * (9 + k * 5)));
            }
        }

        // A body seen from below, face down, head toward the wheels: six wings, four faces, eyes all over.
        void Creature(string model, float angle, int seed)
        {
            var src = Resources.Load<GameObject>("Characters/" + model); if (!src) return;
            var d = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            var holder = new GameObject("Criatura " + model).transform; holder.SetParent(root, false);
            holder.localPosition = d * 10.5f + Vector3.up * (Height + seed * 2f);
            holder.localRotation = Quaternion.LookRotation(Vector3.down, -d) * Quaternion.Euler(0, 0, 8 - seed * 7);
            var body = Instantiate(src, holder); body.transform.localPosition = Vector3.zero; body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = Vector3.one * 5;
            foreach (var c in body.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var r in body.GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", m.color * .45f); owned.Add(m); }
            foreach (var r in body.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; if (r is SkinnedMeshRenderer sk) sk.updateWhenOffscreen = true; }
            Transform Bone(string n) { foreach (var t in body.GetComponentsInChildren<Transform>()) if (t.name == n) return t; return null; }
            var head = Bone("Head"); var chest = Bone("spine_03"); var pelvis = Bone("pelvis");
            // Hands pressed together in front of the chest: they are praying.
            if (chest)
            {
                Vector3 fwd = holder.forward, upw = holder.up, right = holder.right, c = chest.position;
                var palms = c + fwd * 1.5f + upw * .7f;
                foreach (var side in new[] { "l", "r" })
                {
                    Transform upper = Bone("upperarm_" + side), lower = Bone("lowerarm_" + side), hand = Bone("hand_" + side);
                    if (!upper || !lower || !hand) continue;
                    float s = Vector3.Dot(upper.position - c, right) > 0 ? 1 : -1;
                    var elbow = c + right * s * 1.1f + fwd * .7f - upw * .9f;
                    upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
                    lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, palms + right * s * .08f - lower.position) * lower.rotation;
                }
            }

            // Four faces: the man's (the body's own), a lion to his right, an ox to his left, an eagle behind.
            var faces = new GameObject("Cuatro rostros").transform; faces.SetParent(holder, false);
            faces.localPosition = head ? holder.InverseTransformPoint(head.position) + Vector3.up * .5f : Vector3.up * 8; faces.localScale = Vector3.one * 5;
            Stalk(faces, new Vector3(.24f, 0, 0)); Lion(Part(faces, new Vector3(.3f, 0, 0), Vector3.right));
            Stalk(faces, new Vector3(-.24f, 0, 0)); Ox(Part(faces, new Vector3(-.3f, 0, 0), Vector3.left));
            Stalk(faces, new Vector3(0, 0, -.22f)); Eagle(Part(faces, new Vector3(0, .02f, -.28f), Vector3.back));
            for (int i = 0; i < 4; i++) Eye(faces, new Vector3(-.09f + i * .06f, .17f, .1f), .035f);   // a row across the brow
            // The man's face weeps blood: two runs down the cheeks, and drops that let go.
            Renderer eyesMesh = null;
            foreach (var rr in body.GetComponentsInChildren<Renderer>()) if (rr.name.StartsWith("Eyes")) eyesMesh = rr;
            var eyesAt = eyesMesh ? holder.InverseTransformPoint(eyesMesh.bounds.center) : faces.localPosition + Vector3.forward * .5f;
            for (int s = -1; s <= 1; s += 2)
            {
                var at = eyesAt + Vector3.right * s * .16f + Vector3.forward * .3f;
                var run = Prim(PrimitiveType.Cube, holder, at - Vector3.up * .35f, new Vector3(.06f, .7f, .04f), blood);
                run.localRotation = Quaternion.Euler(0, 0, s * 4);
                for (int k = 0; k < 3; k++) Drop(holder, at - Vector3.up * .1f, .09f + k * .03f, k * .33f + s * .1f);
            }

            // Six wings: two over the face, two to fly, two over the feet.
            var shoulders = chest ? holder.InverseTransformPoint(chest.position) : Vector3.up * 6.5f;
            var hips = pelvis ? holder.InverseTransformPoint(pelvis.position) : Vector3.up * 4.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                Wing(holder, shoulders + new Vector3(side * .5f, .6f, -.4f), side, Quaternion.Euler(0, -side * 35, side * 40), seed);
                Wing(holder, shoulders + new Vector3(side * .6f, 0, -.5f), side, Quaternion.Euler(0, 0, -side * 5), seed + 1);
                Wing(holder, hips + new Vector3(side * .4f, -.5f, -.4f), side, Quaternion.Euler(0, -side * 30, -side * 70), seed + 2);
            }

            // Eyes all over the body, taken from the surface of the skinned mesh.
            SkinnedMeshRenderer skin = null;
            foreach (var s in body.GetComponentsInChildren<SkinnedMeshRenderer>()) if (!skin || s.sharedMesh.vertexCount > skin.sharedMesh.vertexCount) skin = s;
            if (skin)
            {
                var baked = new Mesh(); skin.BakeMesh(baked);
                var v = baked.vertices; var n = baked.normals; var rng = new System.Random(seed * 17 + 3);
                for (int i = 0; i < 26; i++)
                {
                    int k = rng.Next(v.Length);
                    var p = holder.InverseTransformPoint(skin.transform.TransformPoint(v[k]) + skin.transform.TransformDirection(n[k]).normalized * .05f);
                    Eye(holder, p, .16f + (float)rng.NextDouble() * .2f);
                }
                Destroy(baked);
            }
        }

        void Wing(Transform parent, Vector3 at, int side, Quaternion pose, int seed)
        {
            var pivot = new GameObject("Ala").transform; pivot.SetParent(parent, false);
            pivot.localPosition = at; pivot.localRotation = pose; pivot.localScale = new Vector3(side * 5, 5, 5);
            wings.Add((pivot, pose, seed * 1.3f));
            var rng = new System.Random(seed * 31 + side);
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 9; i++)
                {
                    float a = Mathf.Lerp(-25, 75, i / 8f) + row * 6, length = (.55f + i * .1f) * (1 - row * .28f);
                    var dir = Quaternion.Euler(0, 0, a) * Vector3.right;
                    var f = Prim(PrimitiveType.Cube, pivot, dir * length * .5f + Vector3.back * row * .01f, new Vector3(length, .012f + row * .004f, .1f - row * .02f), feather);
                    f.localRotation = Quaternion.Euler(0, 0, a) * Quaternion.Euler((float)rng.NextDouble() * 10, 0, 0);
                    if ((i + row) % 2 == 0) Eye(pivot, dir * length * (.55f + row * .1f) + Vector3.forward * .03f, .045f);
                }
        }

        static Transform Part(Transform parent, Vector3 at, Vector3 facing)
        {
            var t = new GameObject("Rostro").transform; t.SetParent(parent, false);
            t.localPosition = at; t.localRotation = Quaternion.LookRotation(facing); return t;
        }
        void Stalk(Transform parent, Vector3 to)
        {
            var s = Prim(PrimitiveType.Sphere, parent, to * .5f, new Vector3(.13f, .13f, .13f) + new Vector3(Mathf.Abs(to.x), 0, Mathf.Abs(to.z)), flesh);
            s.localRotation = Quaternion.identity;
        }
        void Lion(Transform p)
        {
            Prim(PrimitiveType.Sphere, p, Vector3.zero, new Vector3(.24f, .23f, .26f), bone);
            Prim(PrimitiveType.Sphere, p, new Vector3(0, -.05f, .12f), new Vector3(.15f, .12f, .15f), bone);
            Prim(PrimitiveType.Cube, p, new Vector3(0, -.01f, .2f), new Vector3(.06f, .04f, .04f), pupil);
            Prim(PrimitiveType.Cube, p, new Vector3(0, -.11f, .15f), new Vector3(.11f, .035f, .08f), pupil);   // the mouth stays open
            for (int i = 0; i < 30; i++)
            {
                float a = i * Mathf.PI * 2 / 30; var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), -.35f).normalized;
                var spike = Prim(PrimitiveType.Cube, p, dir * .19f, new Vector3(.035f, .035f, .2f + (i % 3) * .05f), horn);
                spike.localRotation = Quaternion.LookRotation(dir);
            }
            Eye(p, new Vector3(-.065f, .035f, .11f), .05f); Eye(p, new Vector3(.065f, .035f, .11f), .05f);
        }
        void Ox(Transform p)
        {
            Prim(PrimitiveType.Cube, p, Vector3.zero, new Vector3(.22f, .2f, .25f), bone);
            Prim(PrimitiveType.Cube, p, new Vector3(0, -.08f, .16f), new Vector3(.17f, .14f, .15f), bone);
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(PrimitiveType.Sphere, p, new Vector3(s * .04f, -.08f, .24f), Vector3.one * .03f, pupil);
                Prim(PrimitiveType.Cube, p, new Vector3(s * .15f, .03f, -.02f), new Vector3(.09f, .03f, .05f), bone);
                // Horn: tapered segments sweeping out, then up and forward.
                var at = new Vector3(s * .1f, .09f, -.02f);
                for (int i = 0; i < 7; i++)
                {
                    var dir = Vector3.Slerp(new Vector3(s, .2f, 0), new Vector3(s * .2f, 1, .7f), i / 6f).normalized;
                    float len = .065f, width = Mathf.Lerp(.055f, .012f, i / 6f);
                    var seg = Prim(PrimitiveType.Cylinder, p, at + dir * len * .5f, new Vector3(width, len * .5f, width), horn);
                    seg.localRotation = Quaternion.FromToRotation(Vector3.up, dir); at += dir * len * .9f;
                }
                Eye(p, new Vector3(s * .1f, .02f, .07f), .045f);
            }
        }
        void Eagle(Transform p)
        {
            Prim(PrimitiveType.Sphere, p, Vector3.zero, new Vector3(.19f, .2f, .22f), feather);
            var at = new Vector3(0, .01f, .1f);
            for (int i = 0; i < 5; i++)
            {
                var dir = Vector3.Slerp(Vector3.forward, Vector3.down, i / 5f);
                float len = .045f, width = Mathf.Lerp(.07f, .015f, i / 4f);
                var seg = Prim(PrimitiveType.Cube, p, at + dir * len * .5f, new Vector3(width, width * .8f, len), horn);
                seg.localRotation = Quaternion.LookRotation(dir); at += dir * len * .85f;
            }
            Prim(PrimitiveType.Cube, p, new Vector3(0, -.05f, .13f), new Vector3(.05f, .02f, .08f), horn);
            for (int i = 0; i < 7; i++)
            {
                var crest = Prim(PrimitiveType.Cube, p, new Vector3((i - 3) * .02f, .09f, -.06f), new Vector3(.018f, .012f, .17f), feather);
                crest.localRotation = Quaternion.Euler(-35 + Mathf.Abs(i - 3) * 6, (i - 3) * 8, 0);
            }
            Eye(p, new Vector3(-.075f, .035f, .06f), .06f); Eye(p, new Vector3(.075f, .035f, .06f), .06f);
        }

        // ------------------------------------------------------------ parts
        void Eye(Transform parent, Vector3 at, float size)
        {
            var e = Prim(PrimitiveType.Sphere, parent, at, Vector3.one * size, sclera);
            Prim(PrimitiveType.Sphere, e, new Vector3(0, 0, .36f), new Vector3(.64f, .64f, .32f), iris);
            Prim(PrimitiveType.Sphere, e, new Vector3(0, 0, .47f), new Vector3(.3f, .3f, .1f), pupil);
            eyes.Add(e);
            if (eyes.Count % 4 == 0) Drop(parent, at, .05f + (eyes.Count % 7) * .01f, eyes.Count * .137f);   // one eye in four weeps
        }
        void Drop(Transform parent, Vector3 from, float speed, float phase)
        {
            var d = Prim(PrimitiveType.Sphere, parent, from, Vector3.one, blood);
            d.localScale = new Vector3(.07f, .3f, .07f) / Mathf.Max(.01f, Mathf.Abs(parent.lossyScale.x));
            drops.Add((d, from, speed, phase));
        }
        Transform Prim(PrimitiveType type, Transform parent, Vector3 at, Vector3 scale, Material m)
        {
            var o = GameObject.CreatePrimitive(type); Destroy(o.GetComponent<Collider>());
            var r = o.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            o.transform.SetParent(parent, false); o.transform.localPosition = at; o.transform.localScale = scale;
            return o.transform;
        }
        Material Mat(Color c, float gloss, float glow)
        {
            var m = new Material(game.surfaceTemplate) { color = c, enableInstancing = true }; m.SetFloat("_Glossiness", gloss);
            if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); }
            owned.Add(m); return m;
        }
        static Mesh Torus(float radius, float tube, int around = 72, int section = 10)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i <= around; i++)
            {
                float a = i * Mathf.PI * 2 / around; var c = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                for (int j = 0; j <= section; j++)
                {
                    float b = j * Mathf.PI * 2 / section; var dir = c * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                    v.Add(c * radius + dir * tube); n.Add(dir);
                }
            }
            for (int i = 0; i < around; i++)
                for (int j = 0; j < section; j++)
                {
                    int a = i * (section + 1) + j, b = a + section + 1;
                    tris.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
                }
            var m = new Mesh { name = "Rueda" }; m.SetVertices(v); m.SetNormals(n); m.SetTriangles(tris, 0); return m;
        }

        float FlashInterval() => Mathf.Lerp(7, 40, game.Remaining / 300) + Random.Range(0, 5f);
        void Toll(float pitch) { bell.pitch = pitch; bell.PlayOneShot(bell.clip, .8f * game.MasterVolume); }

        // ------------------------------------------------------------ sound
        // A large bronze bell: inharmonic partials (hum, prime, tierce, quint, nominal...) each with its own decay.
        static AudioClip Bell()
        {
            const int rate = 22050; const float seconds = 5; var data = new float[(int)(rate * seconds)];
            float f0 = 164.8f;
            float[] ratio = { .5f, 1, 1.183f, 1.506f, 2, 2.514f, 2.662f, 3.011f, 4.166f };
            float[] decay = { 4.5f, 3.8f, 3.2f, 2.6f, 2.2f, 1.6f, 1.4f, 1.1f, .7f };
            float[] gain = { .5f, .9f, .55f, .3f, .6f, .25f, .2f, .18f, .12f };
            var rng = new System.Random(4);
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate, s = 0;
                for (int k = 0; k < ratio.Length; k++) s += Mathf.Sin(2 * Mathf.PI * f0 * ratio[k] * t * (1 + .0015f * k)) * gain[k] * Mathf.Exp(-t / decay[k] * 1.2f);
                s += ((float)rng.NextDouble() * 2 - 1) * .35f * Mathf.Exp(-t * 60);   // the strike
                data[i] = s * .28f;
            }
            var clip = AudioClip.Create("Campana", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
        // A choir that is not quite a choir: close voices beating against each other, and air.
        static AudioClip Choir()
        {
            const int rate = 22050; const float seconds = 8; var data = new float[(int)(rate * seconds)];
            float[] hz = { 110, 110.9f, 164.2f, 220.6f, 233.1f, 329.1f, 440.8f };
            var rng = new System.Random(7); float air = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate, s = 0;
                for (int k = 0; k < hz.Length; k++)
                {
                    float vib = Mathf.Sin(t * Mathf.PI * 2 * (4.6f + k * .3f)) * .004f;
                    float swell = .55f + .45f * Mathf.Sin(t * Mathf.PI * 2 / seconds * (k % 3 + 1) + k);   // whole cycles: the loop is seamless
                    s += Mathf.Sin(t * Mathf.PI * 2 * hz[k] * (1 + vib)) * swell / hz.Length;
                }
                air = Mathf.Lerp(air, (float)rng.NextDouble() * 2 - 1, .05f);
                data[i] = s * .5f + air * .25f;
            }
            var clip = AudioClip.Create("Coro", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
        static AudioClip Tear()
        {
            const int rate = 22050; var data = new float[(int)(rate * .5f)]; var rng = new System.Random(13); float held = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (i % (40 + (i / 900) * 30) == 0) held = (float)rng.NextDouble() * 2 - 1;   // sample-and-hold: digital, broken
                data[i] = held * .55f * (1 - i / (float)data.Length);
            }
            var clip = AudioClip.Create("Desgarro", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }

        void OnDestroy() { foreach (var o in owned) if (o) Destroy(o); }
    }
}
