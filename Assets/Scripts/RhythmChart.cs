using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonlit {
 [Serializable] public class Stroke { public string key; public string jamo; public float time; public int syllable; public int phrase; public bool last; public string eventKind, section; public float strength; }
 [Serializable] public class Syllable { public string glyph; public int phrase; public bool space; public int firstStroke; public int lastStroke; }
 public class RhythmChart {
  public readonly bool easy;
  public readonly SongTimeline timeline;
  public float BPM=>timeline.bpm;
  public float Beat=>60f/BPM;
  public float Window=>easy?.24f:Judgement.Window;
  public string MusicName=>easy?"MoonlitFestival":"MoonlitPress";
  public string Title=>easy?"달빛 잔치 · 쉬움":"달빛 인쇄소 · 도전";
  public int Grade(float delta){float d=Mathf.Abs(delta);return d<=(easy?.10f:Judgement.Perfect)?2:d<=(easy?.18f:Judgement.Good)?1:d<=Window?0:-1;}
  public readonly List<Stroke> strokes=new List<Stroke>();
  public readonly List<Syllable> syllables=new List<Syllable>();
  public static readonly string[] EasyPhrases={"토끼야 가자", "바다로 가자", "용궁에 도착", "간은 집에", "두고 왔소", "토끼는 폴짝"};
  public static readonly string[] ChallengePhrases={"토끼는 바다로 가자", "거북이 등에 올라", "달빛 따라 둥실둥실", "용궁 잔치 열렸네", "용왕님이 말씀하길", "토끼 간이 필요하오", "토끼 눈이 동그래져", "간은 집에 두었지요", "거북이는 깜짝 놀라", "다시 육지 향했네", "바위 위로 폴짝폴짝", "토끼 웃음 터졌네", "간을 두고 다닌다니", "그런 말이 어디 있소", "거북이는 멍하니", "바다만 바라보네"};
  public readonly string[] phrases;
  static readonly string[] initial={"r","R","s","e","E","f","a","q","Q","t","T","d","w","W","c","z","x","v","g"};
  static readonly string[] vowel={"k","o","i","O","j","p","u","P","h","hk","ho","hl","y","n","nj","np","nl","b","m","ml","l"};
  static readonly string[] final={"","r","R","rt","s","sw","sg","e","f","fr","fa","fq","ft","fx","fv","fg","a","q","qt","t","T","d","w","c","z","x","v","g"};
  const string physical="rsefaqtdwczxvgkoiOjpuPhynbml";
  public static string Jamo(char k){string keys="rRseEf aqQtTd wWczxvgkoiOjpuPhynbml".Replace(" ","");string[] names={"ㄱ","ㄲ","ㄴ","ㄷ","ㄸ","ㄹ","ㅁ","ㅂ","ㅃ","ㅅ","ㅆ","ㅇ","ㅈ","ㅉ","ㅊ","ㅋ","ㅌ","ㅍ","ㅎ","ㅏ","ㅐ","ㅑ","ㅒ","ㅓ","ㅔ","ㅕ","ㅖ","ㅗ","ㅛ","ㅜ","ㅠ","ㅡ","ㅣ"};int n=keys.IndexOf(k);return n>=0?names[n]:k.ToString();}
  public static string Keys(char c){int n=c-0xAC00;if(n<0||n>=11172)return "";return initial[n/588]+vowel[(n%588)/28]+final[n%28];}
  public RhythmChart(bool easy=true,SongTimeline song=null){
   this.easy=easy;timeline=song??SongTimeline.Load(MusicName);SongTimeline.Validate(timeline);
   phrases=easy?EasyPhrases:ChallengePhrases;
   for(int p=0;p<phrases.Length;p++){
    var chars=phrases[p].ToCharArray();
    for(int c=0;c<chars.Length;c++){
     string ks=Keys(chars[c]);if(ks.Length==0)continue;
     int index=syllables.Count;var sy=new Syllable{glyph=chars[c].ToString(),phrase=p,space=c>0&&chars[c-1]==' ',firstStroke=strokes.Count};syllables.Add(sy);
     foreach(char k in ks)strokes.Add(new Stroke{key=k.ToString(),jamo=Jamo(k),syllable=index,phrase=p});
     sy.lastStroke=strokes.Count-1;strokes[sy.lastStroke].last=true;
    }
   }
   SongChartGenerator.Schedule(strokes,timeline,easy);
  }
  // Adjacent timing windows meet at their midpoint, including uneven rhythms.
  public float EarlyWindow(int index)=>index==0?Window:Mathf.Min(Window,(strokes[index].time-strokes[index-1].time)*.49f);
  public float LateWindow(int index)=>index+1==strokes.Count?Window:Mathf.Min(Window,(strokes[index+1].time-strokes[index].time)*.49f);
  public float EndTime=>strokes.Count==0?0:strokes[strokes.Count-1].time;
 }
 public static class Judgement {
  public const float Perfect=.065f,Good=.125f,Window=.185f;
  public static int Grade(float delta){float d=Mathf.Abs(delta);return d<=Perfect?2:d<=Good?1:d<=Window?0:-1;}
  public static int Sales(int score,int maximum){if(maximum<=0)return 0;float rate=Mathf.Clamp01((float)score/maximum);return Mathf.RoundToInt(15+985*rate*rate);}
 }
}
