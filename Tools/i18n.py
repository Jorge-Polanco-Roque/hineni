"""Localisation table builder. Spanish is the source language: every visible string is a Spanish literal in the code,
looked up at runtime by Loc.T. This tool

  extract  → Tools/i18n/source.json   every user-facing literal (object names, ids, paths and code keys are skipped)
  build    → Assets/Resources/Lang/<code>.json from Tools/i18n/<code>.json ({spanish: translation})
  chars    → Tools/i18n/chars-<code>.txt, the wide characters a language needs (for the glyph atlas and font subsets)

The Witness speaks backwards: its lines are stored reversed in the code, translated forwards and reversed again here.
Run from the project root:  python3 Tools/i18n.py extract|build|chars
"""
import json, os, re, sys

FILES = ["DemoGame", "HorrorDirector", "HospitalWorld", "HospitalWorld.Horror", "Acts", "Endings", "Controls", "Heaven"]
CODE_CALLS = r"(Cube|Cylinder|Prim|Mat|Prop|Character|GameObject|Find|Bone|Load<\w+>|Load|Shader\.Find|GetField|GetMethod|GetProperty|" \
             r"SetFloat|SetColor|SetTexture|SetTextureOffset|EnableKeyword|GetBuiltinResource<Font>|AudioClip\.Create|Noise|MakeSound|" \
             r"Debug\.Log|StartsWith|EndsWith|Contains|IndexOf|Interact|TryInteract|WritingText|GetString|SetString|GetInt|SetInt|" \
             r"Shapes|Voice|PlayVoice|EndWith|Seen|Atlas|LoadAtlas|Wing|SetOverrideTag|Split|Replace|Substring)"
SKIP_BEFORE = [re.compile(r"\b" + CODE_CALLS + r"\(\s*$"), re.compile(r"Target\([^,()]+,\s*$"), re.compile(r"case\s*$"),
               re.compile(r"[!=]=\s*$"), re.compile(r"\bname\s*=\s*$"), re.compile(r"\bid\s*=\s*$"), re.compile(r"\+\s*$"),
               re.compile(r"Wing(Door)?\([^;]*,\s*$"), re.compile(r"key\s*\+\s*$")]
LITERAL = re.compile(r'(?<![\w@$])"((?:[^"\\]|\\.)*)"')


def unescape(t):
    return re.sub(r"\\(.)", lambda m: {"n": "\n", "t": "\t"}.get(m.group(1), m.group(1)), t)


def natural(t):
    if "{0}" in t: return True                                              # format templates
    if len(re.findall(r"[A-Za-zÁÉÍÓÚáéíóúÑñÜü]", t)) < 2: return False
    if re.fullmatch(r"[a-z0-9]+([-_][a-z0-9]+)+", t): return False          # ids: chapel-door, graffiti-word
    if "/" in t and " " not in t: return False                              # resource paths
    if t.startswith("_") or t.startswith("QA_") or t.endswith((".json", ".wav", ".png")): return False
    if re.fullmatch(r"[a-z]+(_[a-z]+)*_?[lr]?", t) and t in {"pelvis", "spine_01", "spine_02", "spine_03", "neck_01", "Head"}: return False
    return True


def extract():
    seen, out = {}, []
    for f in FILES:
        src = open(f"Assets/Scripts/{f}.cs", encoding="utf-8").read()
        witness = set()
        m = re.search(r'displayName="EL TESTIGO".*?lines=new\[\]\{(.*?)\};', src, re.S)
        if m: witness = {unescape(x.group(1)) for x in LITERAL.finditer(m.group(1))}
        for m in LITERAL.finditer(src):
            before = src[max(0, m.start() - 60):m.start()]
            line_start = src.rfind("\n", 0, m.start()) + 1
            prefix = src[line_start:m.start()]
            if prefix.lstrip().startswith("//"): continue
            if any(p.search(before) for p in SKIP_BEFORE): continue
            t = unescape(m.group(1))
            if not natural(t) or t in seen: continue
            seen[t] = True
            item = {"es": t, "ctx": f"{f}:{src.count(chr(10), 0, m.start()) + 1}"}
            if t in witness: item = {"es": t, "forward": t[::-1], "ctx": item["ctx"] + " (EL TESTIGO, se muestra al revés)"}
            out.append(item)
    os.makedirs("Tools/i18n", exist_ok=True)
    json.dump(out, open("Tools/i18n/source.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(len(out), "strings →", "Tools/i18n/source.json")


def build():
    source = json.load(open("Tools/i18n/source.json", encoding="utf-8"))
    for code in ("en", "pt", "zh", "ja"):
        path = f"Tools/i18n/{code}.json"
        if not os.path.exists(path): continue
        tr = json.load(open(path, encoding="utf-8"))
        items, missing = [], 0
        for s in source:
            key = s.get("forward", s["es"])
            t = tr.get(key)
            if not t: missing += 1; continue
            items.append({"es": s["es"], "t": t[::-1] if "forward" in s else t})
        os.makedirs("Assets/Resources/Lang", exist_ok=True)
        json.dump({"items": items}, open(f"Assets/Resources/Lang/{code}.json", "w", encoding="utf-8"), ensure_ascii=False, indent=0)
        print(code, len(items), "entries,", missing, "missing")


def is_wide(c):   # same ranges as Loc.Wide
    o = ord(c)
    return o >= 0x1100 and (o <= 0x115F or (0x2E80 <= o <= 0xA4CF and o != 0x303F) or 0xAC00 <= o <= 0xD7A3 or
                            0xF900 <= o <= 0xFAFF or 0xFE30 <= o <= 0xFE4F or 0xFF00 <= o <= 0xFF60 or 0xFFE0 <= o <= 0xFFE6)


def narrow():
    """Narrow characters the translations use that the ASCII atlas lacks: appended to AsciiGlyphs.txt (line 2)."""
    lines = open("Assets/Resources/AsciiGlyphs.txt", encoding="utf-8").read().split("\n")
    have = set(lines[0] + lines[1])
    need = set()
    for code in ("en", "pt", "zh", "ja"):
        path = f"Assets/Resources/Lang/{code}.json"
        if os.path.exists(path):
            for i in json.load(open(path, encoding="utf-8"))["items"]:
                need |= {c for c in i["t"] if not is_wide(c) and c not in have and c >= " " and c != "\n"}
    if need:
        lines[1] += "".join(sorted(need))
        open("Assets/Resources/AsciiGlyphs.txt", "w", encoding="utf-8").write("\n".join(lines))
    print("added", "".join(sorted(need)), len(lines[0] + lines[1]), "glyphs")


def chars():
    for code in ("zh", "ja"):
        path = f"Assets/Resources/Lang/{code}.json"
        if not os.path.exists(path): continue
        text = "".join(i["t"] for i in json.load(open(path, encoding="utf-8"))["items"])
        wide = sorted({c for c in text + "中文日本語" if is_wide(c)})   # language names, shown in every language
        open(f"Tools/i18n/chars-{code}.txt", "w", encoding="utf-8").write("".join(wide))
        print(code, len(wide), "characters")


if __name__ == "__main__":
    {"extract": extract, "build": build, "chars": chars, "narrow": narrow}[sys.argv[1]]()
