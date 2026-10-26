using System;
using UnityEngine;

namespace LosQueQuedan
{
    // The five last things a person can do in this hospital. Whichever one you finish is how the world ends for you.
    //  0 Quedarse  · breathe with the man until he no longer needs it (SPACE at the top of each breath)
    //  1 La voz    · tune the line to Lucía through the static (A/D or the mouse), then stay until it cuts
    //  2 Las velas · seven candles in the chapel, all burning at the same time
    //  3 El armario· stay hidden while the Witness searches; hold your breath (SPACE) when it is close
    //  4 El cielo  · look up for longer than anyone should
    public sealed class Acts : MonoBehaviour
    {
        public static readonly string[] Names = { "Quedarse", "La voz", "Las velas", "El armario", "El cielo" };
        public static readonly string[] Endings = { "quedarse", "voz", "velas", "armario", "ojos" };
        public readonly bool[] known = new bool[5];
        public const float SkyNeeded = 36, HideNeeded = 40, CandleBurn = 9;
        public const int BreathsNeeded = 6;

        DemoGame game;
        float clock;
        readonly float[] candleUntil = new float[7];
        float sky, hidden, air = 1, suspicion, lastSkyNote;
        bool holding, warnedEmpty;
        int breaths; float breathClock, lastBreath = -9; string breathNote;
        public bool Tuning { get; private set; }
        float dial = .5f, inBand; Action tuned;
        AudioSource hiss; AudioClip hissClip, gasp;

        public void Initialize(DemoGame owner)
        {
            game = owner;
            hiss = gameObject.AddComponent<AudioSource>(); hiss.loop = true; hiss.spatialBlend = 0; hiss.volume = 0;
            hissClip = Noise("Estática", 2, .5f, 0); gasp = Noise("Jadeo", .7f, .45f, 6);
            hiss.clip = hissClip;
        }
        static AudioClip Noise(string name, float seconds, float gain, float decay)
        {
            const int rate = 22050; var data = new float[(int)(rate * seconds)]; var rng = new System.Random(5); float f = 0;
            for (int i = 0; i < data.Length; i++)
            {
                f = Mathf.Lerp(f, (float)rng.NextDouble() * 2 - 1, decay > 0 ? .08f : .7f);
                data[i] = f * gain * (decay > 0 ? Mathf.Sin(Mathf.PI * i / data.Length) : 1);
            }
            var c = AudioClip.Create(name, data.Length, 1, rate, false); c.SetData(data, 0); return c;
        }

        public bool TryInteract(string id)
        {
            if (!id.StartsWith("candle-")) return false;
            int i = id[id.Length - 1] - '0';
            known[2] = true;
            if (candleUntil[i] > clock) { game.Notify("Ya arde. Las otras se están apagando.", 3); return true; }
            candleUntil[i] = clock + CandleBurn;
            int lit = Lit();
            game.Notify(lit == 1 ? "Una llama. Arden " + CandleBurn + " segundos. La pared pide siete." : "Arden " + lit + " de 7.", 3);
            if (lit == 7) game.EndWith("velas");
            return true;
        }
        int Lit() { int n = 0; foreach (var u in candleUntil) if (u > clock) n++; return n; }

        // ---------------------------------------------------------------- the line
        public void StartTune(Action onTuned)
        {
            known[1] = true; Tuning = true; tuned = onTuned; inBand = 0; dial = UnityEngine.Random.value < .5f ? .1f : .9f;
            hiss.Play();
        }
        float Target => .5f + (Mathf.PerlinNoise(clock * .25f, 7) - .5f) * .8f;

        // ---------------------------------------------------------------- the vigil
        public void Breath()
        {
            known[0] = true;
            breathClock += Time.deltaTime;
            if (Controls.Down(KeyCode.Space) && breathClock - lastBreath > .6f)
            {
                lastBreath = breathClock;
                if (BreathLevel > .8f) { breaths++; breathNote = breaths < BreathsNeeded ? "Respiráis a la vez. " + breaths + " de " + BreathsNeeded : null; }
                else { breathNote = "A destiempo. Él se ahoga un poco, y espera a que lo encuentres."; }
                if (breaths >= BreathsNeeded) game.EndWith("quedarse");
            }
        }
        // His breathing: slow, a little irregular, slower as the end comes.
        float BreathLevel => (Mathf.Sin(breathClock * Mathf.PI * 2 / (4.4f + Mathf.PerlinNoise(breathClock * .2f, 1))) + 1) * .5f;

