using System.Collections.Generic;
using UnityEngine;

namespace HINENI
{
    // Small dread that never resolves: pairs of eyes in the dark that go out when light or a direct look finds them
    // (and open again somewhere else), bloody hands dragged along the corridor, and, now and then, the image tearing
    // for a frame or two, as if the sky were pressing on the ceiling.
    public sealed class Watchers : MonoBehaviour
    {
        static readonly Vector3[] Spots =
        {
            new Vector3(-2.8f, .25f, -.6f),  new Vector3(3.1f, .25f, 23.2f),  new Vector3(-3.2f, .25f, 24.1f),   // under the beds
            new Vector3(4.6f, .55f, 11.6f),  new Vector3(5.75f, .55f, 8.2f),  new Vector3(-9.3f, 2.9f, 16.6f),   // chapel pews, top of the service room
            new Vector3(.4f, 1.75f, 25.6f),  new Vector3(-4.5f, 2.6f, -4.5f), new Vector3(4.3f, 2.9f, 26.3f),   // behind the curtain, corners
            new Vector3(-1.75f, 2.95f, 16.2f),
        };
        DemoGame game;
        readonly List<(Transform t, float until)> pairs = new List<(Transform, float)>();
        float nextTear, tearUntil;

        public void Initialize(DemoGame owner)
        {
            game = owner;
            var m = new Material(game.surfaceTemplate) { color = Color.black };
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.95f, .9f, .75f) * 1.4f);
            for (int i = 0; i < 4; i++)
            {
                var pair = new GameObject("Ojos en lo oscuro").transform; pair.SetParent(transform);
                for (int s = -1; s <= 1; s += 2)
                {
                    var e = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(e.GetComponent<Collider>());
                    e.GetComponent<Renderer>().sharedMaterial = m; e.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    e.transform.SetParent(pair, false); e.transform.localPosition = new Vector3(s * .045f, 0, 0); e.transform.localScale = new Vector3(.028f, .018f, .01f);
                }
                pairs.Add((pair, 0)); Move(i);
            }
            Hands();
            nextTear = Time.time + Random.Range(40, 80);
        }

        void Move(int i)
        {
            Vector3 eye = game.player.view.transform.position, spot;
            int tries = 0;
            do spot = Spots[Random.Range(0, Spots.Length)]; while (Vector3.Distance(spot, eye) < 5 && ++tries < 10);
            pairs[i] = (pairs[i].t, Time.time + Random.Range(3f, 9f));   // closed for a while before they open elsewhere
            pairs[i].t.position = spot; pairs[i].t.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!game || !game.Started || game.Ended) { foreach (var p in pairs) p.t.gameObject.SetActive(false); return; }
            var cam = game.player.view.transform; var torch = game.horror && game.horror.Data.lightOn;
            for (int i = 0; i < pairs.Count; i++)
            {
                var (t, until) = pairs[i];
                if (Time.time < until) continue;
                var to = t.position - cam.position; float d = to.magnitude;
                bool lit = torch && Vector3.Dot(cam.forward, to / d) > .93f && d < 12;
                bool stared = Vector3.Dot(cam.forward, to / d) > .985f;
                if (t.gameObject.activeSelf && (lit || stared || d < 2.5f)) { Move(i); continue; }
                t.gameObject.SetActive(!game.ReduceEffects);
                t.rotation = Quaternion.LookRotation(t.position - cam.position);
                // They blink, out of step with each other.
                t.localScale = new Vector3(1, Mathf.PerlinNoise(Time.time * .7f, i) > .82f ? .1f : 1, 1);
            }
            // A tear of a frame or two: never while the sky is already open, never with reduced effects.
            if (Time.time > nextTear && game.SimulationActive && !game.ReduceEffects && !(game.heaven && game.heaven.InSky))
            { tearUntil = Time.time + Random.Range(.08f, .18f); nextTear = Time.time + Random.Range(45, 95); }
        }
        void LateUpdate() { if (game && Time.time < tearUntil) game.Ink.glitch = .8f; }

        // Hands: someone held the wall while they were dragged toward the service room.
        void Hands()
        {
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float u = (x - 32) / 32f, v = (y - 22) / 32f;
                    bool palm = u * u / .2f + v * v / .16f < 1;
                    bool finger = false;
                    for (int f = 0; f < 5; f++)
                    {
                        float fx = -.42f + f * .21f + (f == 0 ? -.12f : 0), len = f == 0 ? .38f : f == 2 ? .7f : .58f, fy0 = f == 0 ? -.05f : .3f;
                        if (Mathf.Abs(u - fx - (v - fy0) * (f == 0 ? -.6f : (f - 2) * .12f)) < .075f && v > fy0 && v < fy0 + len) finger = true;
                    }
                    float smear = palm || finger ? 1 : (v < -.2f && Mathf.Abs(u) < .4f && Mathf.PerlinNoise(x * .3f, y * .1f) > .45f + (-.2f - v) * .6f) ? 1 : 0;
                    tex.SetPixel(x, y, new Color(1, 1, 1, smear * (Mathf.PerlinNoise(x * .4f, y * .4f) > .2f ? 1 : 0)));
                }
            tex.Apply();
            var m = new Material(game.world.BloodMaterial) { mainTexture = tex };   // same cut-out setup as the stains
            var rng = new System.Random(3);
            for (int i = 0; i < 9; i++)
            {
                float z = 7.6f + i * .72f + (float)rng.NextDouble() * .2f;
                if ((z > 12.6f && z < 15.3f) || (z > 7.7f && z < 9.1f)) continue;   // the service room opening and the ward door
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "Mano";
                q.transform.SetParent(transform); q.transform.position = new Vector3(-1.885f, 1.05f + (float)rng.NextDouble() * .6f - i * .04f, z);
                q.transform.rotation = Quaternion.LookRotation(Vector3.left) * Quaternion.Euler(0, 0, -70 + (float)rng.NextDouble() * 30);
                q.transform.localScale = Vector3.one * .26f;
                q.GetComponent<Renderer>().sharedMaterial = m; q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}

