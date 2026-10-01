"""Offline PCM WAV onset analysis for songs without composer event data.

Example: python Tools/analyze_song.py song.wav --name MySong --bpm 120
No BPM grid is used to create timestamps. BPM is supplied as chart metadata.
This creates candidates, not a finished/quality-guaranteed playable chart.
"""
import argparse
import hashlib
import json
import wave
from pathlib import Path
import numpy as np


def read_pcm(path):
    with wave.open(str(path), 'rb') as w:
        channels, width, sr = w.getnchannels(), w.getsampwidth(), w.getframerate()
        raw = w.readframes(w.getnframes())
    if width == 1:
        data = (np.frombuffer(raw, np.uint8).astype(np.float64) - 128) / 128
    elif width == 2:
        data = np.frombuffer(raw, '<i2').astype(np.float64) / 32768
    elif width == 3:
        b = np.frombuffer(raw, np.uint8).reshape(-1, 3).astype(np.int32)
        data = b[:, 0] | (b[:, 1] << 8) | (b[:, 2] << 16)
        data = ((data ^ 0x800000) - 0x800000).astype(np.float64) / 8388608
    elif width == 4:
        data = np.frombuffer(raw, '<i4').astype(np.float64) / 2147483648
    else:
        raise ValueError('Use uncompressed 8/16/24/32-bit integer PCM WAV')
    return data.reshape(-1, channels).mean(axis=1), sr


def detect_attacks(y, sr):
    if len(y) < sr or np.max(np.abs(y)) < 1e-6:
        return []
    # 10 ms steps, ~46 ms centered windows. Spectral change detects attacks,
    # including quiet attacks following louder sustained sounds.
    hop = max(1, round(sr * .01))
    size = 2 ** int(np.ceil(np.log2(sr * .046)))
    padded = np.pad(y, (size // 2, size // 2))
    frames = np.lib.stride_tricks.sliding_window_view(padded, size)[::hop]
    spectra = np.abs(np.fft.rfft(frames * np.hanning(size), axis=1))
    logmag = np.log1p(spectra * 20)
    flux = np.r_[0, np.maximum(np.diff(logmag, axis=0), 0).mean(axis=1)]
    energy = np.sqrt(np.mean(frames ** 2, axis=1))
    smooth = np.convolve(flux, [0.2, 0.6, 0.2], mode='same')
    scale = max(float(np.quantile(smooth, .95)), 1e-8)
    peaks = []
    for i in range(2, len(smooth) - 2):
        local = smooth[max(0, i-20):min(len(smooth), i+21)]
        threshold = max(float(np.median(local)) * 1.6, scale * .08)
        if smooth[i] < threshold or smooth[i] != np.max(smooth[i-2:i+3]):
            continue
        if energy[i] < max(float(energy.max()) * .01, 1e-6):
            continue
        if energy[i+1] <= energy[i-2] * 1.08:
            continue  # A changing decay/tail is not a new physical attack.
        # Move to the beginning of the energy rise, rather than its delayed peak.
        begin = max(0, i-6)
        for j in range(i-1, begin, -1):
            if energy[j] <= energy[j-1] and energy[j] <= energy[j+1]:
                begin = j
                break
        time = begin * hop / sr
        strength = .6 + .4 * min(1, float(smooth[i]) / scale)
        if peaks and time - peaks[-1]['time'] < .09:
            if strength > peaks[-1]['strength']:
                peaks[-1] = dict(time=round(time, 7), strength=round(strength, 4), kind='audio', section='audio')
        else:
            peaks.append(dict(time=round(time, 7), strength=round(strength, 4), kind='audio', section='audio'))
    return peaks


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('wav', type=Path)
    parser.add_argument('--name', required=True)
    parser.add_argument('--bpm', type=float, required=True)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    if not 0 < args.bpm <= 1000 or Path(args.name).name != args.name or '/' in args.name or '\\' in args.name:
        parser.error('Supply a positive BPM and a simple asset name')
    y, sr = read_pcm(args.wav)
    events = detect_attacks(y, sr)
    if not events:
        parser.error('No audible attacks found; no fixed-grid fallback was generated')
    target = args.output or Path(__file__).resolve().parents[1] / 'Assets/Resources/Charts' / (args.name + '.json')
    data = dict(schemaVersion=1, song=args.name, source='audio-spectral-flux-v1', bpm=args.bpm,
                duration=len(y)/sr, sampleRate=sr,
                audioSha256=hashlib.sha256(args.wav.read_bytes()).hexdigest(), events=events)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(dict(events=len(events), duration=data['duration'], output=str(target))))


if __name__ == '__main__':
    main()
