"""The window's sound: sirens and screams from the burning city, over the synthetic shots and laughter.

Real recordings from Wikimedia Commons (downloaded once into Tools/sounds/cache):
  Civil-defense-siren-waver.ogg (public domain, Techtonic) · American police siren i.ogg (public domain, lezer)
  20080618KamagasakiRiot.ogg (CC0, Kamagasaki450) · Scream-c/d/e.wav (CC BY-SA 4.0, Raspberry Pi Foundation, Andy Oakley)
Output: Assets/Resources/Sounds/ciudad.wav, a 40 s seamless loop (mono, 22 kHz). Run with ~/.venvs/kokoro/bin/python.
"""
import os, subprocess, urllib.request
import numpy as np

SR, LEN, FADE = 22050, 40, 3
CACHE = "Tools/sounds/cache"
SRC = {
    "siren-civil": "https://upload.wikimedia.org/wikipedia/commons/6/69/Civil-defense-siren-waver.ogg",
    "siren-police": "https://upload.wikimedia.org/wikipedia/commons/a/ae/American_police_siren_i.ogg",
    "crowd": "https://upload.wikimedia.org/wikipedia/commons/0/07/20080618KamagasakiRiot.ogg",
    "scream-c": "https://upload.wikimedia.org/wikipedia/commons/d/dc/Scream-c.wav",
    "scream-d": "https://upload.wikimedia.org/wikipedia/commons/7/73/Scream-d.wav",
    "scream-e": "https://upload.wikimedia.org/wikipedia/commons/9/92/Scream-e.wav",
}

def load(name, start=0, dur=None, af="anull"):
    path = f"{CACHE}/{name}"
    if not os.path.exists(path):
        os.makedirs(CACHE, exist_ok=True)
        req = urllib.request.Request(SRC[name], headers={"User-Agent": "hineni-build/1.0"})
        open(path, "wb").write(urllib.request.urlopen(req).read())
    cmd = ["ffmpeg", "-loglevel", "error", "-ss", str(start)] + (["-t", str(dur)] if dur else []) + \
          ["-i", path, "-af", af, "-ac", "1", "-ar", str(SR), "-f", "f32le", "-"]
    a = np.frombuffer(subprocess.run(cmd, capture_output=True, check=True).stdout, dtype=np.float32).copy()
    return a / (np.abs(a).max() + 1e-9)

def far(extra=""):   # distance: highs gone, a street's worth of echo
    return "lowpass=f=2600,aecho=0.8:0.6:180|420:0.35|0.2" + extra

n = (LEN + FADE) * SR
mix = np.zeros(n, np.float32)
def put(a, at, gain):
    at = int(at * SR); a = a[: max(0, n - at)]; mix[at: at + len(a)] += a * gain

put(load("crowd", 130, LEN + FADE, far()), 0, .55)                               # the crowd, roaring
put(load("siren-civil", 6, LEN + FADE, far(",volume=1")), 0, .42)                 # the city's siren, rising and falling
police = load("siren-police", 0, None, far(",highpass=f=300"))
put(police, 4, .3); put(load("siren-police", 0, None, far(",asetrate=22050*0.9,aresample=22050")), 24, .22)
rng = np.random.default_rng(41)
screams = [load(f"scream-{c}", 0, None, far()) for c in "cde"]
for t in np.sort(rng.uniform(0, LEN, 18)):                                       # screams, near and far
    s = screams[rng.integers(3)]
    pitch = rng.uniform(.78, 1.12); idx = np.arange(0, len(s) - 1, pitch)
    put(np.interp(idx, np.arange(len(s)), s).astype(np.float32), t, rng.uniform(.25, .7))
put(load("../ciudad-sintetica.wav", 0, LEN + FADE), 0, .35)                    # shots, laughter, fire
x = FADE * SR; r = np.linspace(0, 1, x, dtype=np.float32)
loop = mix[x:].copy(); loop[-x:] = loop[-x:] * np.cos(r * np.pi / 2) + mix[:x] * np.sin(r * np.pi / 2)
loop *= .9 / np.abs(loop).max()
subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-f", "f32le", "-ar", str(SR), "-ac", "1", "-i", "-", "-c:a", "pcm_s16le",
                "Assets/Resources/Sounds/ciudad.wav"], input=loop.tobytes(), check=True)
print(f"ciudad.wav {len(loop) / SR:.1f} s")
