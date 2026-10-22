"""CROSSION original procedural sketches. No external recordings, samples or MIDI.
Outputs are drafts for listening, not automatically imported or connected in Unity.
"""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

RATE = 48000
OUT = Path(__file__).resolve().parents[2] / "AudioDrafts"
RNG = np.random.default_rng(1201026)


def frequency(note):
    return 440.0 * 2.0 ** ((note - 69) / 12.0)


class Track:
    def __init__(self, bpm, bars):
        self.beat = 60.0 / bpm
        self.n = round(bars * 4 * self.beat * RATE)
        self.samples = np.zeros((self.n, 2), np.float64)

    def add(self, signal, beat, gain, pan=0.0):
        signal = np.asarray(signal)
        start = round(beat * self.beat * RATE) % self.n
        stereo = signal[:, None] * gain * np.array([
            np.sqrt((1 - pan) / 2), np.sqrt((1 + pan) / 2)])
        # Periodic tails are folded into the beginning for seamless AudioSource.loop playback.
        used = 0
        while used < len(signal):
            block = min(self.n - start, len(signal) - used)
            self.samples[start:start + block] += stereo[used:used + block]
            used += block
            start = 0

    def pad(self, note, beat, beats, gain, pan):
        duration = beats * self.beat
        t = np.arange(round((duration + 2.4) * RATE)) / RATE
        f = frequency(note)
        env = (1 - np.exp(-t / 0.65)) * np.exp(-np.maximum(0, t - duration) / 0.65)
        sound = np.zeros_like(t)
        for ratio, amplitude in [(1, 1), (2, .20), (3, .065)]:
            for detune in [-.0015, .0015]:
                sound += amplitude * .5 * np.sin(2 * np.pi * f * ratio * (1 + detune) * t)
        sound *= env * (.96 + .04 * np.sin(2 * np.pi * .31 * t))
        self.add(sound, beat, gain, pan)

    def pluck(self, note, beat, gain, pan, bright=False):
        t = np.arange(round(3.6 * RATE)) / RATE
        f = frequency(note)
        attack = 1 - np.exp(-t / .006)
        sound = np.zeros_like(t)
        for harmonic, level, decay in [(1, 1, .9), (2, .32, .38), (3, .13, .22), (5, .035, .12)]:
            sound += level * np.sin(2 * np.pi * f * harmonic * t) * np.exp(-t / decay)
        if bright:
            sound += .10 * np.sin(2 * np.pi * f * 2.003 * t) * np.exp(-t / .6)
        self.add(sound * attack, beat, gain, pan)

    def bell(self, note, beat, gain, pan):
        t = np.arange(round(5 * RATE)) / RATE
        f = frequency(note)
        sound = np.sin(2 * np.pi * f * t + 1.1 * np.sin(2 * np.pi * f * 2 * t) * np.exp(-t / .15))
        sound *= np.exp(-t / 1.15) * (1 - np.exp(-t / .012))
        self.add(sound, beat, gain, pan)

    def bass(self, note, beat, beats, gain):
        duration = beats * self.beat
        t = np.arange(round((duration + .25) * RATE)) / RATE
        env = (1 - np.exp(-t / .035)) * np.exp(-t / (duration * 1.8))
        env *= np.exp(-np.maximum(0, t - duration + .08) / .06)
        f = frequency(note)
        sound = (np.sin(2 * np.pi * f * t) + .16 * np.sin(2 * np.pi * f * 2 * t)) * env
        self.add(sound, beat, gain)

    def kick(self, beat, gain):
        t = np.arange(round(.48 * RATE)) / RATE
        phase = 2 * np.pi * (46 * t + 36 * .028 * (1 - np.exp(-t / .028)))
        self.add(np.sin(phase) * np.exp(-t / .11) * (1 - np.exp(-t / .002)), beat, gain)

    def tick(self, beat, gain, pan=0):
        t = np.arange(round(.12 * RATE)) / RATE
        noise = RNG.normal(0, 1, len(t))
        high = noise - np.convolve(noise, np.ones(9) / 9, mode="same")
        self.add(high * np.exp(-t / .018) * (1 - np.exp(-t / .001)), beat, gain, pan)

    def rim(self, beat, gain):
        t = np.arange(round(.22 * RATE)) / RATE
        sound = (.55 * np.sin(2 * np.pi * 360 * t) + .2 * np.sin(2 * np.pi * 731 * t))
        sound += .10 * RNG.normal(0, 1, len(t))
        self.add(sound * np.exp(-t / .035) * (1 - np.exp(-t / .001)), beat, gain, -.1)

    def render(self, filename):
        dry = self.samples.copy()
        wet = np.zeros_like(dry)
        for seconds, gain in [(.113, .065), (.227, .10), (.379, .075), (.557, .055), (.733, .04), (1.127, .025)]:
            # Circular reverb retains the natural tail at the repeat boundary.
            delayed = np.roll(dry[:, ::-1], round(seconds * RATE), axis=0)
            wet += delayed * gain
        sound = dry + wet
        sound -= sound.mean(axis=0)
        sound = np.tanh(sound * 1.25)
        peak = np.max(np.abs(sound))
        sound *= .75 / max(peak, 1e-9)  # -2.5 dBFS peak headroom.
        path = OUT / filename
        with wave.open(str(path), "wb") as output:
            output.setnchannels(2)
            output.setsampwidth(2)
            output.setframerate(RATE)
            output.writeframes((sound * 32767).astype("<i2").tobytes())
        boundary = float(np.max(np.abs(sound[0] - sound[-1])))
        # Short edge smoothing only if the boundary has an unexpectedly large discontinuity.
        if boundary > .02:
            raise RuntimeError(f"Inspect loop boundary: {filename}: {boundary}")
        report = {
            "file": filename, "seconds": round(self.n / RATE, 3), "rate": RATE,
            "channels": 2, "peak_dbfs": round(float(20 * np.log10(np.max(np.abs(sound)))), 2),
            "rms_dbfs": round(float(20 * np.log10(np.sqrt(np.mean(sound ** 2)))), 2),
            "loop_boundary_step": boundary, "finite": bool(np.isfinite(sound).all()),
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest()
        }
        print(json.dumps(report))
        return report


