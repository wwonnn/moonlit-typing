using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Moonlit {
 public class MoonlitGame:MonoBehaviour {
  public enum Mode{Menu,Playing,Paused,Result} public Mode mode;public MoonlitUI ui;public MoonlitStage stage;public RhythmChart chart;
  AudioSource music;AudioSource[] voices;AudioClip perfectSound,goodSound,landSound,missSound,countSound,coinSound;int voice;
  int cursor,combo,maxCombo,score,perfect,good,miss,lastBeat=-999,best;bool demo,reduced;bool[] failed;float offset,volume=.8f,reactionUntil;double startDsp,pauseDsp;float pausedTime;int resultCoins;
  const string keys="abcdefghijklmnopqrstuvwxyz";
  #if UNITY_WEBGL && !UNITY_EDITOR
  [DllImport("__Internal")] static extern void MoonlitInstall(string target);
  [DllImport("__Internal")] static extern double MoonlitNow();
  #endif
  public float SongTime=>mode==Mode.Paused?pausedTime:(float)(AudioSettings.dspTime-startDsp);
  public void Start(){
   Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Screen.sleepTimeout=SleepTimeout.NeverSleep;Input.imeCompositionMode=IMECompositionMode.Off;
   gameObject.name="MoonlitGame";chart=new RhythmChart();failed=new bool[chart.syllables.Count];stage=new GameObject("Moonlit 3D Stage").AddComponent<MoonlitStage>();stage.Build();
   ui=new GameObject("Game UI",typeof(RectTransform)).AddComponent<MoonlitUI>();ui.Build();music=gameObject.AddComponent<AudioSource>();music.clip=Resources.Load<AudioClip>("Audio/MoonlitPress");music.volume=.72f;music.playOnAwake=false;
   perfectSound=Resources.Load<AudioClip>("Audio/KeyPerfect");goodSound=Resources.Load<AudioClip>("Audio/KeyGood");landSound=Resources.Load<AudioClip>("Audio/InkLand");missSound=Resources.Load<AudioClip>("Audio/Miss");countSound=Resources.Load<AudioClip>("Audio/Count");coinSound=Resources.Load<AudioClip>("Audio/Coin");
   voices=new AudioSource[12];for(int i=0;i<voices.Length;i++){voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;}
   offset=PlayerPrefs.GetFloat("timingOffset",0);volume=PlayerPrefs.GetFloat("volume",.8f);best=PlayerPrefs.GetInt("bestScore",0);reduced=PlayerPrefs.GetInt("reduced",0)==1;AudioListener.volume=volume;ui.Reduced(reduced);
   ui.onStart=()=>Begin(false);ui.onDemo=()=>Begin(true);ui.onRetry=()=>Begin(false);ui.onPause=TogglePause;ui.onResume=Resume;
   ui.onOffset=v=>{offset=Mathf.Clamp(offset+v,-.2f,.2f);PlayerPrefs.SetFloat("timingOffset",offset);ui.Pause(offset,volume,reduced);};
   ui.onVolume=v=>{volume=Mathf.Clamp01(volume+v);AudioListener.volume=volume;PlayerPrefs.SetFloat("volume",volume);ui.Pause(offset,volume,reduced);};
   ui.onReduced=v=>{reduced=v;ui.Reduced(v);PlayerPrefs.SetInt("reduced",v?1:0);ui.Pause(offset,volume,reduced);};
   ui.Menu(offset,volume);mode=Mode.Menu;
   #if UNITY_WEBGL && !UNITY_EDITOR
   MoonlitInstall(gameObject.name);
   #endif
  }
  public void Begin(bool auto){CancelInvoke(nameof(SaleSpark));Debug.Log("MOONLIT_START demo="+auto);music.Stop();demo=auto;cursor=combo=maxCombo=score=perfect=good=miss=0;lastBeat=-999;Array.Clear(failed,0,failed.Length);ui.ResetBook(chart);ui.HideModal();ui.combo.text=auto?"자동 연주":"0 연타";ui.scoreText.text="000000";ui.title.text=auto?"달빛 인쇄소 · 자동":"달빛 인쇄소";ui.SetPhrase(chart,0,0,0);mode=Mode.Playing;startDsp=AudioSettings.dspTime+4*RhythmChart.Beat;music.PlayScheduled(startDsp);stage.Motion("Writing");if(stage.actor)stage.actor.speed=1.18f;reactionUntil=0;}
  void Update(){if(ui==null)return;if(mode!=Mode.Paused)ui.Tick(Time.unscaledDeltaTime);
   #if !UNITY_WEBGL || UNITY_EDITOR
   if(Input.GetKeyDown(KeyCode.Return)&&(mode==Mode.Menu||mode==Mode.Result))Begin(false);if(Input.GetKeyDown(KeyCode.F2)&&(mode==Mode.Menu||mode==Mode.Result))Begin(true);if(Input.GetKeyDown(KeyCode.Escape))TogglePause();if(mode==Mode.Playing&&!demo){bool shift=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);foreach(char key in keys){KeyCode code=(KeyCode)Enum.Parse(typeof(KeyCode),key.ToString().ToUpper());if(Input.GetKeyDown(code)){char k=shift&&"reqtwo p".Replace(" ","").Contains(key.ToString())?char.ToUpper(key):key;Press(k.ToString(),SongTime-offset);}}}
   #endif
   if(mode!=Mode.Playing)return;float time=SongTime;int beat=Mathf.FloorToInt(time/RhythmChart.Beat);if(beat!=lastBeat){lastBeat=beat;ui.Beat();if(time<0){Play(countSound,.55f);ui.Feedback((-beat).ToString(),ui.paper,0,new Vector2(800,680),false);}else if(time<RhythmChart.Beat*.2f)ui.Feedback("시작!",ui.gold,0,new Vector2(800,680),true);}
   if(demo){while(cursor<chart.strokes.Count&&time>=chart.strokes[cursor].time)Resolve(2);}
   else{while(cursor<chart.strokes.Count&&time-offset>chart.strokes[cursor].time+Judgement.Window)Resolve(-1);}
   ui.Rhythm(chart,cursor,time-offset);ui.progress.rectTransform.sizeDelta=new Vector2(370*Mathf.Clamp01(time/music.clip.length),5);ui.progressText.text=$"144 BPM   {Mathf.Max(0,time):00.0} / {music.clip.length:00.0}초";
   if(reactionUntil>0&&Time.unscaledTime>reactionUntil){stage.Motion("Writing");reactionUntil=0;}
   if(time>=music.clip.length){Finish();}
  }
  public void WebKey(string data){var parts=data.Split('|');string key=parts[0];if(key=="Enter"&&(mode==Mode.Menu||mode==Mode.Result)){Begin(false);return;}if(key=="F2"&&(mode==Mode.Menu||mode==Mode.Result)){Begin(true);return;}if(key=="Escape"){TogglePause();return;}if(mode!=Mode.Playing||demo)return;float stamp=SongTime-offset;
   #if UNITY_WEBGL && !UNITY_EDITOR
   if(parts.Length>1&&double.TryParse(parts[1],System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture,out double eventTime))stamp-=(float)Math.Max(0,(MoonlitNow()-eventTime)/1000.0);
   #endif
   Press(key,stamp);
  }
  void Press(string key,float time){if(cursor>=chart.strokes.Count||time<0)return;var target=chart.strokes[cursor];float delta=time-target.time;if(Mathf.Abs(delta)>Judgement.Window){Play(goodSound,.15f);return;}if(key!=target.key){Resolve(-1);return;}Resolve(Judgement.Grade(delta));}
  void Resolve(int grade){if(cursor>=chart.strokes.Count)return;var n=chart.strokes[cursor];bool success=grade>=0;Vector2 source=ui.SyllableSource(chart,n.syllable);
   if(success){combo++;maxCombo=Mathf.Max(maxCombo,combo);if(grade==2){perfect++;score+=1000;Play(perfectSound,.52f);}else{good++;score+=grade==1?650:350;Play(goodSound,.5f);}ui.Feedback(grade==2?"찰떡!":grade==1?"좋아!":"아슬!",grade==2?ui.gold:ui.blue,combo,source,!reduced);stage.pulse=reduced?.2f:1;
    if(combo%32==0){ui.Burst(new Vector2(1320,64),ui.gold,reduced?6:28,220);}
   }else{combo=0;miss++;failed[n.syllable]=true;Play(missSound,.4f);ui.Feedback("다음 박자!",new Color(.72f,.52f,.46f),0,source,false);if(Time.unscaledTime>reactionUntil){stage.Motion("SittingDisbelief");reactionUntil=Time.unscaledTime+.48f;}}
   if(n.last){if(!failed[n.syllable])ui.Fly(n.syllable,source,()=>{Play(landSound,.45f);stage.pulse=reduced?.1f:.4f;});else ui.MarkMissing(n.syllable);}
   cursor++;ui.scoreText.text=score.ToString("D6");if(cursor<chart.strokes.Count){var next=chart.strokes[cursor];ui.SetPhrase(chart,next.phrase,next.syllable,cursor);}else ui.subline.text="마지막 장을 묶는 중…";
  }
  void Play(AudioClip clip,float level){if(!clip)return;var a=voices[voice++%voices.Length];a.pitch=1;a.PlayOneShot(clip,level);}
  public void TogglePause(){if(mode==Mode.Playing){pausedTime=SongTime;pauseDsp=AudioSettings.dspTime;mode=Mode.Paused;music.Pause();if(stage.actor)stage.actor.speed=0;ui.Pause(offset,volume,reduced);}else if(mode==Mode.Paused)Resume();}
  void Resume(){if(mode!=Mode.Paused)return;double elapsed=AudioSettings.dspTime-pauseDsp;startDsp+=elapsed;mode=Mode.Playing;if(pausedTime<0){music.Stop();music.PlayScheduled(startDsp);}else music.UnPause();if(stage.actor)stage.actor.speed=1.18f;ui.HideModal();}
  public void WebBlur(string unused){if(mode==Mode.Playing)TogglePause();}
  void OnApplicationFocus(bool focus){if(!focus&&mode==Mode.Playing)TogglePause();}
  void Finish(){mode=Mode.Result;music.Stop();stage.Motion("SittingVictory");if(stage.actor)stage.actor.speed=1;int sales=Judgement.Sales(score,chart.strokes.Count*1000);if(!demo&&score>best){best=score;PlayerPrefs.SetInt("bestScore",best);}PlayerPrefs.Save();Debug.Log($"MOONLIT_RESULT demo={demo} score={score} perfect={perfect} good={good} miss={miss} sales={sales}");ui.Result(score,perfect,good,miss,maxCombo,sales,demo,best);resultCoins=0;InvokeRepeating(nameof(SaleSpark),.1f,.16f);}
  void SaleSpark(){if(mode!=Mode.Result||resultCoins++>16){CancelInvoke(nameof(SaleSpark));return;}Play(coinSound,.32f);ui.Burst(new Vector2(UnityEngine.Random.Range(460,1100),350),ui.gold,reduced?3:12,130);}
 }
}
