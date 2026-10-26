using System.Collections.Generic;
using UnityEngine;

namespace LosQueQuedan
{
    // Full-screen ASCII pass plus the text grid the interface writes into.
    // The world stays a normal 3D scene; only its image becomes characters.
    [RequireComponent(typeof(Camera))]
    public sealed class InkCamera : MonoBehaviour
    {
        public const int Plain = 0, Panel = 1, Inverted = 2;
        public Shader inkShader;
        public bool enabledEffect = true;   // false = raw 3D image under the text interface
        public bool largeText;
        [Header("Tono")] [Range(0, .5f)] public float blackPoint = .04f;
        [Range(.2f, 1)] public float whitePoint = .7f;
        [Range(.4f, 2.5f)] public float gamma = 1.2f;
        [Range(0, 1)] public float minBrightness = .14f;
        [Header("Forma")] [Range(1, 6)] public float directionalContrast = 3f;
        [Range(1, 6)] public float cellContrast = 1.6f;
        [Range(0, 1)] public float flatThreshold = .22f;
        public string ramp = " .:-=+*#%@";
        [Header("Contorno")] [Range(0, 1)] public float contourInk = .55f;
        [Range(0, 1)] public float contourDepth = .04f;
        [Range(0, 1)] public float contourNormal = .15f;
        public float contourFade = 16;
        [Header("Acabado")] [Range(0, .3f)] public float canvasDots = .08f;
        [Range(0, 1)] public float panelDim = .03f;
        [Range(0, 1)] public float reveal = 1;
        public Color ink = new Color(.89f, .88f, .84f), paper = new Color(.008f, .01f, .01f), accent = new Color(.86f, .2f, .16f);
        [Header("Exposición")] [Range(0, 6)] public float autoExposure = 2.6f;
        [Range(0, 2)] public float localContrast = .9f;
        [Header("Color")] [Range(0, 1)] public float colorAmount = .9f;
        [Range(0, 3)] public float saturation = 1.5f;
        public Color shadowTint = new Color(.36f, .56f, .52f);

        [System.NonSerialized] public float glitch, heaven;   // set by Heaven: tearing 0..1, engraving palette 0/1
        [System.NonSerialized] public Color engravePaper = new Color(.88f, .82f, .68f), engraveInk = new Color(.02f, .015f, .015f), engraveBlood = new Color(.62f, .01f, 0);

        // Last glyph grid (R glyph, G scene tone, B 1 = matched by shape), kept for the art director.
        [System.NonSerialized] public bool keepCells;
        public RenderTexture Cells { get; private set; }
        public int Cols { get; private set; }       // interface grid
        public int Rows { get; private set; }       // fully visible interface rows
        public Vector2Int Mouse { get; private set; }

        Material material;
        RenderTexture mean;
        Texture2D world1, world2, ui1, ui2, ui;
        byte[] cells;
        int uiCols, uiRows, worldCols, worldRows, matchable, glyphCount;
        Vector2Int cellPx, worldPx;   // interface cell (9x18 per scale) and the finer world cell (7x14 per scale)
        readonly Dictionary<char, int> index = new Dictionary<char, int>();
        readonly List<Vector4> shapeA = new List<Vector4>(), shapeB = new List<Vector4>();

        void Awake()
        {
            var lines = Resources.Load<TextAsset>("AsciiGlyphs").text.Replace("\r", "").Split('\n');
            matchable = lines[0].Length;
            string all = lines[0] + lines[1];
            glyphCount = all.Length;
            for (int i = 0; i < all.Length; i++) index[all[i]] = i;
            var preset = Resources.Load<TextAsset>("LookPreset");   // written by ArtDirector --tune
            if (preset) JsonUtility.FromJsonOverwrite(preset.text, this);
            ui1 = LoadAtlas("AsciiAtlas1x"); ui2 = LoadAtlas("AsciiAtlas2x");
            world1 = LoadAtlas("AsciiWorld1x"); world2 = LoadAtlas("AsciiWorldFine");   // Retina: 6x12 physical px, Meslo Bold
            BuildShapes(world2, 6, 12);
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals;
            Layout();
        }