def lobby():
    track = Track(72, 32)
    # D minor/add9 - Bb maj7 - F add9 - C suspended: eight-bar phrase.
    chords = [[50, 57, 60, 64], [46, 53, 57, 60], [53, 60, 65, 67], [48, 55, 62, 65]]
    melody = [74, 77, 81, 79, 77, 74, 72, 69]
    for bar in range(32):
        chord = chords[(bar // 2) % 4]
        beat = bar * 4
        if bar % 2 == 0:
            for j, note in enumerate(chord):
                track.pad(note, beat, 7.7, .042, (j - 1.5) * .30)
            track.bass(chord[0] - 12, beat, 7.5, .10)
        for j, offset in enumerate([.0, 1.5, 2.5]):
            track.pluck(chord[(j + bar) % 4] + 12, beat + offset, .075, (-.45 if j % 2 == 0 else .45))
        if bar % 2 == 1:
            track.bell(melody[(bar // 2) % 8], beat + 1, .065, .18)
        if 8 <= bar < 24:
            track.tick(beat + 1, .009, -.35)
            track.tick(beat + 3, .009, .35)
    return track.render("CROSSION_01_Confluence_Lobby_Draft.wav")


def game():
    track = Track(90, 32)
    chords = [[50, 57, 60, 64], [48, 55, 58, 62], [46, 53, 57, 60], [48, 55, 62, 65]]
    for bar in range(32):
        chord = chords[(bar // 2) % 4]
        beat = bar * 4
        if bar % 2 == 0:
            for j, note in enumerate(chord):
                track.pad(note, beat, 7.7, .035, (j - 1.5) * .30)
        for offset in [0, 2.5]:
            track.bass(chord[0] - 12, beat + offset, 1.3, .13)
        for j in range(8):
            index = [0, 2, 1, 3, 2, 1, 3, 1][j]
            track.pluck(chord[index] + 12, beat + j * .5, .045 if j % 2 else .065,
                        -.4 if j % 2 else .4, bright=True)
        track.kick(beat, .14)
        track.kick(beat + 2, .09)
        track.rim(beat + 1, .05)
        track.rim(beat + 3, .045)
        for j in range(8):
            track.tick(beat + .5 * j, .012 if j % 2 else .009, -.25 if j % 2 else .25)
        if bar % 4 == 2:
            for offset, note in [(0, 74), (1.5, 77), (3, 72 if (bar // 4) % 2 else 81)]:
                track.bell(note, beat + offset, .047, .1)
    return track.render("CROSSION_02_Rift_Table_Gameplay_Draft.wav")


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    reports = [lobby(), game()]
    (OUT / "audio_quality_report.json").write_text(json.dumps(reports, indent=2), encoding="utf-8")
