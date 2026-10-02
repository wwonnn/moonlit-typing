using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Moonlit {
 public class MoonlitGame : MonoBehaviour {
  public enum Mode { Menu, Playing, Paused, Result }
  public Mode mode;
  public MoonlitUI ui; public MoonlitStage stage; public RhythmChart chart;
  AudioSource music,brush; AudioSource[] voices;
  HoldJudgement hold; float lastHint=-10;
  AudioClip[] taps, punches, thumps,woods; AudioClip countSound, coinSound;
  int voice, soundIndex, cursor, combo, maxCombo, score, perfect, good, miss, lastBeat=-999, best;
  bool demo, reduced, celebrationPending;
  int stageIndex;
  bool easy=>chart.stage.tutorial;
  bool[] failed;
  float offset, volume=.8f, reactionUntil, pausedTime;
  double startDsp, pauseDsp;
  int resultCoins;
  public const float SfxGain=1.12f;
  const string keys="abcdefghijklmnopqrstuvwxyz";
#if UNITY_WEBGL && !UNITY_EDITOR
  [DllImport("__Internal")] static extern void MoonlitInstall(string target);
  [DllImport("__Internal")] static extern double MoonlitNow();
#endif
  public float SongTime => mode==Mode.Paused ? pausedTime : (float)(AudioSettings.dspTime-startDsp);
  string BestKey=>easy?"bestScoreMusicV1Easy":"bestScoreStagePerformanceV1_"+chart.stage.id;
  public void Start() {
   Application.targetFrameRate=60; QualitySettings.vSyncCount=0;
   Screen.sleepTimeout=SleepTimeout.NeverSleep; Input.imeCompositionMode=IMECompositionMode.Off;
   gameObject.name="MoonlitGame";
   stage=new GameObject("Moonlit 3D Stage").AddComponent<MoonlitStage>();stage.Build();
   ui=new GameObject("Game UI",typeof(RectTransform)).AddComponent<MoonlitUI>();ui.Build();
   music=gameObject.AddComponent<AudioSource>();music.volume=.62f;music.playOnAwake=false;
   woods=LoadSet("impactWood_heavy",5);taps=LoadSet("impactWood_light",5);punches=LoadSet("impactPunch_medium",5);thumps=LoadSet("impactPunch_heavy",5);
   countSound=taps[0];coinSound=taps[3];
   brush=gameObject.AddComponent<AudioSource>();brush.clip=Resources.Load<AudioClip>("Audio/BrushFriction");brush.loop=true;brush.playOnAwake=false;brush.volume=.28f*SfxGain;
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
  void ShowMenu(){ClearHold();CancelInvoke(nameof(SaleSpark));music.Stop();mode=Mode.Menu;celebrationPending=false;reactionUntil=0;ui.ResetBook(chart);ui.title.text=chart.stage.title;ui.title.fontSize=chart.stageIndex==2?23:30;ui.progressText.text=$"{(easy?"":"약 ")}{chart.BPM:0} BPM · {chart.strokes.Count}번 입력";ui.progress.rectTransform.sizeDelta=new Vector2(0,5);ui.Menu(chart);if(stage.actor)stage.actor.speed=1;stage.Motion("SeatedIdle");}
  public void Begin(bool auto) {
   ClearHold();CancelInvoke(nameof(SaleSpark));music.Stop();demo=auto;cursor=combo=maxCombo=score=perfect=good=miss=0;
   lastBeat=-999;Array.Clear(failed,0,failed.Length);ui.ResetBook(chart);ui.HideModal();
   ui.combo.text=auto?"자동 연주":"0 연타";ui.scoreText.text="000000";ui.title.text=chart.stage.title+(auto?" · 자동":"");ui.title.fontSize=chart.stageIndex==2?(auto?19:23):auto?25:30;
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
   if(mode==Mode.Playing&&!demo){bool shift=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);foreach(char key in keys){KeyCode code=(KeyCode)Enum.Parse(typeof(KeyCode),key.ToString().ToUpper());if(Input.GetKeyUp(code))Release(key.ToString(),SongTime-offset);if(Input.GetKeyDown(code)){char k=shift&&"reqtwop".Contains(key.ToString())?char.ToUpper(key):key;Press(k.ToString(),SongTime-offset);}}}
#endif
   if(mode!=Mode.Playing)return;
   float time=SongTime;int beat=Mathf.FloorToInt(time/chart.Beat);
   if(beat!=lastBeat){lastBeat=beat;ui.Beat();if(time<0){Play(countSound,.6f);ui.Feedback((-beat).ToString(),ui.paper,0,new Vector2(800,680),false);}else if(time<chart.Beat*.2f)ui.Feedback("시작!",ui.gold,0,new Vector2(800,680),true);}
   if(demo){while(cursor<chart.strokes.Count){var n=chart.strokes[cursor];if(hold!=null){if(time<hold.endTime)break;CompleteHold(time,true);}else{if(time<n.time)break;if(n.articulation=="hold")BeginHold(2,0);else Resolve(2,"",0);}}}
   else AdvanceExpired(time-offset);
   if(hold!=null){var n=chart.strokes[cursor];ui.HoldVisual(n,Mathf.Clamp01((time-offset-n.time)/(n.endTime-n.time)),hold.needsGrip);}

   ui.Rhythm(chart,cursor,time-offset);
   ui.progress.rectTransform.sizeDelta=new Vector2(370*Mathf.Clamp01(time/music.clip.length),5);
   ui.progressText.text=$"{(easy?"":"약 ")}{chart.BPM:0} BPM   {Mathf.Max(0,time):00.0} / {music.clip.length:00.0}초";
   if(celebrationPending&&hold==null&&reactionUntil==0&&(cursor>=chart.strokes.Count||chart.strokes[cursor].time-time>1.35f)){
    celebrationPending=false;stage.Motion("SittingVictory",.18f);reactionUntil=Time.unscaledTime+1.15f;
    ui.Celebrate(combo);Play(thumps[(soundIndex++)%thumps.Length],.75f);
    Debug.Log("MOONLIT_COMBO_CELEBRATION "+combo);
   }
   if(reactionUntil>0&&Time.unscaledTime>reactionUntil){stage.Motion("Writing",.22f);reactionUntil=0;}
   if(time>=music.clip.length)Finish();
  }
  public void WebKey(string data) {
   var parts=data.Split('|');bool up=parts[0]=="up";bool structured=parts[0]=="down"||up;string key=structured?parts[1]:parts[0];
   if(!up&&key=="Enter"&&(mode==Mode.Menu||mode==Mode.Result)){Begin(false);return;}
   if(!up&&key=="F2"&&(mode==Mode.Menu||mode==Mode.Result)){Begin(true);return;}
   if(!up&&key=="Escape"){TogglePause();return;}if(mode!=Mode.Playing||demo)return;
   float stamp=SongTime-offset;int timestampIndex=structured?2:1;
#if UNITY_WEBGL && !UNITY_EDITOR
   if(parts.Length>timestampIndex&&double.TryParse(parts[timestampIndex],System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture,out double eventTime))stamp-=(float)Math.Max(0,(MoonlitNow()-eventTime)/1000.0);
#endif
   if(up)Release(key,stamp);else Press(key,stamp);
  }
  void AdvanceExpired(float time,bool fromInput=false){
   while(cursor<chart.strokes.Count){
    if(hold!=null){if(!hold.Expired(time))break;Resolve(-1,"놓는 박자를 놓쳤어요",time-hold.endTime);}
    else if(time>chart.strokes[cursor].time+chart.LateWindow(cursor))Resolve(-1,fromInput?"늦었어요":"놓쳤어요",time-chart.strokes[cursor].time);
    else break;
   }
  }
  void Press(string key,float time){
   if(time<0)return;AdvanceExpired(time,true);if(cursor>=chart.strokes.Count)return;
   if(hold!=null){if(hold.Matches(key)&&hold.needsGrip){hold.needsGrip=false;brush.Play();ui.HoldVisual(chart.strokes[cursor],0,false);}else Hint("같은 키를 꾹!",key);return;}
   var target=chart.strokes[cursor];float delta=time-target.time;
   if(delta < -chart.EarlyWindow(cursor)){if(delta>=-.42f)Hint("아직 일러요",key);return;}
   if(delta>chart.LateWindow(cursor))return;
   if(key!=target.key){Resolve(-1,"다른 키",delta);return;}
   int grade=chart.Grade(delta);if(target.articulation=="hold")BeginHold(grade,delta);else Resolve(grade,"",delta);
  }
  void Hint(string reason,string key){if(Time.unscaledTime-lastHint<.18f)return;lastHint=Time.unscaledTime;Debug.Log("MOONLIT_HINT "+reason);ui.KeyFeedback(reason,"입력 [ "+key.ToUpper()+" ] · 박자선을 기다리세요",false,false,false,combo,ui.SyllableSource(chart,chart.strokes[cursor].syllable));Play(punches[0],.23f);}
  void BeginHold(int grade,float delta){var n=chart.strokes[cursor];hold=new HoldJudgement(n,grade,chart.ReleaseWindow(cursor));
   Play(taps[(soundIndex++)%5],.78f);brush.Play();ui.KeyFeedback(grade==2?"찰떡 · 꾹!":delta<0?"조금 빨라요 · 꾹!":"조금 늦어요 · 꾹!","[ "+n.key.ToUpper()+" ] 유지 · 꼬리가 오면 떼세요",true,false,true,combo,ui.SyllableSource(chart,n.syllable),grade);
   Debug.Log($"MOONLIT_HOLD_START index={cursor} key={n.key} head={n.time:F3} tail={n.endTime:F3} grade={grade}");
  }
  void Release(string key,float time){if(hold==null||!hold.Matches(key))return;CompleteHold(time,false);}
  void CompleteHold(float time,bool auto){if(hold==null)return;int grade=auto?2:hold.Release(time);float delta=time-hold.endTime;string reason=grade<0?(delta<0?"조금 더 꾹!":"놓는 박자를 놓쳤어요"):"쭉—탁!";
   Debug.Log($"MOONLIT_HOLD_END index={cursor} grade={grade} delta={delta:F3}");Resolve(grade,reason,delta);
  }
  void ClearHold(){hold=null;if(brush)brush.Stop();if(ui)ui.EndHold();}
  void Resolve(int grade,string reason="",float delta=0) {
   if(cursor>=chart.strokes.Count)return;var n=chart.strokes[cursor];Vector2 source=ui.SyllableSource(chart,n.syllable);bool held=hold!=null;bool accent=n.articulation=="accent";ClearHold();
   if(grade>=0){
    combo++;maxCombo=Mathf.Max(maxCombo,combo);if(grade==2){perfect++;score+=1000;}else{good++;score+=grade==1?650:350;}
    Play((accent?woods:held?woods:taps)[(soundIndex++)%5],grade==2?.85f:.64f);
    if(accent)Play(thumps[(soundIndex++)%5],.30f);
    string word=reason!=""?reason:grade==2?(accent?"힘차게!":n.syncopated?"엇박 찰떡!":"찰떡!"):delta<0?"조금 빨라요":"조금 늦어요";
    string detail=grade==2?held?"붓 긋기 성공":accent?"강조 박자 성공":n.syncopated?"엇박 성공":"정확한 박자":$"{Mathf.Abs(delta)*1000:0} ms · "+(delta<0?"조금만 기다려요":"조금 더 빠르게");
    ui.KeyFeedback(word,detail,true,accent,held,combo,source,grade);
    if(combo%20==0)celebrationPending=true;
   } else {
    combo=0;miss++;failed[n.syllable]=true;celebrationPending=false;
    Play(punches[(soundIndex++)%5],.45f);
    ui.KeyFeedback(reason==""?"놓쳤어요":reason,reason=="다른 키"?"정답 [ "+n.key.ToUpper()+" ] · 다음 박자로":held?"같은 키를 꼬리까지 유지하세요":"다음 박자에 다시 맞춰요",false,false,false,0,source);
   }
   if(n.last){
    if(!failed[n.syllable]){bool phraseEnd=cursor+1>=chart.strokes.Count||chart.strokes[cursor+1].phrase!=n.phrase;
     ui.Fly(n.syllable,source,()=>{Play((phraseEnd?thumps:punches)[(soundIndex++)%5],phraseEnd?.9f:.78f);},phraseEnd,accent);
    }else ui.MarkMissing(n.syllable);
   }
   Debug.Log($"MOONLIT_KEY index={cursor} kind={n.articulation} grade={grade} delta={delta:F3} reason={reason}");
   cursor++;ui.scoreText.text=score.ToString("D6");
   if(cursor<chart.strokes.Count){var next=chart.strokes[cursor];ui.SetPhrase(chart,next.phrase,next.syllable,cursor);}else ui.subline.text="마지막 장을 묶는 중…";
  }
  void Play(AudioClip clip,float level){if(!clip)return;var a=voices[voice++%voices.Length];a.pitch=1;a.PlayOneShot(clip,Mathf.Min(1,level*SfxGain));}
  public void TogglePause(){if(mode==Mode.Playing){pausedTime=SongTime;pauseDsp=AudioSettings.dspTime;mode=Mode.Paused;music.Pause();ui.effects.SetPaused(true);foreach(var a in voices)a.Stop();if(brush)brush.Stop();if(hold!=null)hold.needsGrip=true;if(stage.actor)stage.actor.speed=0;ui.Pause(offset,volume,reduced);}else if(mode==Mode.Paused)Resume();}
  void Resume(){if(mode!=Mode.Paused)return;double elapsed=AudioSettings.dspTime-pauseDsp;startDsp+=elapsed;if(reactionUntil>0)reactionUntil+=(float)elapsed;mode=Mode.Playing;if(pausedTime<0){music.Stop();music.PlayScheduled(startDsp);}else music.UnPause();ui.effects.SetPaused(false);if(stage.actor)stage.actor.speed=easy?1:1.18f;ui.HideModal();if(hold!=null)ui.HoldVisual(chart.strokes[cursor],Mathf.Clamp01((pausedTime-offset-chart.strokes[cursor].time)/(hold.endTime-chart.strokes[cursor].time)),true);}
  public void WebBlur(string unused){if(mode==Mode.Playing)TogglePause();}
  void OnApplicationFocus(bool focus){if(!focus&&mode==Mode.Playing)TogglePause();}
  void Finish(){ClearHold();mode=Mode.Result;music.Stop();stage.Motion("SittingVictory");if(stage.actor)stage.actor.speed=1;int sales=Judgement.Sales(score,chart.strokes.Count*1000);if(!demo&&score>best){best=score;PlayerPrefs.SetInt(BestKey,best);}PlayerPrefs.Save();Debug.Log($"MOONLIT_RESULT easy={easy} demo={demo} score={score} perfect={perfect} good={good} miss={miss} sales={sales}");ui.Result(score,perfect,good,miss,maxCombo,sales,demo,best,chart.stage.storyTitle);resultCoins=0;InvokeRepeating(nameof(SaleSpark),.1f,.16f);}
  void SaleSpark(){if(mode!=Mode.Result||resultCoins++>16){CancelInvoke(nameof(SaleSpark));return;}Play(coinSound,.3f);ui.Burst(new Vector2(UnityEngine.Random.Range(460,1100),350),ui.gold,reduced?3:12,130);}
 }
}
