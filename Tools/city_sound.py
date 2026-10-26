"""The burning city heard through the common-room window: a 40-second loop of fire roar, distant gunfire (single shots
and bursts), screams and deranged laughter. Screams and laughter are Kokoro voices, stretched, pitched and pushed far
away with filtering and echo; shots and fire are synthesised. Output: Assets/Resources/Sounds/ciudad.wav

Run from the project root:  ~/.venvs/kokoro/bin/python Tools/city_sound.py
"""
import os, subprocess, tempfile, wave
import numpy as np
from kokoro_onnx import Kokoro

RATE, SECONDS = 22050, 40
OUT = "Assets/Resources/Sounds/ciudad.wav"
rng = np.random.default_rng(13)
tts = Kokoro(os.path.expanduser("~/.cache/hyperframes/tts/models/kokoro-v1.0.onnx"),
             os.path.expanduser("~/.cache/hyperframes/tts/voices/voices-v1.0.bin"))


def write(path, x):
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype("<i2").tobytes())


def read(path):
    with wave.open(path) as w:
        return np.frombuffer(w.readframes(w.getnframes()), "<i2").astype(np.float32) / 32767


def voice(text, voices, speed, filters):
    style = sum(tts.get_voice_style(v) * k for v, k in voices.items())
    audio, rate = tts.create(text, voice=style, speed=speed, lang="es")
    a, b = tempfile.mktemp(suffix=".wav"), tempfile.mktemp(suffix=".wav")
    with wave.open(a, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(rate)
        w.writeframes((np.clip(audio, -1, 1) * 32767).astype("<i2").tobytes())
    subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-i", a, "-af", filters, "-ar", str(RATE), "-ac", "1", b], check=True)
    x = read(b); os.remove(a); os.remove(b); return x


def place(mix, clip, at, gain):
    i = int(at * RATE) % len(mix)
    n = min(len(clip), len(mix) - i)
    mix[i:i + n] += clip[:n] * gain
    if n < len(clip): mix[:len(clip) - n] += clip[n:] * gain   # wrap: the loop has no seam


mix = np.zeros(RATE * SECONDS, np.float32)

# Fire: brown noise with a slow surge.
brown = np.cumsum(rng.normal(0, 1, len(mix))); brown -= np.convolve(brown, np.ones(4000) / 4000, "same")
t = np.arange(len(mix)) / RATE
mix += brown / np.abs(brown).max() * .25 * (0.7 + 0.3 * np.sin(2 * np.pi * t / SECONDS * 3))


def shot():
    n = int(RATE * .6); x = rng.normal(0, 1, n) * np.exp(-np.arange(n) / RATE * 28)
    x[: int(RATE * .004)] *= 3
    return np.convolve(x, np.ones(6) / 6, "same").astype(np.float32)


at = 0.7
while at < SECONDS:
    if rng.random() < .35:   # a burst
        for k in range(rng.integers(3, 8)): place(mix, shot(), at + k * .11, .55)
    else:
        place(mix, shot(), at, .7)
    at += rng.uniform(1.2, 4.5)

far = "lowpass=f=2400,highpass=f=180,aecho=0.8:0.6:180|420|900:0.45|0.3|0.2"
screams = [("¡Aaaaaaaaaah!", {"ef_dora": 1}, .7, "asetrate=27000,aresample=24000,vibrato=f=6:d=0.3," + far),
           ("¡Nooooooooo!", {"em_alex": 1}, .6, "asetrate=30000,aresample=24000,vibrato=f=5:d=0.4," + far),
           ("¡Socorro! ¡Socorro!", {"if_sara": .5, "ef_dora": .5}, .9, "asetrate=28000,aresample=24000," + far),
           ("¡Aaaaaaah! ¡Mi hijo!", {"ef_dora": .6, "pf_dora": .4}, .75, "asetrate=29000,aresample=24000,vibrato=f=7:d=0.35," + far)]
laughs = [("¡Ja, ja, ja, ja, ja, ja, ja, ja!", {"em_santa": 1}, 1.25, "asetrate=21000,aresample=24000,vibrato=f=9:d=0.5," + far),
          ("¡Ji, ji, ji, ji, ji, ji, ji!", {"ef_dora": 1}, 1.35, "asetrate=31000,aresample=24000,vibrato=f=11:d=0.4," + far),
          ("¡Ja, ja, ja! ¡Se acabó! ¡Ja, ja, ja!", {"em_alex": .6, "pm_alex": .4}, 1.2, "asetrate=19500,aresample=24000,vibrato=f=8:d=0.6," + far)]
clips = [(voice(*s), .55) for s in screams] + [(voice(*l), .45) for l in laughs]
for i, at in enumerate(np.sort(rng.uniform(0, SECONDS, 11))):
    clip, gain = clips[i % len(clips)]
    place(mix, clip, at, gain * rng.uniform(.6, 1))

mix /= np.abs(mix).max() * 1.15
os.makedirs(os.path.dirname(OUT), exist_ok=True)
write(OUT, mix)
print("wrote", OUT, f"{SECONDS}s")
