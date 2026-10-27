using System;
using System.Collections.Generic;
using UnityEngine;

namespace HINENI
{
    // One input layer for keyboard + mouse and for phones. On a phone (or as soon as the screen is touched) the game shows
    // two floating sticks — left half moves, right half looks — a contextual USE / BREATHE button, and a CONTROLES button
    // that opens the rest (flashlight, hang up, pause). Everything is drawn into the ASCII interface grid.
    // Gameplay code asks Controls instead of Input: Down/Held for keys, Axis for movement and look.
    [DefaultExecutionOrder(-100)]
    public sealed class Controls : MonoBehaviour
    {
        public static bool Touch { get; private set; }
        static Controls I;

        sealed class Pad { public int id = -1; public Vector2 origin, now; public Vector2 Value(float radius) => Vector2.ClampMagnitude((now - origin) / radius, 1); }
        sealed class Key { public KeyCode code; public string label; public Rect rect; public bool visible; }
        readonly Pad move = new Pad(), look = new Pad();
        readonly List<Key> keys = new List<Key>();
        readonly HashSet<KeyCode> down = new HashSet<KeyCode>(), held = new HashSet<KeyCode>();
        readonly Dictionary<int, Key> pressing = new Dictionary<int, Key>();
        Key toggle;
        bool panel;
        float Radius => Mathf.Min(Screen.width, Screen.height) * .13f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var o = new GameObject("Controles"); DontDestroyOnLoad(o); I = o.AddComponent<Controls>();
            Touch = Application.isMobilePlatform || Array.IndexOf(Environment.GetCommandLineArgs(), "--touch") >= 0;
        }

        public static bool Down(KeyCode k) => Input.GetKeyDown(k) || (I && I.down.Contains(k));
        public static bool Held(KeyCode k) => Input.GetKey(k) || (I && I.held.Contains(k));
        public static float Axis(string axis)
        {
            if (!Touch || !I) return Input.GetAxisRaw(axis);
            float r = I.Radius;
            switch (axis)
            {
                case "Horizontal": return Mathf.Clamp(Input.GetAxisRaw(axis) + I.move.Value(r).x, -1, 1);
                case "Vertical": return Mathf.Clamp(Input.GetAxisRaw(axis) + I.move.Value(r).y, -1, 1);
                // A stick looks at a rate (about 150°/s at full tilt), with a soft centre for small corrections.
                case "Mouse X": { var v = I.look.Value(r).x; return Mathf.Sign(v) * v * v * 95 * Time.deltaTime; }
                case "Mouse Y": { var v = I.look.Value(r).y; return Mathf.Sign(v) * v * v * 70 * Time.deltaTime; }
            }
            return Input.GetAxisRaw(axis);
        }
        // Whether the sticks and buttons are live (gameplay); menus and dialogues use plain taps.
        static bool Playing => DemoGame.Instance && DemoGame.Instance.Started && !DemoGame.Instance.Ended && !DemoGame.Instance.DialogueOpen && !DemoGame.Instance.Paused;

