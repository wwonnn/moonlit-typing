"""Offline PCM WAV onset analysis for songs without composer event data.

Example: python Tools/analyze_song.py song.wav --name MySong --bpm 120
No BPM grid is used to create timestamps. BPM is supplied or estimated as chart metadata.
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


def onset_features(y, sr):
    hop = max(1, round(sr * .01))
    size = 2 ** int(np.ceil(np.log2(sr * .046)))
    padded = np.pad(y, (size // 2, size // 2))
    frames = np.lib.stride_tricks.sliding_window_view(padded, size)[::hop]
    flux = np.zeros(len(frames)); energy = np.zeros(len(frames))
    window = np.hanning(size); previous = None
    # Bound peak memory for full-length MP3 exports rather than constructing a
    # multi-gigabyte full-song spectrogram.
    for start in range(0, len(frames), 512):
        block = frames[start:start+512]
        logmag = np.log1p(np.abs(np.fft.rfft(block * window, axis=1)) * 20)
        changes = np.diff(logmag, axis=0)
        if previous is None:
            first = np.zeros(logmag.shape[1])
        else:
            first = logmag[0] - previous
        flux[start:start+len(block)] = np.maximum(np.vstack([first, changes]), 0).mean(axis=1)
        energy[start:start+len(block)] = np.sqrt(np.mean(block ** 2, axis=1))
        previous = logmag[-1]
    smooth = np.convolve(flux, [0.2, 0.6, 0.2], mode='same')
    return smooth, energy, hop


def estimate_bpm(smooth, frame_seconds):
    centered = np.maximum(smooth - np.median(smooth), 0)
    size = 2 ** int(np.ceil(np.log2(len(centered) * 2)))
    spectrum = np.fft.rfft(centered, n=size)
    correlation = np.fft.irfft(spectrum * np.conj(spectrum), n=size)[:len(centered)]
    correlation /= np.maximum(1, len(centered) - np.arange(len(centered)))
    lo = round(60 / (180 * frame_seconds)); hi = round(60 / (70 * frame_seconds))
    candidates = []
    for lag in range(lo, hi+1):
        bpm = 60 / (lag * frame_seconds)
        score = sum(weight*correlation[lag*k] for k,weight in [(1,1),(2,.5),(3,.25)] if lag*k<len(correlation))
        score *= np.exp(-.5 * (np.log(bpm/120) / .45) ** 2)
        candidates.append((float(score), bpm))
    candidates.sort(reverse=True)
    return round(candidates[0][1], 2), [(round(b, 2), round(v, 6)) for v, b in candidates[:8]]


def detect_attacks(y, sr, features=None):
    if len(y) < sr or np.max(np.abs(y)) < 1e-6:
        return []
    # 10 ms steps, ~46 ms windows. Spectral change follows actual sound attacks.
    smooth, energy, hop = features or onset_features(y, sr)
    scale = max(float(np.quantile(smooth, .95)), 1e-8)
    peaks = []
    for i in range(2, len(smooth) - 2):
        local = smooth[max(0, i-20):min(len(smooth), i+21)]
        threshold = max(float(np.median(local)) * 1.6, scale * .08, float(smooth.max()) * .003)
        if smooth[i] < threshold or smooth[i] != np.max(smooth[i-2:i+3]):
            continue
        if energy[i] < max(float(energy.max()) * .01, 1e-6):
            continue
        if energy[i] < np.max(energy[max(0, i-10):i+11]) * .12:
            continue  # Reject quiet decay tails while retaining compressed drum attacks.
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
    parser.add_argument('--bpm', type=float, help='Optional known tempo; otherwise estimate from audio')
    parser.add_argument('--output', type=Path)
    parser.add_argument('--source-audio', type=Path, help='Original imported audio when WAV is a decoded analysis copy')
    args = parser.parse_args()
    if (args.bpm is not None and not 0 < args.bpm <= 1000) or Path(args.name).name != args.name or '/' in args.name or '\\' in args.name:
        parser.error('Supply a positive BPM and a simple asset name')
    y, sr = read_pcm(args.wav)
    features = onset_features(y, sr)
    bpm, tempo_candidates = estimate_bpm(features[0], features[2]/sr) if args.bpm is None else (args.bpm, [])
    events = detect_attacks(y, sr, features)
    if not events:
        parser.error('No audible attacks found; no fixed-grid fallback was generated')
    target = args.output or Path(__file__).resolve().parents[1] / 'Assets/Resources/Charts' / (args.name + '.json')
    data = dict(schemaVersion=1, song=args.name, source='audio-spectral-flux-v2', bpm=bpm, tempoCandidates=tempo_candidates,
                duration=len(y)/sr, sampleRate=sr,
                audioSha256=hashlib.sha256((args.source_audio or args.wav).read_bytes()).hexdigest(), events=events)
    if args.source_audio:
        data['analysisAudioSha256'] = hashlib.sha256(args.wav.read_bytes()).hexdigest()
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(dict(events=len(events), bpm=bpm, duration=data['duration'], tempoCandidates=tempo_candidates, output=str(target))))


if __name__ == '__main__':
    main()