        void Update()
        {
            if (!game || !game.Started) return;
            float dt = Time.deltaTime;
            if (game.SimulationActive) clock += dt;
            var world = game.world;
            for (int i = 0; i < 7; i++)
            {
                float left = candleUntil[i] - clock; bool on = left > 0;
                if (world.flames[i].activeSelf != on) world.flames[i].SetActive(on);
                // A candle gutters before it goes out.
                world.candleLights[i].intensity = on ? (left < 2 ? .4f + Mathf.PerlinNoise(clock * 9, i) * .5f : .9f + Mathf.PerlinNoise(clock * 5, i) * .3f) : 0;
                if (on) world.flames[i].transform.localScale = new Vector3(.03f, .06f + Mathf.PerlinNoise(clock * 7, i) * .04f, .03f) * Mathf.Clamp01(left / 2 + .3f);
            }
            if (!game.SimulationActive) { hiss.volume = 0; return; }

            if (Tuning)
            {
                dial = Mathf.Clamp01(dial + Controls.Axis("Horizontal") * .9f * dt + Controls.Axis("Mouse X") * .012f * game.player.sensitivity);
                float off = Mathf.Abs(dial - Target);
                if (off < .07f) inBand += dt; else inBand = Mathf.Max(0, inBand - dt * .5f);
                hiss.volume = Mathf.Clamp01(off * 3 + .08f) * game.MasterVolume * .8f;
                if (Controls.Down(KeyCode.Q)) { Tuning = false; hiss.Stop(); game.Notify("Cuelgas. El tono sigue sonando dentro del auricular.", 5); }
                else if (inBand > 4) { Tuning = false; hiss.Stop(); tuned?.Invoke(); }
                return;
            }

            var heaven = game.heaven;
            if (heaven && heaven.InSky)
            {
                known[4] = true; sky += dt;
                if (sky - lastSkyNote > 12 && sky < SkyNeeded) { lastSkyNote = sky; game.Notify(sky < 20 ? "No rezan por ellos mismos." : "Saben tu nombre. Lo pronuncian mal, con cuidado.", 4); }
                if (sky >= SkyNeeded) { game.EndWith("ojos"); return; }
            }

            var h = game.horror;
            if (h && h.Hidden)
            {
                known[3] = true;
                if (!h.ActiveThreat)
                {
                    if (!warnedEmpty) { warnedEmpty = true; game.Notify("Aquí dentro no te busca nadie. Todavía. El armario espera a que haya alguien fuera.", 6); }
                }
                else
                {
                    hidden += dt;
                    bool near = h.ThreatDistance < 4.5f;
                    holding = Controls.Held(KeyCode.Space) && air > 0;
                    air = Mathf.Clamp01(air + (holding ? -dt / 7 : dt / 3.5f));
                    if (holding && air <= 0) { holding = false; hiss.PlayOneShot(gasp, game.MasterVolume); suspicion += .45f; }
                    suspicion = Mathf.Clamp01(suspicion + (near && !holding ? dt * .55f : -dt * .12f));
                    if (suspicion >= 1) { suspicion = 0; hidden = 0; air = 1; h.Capture(); return; }
                    if (hidden >= HideNeeded) { game.EndWith("armario"); return; }
                }
            }
            else { warnedEmpty = false; air = 1; suspicion = 0; }
        }

