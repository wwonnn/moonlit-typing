"""Moonlit Festival: original 120 BPM, G-major pentatonic dance composition.
Musical accompaniment is busy; the physical typing chart deliberately leaves rests.
No external music samples. Kenney impact recordings are used separately as game SFX.
"""
import json, re, wave
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Resources/Audio'; OUT.mkdir(parents=True,exist_ok=True)
SR=32000; BPM=120; B=60/BPM
rng=np.random.default_rng(20261001)
code=(ROOT/'Assets/Scripts/RhythmChart.cs').read_text(encoding='utf8')
phrases=re.findall(r'"(.*?)"',re.search(r'EasyPhrases=\{(.*?)\};',code).group(1))
v=['k','o','i','O','j','p','u','P','h','hk','ho','hl','y','n','nj','np','nl','b','m','ml','l']
f=['','r','R','rt','s','sw','sg','e','f','fr','fa','fq','ft','fx','fv','fg','a','q','qt','t','T','d','w','c','z','x','v','g']
grid=4; notes=[]; syllables=0
for p,phrase in enumerate(phrases):
 if p: grid=int(np.ceil(grid/4))*4+4
 for c in phrase:
  if not '\uac00'<=c<='\ud7a3':continue
  n=ord(c)-0xac00; count=1+len(v[n%588//28])+len(f[n%28])
  notes.extend((grid+k)*B for k in range(count)); grid+=max(4,count+1); syllables+=1
bars=int(np.ceil((grid+4)/4)); duration=bars*4*B+2
song=np.zeros((int(duration*SR),2),np.float64)
def tv(d):return np.arange(round(d*SR))/SR
def hz(m):return 440*2**((m-69)/12)
def put(sig,beat,amp=1,pan=0):
 i=round(beat*B*SR);n=min(len(sig),len(song)-i)
 if n<=0:return
 song[i:i+n]+=sig[:n,None]*np.array([np.cos((pan+1)*np.pi/4),np.sin((pan+1)*np.pi/4)])*amp
def pluck(m,d=.65):
 t=tv(d);a=hz(m);y=np.zeros_like(t)
 for k in range(1,9):y+=np.sin(2*np.pi*a*k*t+0.03*np.sin(2*np.pi*5*t))/(k**1.4)*np.exp(-t*(4+k*1.5))
 return y*(1-np.exp(-t*550))
def reed(m,d=.34):
 t=tv(d);phase=2*np.pi*hz(m)*(t+.00016*np.sin(2*np.pi*5*t))
 y=sum(np.sin(phase*k)/(k**1.5) for k in [1,2,3,4,5])
 env=np.minimum(t/.025,1)*np.minimum((d-t)/.08,1)*np.exp(-t*1.1)
 return y*env*.55
def bass(m,d=.26):
 t=tv(d);phase=2*np.pi*hz(m)*t
 return (np.sin(phase)+.22*np.sin(phase*2)+.10*np.sin(phase*3))*np.minimum(t*150,1)*np.exp(-t*8)
def kick():
 t=tv(.32);return np.sin(2*np.pi*(48*t+80*.022*(1-np.exp(-t/.022))))*np.exp(-t*14)+rng.normal(0,.08,len(t))*np.exp(-t*250)
def clap():
 t=tv(.19);n=rng.normal(0,1,len(t));high=np.r_[0,np.diff(n)]
 env=sum(np.where(t>=a,np.exp(-np.maximum(t-a,0)*65),0) for a in [0,.009,.021])
 return high*env*.18+np.sin(2*np.pi*170*t)*np.exp(-t*30)*.18
def hat(opened=False):
 t=tv(.15 if opened else .06);n=rng.normal(0,1,len(t));return np.r_[0,np.diff(n)]*np.exp(-t*(35 if opened else 85))*.14
def tom(m):
 t=tv(.22);return np.sin(2*np.pi*hz(m)*t+2*(1-np.exp(-t*35)))*np.exp(-t*18)+rng.normal(0,.1,len(t))*np.exp(-t*60)
motifs=[[74,76,79,81,79,76],[71,74,76,79,76,74],[74,79,81,83,81,79],[76,74,71,69,71,74]]
chords=[(43,[67,71,74]),(40,[64,67,71]),(36,[64,67,72]),(38,[66,69,74])]
for bar in range(bars):
 start=bar*4;root,chord=chords[(bar//2)%4];intro=bar<2;bridge=16<=bar<20;outro=bar>=bars-2
 for b in range(4):
  if not bridge or b%2==0:put(kick(),start+b,.67 if not intro else .42)
  if b%2:put(clap(),start+b,.6)
  put(hat(),start+b,.4,-.3);put(hat(True),start+b+.5,.38,.3)
  if not intro:put(bass(root if b%2==0 else root+7),start+b+.5,.34)
 for b in [.5,1.75,2.5,3.5]:
  if not bridge:
   for m in chord:put(reed(m,.16),start+b,.078,-.45)
 for j,m in enumerate(motifs[(bar//2)%4]):
  beat=[0,.75,1.5,2,2.75,3.5][j]
  if intro and j%2:continue
  if bridge and j not in [0,3]:continue
  sound=pluck(m) if bar//8%2==0 else reed(m,.32)
  put(sound,start+beat,.25 if not outro else .14,.12)
  put(sound,start+beat+.75,.047,-.5)
 if bar%4==3:
  for j in range(4):put(tom(50-j*3),start+3+j*.25,.18,(-1)**j*.3)
 if bar%8==0:
  for m in chord:put(pluck(m+12,1.2),start,.12,.45)
song=np.tanh(song*1.15)
song[-2*SR:]*=np.linspace(1,0,2*SR)[:,None]
song*=.88/max(np.abs(song).max(),.001)
with wave.open(str(OUT/'MoonlitFestival.wav'),'wb') as w:
 w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes((song*32767).astype('<i2').tobytes())
report={'title':'달빛 잔치 / Moonlit Festival','bpm':BPM,'duration':duration,'bars':bars,'strokes':len(notes),'syllables':syllables,'lastNote':notes[-1],'minimumKeyGap':float(np.diff(notes).min()),'averageKeysPerSecond':len(notes)/duration,'peak':float(np.abs(song).max()),'rms':float(np.sqrt(np.mean(song**2))),'phrases':phrases}
(ROOT/'easy-music-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps(report,ensure_ascii=False,indent=2))