        void Update()
        {
            down.Clear(); held.Clear();
            if (Input.touchCount > 0) Touch = true;
            if (!Touch) return;
            Layout();
            if (!Playing) { move.id = look.id = -1; pressing.Clear(); return; }
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    var hit = keys.Find(k => k.visible && k.rect.Contains(t.position));
                    if (hit != null)
                    {
                        pressing[t.fingerId] = hit;
                        if (hit == toggle) panel = !panel; else down.Add(hit.code);
                    }
                    else if (t.position.x < Screen.width * .5f && move.id < 0) { move.id = t.fingerId; move.origin = move.now = t.position; }
                    else if (t.position.x >= Screen.width * .5f && look.id < 0) { look.id = t.fingerId; look.origin = look.now = t.position; }
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    pressing.Remove(t.fingerId);
                    if (t.fingerId == move.id) move.id = -1;
                    if (t.fingerId == look.id) look.id = -1;
                }
                else
                {
                    if (t.fingerId == move.id) move.now = t.position;
                    if (t.fingerId == look.id) look.now = t.position;
                }
            }
            foreach (var k in pressing.Values) if (k != toggle) held.Add(k.code);
            if (move.id < 0) move.now = move.origin;
            if (look.id < 0) look.now = look.origin;
        }

        // Buttons in screen pixels (origin bottom-left), rebuilt every frame for the current context and orientation.
        void Layout()
        {
            keys.Clear();
            float w = Screen.width, h = Screen.height, bw = Mathf.Max(w * .12f, 150), bh = Mathf.Max(h * .085f, 64);
            var g = DemoGame.Instance;
            toggle = Add(KeyCode.None, panel ? "CERRAR" : "CONTROLES", new Rect(w * .5f - bw * .55f, h * .04f, bw * 1.1f, bh), true);
            // Contextual: what the moment asks for, beside the look stick.
            bool sitting = g && g.Sitting, hidden = g && g.horror && g.horror.Hidden, tuning = g && g.acts && g.acts.Tuning;
            bool focus = g && g.player && g.player.Focus;
            var ctx = new Rect(w * .8f, h * .42f, bw * 1.15f, bh * 1.2f);
            if (sitting || hidden) Add(KeyCode.Space, hidden ? "CONTENER" : "RESPIRAR", ctx, true);
            else if (tuning) Add(KeyCode.Q, "COLGAR", ctx, true);
            else Add(KeyCode.E, "USAR", ctx, focus);
            if (sitting || hidden) Add(KeyCode.E, hidden ? "SALIR" : "LEVANTARSE", new Rect(ctx.x, ctx.yMax + bh * .3f, ctx.width, bh), true);
            // The panel: everything a keyboard can do.
            string[] labels = { "USAR  E", "LINTERNA  F", "ESPACIO", "COLGAR  Q", "PAUSA" };
            KeyCode[] codes = { KeyCode.E, KeyCode.F, KeyCode.Space, KeyCode.Q, KeyCode.Escape };
            float total = labels.Length * bw + (labels.Length - 1) * bw * .1f, x0 = w * .5f - total / 2;
            for (int i = 0; i < labels.Length; i++) Add(codes[i], labels[i], new Rect(x0 + i * bw * 1.1f, h * .04f + bh * 1.35f, bw, bh), panel);
        }
        Key Add(KeyCode code, string label, Rect r, bool visible) { var k = new Key { code = code, label = label, rect = r, visible = visible }; keys.Add(k); return k; }

        public static void Draw(InkCamera ink, int C, int R)
        {
            if (!Touch || !I) return;
            if (Screen.height > Screen.width)
            {
                ink.Clear(); ink.Fill(0, 0, C, R + 1, InkCamera.Panel);
                string a = Loc.T("GIRA EL TELÉFONO"), b = Loc.T("HINENI se juega en horizontal");
                ink.Put((C - Loc.W(a)) / 2, R / 2 - 1, a, 1, true); ink.Put((C - Loc.W(b)) / 2, R / 2 + 1, b, .6f);
                return;
            }
            if (!Playing) return;
            float cw = Screen.width / (float)C, ch = Screen.height / (float)R;
            Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x / cw), Mathf.FloorToInt((Screen.height - p.y) / ch));
            void Stick(Pad pad, Vector2 rest, string label)
            {
                var o = pad.id >= 0 ? pad.origin : rest; float r = I.Radius;
                for (int a = 0; a < 28; a++)
                {
                    var c = Cell(o + new Vector2(Mathf.Cos(a * Mathf.PI / 14), Mathf.Sin(a * Mathf.PI / 14)) * r);
                    ink.Put(c.x, c.y, "○", pad.id >= 0 ? .85f : .5f);
                }
                var k = Cell(o + Vector2.ClampMagnitude(pad.now - pad.origin, r) * (pad.id >= 0 ? 1 : 0));
                ink.Put(k.x - 1, k.y - 1, "▄▄▄", pad.id >= 0 ? 1 : .6f); ink.Put(k.x - 1, k.y, "███", pad.id >= 0 ? 1 : .6f);
                var l = Cell(o - new Vector2(0, r * 1.25f)); label = Loc.T(label); ink.Put(l.x - Loc.W(label) / 2, l.y, label, .6f);
            }
            Stick(I.move, new Vector2(Screen.width * .14f, Screen.height * .32f), "MOVER");
            Stick(I.look, new Vector2(Screen.width * .86f, Screen.height * .22f), "MIRAR");
            foreach (var k in I.keys)
            {
                if (!k.visible) continue;
                var a = Cell(new Vector2(k.rect.xMin, k.rect.yMax)); var b = Cell(new Vector2(k.rect.xMax, k.rect.yMin));
                int w = Mathf.Max(3, b.x - a.x), h = Mathf.Max(3, b.y - a.y + 1);
                bool on = I.pressing.ContainsValue(k);
                ink.Frame(a.x, a.y, w, h, on ? .9f : .45f);
                if (on) ink.Fill(a.x + 1, a.y + 1, w - 2, h - 2, InkCamera.Inverted, .9f);
                var text = Loc.T(k.label); ink.Put(a.x + (w - Loc.W(text)) / 2, a.y + h / 2, text, on ? .05f : .9f, k.code == KeyCode.Space || k == I.toggle);
            }
        }
    }
}
