"""Original 144 BPM pentatonic dance instrumental and game sounds. No samples."""
import numpy as np, wave, json
from music_events import EventRecorder
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Resources/Audio';OUT.mkdir(parents=True,exist_ok=True)
SR=32000; BPM=144; B=60/BPM
# Composition length is independent of all typing phrases.
bars=49; duration=bars*4*B+2
recorder=EventRecorder('MoonlitPress',BPM,SR,duration)
section='intro'
rng=np.random.default_rng(20260930)
song=np.zeros((int(duration*SR),2),np.float64)
def tvec(d):return np.arange(int(d*SR))/SR
def hz(m):return 440*2**((m-69)/12)
def put(buf,sig,start,amp=1,pan=0,kind='accompaniment',strength=.2):
 i=int(start*SR);n=min(len(sig),len(buf)-i)
 if n<=0 or i<0:return
 recorder.add(i/SR,kind,strength,section)
 gains=np.array([np.cos((pan+1)*np.pi/4),np.sin((pan+1)*np.pi/4)])
 buf[i:i+n]+=sig[:n,None]*gains*amp
def kick():
 t=tvec(.36);phase=2*np.pi*(48*t+75*.025*(1-np.exp(-t/.025)))
 return np.sin(phase)*np.exp(-t*15)+rng.normal(0,.15,len(t))*np.exp(-t*220)
def snare():
 t=tvec(.22);noise=rng.normal(0,1,len(t));noise=np.concatenate(([0],np.diff(noise)))
 return .4*noise*np.exp(-t*32)+.45*np.sin(2*np.pi*185*t)*np.exp(-t*27)
def hat(open=False):
 t=tvec(.20 if open else .065);n=rng.normal(0,1,len(t));n=np.concatenate(([0],np.diff(n)))
 return n*np.exp(-t*(26 if open else 85))*.2
def pluck(note,d=.48):
 t=tvec(d);fund=hz(note);y=np.zeros_like(t)
 for k,a in [(1,1),(2,.38),(3,.2),(5,.09)]:y+=a*np.sin(2*np.pi*fund*k*t)*np.exp(-t*(5+k*2))
 return y*(1-np.exp(-t*750))*.5
def bass(note,d=.19):
 t=tvec(d);f0=hz(note);y=np.sin(2*np.pi*f0*t)+.23*np.sin(2*np.pi*f0*2*t)+.1*np.sin(2*np.pi*f0*3*t)
 return np.tanh(y*1.8)*np.minimum(t*180,1)*np.exp(-t*6)*.5
def bell(note,d=.65):
 t=tvec(d);f0=hz(note);return (.65*np.sin(2*np.pi*f0*t)+.18*np.sin(2*np.pi*f0*2.002*t)+.1*np.sin(2*np.pi*f0*3*t))*np.exp(-t*6)*(1-np.exp(-t*400))
def drum(note=42):
 t=tvec(.3);return np.sin(2*np.pi*hz(note)*t+2*(1-np.exp(-t*30)))*np.exp(-t*13)+rng.normal(0,.11,len(t))*np.exp(-t*70)
melodies=[[74,77,79,81,79,77,74,72],[74,77,79,84,81,79,77,79],[81,79,77,74,72,74,77,79],[77,74,72,69,72,74,77,74]]
roots=[38,34,41,36]
for bar in range(bars):
 start=bar*4*B; section_index=bar//8; intro=bar<2; bridge=section_index==3; outro=bar>=bars-2
 section='intro' if intro else 'outro' if outro else 'bridge' if bridge else 'chorus' if section_index%2 else 'verse'
 root=roots[(bar//2)%4]
 for beat in range(4):
  if not bridge or beat in [0,2]:put(song,kick(),start+beat*B,.73 if not intro else .46,kind='kick',strength=.75)
  if beat%2 and not intro:put(song,snare(),start+beat*B,.36,kind='snare',strength=.95)
  put(song,drum(42+beat%2*7),start+beat*B+B*.75,.14 if not bridge else .28,(-1)**beat*.28,kind='tom',strength=.7)
 for n in range(8):
  put(song,hat(n%2==1),start+n*B/2,.5 if not intro else .25,(-1)**n*.45,kind='hat',strength=.3)
  if not bridge and not outro:put(song,bass(root+(12 if n%4==3 else 0)),start+n*B/2,.45,kind='bass',strength=.4)
 pattern=melodies[(bar//2+section_index)%4]
 for n,note in enumerate(pattern):
  if (intro and n%2) or (bridge and n%2==1):continue
  sound=pluck(note+ (12 if section_index%3==2 else 0))
  put(song,sound,start+n*B/2,.32,(-1)**n*.22,kind='melody',strength=1 if n in [0,4] else .84)
  put(song,sound,start+n*B/2+B*.75,.085,(-1)**(n+1)*.6,kind='echo',strength=.15)
 if bar%2==0:
  for k in [root+36,root+43,root+48]:put(song,bell(k,1.2),start,.1,.4)
 if bar%8==7:
  for n in range(8):put(song,drum(47+n%3*5),start+3*B+n*B/8,.13+n*.008,(-1)**n*.35,kind='tom',strength=.85)
# Gentle tape-like saturation, controlled peaks and ending fade.
song=np.tanh(song*1.25)
fade=int(2*SR);song[-fade:]*=np.linspace(1,0,fade)[:,None]
song*=.88/max(np.max(np.abs(song)),.001)
def save(name,arr):
 if arr.ndim==1:arr=np.stack([arr,arr],axis=1)
 arr=np.clip(arr,-.98,.98)
 with wave.open(str(OUT/(name+'.wav')),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes((arr*32767).astype('<i2').tobytes())
save('MoonlitPress',song)
event_count=recorder.save(ROOT)
report={'title':'달빛 인쇄소 / Moonlit Press','composer':'Original procedural composition for this project','bpm':BPM,'duration':duration,'bars':bars,'sampleRate':SR,'musicEvents':event_count,'peak':float(np.max(np.abs(song))),'rms':float(np.sqrt(np.mean(song**2))),'source':'composer-instrument-events'}
(ROOT/'music-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps(report,ensure_ascii=False,indent=2))
