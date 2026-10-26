using System;
using System.Collections.Generic;
using UnityEngine;

namespace LosQueQuedan
{
    // Languages. Spanish is the source: every visible string is written in Spanish in the code and looked up here
    // (Resources/Lang/<code>.json, built by Tools/i18n.py). A missing entry falls back to the Spanish text.
    public static class Loc
    {
        public static readonly string[] Codes = { "es", "en", "pt", "zh", "ja" };
        public static readonly string[] Names = { "Español", "English", "Português", "中文", "日本語" };
        public static string Lang { get; private set; } = "en";        // text
        public static string VoiceLang { get; private set; } = "ja";   // spoken lines, chosen separately
        public static bool CJK => Lang == "zh" || Lang == "ja";
        static readonly Dictionary<string, Dictionary<string, string>> tables = new Dictionary<string, Dictionary<string, string>>();
        static bool ready;

        [Serializable] class Entry { public string es, t; }
        [Serializable] class Table { public Entry[] items; }

        // Defaults: English text, Japanese voices. Both are remembered (".v2": choices saved before these defaults are dropped); --lang / --voice override them (QA).
        public static void Init()
        {
            if (ready) return;
            string text = "en", voice = "ja";
            try { text = PlayerPrefs.GetString("lang.v2", text); voice = PlayerPrefs.GetString("voice.v2", voice); } catch { }
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "--lang"); if (i >= 0 && i + 1 < args.Length) text = args[i + 1];
            int j = Array.IndexOf(args, "--voice"); if (j >= 0 && j + 1 < args.Length) voice = args[j + 1];
            if (Application.platform == RuntimePlatform.WebGLPlayer)   // links can carry them too: ?lang=pt&voice=es
            {
                var url = Application.absoluteURL;
                string Param(string k) { int p = url.IndexOf(k + "="); return p < 0 ? null : url.Substring(p + k.Length + 1).Split('&', '#')[0]; }
                text = Param("lang") ?? text; voice = Param("voice") ?? voice;
            }
            Set(Array.IndexOf(Codes, text) >= 0 ? text : "en", false);
            SetVoice(Array.IndexOf(Codes, voice) >= 0 ? voice : "ja", false);
        }
        public static void Set(string code, bool save)
        {
            ready = true; Lang = code;
            if (save) try { PlayerPrefs.SetString("lang.v2", code); PlayerPrefs.Save(); } catch { }
        }
        public static void SetVoice(string code, bool save)
        {
            VoiceLang = code;
            if (save) try { PlayerPrefs.SetString("voice.v2", code); PlayerPrefs.Save(); } catch { }
        }
        static Dictionary<string, string> TableFor(string code)
        {
            if (tables.TryGetValue(code, out var t)) return t;
            t = new Dictionary<string, string>();
            var file = Resources.Load<TextAsset>("Lang/" + code);
            if (file) foreach (var e in JsonUtility.FromJson<Table>(file.text).items) if (!string.IsNullOrEmpty(e.t)) t[e.es] = e.t;
            return tables[code] = t;
        }
        public static string T(string es) => In(Lang, es);
        // The same Spanish line in another language (voices are looked up in the voice language).
        public static string In(string code, string es)
        {
            if (string.IsNullOrEmpty(es) || code == "es") return es;
            return TableFor(code).TryGetValue(es, out var t) ? t : es;
        }
        public static string F(string es, params object[] args) => string.Format(T(es), args);

        // East Asian wide characters take two cells of the text grid.
        public static bool Wide(char c) =>
            c >= 0x1100 && (c <= 0x115F || (c >= 0x2E80 && c <= 0xA4CF && c != 0x303F) || (c >= 0xAC00 && c <= 0xD7A3) ||
                            (c >= 0xF900 && c <= 0xFAFF) || (c >= 0xFE30 && c <= 0xFE4F) || (c >= 0xFF00 && c <= 0xFF60) || (c >= 0xFFE0 && c <= 0xFFE6));
        public static int W(string s) { int n = 0; if (s != null) foreach (var c in s) n += Wide(c) ? 2 : 1; return n; }
        // The first characters of s that fit in a given width.
        public static string Fit(string s, int width)
        {
            int n = 0; for (int i = 0; i < s.Length; i++) { n += Wide(s[i]) ? 2 : 1; if (n > width) return s.Substring(0, i); }
            return s;
        }
    }
}
