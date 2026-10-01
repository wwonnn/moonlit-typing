import tempfile
import unittest
import wave
from pathlib import Path
import numpy as np
from analyze_song import detect_attacks, read_pcm


class AudioAnalysisTests(unittest.TestCase):
    def test_silence_does_not_create_notes(self):
        self.assertEqual(detect_attacks(np.zeros(96000), 32000), [])

    def test_irregular_attacks_follow_audio_not_tempo(self):
        sr = 32000
        y = np.zeros(sr * 4)
        expected = [.5, 1.1, 2.0, 2.7]
        for time in expected:
            t = np.arange(round(.12 * sr)) / sr
            sound = .5 * np.sin(2 * np.pi * 180 * t) * np.exp(-t * 40)
            start = round(time * sr)
            y[start:start+len(sound)] += sound
        events = detect_attacks(y, sr)
        self.assertEqual(len(events), len(expected))
        for event, time in zip(events, expected):
            self.assertLess(abs(event['time'] - time), .07)
        shifted = detect_attacks(np.r_[np.zeros(round(.2 * sr)), y], sr)
        self.assertEqual(len(shifted), len(events))
        for a, b in zip(events, shifted):
            self.assertAlmostEqual(b['time'] - a['time'], .2, places=2)

    def test_stereo_pcm_duration_and_downmix(self):
        sr = 16000
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'stereo.wav'
            samples = np.tile(np.array([16384, -8192], dtype='<i2'), (sr, 1))
            with wave.open(str(path), 'wb') as w:
                w.setnchannels(2); w.setsampwidth(2); w.setframerate(sr)
                w.writeframes(samples.tobytes())
            y, rate = read_pcm(path)
            self.assertEqual(rate, sr)
            self.assertEqual(len(y), sr)
            self.assertTrue(np.allclose(y, .125))


if __name__ == '__main__':
    unittest.main()