namespace HINENI
{
    // Blood from the eyes: two runs down the face, following the head, and drops that let go and fall.
    public sealed class Weeping : MonoBehaviour
    {
        readonly System.Collections.Generic.List<(Transform drop, Transform from, float speed, float phase)> drops = new System.Collections.Generic.List<(Transform, Transform, float, float)>();
        public void Begin(Transform head, Vector3 eyesCentre, Transform body, Material blood, Material socket = null)
        {
            var right = body.right; var down = -body.up;
            for (int s = -1; s <= 1; s += 2)
            {
                var eye = eyesCentre + right * s * .032f + body.forward * .012f;
                if (socket)
                {
                    // Hollow, sunken eye: a dark oval over the eye, and the blood comes out of it.
                    var hole = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(hole.GetComponent<Collider>());
                    hole.GetComponent<Renderer>().sharedMaterial = socket;
                    hole.transform.position = eye + body.forward * .01f; hole.transform.rotation = body.rotation;
                    hole.transform.SetParent(head, true); hole.transform.localScale = Vector3.Scale(new Vector3(.046f, .042f, .016f), Inv(head.lossyScale));
                }
                var run = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(run.GetComponent<Collider>());
                run.GetComponent<Renderer>().sharedMaterial = blood;
                run.transform.position = eye + down * .085f + body.forward * .006f; run.transform.rotation = body.rotation * Quaternion.Euler(0, 0, s * 6);
                run.transform.SetParent(head, true); run.transform.localScale = Vector3.Scale(new Vector3(.014f, .16f, .007f), Inv(head.lossyScale));
                var anchor = new GameObject("Lágrima").transform; anchor.position = eye + down * .16f; anchor.SetParent(head, true);
                for (int k = 0; k < 3; k++)
                {
                    var d = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(d.GetComponent<Collider>());
                    d.GetComponent<Renderer>().sharedMaterial = blood; d.transform.localScale = new Vector3(.012f, .03f, .012f);
                    drops.Add((d.transform, anchor, .55f + k * .2f, k * .37f + s * .1f));
                }
            }
        }
        static Vector3 Inv(Vector3 v) => new Vector3(1 / v.x, 1 / v.y, 1 / v.z);
        void Update()
        {
            foreach (var d in drops)
            {
                float t = Mathf.Repeat(Time.time * d.speed + d.phase, 1);
                d.drop.position = d.from.position + Vector3.down * (t * t * 1.6f);   // they gather, then fall
            }
        }
        void OnEnable() { foreach (var d in drops) if (d.drop) d.drop.gameObject.SetActive(true); }
        void OnDisable() { foreach (var d in drops) if (d.drop) d.drop.gameObject.SetActive(false); }   // it leaves; so does its blood
        void OnDestroy() { foreach (var d in drops) if (d.drop) Destroy(d.drop.gameObject); }
    }
}
