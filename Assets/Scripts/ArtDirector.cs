using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LosQueQuedan
{
    // Art director: walks a shot list, measures every shot on the real ASCII grid against the rules in
    // Docs/DIRECCION_DE_ARTE.md, and writes a contact sheet and a report with concrete notes.
    // With --tune it also searches the tone curve that best satisfies every shot at once and writes
    // Assets/Resources/LookPreset.json, which InkCamera loads at start.
    // Opt-in only:  "Los que quedan.app/Contents/MacOS/..." --art-director [--tune] [--art-out dir] [--preset-out file]
    public sealed class ArtDirector : MonoBehaviour
    {
        struct Shot
        {
            public string name, focal, intent; public Vector3 eye; public float yaw, pitch, height, fov;
            public Shot(string name, string focal, string intent, Vector3 eye, float yaw, float pitch = 0, float height = 1.62f, float fov = 67)
            { this.name = name; this.focal = focal; this.intent = intent; this.eye = eye; this.yaw = yaw; this.pitch = pitch; this.height = height; this.fov = fov; }
        }

        static readonly Shot[] Shots =
        {
            new Shot("01-despertar", "Ines", "Inés recortada por la luz de cabecera; la habitación cae a negro.", new Vector3(0, .06f, -2.7f), 15),
            new Shot("02-cama", "clipboard", "La cama vacía y el expediente: el lugar del que vienes.", new Vector3(.8f, .06f, -4.1f), -38, 8),
            new Shot("03-pasillo", "wheelchair_01", "Ritmo de fluorescentes; la silla de ruedas como única figura.", new Vector3(0, .06f, 5.6f), 0),
            new Shot("04-sala", "Paciente", "El hombre en la cama, a contraluz de la ventana.", new Vector3(-.4f, .06f, 19), 30),
            new Shot("05-vigilia", "Paciente", "Sentado frente a él: rostro, manos, manta.", new Vector3(1.45f, .06f, 22.7f), 90, 6, 1.12f),
            new Shot("06-ventana", "Ventana final", "La ciudad en negativo contra el cielo.", new Vector3(0, .06f, 23.6f), 0, -6),
            new Shot("07-silla", "Mujer", "La mujer de la silla te mira con la cabeza ladeada.", new Vector3(.9f, .06f, 6.4f), 8),
            new Shot("08-esquina", "Esquina", "Brazos abiertos contra la esquina; la cabeza, vuelta hacia ti.", new Vector3(-2.4f, .06f, 24.3f), -42),
            new Shot("09-rostro", "Ines", "Primer plano: el rostro de Inés debe leerse sin ayuda.", new Vector3(.49f, .06f, -1.5f), 27, 0, 1.6f, 16),
        };

        struct Metrics { public float exposure, negative, range, focal, definition, score; public List<string> notes; }

        DemoGame game; InkCamera ink;
        readonly string[] args = Environment.GetCommandLineArgs();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--art-director") >= 0)
                new GameObject("Director de arte").AddComponent<ArtDirector>();
        }

        string Arg(string name, string fallback)
        { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }

        IEnumerator Start()
        {
            yield return new WaitForSeconds(3.5f);
            game = DemoGame.Instance; ink = game.Ink;
            DemoGame.HideInterface = true; ink.keepCells = true;
            string outDir = Arg("--art-out", Path.Combine(Application.persistentDataPath, "director-de-arte"));
            Directory.CreateDirectory(outDir);
            var report = new StringBuilder("# Director de arte · informe\n\n");

            if (Array.IndexOf(args, "--tune") >= 0)
            {
                float best = -1; float[] pick = null;
                foreach (var bp in new[] { .05f, .1f, .15f })
                foreach (var wp in new[] { .25f, .35f, .5f })
                foreach (var g in new[] { .9f, 1.2f })
                foreach (var ci in new[] { .5f, .7f })
                foreach (var ae in new[] { 3f, 5f })
                {
                    ink.blackPoint = bp; ink.whitePoint = wp; ink.gamma = g; ink.contourInk = ci; ink.autoExposure = ae;
                    float total = 0;
                    foreach (var s in Shots) { yield return Frame(s); total += Measure(s).score; }
                    if (total > best) { best = total; pick = new[] { bp, wp, g, ci, ae }; }
                }
                ink.blackPoint = pick[0]; ink.whitePoint = pick[1]; ink.gamma = pick[2]; ink.contourInk = pick[3]; ink.autoExposure = pick[4];
                string json = JsonUtility.ToJson(new Preset { blackPoint = pick[0], whitePoint = pick[1], gamma = pick[2], contourInk = pick[3], autoExposure = pick[4] }, true);
                string presetPath = Arg("--preset-out", Path.Combine(outDir, "LookPreset.json"));
                File.WriteAllText(presetPath, json);
                report.Append($"Curva elegida (media {best / Shots.Length:0.00}): blackPoint {pick[0]}, whitePoint {pick[1]}, gamma {pick[2]}, contorno {pick[3]}, autoexposición {pick[4]}.\n");
                report.Append($"Preset escrito en `{presetPath}`.\n\n");
            }

            report.Append("| Plano | Nota | Exposición | Espacio negativo | Rango | Contraste del sujeto | Definición |\n|---|---|---|---|---|---|---|\n");
            var thumbs = new List<Texture2D>(); var details = new StringBuilder();
            foreach (var s in Shots)
            {
                yield return Frame(s);
                var m = Measure(s);
                var ascii = Capture(); File.WriteAllBytes(Path.Combine(outDir, s.name + ".png"), ascii.EncodeToPNG());
                ink.enabledEffect = false; yield return Frame(s);
                var raw = Capture(); File.WriteAllBytes(Path.Combine(outDir, s.name + "-3d.png"), raw.EncodeToPNG());
                ink.enabledEffect = true;
                thumbs.Add(ascii); thumbs.Add(raw);
                string verdict = m.score >= .8f && m.notes.Count == 0 ? "aprobado" : m.score >= .6f ? "revisar" : "rehacer";
                report.Append($"| {s.name} | **{m.score:0.00}** {verdict} | {m.exposure:0.00} | {m.negative:P0} | {m.range:0.00} | {m.focal:0.0}× | {m.definition:P0} |\n");
                details.Append($"\n### {s.name}\n_{s.intent}_\n\n");
                if (m.notes.Count == 0) details.Append("Sin observaciones.\n");
                foreach (var n in m.notes) details.Append("- " + n + "\n");
            }
            report.Append(details);
            report.Append("\nObjetivos: exposición 0,12–0,32 · espacio negativo 30–70 % · rango ≥ 0,55 · sujeto ≥ 2,2× el fondo · definición del sujeto ≥ 45 % de celdas de forma.\n");
            File.WriteAllText(Path.Combine(outDir, "informe.md"), report.ToString());
            File.WriteAllBytes(Path.Combine(outDir, "hoja-de-contactos.png"), Sheet(thumbs).EncodeToPNG());
            Debug.Log("ART_DIRECTOR_DONE " + outDir);
            Application.Quit(0);
        }

        [Serializable] class Preset { public float blackPoint, whitePoint, gamma, contourInk, autoExposure; }

        IEnumerator Frame(Shot s)
        {
            game.player.Teleport(s.eye, s.yaw);
            game.player.view.transform.localPosition = new Vector3(0, s.height, 0);
            game.player.view.transform.localRotation = Quaternion.Euler(s.pitch, 0, 0);
            game.player.view.fieldOfView = s.fov;
            yield return null; yield return new WaitForEndOfFrame();
        }

        Texture2D Capture() => ScreenCapture.CaptureScreenshotAsTexture();

        Metrics Measure(Shot s)
        {
            var rt = ink.Cells; var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, false);
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply(false);
            RenderTexture.active = prev;
            var px = t.GetPixels(); Destroy(t);

            var box = FocalBox(s.focal, rt.width, rt.height);
            var tones = new List<float>(px.Length);
            float inSum = 0, outSum = 0, shapeIn = 0; int inN = 0, outN = 0, dark = 0;
            for (int y = 0; y < rt.height; y++)
                for (int x = 0; x < rt.width; x++)
                {
                    var c = px[y * rt.width + x]; float tone = c.g;
                    tones.Add(tone); if (tone < .05f) dark++;
                    if (box.Contains(new Vector2(x + .5f, y + .5f))) { inSum += tone; inN++; if (c.b > .5f) shapeIn++; }
                    else { outSum += tone; outN++; }
                }
            tones.Sort();
            var m = new Metrics { notes = new List<string>() };
            m.exposure = tones.Average();
            m.negative = dark / (float)tones.Count;
            m.range = tones[(int)(tones.Count * .95f)] - tones[(int)(tones.Count * .05f)];
            m.focal = inN > 0 ? (inSum / inN) / Mathf.Max(outSum / Mathf.Max(outN, 1), .01f) : 0;
            m.definition = inN > 0 ? shapeIn / inN : 0;
            float sExp = Band(m.exposure, .12f, .32f, .12f), sNeg = Band(m.negative, .3f, .7f, .25f);
            float sRange = Mathf.Min(1, m.range / .55f), sFocal = Mathf.Min(1, m.focal / 2.2f), sDef = Mathf.Min(1, m.definition / .45f);
            m.score = (sExp + sNeg + sRange + sFocal * 1.5f + sDef * 1.5f) / 6;
            if (sExp < .7f) m.notes.Add(m.exposure < .12f
                ? "Subexpuesto: sube la luz clave del plano o baja blackPoint; ahora solo se leen puntos."
                : "Sobreexpuesto: baja el ambiente o sube blackPoint/gamma; la textura invade todo.");
            if (sNeg < .7f) m.notes.Add(m.negative < .3f
                ? "Poco espacio negativo: oscurece superficies secundarias (paredes, suelo) para que el negro respire."
                : "El plano se pierde en negro: añade un relleno o rebote que dibuje el espacio.");
            if (sRange < .7f) m.notes.Add("Imagen plana: separa más la luz clave del ambiente.");
            if (sFocal < .7f) m.notes.Add($"El ojo no va a «{s.focal}»: luz de recorte sobre el sujeto u oscurece el fondo.");
            if (sDef < .7f) m.notes.Add($"Silueta de «{s.focal}» poco definida: luz lateral/recorte para crear bordes, o sube contourInk.");
            return m;
        }

        static float Band(float x, float lo, float hi, float soft)
            => x < lo ? Mathf.Max(0, 1 - (lo - x) / soft) : x > hi ? Mathf.Max(0, 1 - (x - hi) / soft) : 1;

        // Screen rectangle of the focal object, in grid cells (origin bottom-left like the cell texture).
        Rect FocalBox(string name, int cols, int rows)
        {
            var go = GameObject.Find(name);
            if (!go) return Rect.zero;
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return Rect.zero;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var cam = game.player.view; Vector2 min = Vector2.one * 9, max = -Vector2.one * 9;
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2 - Vector3.one);
                var v = cam.WorldToViewportPoint(corner);
                if (v.z <= 0) continue;
                min = Vector2.Min(min, v); max = Vector2.Max(max, v);
            }
            min = Vector2.Max(min, Vector2.zero); max = Vector2.Min(max, Vector2.one);
            return max.x > min.x && max.y > min.y ? Rect.MinMaxRect(min.x * cols, min.y * rows, max.x * cols, max.y * rows) : Rect.zero;
        }

        // Two columns per shot: ASCII and the raw 3D frame it came from.
        static Texture2D Sheet(List<Texture2D> shots)
        {
            const int w = 576, h = 360, gap = 8;
            int rows = (shots.Count + 1) / 2;
            var sheet = new Texture2D(w * 2 + gap * 3, rows * (h + gap) + gap, TextureFormat.RGB24, false);
            var fill = Enumerable.Repeat(new Color32(20, 20, 22, 255), sheet.width * sheet.height).ToArray();
            sheet.SetPixels32(fill);
            for (int i = 0; i < shots.Count; i++)
            {
                int ox = gap + (i % 2) * (w + gap), oy = sheet.height - (i / 2 + 1) * (h + gap);
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    sheet.SetPixel(ox + x, oy + y, shots[i].GetPixelBilinear((x + .5f) / w, (y + .5f) / h));
            }
            sheet.Apply(false);
            return sheet;
        }
    }
}
