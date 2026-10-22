"""Original synthetic placeholder SFX; no imported recordings or samples."""
from pathlib import Path
import json
import wave
import numpy as np

RATE = 48000
OUT = Path(__file__).resolve().parents[2] / 'Assets' / 'Audio' / 'SFX'
RNG = np.random.default_rng(121026)

def save(name, x):
    x = np.asarray(x, dtype=np.float64)
    x -= x.mean()
    fade = min(len(x) // 4, 480)
    x[:fade] *= np.linspace(0, 1, fade)
    x[-fade:] *= np.linspace(1, 0, fade)
    x *= 0.65 / max(1e-9, np.max(np.abs(x)))
    assert np.isfinite(x).all() and np.max(np.abs(x)) < 1
    stereo = np.repeat(x[:, None], 2, axis=1)
    with wave.open(str(OUT / (name + '.wav')), 'wb') as f:
        f.setnchannels(2)
        f.setsampwidth(2)
        f.setframerate(RATE)
        f.writeframes(np.rint(stereo * 32767).astype('<i2').tobytes())
    return {'file': name + '.wav', 'seconds': round(len(x) / RATE, 3),
            'peak_dbfs': round(20 * np.log10(np.max(np.abs(x))), 2)}

def chime(notes, spacing, duration):
    x = np.zeros(round(duration * RATE))
    for i, note in enumerate(notes):
        start = round(i * spacing * RATE)
        t = np.arange(len(x) - start) / RATE
        f = 440 * 2 ** ((note - 69) / 12)
        env = (1 - np.exp(-t / .004)) * np.exp(-t / .23)
        x[start:] += env * (np.sin(2 * np.pi * f * t) + .18 * np.sin(4 * np.pi * f * t))
    return x

OUT.mkdir(parents=True, exist_ok=True)
report = []
t = np.arange(round(.085 * RATE)) / RATE
report.append(save('UI_Click', np.sin(2 * np.pi * (950*t - 1600*t*t)) * np.exp(-t/.014)))
t = np.arange(round(.23 * RATE)) / RATE
noise = RNG.normal(size=len(t))
noise = np.convolve(noise, np.ones(9)/9, mode='same')
swish = noise * (1 - np.exp(-t/.006)) * np.exp(-t/.032)
impact_time = np.maximum(0, t - .07)
impact = (t >= .07) * np.sin(2*np.pi*(145*impact_time - 180*impact_time**2)) * np.exp(-impact_time/.026)
report.append(save('Card_Place', swish*.6 + impact*.55))
report.append(save('Trick_Win', chime([74, 81], .095, .65)))
report.append(save('Round_Success', chime([62, 65, 69, 74], .11, 1.0)))
report.append(save('Round_Failure', chime([65, 62, 57], .12, .9)))
print(json.dumps(report, indent=2))
