"""Voices every spoken line with Kokoro (offline) into Assets/Resources/Voices/<key>.wav.

Lines are read straight from the C# sources: Say(...) dialogue in DemoGame.cs and the whispered
Uncanny lines in HospitalWorld*.cs. The key is FNV-1a of "SPEAKER\\ntext" (DemoGame.VoiceKey does the same),
so editing a line only regenerates that line. Stage directions in [brackets] are not spoken.

Run from the project root:  ~/.venvs/kokoro/bin/python Tools/voices.py
"""
import os, re, subprocess, sys, tempfile
import numpy as np
from kokoro_onnx import Kokoro

MODEL = os.path.expanduser("~/.cache/hyperframes/tts/models/kokoro-v1.0.onnx")
VOICES_BIN = os.path.expanduser("~/.cache/hyperframes/tts/voices/voices-v1.0.bin")
OUT = "Assets/Resources/Voices"

# Spanish has one female and two male Kokoro voices; blending each with a related voice of the same
# gender, plus speed and pitch, gives every character a timbre of their own. Narration is not voiced.
CAST = {
    "INÉS":                    dict(mix={"ef_dora": 1.0}, speed=.95, pitch=1.0),
    "LA MUJER DE LA SILLA":    dict(mix={"ef_dora": .5, "if_sara": .5}, speed=.82, pitch=.93),
    "LUCÍA":                   dict(mix={"ef_dora": .55, "pf_dora": .45}, speed=.97, pitch=1.04),
    "EL MÉDICO":               dict(mix={"em_alex": 1.0}, speed=1.08, pitch=1.0),
    "EL HOMBRE":               dict(mix={"em_santa": .7, "im_nicola": .3}, speed=.85, pitch=.95),
    "EL HOMBRE DE LA ESQUINA": dict(mix={"em_santa": .5, "pm_santa": .5}, speed=.78, pitch=.86),
    "RADIO DE EMERGENCIAS":    dict(mix={"em_alex": .6, "pm_alex": .4}, speed=1.0, pitch=1.0, radio=True),
}
# Whispered lines are attached to characters by object, not by speaker name.
WHISPERER = {"Esquina": "EL HOMBRE DE LA ESQUINA"}


def key(speaker, text):
    h = 2166136261
    for b in (speaker + "\n" + text).encode("utf-8"):
        h = ((h ^ b) * 16777619) & 0xFFFFFFFF
    return f"{h:08x}"


def literals(code):
    """C# string literals in a piece of code, unescaped."""
    unescape = lambda t: re.sub(r"\\(.)", lambda m: {"n": "\n", "t": "\t"}.get(m.group(1), m.group(1)), t)
    return [unescape(m.group(1)) for m in re.finditer(r'"((?:[^"\\]|\\.)*)"', code)]


def call_args(src, start):
    """Top-level arguments of the call whose '(' is at src[start]."""
    depth, i, args, cur, s = 0, start, [], "", None
    while i < len(src):
        c = src[i]
        if s:
            cur += c
            if c == "\\": cur += src[i + 1]; i += 1
            elif c == s: s = None
        elif c == '"': s = c; cur += c
        elif c in "([{": depth += 1; cur += c if depth > 1 else ""
        elif c in ")]}":
            depth -= 1
            if depth == 0: args.append(cur); return args
            cur += c
        elif c == "," and depth == 1: args.append(cur); cur = ""
        else: cur += c
        i += 1
    return args


def dialogue():
    lines = []
    src = open("Assets/Scripts/DemoGame.cs", encoding="utf-8").read()
    for m in re.finditer(r"\bSay\(", src):
        args = call_args(src, m.end() - 1)
        if len(args) < 2: continue
        speakers, bodies = literals(args[0]), literals(args[1])
        if not speakers: continue
        pairs = zip(speakers, bodies) if len(speakers) == len(bodies) else ((speakers[0], b) for b in bodies)
        lines += [(s, b) for s, b in pairs if s in CAST]
    for path in ("Assets/Scripts/HospitalWorld.cs", "Assets/Scripts/HospitalWorld.Horror.cs"):
        src = open(path, encoding="utf-8").read()
        for m in re.finditer(r'displayName\s*=\s*"([^"]+)"(.*?)lines\s*=\s*new\[\]\s*\{(.*?)\};', src, re.S):
            lines += [(m.group(1), t) for t in literals(m.group(3)) if m.group(1) in CAST]
        for m in re.finditer(r'Find\("(\w+)"\).*?lines\s*=\s*new\[\]\s*\{(.*?)\};', src, re.S):
            if m.group(1) in WHISPERER: lines += [(WHISPERER[m.group(1)], t) for t in literals(m.group(2))]
    return list(dict.fromkeys(lines))