        static Texture2D LoadAtlas(string name)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            t.LoadImage(Resources.Load<TextAsset>(name).bytes);
            t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        // Average coverage of each glyph in a 2x3 grid, normalised so the densest region is 1.
        void BuildShapes(Texture2D atlas, int w, int h)
        {
            var px = atlas.GetPixels32();
            var v = new float[matchable, 6];
            float peak = 0;
            for (int g = 0; g < matchable; g++)
            {
                var count = new float[6];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int top = h - 1 - y;
                        int k = (int)((top + .5f) / h * 3) * 2 + (int)((x + .5f) / w * 2);
                        v[g, k] += px[y * atlas.width + g * w + x].a / 255f; count[k]++;
                    }
                for (int k = 0; k < 6; k++) { v[g, k] /= count[k]; peak = Mathf.Max(peak, v[g, k]); }
            }
            for (int g = 0; g < 96; g++)
            {
                bool ok = g < matchable;
                shapeA.Add(ok ? new Vector4(v[g, 0], v[g, 1], v[g, 2], v[g, 3]) / peak : Vector4.one * 99);
                shapeB.Add(ok ? new Vector4(v[g, 4], v[g, 5], 0, 0) / peak : Vector4.one * 99);
            }
        }

        void Layout()
        {
            int k = Mathf.Max(1, Mathf.RoundToInt(Screen.height / 900f));
            // On Retina the world grid uses the physical pixels: twice the cells, glyphs drawn natively sharp.
            worldPx = k == 1 ? new Vector2Int(7, 14) : new Vector2Int(6 * k / 2, 12 * k / 2);
            worldCols = Mathf.CeilToInt(Screen.width / (float)worldPx.x); worldRows = Mathf.CeilToInt(Screen.height / (float)worldPx.y);
            k *= largeText ? 2 : 1;
            cellPx = new Vector2Int(9 * k, 18 * k);
            int c = Mathf.CeilToInt(Screen.width / (float)cellPx.x), r = Mathf.CeilToInt(Screen.height / (float)cellPx.y);
            Cols = Screen.width / cellPx.x; Rows = Screen.height / cellPx.y;
            if (c == uiCols && r == uiRows && ui) return;
            uiCols = c; uiRows = r;
            if (ui) Destroy(ui);
            ui = new Texture2D(c, r, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            cells = new byte[c * r * 4];
        }

        void Update()
        {
            Layout();
            var m = Input.mousePosition;
            Mouse = new Vector2Int(Mathf.FloorToInt(m.x / cellPx.x), Mathf.FloorToInt((Screen.height - m.y) / cellPx.y));
        }

        // ---- Text grid. Row 0 is the top of the screen. The interface clears and redraws it every frame. ----
        public void Clear() { System.Array.Clear(cells, 0, cells.Length); }

        int Offset(int x, int y) => ((uiRows - 1 - y) * uiCols + x) * 4;
        bool Inside(int x, int y) => x >= 0 && y >= 0 && x < uiCols && y < uiRows;

        public void Put(int x, int y, string text, float bright = 1, bool accentColor = false)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (!Inside(x + i, y)) continue;
                int o = Offset(x + i, y);
                cells[o] = (byte)((index.TryGetValue(text[i], out int g) ? g : index['?']) + 1);
                cells[o + 1] = (byte)(Mathf.Clamp01(bright) * 255);
                cells[o + 3] = (byte)(accentColor ? 255 : 0);
            }
        }

        public void Fill(int x, int y, int w, int h, int mode, float bright = 1)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    if (Inside(i, j)) { int o = Offset(i, j); cells[o + 2] = (byte)mode; if (mode == Inverted) cells[o + 1] = (byte)(bright * 255); }
        }

        public void Frame(int x, int y, int w, int h, float bright, bool fill = true)
        {
            if (fill) Fill(x, y, w, h, Panel);
            Put(x, y, "┌" + new string('─', w - 2) + "┐", bright);
            Put(x, y + h - 1, "└" + new string('─', w - 2) + "┘", bright);
            for (int j = y + 1; j < y + h - 1; j++) { Put(x, j, "│", bright); Put(x + w - 1, j, "│", bright); }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!inkShader || !inkShader.isSupported) { Graphics.Blit(source, destination); return; }
            if (!material) material = new Material(inkShader) { hideFlags = HideFlags.HideAndDontSave };
            Layout();
            ui.SetPixelData(cells, 0); ui.Apply(false);
            var grid = RenderTexture.GetTemporary(worldCols, worldRows, 0, RenderTextureFormat.ARGBHalf);
            grid.filterMode = FilterMode.Point;
            material.SetVector("_Grid", new Vector4(worldCols, worldRows, worldPx.x, worldPx.y));
            material.SetVector("_UIGrid", new Vector4(uiCols, uiRows, cellPx.x, cellPx.y));
            material.SetVector("_Contour", new Vector4(contourInk, contourDepth, contourNormal, contourFade));
            material.SetVector("_Tone", new Vector4(blackPoint, whitePoint, gamma, minBrightness));
            material.SetVector("_Shape", new Vector4(directionalContrast, cellContrast, matchable, glyphCount));
            material.SetVector("_Look", new Vector4(canvasDots, panelDim, reveal, enabledEffect ? 0 : 1));
            material.SetVectorArray("_ShapeA", shapeA);
            material.SetVectorArray("_ShapeB", shapeB);
            var rampGlyphs = new float[16];
            for (int i = 0; i < ramp.Length && i < 16; i++) rampGlyphs[i] = index[ramp[i]];
            material.SetFloatArray("_RampGlyph", rampGlyphs);
            material.SetVector("_Ramp", new Vector4(flatThreshold, Mathf.Min(ramp.Length, 16), 0, 0));
            material.SetColor("_Shadow", shadowTint);
            material.SetVector("_Color", new Vector4(colorAmount, saturation, 0, 0));
            material.SetVector("_Glitch", new Vector4(glitch, Time.unscaledTime, heaven, 0));
            material.SetColor("_EngPaper", engravePaper); material.SetColor("_EngInk", engraveInk); material.SetColor("_EngBlood", engraveBlood);
            material.SetColor("_Ink", ink); material.SetColor("_Paper", paper); material.SetColor("_Accent", accent);
            material.SetTexture("_Glyphs", worldPx.x == 7 ? world1 : world2);
            material.SetTexture("_UIGlyphs", cellPx.x >= 18 ? ui2 : ui1);
            material.SetTexture("_UI", ui);
            if (!mean)
            {
                mean = new RenderTexture(256, 128, 0, RenderTextureFormat.ARGBHalf) { useMipMap = true, autoGenerateMips = true, hideFlags = HideFlags.HideAndDontSave };
                mean.Create();
            }
            Graphics.Blit(source, mean);
            material.SetTexture("_Mean", mean);
            material.SetVector("_Exposure", new Vector4(autoExposure, mean.mipmapCount - 1, localContrast, 0));
            Graphics.Blit(source, grid, material, 0);
            material.SetTexture("_Cells", grid);
            Graphics.Blit(source, destination, material, 1);
            if (keepCells)
            {
                if (!Cells || Cells.width != worldCols || Cells.height != worldRows)
                { if (Cells) Cells.Release(); Cells = new RenderTexture(worldCols, worldRows, 0, RenderTextureFormat.ARGBHalf) { filterMode = FilterMode.Point }; }
                Graphics.Blit(grid, Cells);
            }
            RenderTexture.ReleaseTemporary(grid);
        }

        void OnDestroy() { foreach (var o in new Object[] { material, world1, world2, ui1, ui2, ui, Cells, mean }) if (o) Destroy(o); }
    }
}