        // ---------------------------------------------------------------- interface
        public void Draw(InkCamera ink, int C, int R)
        {
            // What you could still do: unknown acts stay as a question.
            int x = C - 30, y = 4;
            ink.Fill(x - 2, y - 1, 30, 8, InkCamera.Panel);
            ink.Put(x, y - 1, "LO ÚLTIMO QUE HAGAS", .55f);
            for (int i = 0; i < 5; i++)
            {
                string label = known[i] ? Names[i] : "· · ·", progress = "";
                if (known[i])
                    progress = i == 0 ? breaths + "/" + BreathsNeeded : i == 2 ? Lit() + "/7" : i == 3 ? Mathf.FloorToInt(hidden / HideNeeded * 100) + "%" : i == 4 ? Mathf.FloorToInt(sky / SkyNeeded * 100) + "%" : "";
                ink.Put(x, y + 1 + i, (known[i] ? "○ " : "  ") + label, known[i] ? .8f : .3f);
                if (progress.Length > 0) ink.Put(x + 22, y + 1 + i, progress, .6f, true);
            }
            int cx = C / 2;
            if (Tuning)
            {
                int w = Mathf.Min(70, C - 10), bx = cx - w / 2, by = R / 2 + 6;
                ink.Fill(bx - 3, by - 3, w + 6, 9, InkCamera.Panel);
                ink.Put(bx, by - 2, "LÍNEA AL EXTERIOR · busca la voz entre la estática", .85f);
                var rng = new System.Random(Mathf.FloorToInt(clock * 20));
                float off = Mathf.Abs(dial - Target);
                var noise = new char[w];
                for (int i = 0; i < w; i++) noise[i] = rng.NextDouble() < off * 2.2f ? "░▒▓·:"[rng.Next(5)] : '─';
                ink.Put(bx, by, new string(noise), .5f);
                ink.Put(bx + Mathf.RoundToInt(dial * (w - 1)), by, "█", 1, off < .07f);
                ink.Put(bx, by + 2, "SEÑAL " + new string('█', Mathf.RoundToInt(inBand / 4 * 20)).PadRight(20, '░'), .7f, true);
                ink.Put(bx, by + 4, Controls.Touch ? "palanca izquierda o derecha · sintonizar      COLGAR" : "A / D o ratón · sintonizar      Q · colgar", .4f);
            }
            if (game.Sitting)
            {
                int w = 40, bx = cx - w / 2, by = R - 9;
                ink.Fill(bx - 3, by - 2, w + 6, 6, InkCamera.Panel);
                ink.Put(bx, by - 1, Controls.Touch ? "RESPIRA CON ÉL · RESPIRAR arriba del todo" : "RESPIRA CON ÉL · ESPACIO arriba del todo", .8f);
                int fill = Mathf.RoundToInt(BreathLevel * w);
                ink.Put(bx, by, new string('▓', fill).PadRight(w, '·'), .4f + BreathLevel * .6f, BreathLevel > .8f);
                ink.Put(bx + Mathf.RoundToInt(w * .8f), by + 1, "▲", .5f);
                if (breathNote != null && breathClock - lastBreath < 3) ink.Put(bx, by + 2, breathNote, .6f);
            }
            var h = game.horror;
            if (h && h.Hidden && h.ActiveThreat)
            {
                int w = 30, bx = cx - w / 2, by = R - 12;
                ink.Fill(bx - 3, by - 2, w + 6, 6, InkCamera.Panel);
                ink.Put(bx, by - 1, holding ? "CONTIENES EL ALIENTO" : h.ThreatDistance < 4.5f ? Controls.Touch ? "ESTÁ AHÍ FUERA · mantén CONTENER" : "ESTÁ AHÍ FUERA · mantén ESPACIO" : "Respira. Despacio.", .85f, h.ThreatDistance < 4.5f);
                ink.Put(bx, by, "AIRE    " + new string('█', Mathf.RoundToInt(air * 20)).PadRight(20, '░'), .7f);
                ink.Put(bx, by + 1, "TE OYE  " + new string('█', Mathf.RoundToInt(suspicion * 20)).PadRight(20, '░'), .7f, true);
                ink.Put(bx, by + 2, Mathf.FloorToInt(hidden) + " / " + HideNeeded + " s", .4f);
            }
        }
    }
}