def speakable(text):
    text = re.sub(r"\[[^\]]*\]", " ", text)           # stage directions are read, not heard
    text = text.replace("«", "").replace("»", "").replace("…", "... ")
    return re.sub(r"\s+", " ", text).strip()


def main():
    os.makedirs(OUT, exist_ok=True)
    tts = Kokoro(MODEL, VOICES_BIN)
    wanted = {}
    for speaker, text in dialogue():
        wanted[key(speaker, text)] = (speaker, text)
    for f in os.listdir(OUT):                          # drop lines that no longer exist
        if f.endswith(".wav") and f[:-4] not in wanted and not f.startswith("lengua_"):
            os.remove(os.path.join(OUT, f)); meta = os.path.join(OUT, f + ".meta")
            if os.path.exists(meta): os.remove(meta)
    made = 0
    for k, (speaker, text) in wanted.items():
        dest = os.path.join(OUT, k + ".wav")
        if os.path.exists(dest): continue
        cast = CAST[speaker]
        style = sum(tts.get_voice_style(v) * w for v, w in cast["mix"].items())
        audio, rate = tts.create(speakable(text), voice=style, speed=cast["speed"], lang="es")
        with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
            pcm = (np.clip(audio, -1, 1) * 32767).astype("<i2")
            import wave
            with wave.open(tmp.name, "wb") as w:
                w.setnchannels(1); w.setsampwidth(2); w.setframerate(rate); w.writeframes(pcm.tobytes())
        p = cast["pitch"]
        filters = [f"asetrate={int(rate * p)}", f"aresample={rate}", f"atempo={1 / p:.4f}"] if p != 1 else []
        if cast.get("radio"): filters += ["highpass=f=450", "lowpass=f=2800", "acrusher=bits=10:mix=.35", "volume=1.6"]
        filters += ["apad=pad_dur=0.15", "loudnorm=I=-18:TP=-2"]
        subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-i", tmp.name, "-af", ",".join(filters),
                        "-ar", "24000", "-ac", "1", "-c:a", "pcm_s16le", dest], check=True)
        os.remove(tmp.name); made += 1
        print(f"{k} {speaker}: {speakable(text)[:60]}")
    print(f"{len(wanted)} lines, {made} generated")


# The angels pray in no language: invented syllables, a deep male and a female voice, one of them reversed,
# in a long stone echo. Assets/Resources/Voices/lengua_<n>.wav, played in order by Heaven.cs.
TONGUES = [
    "Ehl vaoth sarúq, nimmah tal qoreth. Ishva, ishva, ombrael.",
    "Qadh mirún ezzeroth, ha ha veleth samm. Uriah tholom.",
    "Nai ruach keddemoth, anna ve anna. Zarith ombu kal.",
    "Thoth amen thoreth, saba saba. Elim gaeroth nun.",
    "Imm velach orrun, kethem, kethem ishrai. Abaddo saruq.",
    "Ommah tirzeh, vael vael, harashim dov. Lehem ad iyyon.",
]


def tongues(tts):
    for i, line in enumerate(TONGUES):
        dest = os.path.join(OUT, f"lengua_{i}.wav")
        if os.path.exists(dest): continue
        parts = []
        for voice in ("em_santa", "ef_dora"):
            audio, rate = tts.create(line, voice=voice, speed=.72, lang="es")
            f = tempfile.NamedTemporaryFile(suffix=".wav", delete=False).name
            import wave
            with wave.open(f, "wb") as w:
                w.setnchannels(1); w.setsampwidth(2); w.setframerate(rate)
                w.writeframes((np.clip(audio, -1, 1) * 32767).astype("<i2").tobytes())
            parts.append(f)
        graph = (f"[0]asetrate={int(rate * .62)},aresample={rate},atempo=1.2[a];"
                 f"[1]asetrate={int(rate * .82)},aresample={rate},areverse,volume=0.7[b];"
                 "[a][b]amix=inputs=2:duration=longest,aecho=0.8:0.75:140|310|620:0.45|0.3|0.2,"
                 "lowpass=f=5000,apad=pad_dur=0.6,loudnorm=I=-17:TP=-2")
        subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-i", parts[0], "-i", parts[1], "-filter_complex", graph,
                        "-ar", "24000", "-ac", "1", "-c:a", "pcm_s16le", dest], check=True)
        for f in parts: os.remove(f)
        print("lengua", i)


if __name__ == "__main__":
    main()
    tongues(Kokoro(MODEL, VOICES_BIN))
