using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Moonlit {
 public class MoonlitGame : MonoBehaviour {
  public enum Mode { Menu, Playing, Paused, Result }
  public Mode mode;
  public MoonlitUI ui; public MoonlitStage stage; public RhythmChart chart;
  AudioSource music; AudioSource[] voices;
  AudioClip[] taps, punches, thumps; AudioClip countSound, coinSound;
  int voice, soundIndex, cursor, combo, maxCombo, score, perfect, good, miss, lastBeat=-999, best;
  bool demo, reduced, celebrationPending;
  int stageIndex;
  bool easy=>chart.stage.tutorial;
  bool[] failed;
  float offset, volume=.8f, reactionUntil, pausedTime;
  double startDsp, pauseDsp;
  int resultCoins;
  const string keys="abcdefghijklmnopqrstuvwxyz";
#if UNITY_WEBGL && !UNITY_EDITOR
  [DllImport("__Internal")] static extern void MoonlitInstall(string target);
  [DllImport("__Internal")] static extern double MoonlitNow();
#endif
  public float SongTime => mode==Mode.Paused ? pausedTime : (float)(AudioSettings.dspTime-startDsp);
  string BestKey=>easy?"bestScoreMusicV1Easy":"bestScoreStage_"+chart.stage.id;
  public void Start() {
   Application.targetFrameRate=60; QualitySettings.vSyncCount=0;
   Screen.sleepTimeout=SleepTimeout.NeverSleep; Input.imeCompositionMode=IMECompositionMode.Off;
   gameObject.name="MoonlitGame";
   stage=new GameObject("Moonlit 3D Stage").AddComponent<MoonlitStage>();stage.Build();
   ui=new GameObject("Game UI",typeof(RectTransform)).AddComponent<MoonlitUI>();ui.Build();
   music=gameObject.AddComponent<AudioSource>();music.volume=.62f;music.playOnAwake=false;
   taps=LoadSet("impactWood_light",5);punches=LoadSet("impactPunch_medium",5);thumps=LoadSet("impactPunch_heavy",5);
   countSound=taps[0];coinSound=taps[3];
   voices=new AudioSource[16];for(int i=0;i<voices.Length;i++){voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;}
   offset=PlayerPrefs.GetFloat("timingOffset",0);volume=PlayerPrefs.GetFloat("volume",.8f);
   reduced=PlayerPrefs.GetInt("reduced",0)==1;AudioListener.volume=volume;ui.Reduced(reduced);
   ui.onStart=()=>Begin(false);ui.onDemo=()=>Begin(true);ui.onRetry=()=>Begin(false);
   ui.onPause=TogglePause;ui.onResume=Resume;ui.onMenu=ShowMenu;ui.onStage=SelectStage;
   ui.onOffset=v=>{offset=Mathf.Clamp(offset+v,-.2f,.2f);PlayerPrefs.SetFloat("timingOffset",offset);ui.Pause(offset,volume,reduced);};
   ui.onVolume=v=>{volume=Mathf.Clamp01(volume+v);AudioListener.volume=volume;PlayerPrefs.SetFloat("volume",volume);ui.Pause(offset,volume,reduced);};
   ui.onReduced=v=>{reduced=v;ui.Reduced(v);PlayerPrefs.SetInt("reduced",v?1:0);ui.Pause(offset,volume,reduced);};
   SelectStage(0);
#if UNITY_WEBGL && !UNITY_EDITOR
   MoonlitInstall(gameObject.name);
#endif
  }
  AudioClip[] LoadSet(string stem,int count){var set=new AudioClip[count];for(int i=0;i<count;i++){set[i]=Resources.Load<AudioClip>($"Audio/Impacts/{stem}_{i:000}");if(!set[i])Debug.LogError("Missing impact recording: "+stem+i);}return set;}
  void SelectStage(int value){stageIndex=value;chart=new RhythmChart(stageIndex);failed=new bool[chart.syllables.Count];music.clip=Resources.Load<AudioClip>("Audio/"+chart.MusicName);best=PlayerPrefs.GetInt(BestKey,0);ShowMenu();}
  void ShowMenu(){CancelInvoke(nameof(SaleSpark));music.Stop();mode=Mode.Menu;celebrationPending=false;reactionUntil=0;ui.ResetBook(chart);ui.title.text=chart.stage.title;ui.title.fontSize=30;ui.progressText.text=$"{(easy?"":"약 ")}{chart.BPM:0} BPM · {chart.strokes.Count}번 입력";ui.progress.rectTransform.sizeDelta=new Vector2(0,5);ui.Menu(chart);if(stage.actor)stage.actor.speed=1;stage.Motion("SeatedIdle");}
  public void Begin(bool auto) {
   CancelInvoke(nameof(SaleSpark));music.Stop();demo=auto;cursor=combo=maxCombo=score=perfect=good=miss=0;
   lastBeat=-999;Array.Clear(failed,0,failed.Length);ui.ResetBook(chart);ui.HideModal();
   ui.combo.text=auto?"자동 연주":"0 연타";ui.scoreText.text="000000";ui.title.text=chart.stage.title+(auto?" · 자동":"");ui.title.fontSize=auto?25:30;
   ui.SetPhrase(chart,0,0,0);mode=Mode.Playing;startDsp=AudioSettings.dspTime+4*chart.Beat;
   music.PlayScheduled(startDsp);stage.Motion("Writing");if(stage.actor)stage.actor.speed=easy?1:1.18f;
   reactionUntil=0;celebrationPending=false;Debug.Log($"MOONLIT_START easy={easy} demo={auto} stage={chart.stage.id} notes={chart.strokes.Count} chart={SongChartGenerator.Version} source={chart.timeline.source}");
  }
  void Update() {
   if(ui==null)return;if(mode!=Mode.Paused)ui.Tick(Time.unscaledDeltaTime);
#if !UNITY_WEBGL || UNITY_EDITOR
   if(Input.GetKeyDown(KeyCode.Return)&&(mode==Mode.Menu||mode==Mode.Result))Begin(false);
   if(Input.GetKeyDown(KeyCode.F2)&&(mode==Mode.Menu||mode==Mode.Result))Begin(true);
   if(Input.GetKeyDown(KeyCode.Escape))TogglePause();
   if(mode==Mode.Playing&&!demo){bool shift=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);foreach(char key in keys){KeyCode code=(KeyCode)Enum.Parse(typeof(KeyCode),key.ToString().ToUpper());if(Input.GetKeyDown(code)){char k=shift&&"reqtwop".Contains(key.ToString())?char.ToUpper(key):key;Press(k.ToString(),SongTime-offset);}}}
#endif
   if(mode!=Mode.Playing)return;
   float time=SongTime;int beat=Mathf.FloorToInt(time/chart.Beat);
   if(beat!=lastBeat){lastBeat=beat;ui.Beat();if(time<0){Play(countSound,.6f);ui.Feedback((-beat).ToString(),ui.paper,0,new Vector2(800,680),false);}else if(time<chart.Beat*.2f)ui.Feedback("시작!",ui.gold,0,new Vector2(800,680),true);}
   if(demo){while(cursor<chart.strokes.Count&&time>=chart.strokes[cursor].time)Resolve(2);}
   else {while(cursor<chart.strokes.Count&&time-offset>chart.strokes[cursor].time+chart.LateWindow(cursor))Resolve(-1);}
   ui.Rhythm(chart,cursor,time-offset);
   ui.progress.rectTransform.sizeDelta=new Vector2(370*Mathf.Clamp01(time/music.clip.length),5);
   ui.progressText.text=$"{(easy?"":"약 ")}{chart.BPM:0} BPM   {Mathf.Max(0,time):00.0} / {music.clip.length:00.0}초";
   if(celebrationPending&&reactionUntil==0&&(cursor>=chart.strokes.Count||chart.strokes[cursor].time-time>1.35f)){
    celebrationPending=false;stage.Motion("SittingVictory",.18f);reactionUntil=Time.unscaledTime+1.15f;
    ui.Celebrate(combo);Play(thumps[(soundIndex++)%thumps.Length],.75f);
    Debug.Log("MOONLIT_COMBO_CELEBRATION "+combo);
   }
   if(reactionUntil>0&&Time.unscaledTime>reactionUntil){stage.Motion("Writing",.22f);reactionUntil=0;}
   if(time>=music.clip.length)Finish();
  }
  public void WebKey(string data) {
   var parts=data.Split('|');string key=parts[0];
   if(key=="Enter"&&(mode==Mode.Menu||mode==Mode.Result)){Begin(false);return;}
   if(key=="F2"&&(mode==Mode.Menu||mode==Mode.Result)){Begin(true);return;}
   if(key=="Escape"){TogglePause();return;}if(mode!=Mode.Playing||demo)return;
   float stamp=SongTime-offset;
#if UNITY_WEBGL && !UNITY_EDITOR
   if(parts.Length>1&&double.TryParse(parts[1],System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture,out double eventTime))stamp-=(float)Math.Max(0,(MoonlitNow()-eventTime)/1000.0);
#endif
   Press(key,stamp);
  }
  void Press(string key,float time){if(time<0)return;while(cursor<chart.strokes.Count&&time>chart.strokes[cursor].time+chart.LateWindow(cursor))Resolve(-1);if(cursor>=chart.strokes.Count)return;var target=chart.strokes[cursor];float delta=time-target.time;if(delta < -chart.EarlyWindow(cursor)||delta > chart.LateWindow(cursor))return;if(key!=target.key){Resolve(-1);return;}Resolve(chart.Grade(delta));}
  void Resolve(int grade) {
   if(cursor>=chart.strokes.Count)return;var n=chart.strokes[cursor];Vector2 source=ui.SyllableSource(chart,n.syllable);
   if(grade>=0){
    combo++;maxCombo=Mathf.Max(maxCombo,combo);if(grade==2){perfect++;score+=1000;}else{good++;score+=grade==1?650:350;}
    Play(taps[(soundIndex++)%taps.Length],grade==2?.85f:.64f);
    ui.Feedback(grade==2?"찰떡!":grade==1?"좋아!":"아슬!",grade==2?ui.gold:ui.blue,combo,source,!reduced);
    if(combo%20==0)celebrationPending=true;
   } else {
    combo=0;miss++;failed[n.syllable]=true;celebrationPending=false;
    Play(punches[(soundIndex++)%punches.Length],.25f);
    ui.Feedback("다음 박자!",new Color(.72f,.52f,.46f),0,source,false);
   }
   if(n.last){
    if(!failed[n.syllable]){bool phraseEnd=cursor+1>=chart.strokes.Count||chart.strokes[cursor+1].phrase!=n.phrase;
     ui.Fly(n.syllable,source,()=>{Play((phraseEnd?thumps:punches)[(soundIndex++)%5],phraseEnd?.9f:.78f);},phraseEnd);
    }else ui.MarkMissing(n.syllable);
   }
   cursor++;ui.scoreText.text=score.ToString("D6");
   if(cursor<chart.strokes.Count){var next=chart.strokes[cursor];ui.SetPhrase(chart,next.phrase,next.syllable,cursor);}else ui.subline.text="마지막 장을 묶는 중…";
  }
  void Play(AudioClip clip,float level){if(!clip)return;var a=voices[voice++%voices.Length];a.pitch=1;a.PlayOneShot(clip,level);}
  public void TogglePause(){if(mode==Mode.Playing){pausedTime=SongTime;pauseDsp=AudioSettings.dspTime;mode=Mode.Paused;music.Pause();ui.effects.SetPaused(true);foreach(var a in voices)a.Stop();if(stage.actor)stage.actor.speed=0;ui.Pause(offset,volume,reduced);}else if(mode==Mode.Paused)Resume();}
  void Resume(){if(mode!=Mode.Paused)return;double elapsed=AudioSettings.dspTime-pauseDsp;startDsp+=elapsed;if(reactionUntil>0)reactionUntil+=(float)elapsed;mode=Mode.Playing;if(pausedTime<0){music.Stop();music.PlayScheduled(startDsp);}else music.UnPause();ui.effects.SetPaused(false);if(stage.actor)stage.actor.speed=easy?1:1.18f;ui.HideModal();}
  public void WebBlur(string unused){if(mode==Mode.Playing)TogglePause();}
  void OnApplicationFocus(bool focus){if(!focus&&mode==Mode.Playing)TogglePause();}
  void Finish(){mode=Mode.Result;music.Stop();stage.Motion("SittingVictory");if(stage.actor)stage.actor.speed=1;int sales=Judgement.Sales(score,chart.strokes.Count*1000);if(!demo&&score>best){best=score;PlayerPrefs.SetInt(BestKey,best);}PlayerPrefs.Save();Debug.Log($"MOONLIT_RESULT easy={easy} demo={demo} score={score} perfect={perfect} good={good} miss={miss} sales={sales}");ui.Result(score,perfect,good,miss,maxCombo,sales,demo,best,chart.stage.storyTitle);resultCoins=0;InvokeRepeating(nameof(SaleSpark),.1f,.16f);}
  void SaleSpark(){if(mode!=Mode.Result||resultCoins++>16){CancelInvoke(nameof(SaleSpark));return;}Play(coinSound,.3f);ui.Burst(new Vector2(UnityEngine.Random.Range(460,1100),350),ui.gold,reduced?3:12,130);}
 }
}
