"""Record audible instrument attacks independently of text/chart generation."""
import hashlib
import json
from pathlib import Path


class EventRecorder:
    def __init__(self, song, bpm, sample_rate, duration):
        self.song, self.bpm, self.sample_rate, self.duration = song, bpm, sample_rate, duration
        self.events = []

    def add(self, time, kind, strength, section):
        # Use the exact sample at which the composer mixes the instrument.
        self.events.append(dict(time=round(time, 7), kind=kind,
                                strength=round(strength, 4), section=section))

    def save(self, root):
        audio = Path(root) / 'Assets/Resources/Audio' / (self.song + '.wav')
        target = Path(root) / 'Assets/Resources/Charts' / (self.song + '.json')
        target.parent.mkdir(parents=True, exist_ok=True)
        data = dict(schemaVersion=1, song=self.song, bpm=self.bpm, duration=self.duration,
                    sampleRate=self.sample_rate, audioSha256=hashlib.sha256(audio.read_bytes()).hexdigest(),
                    source='composer-instrument-events', events=sorted(self.events, key=lambda e: e['time']))
        target.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
        return len(self.events)
