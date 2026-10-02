"""Arrange the supplied song without moving notes onto a BPM grid.

Read the unarranged Unity export in .local/door-base-chart.json. Heads remain
actual spectral onsets; held tails follow energy releases. Tempo phase is only
used to annotate offbeats. This is a reproducible song-specific arrangement.
"""
import argparse
import json
from pathlib import Path
import numpy as np
from analyze_song import read_pcm


def arrange(root, target):
    base=json.loads((root/'.local/door-base-chart.json').read_text(encoding='utf-8'))
    chart=next(s for s in base['songs'] if s['song']=='DoorClicks')
    audio=json.loads((root/'Assets/Resources/Charts/DoorClicks.json').read_text(encoding='utf-8'))
    notes=chart['strokes']
    y,sr=read_pcm(root/'.local/DoorClicks-analysis.wav')
    hop=round(sr*.01); window=round(sr*.04)
    padded=np.pad(y,(window//2,window//2)); power=np.r_[0,np.cumsum(padded*padded)]
    rms=np.sqrt(np.maximum(0,(power[window::hop]-power[:-window:hop])/window))
    beat=60/audio['bpm']
    attacks=audio['events']
    pulse=sum(e['strength']**4*np.exp(2j*np.pi*e['time']/beat) for e in attacks)
    phase=float(np.angle(pulse)/(2*np.pi)*beat)%beat
    directions={}
    def note(i):
        return directions.setdefault(i,dict(index=i,time=-1,endTime=0,articulation='tap',syncopated=False))
    for i,n in enumerate(notes):
        position=((n['time']-phase)/beat)%1
        if .30<=position<=.70:
            note(i)['syncopated']=True
    # Spread sustained brush strokes across phrases, leaving a clear release gap.
    held=[]; releases=[]
    for p in range(len(chart['phrases'])):
        candidates=[]
        for i,n in enumerate(notes[:-1]):
            if n['phrase']!=p or n['last'] or notes[i+1]['phrase']!=p:
                continue
            earliest=n['time']+.45
            latest=min(n['time']+1.15,notes[i+1]['time']-.36)
            frames=np.arange(int(np.ceil(earliest/.01)),int(np.floor(latest/.01))+1)
            if not len(frames): continue
            falls=rms[np.maximum(0,frames-5)]-rms[np.minimum(len(rms)-1,frames+5)]
            best=int(np.argmax(falls))
            if falls[best]<=0: continue
            candidates.append((float(falls[best]),i,round(float(frames[best])*.01,2)))
        releases.extend(candidates)
        if candidates:
            _,i,end=max(candidates)
            note(i).update(articulation='hold',endTime=end)
            held.append(i)
    for _,i,end in sorted(releases,reverse=True):
        if len(held)>=6: break
        if i in held or any(abs(notes[i]['time']-notes[j]['time'])<2 for j in held): continue
        note(i).update(articulation='hold',endTime=end)
        held.append(i)
    # Phrase completions and strong attacks within rapid answers land with force.
    accented={i for i,n in enumerate(notes) if i+1==len(notes) or notes[i+1]['phrase']!=n['phrase']}
    strong=sorted((i for i,n in enumerate(notes) if i not in held and i not in accented),
                  key=lambda i: (notes[i]['strength']+.08*directions.get(i,{}).get('syncopated',False)),reverse=True)
    for i in strong:
        if len(accented)>=26: break
        if all(abs(notes[i]['time']-notes[j]['time'])>=1.25 for j in accented): accented.add(i)
    for i in accented: note(i)['articulation']='accent'
    labels=['툭탁 · 심술쟁이의 등장','짧게 타닥! · 욕심 한가득','엇박으로 · 스님 등장',
            '꾹—탁 · 볏짚의 변신','팍! · 가짜가 나타났다','타닥 타닥 · 진짜는 누구?',
            '엇박 대화 · 티격태격','탁! 탁! · 원님 앞에서','숨 고르기 · 빈손으로',
            '꾹—탁 · 잘못을 뉘우치고','힘차게 · 마음을 고쳐','함께 · 마지막 도장']
    starts=[next(n['time'] for n in notes if n['phrase']==p) for p in range(len(labels))]
    sections=[dict(start=starts[p],end=starts[p+1] if p+1<len(starts) else audio['duration'],
        label=(('꾹—탁 · '+label.split(' · ')[-1]) if any(notes[i]['phrase']==p for i in held) else label.replace('꾹—탁','타닥!')))
        for p,label in enumerate(labels)]
    result=dict(revision='brush-performance-v1',audioSha256=audio['audioSha256'],analysisAudioSha256=audio['analysisAudioSha256'],
                tailSource='audio-energy-release',notes=sorted(directions.values(),key=lambda d:d['index']),sections=sections)
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    gaps=np.diff([n['time'] for n in notes])
    print(json.dumps(dict(keys=len(notes),syllables=len(chart['syllables']),density=len(notes)/audio['duration'],
        holds=len(held),accents=len(accented),offbeats=sum(d['syncopated'] for d in directions.values()),
        shortGaps=int(sum(gaps<.45)),minimumGap=float(min(gaps)),phase=phase,output=str(target))))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project',type=Path,default=Path(__file__).resolve().parents[1])
    parser.add_argument('--output',type=Path)
    args=parser.parse_args()
    arrange(args.project,args.output or args.project/'Assets/Resources/Performances/DoorClicks.json')
