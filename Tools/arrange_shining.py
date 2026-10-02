"""Reproducible authored articulations over the song's real attack timestamps.

Uses the unarranged v0.4 chart saved at .local/base-chart.json, never an already
arranged export. Tails use audio energy releases and always leave space for the next key.
This is a small authored arrangement, not automatic instrument transcription.
"""
import json
import numpy as np
from analyze_song import read_pcm
from pathlib import Path

root = Path(__file__).resolve().parents[1]
base = json.loads((root/'.local/base-chart.json').read_text(encoding='utf-8'))['songs'][1]
audio = json.loads((root/'Assets/Resources/Charts/ShiningMoment.json').read_text(encoding='utf-8'))
notes = base['strokes']
y, sr = read_pcm(root/'.local/ShiningMoment-analysis.wav')
hop=round(sr*.01);window=round(sr*.04)
padded=np.pad(y,(window//2,window//2))
power=np.r_[0,np.cumsum(padded*padded)]
rms=np.sqrt(np.maximum(0,(power[window::hop]-power[:-window:hop])/window))
# The opening now answers a held brush stroke with a short offbeat pair.
notes[1]['time'] = 2.98
notes[2]['time'] = 3.39
directions = {i: dict(index=i, time=-1, endTime=0, articulation='tap', syncopated=False) for i in (1, 2)}
for i in (1, 2):
    directions[i].update(time=notes[i]['time'], syncopated=True)
for i in (0, 20, 29, 37, 56, 76, 106, 108, 136, 166, 191, 212, 222):
    n = notes[i]
    assert not n['last']
    # Tails follow an energy fall/phrase release, not an invented BPM grid.
    earliest=n['time']+.45
    latest=min(n['time']+1.4,notes[i+1]['time']-.36)
    frames_allowed=np.arange(int(np.ceil(earliest/.01)),int(np.floor(latest/.01))+1)
    assert len(frames_allowed), ('No release budget', i)
    falls=rms[np.maximum(0,frames_allowed-5)]-rms[np.minimum(len(rms)-1,frames_allowed+5)]
    end=round(float(frames_allowed[np.argmax(falls)])*.01,2)
    if i==0: end=2.28  # Audible opening attack answering the held first brush stroke.
    directions.setdefault(i, dict(index=i, time=-1, endTime=0, articulation='tap', syncopated=False))
    directions[i].update(articulation='hold', endTime=end)
# Accented completions retain the one-key, one-note scoring budget.
accents = [i for i,n in enumerate(notes) if i+1==len(notes) or notes[i+1]['phrase'] != n['phrase']]
accents += [2, 4, 12, 71, 91, 187]
for i in accents:
    directions.setdefault(i, dict(index=i, time=-1, endTime=0, articulation='tap', syncopated=False))
    directions[i]['articulation'] = 'accent'
labels = ['붓 긋기 · 엇박으로 타닥', '꾹—탁 · 가난해도 신나게', '타닥! · 제비의 발걸음',
          '숨 고르기 · 작은 제비', '툭탁 · 정성껏 돌보기', '길게 긋고 · 탁!',
          '힘차게 · 박씨 한 알', '느긋하게 · 자라나는 박', '가볍게 · 타닥',
          '꾹—탁 · 슬근슬근', '팡! · 보화가 쏟아져', '함께 · 마지막 도장']
starts = [next(n['time'] for n in notes if n['phrase']==p) for p in range(len(labels))]
sections = [dict(start=starts[p],end=starts[p+1] if p+1<len(starts) else audio['duration'],label=label) for p,label in enumerate(labels)]
data = dict(revision='brush-performance-v1',audioSha256=audio['audioSha256'],analysisAudioSha256=audio['analysisAudioSha256'],tailSource='audio-energy-release',notes=sorted(directions.values(),key=lambda d:d['index']),sections=sections)
target = root/'Assets/Resources/Performances/ShiningMoment.json'
target.parent.mkdir(parents=True,exist_ok=True)
target.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(holds=sum(d['articulation']=='hold' for d in directions.values()),accents=len(accents),output=str(target))))
