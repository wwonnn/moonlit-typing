using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonlit {
 [Serializable] public class Stroke { public string key; public string jamo; public float time; public int syllable; public int phrase; public bool last; }
 [Serializable] public class Syllable { public string glyph; public int phrase; public bool space; public int firstStroke; public int lastStroke; }
 public class RhythmChart {
  public const float BPM=144f, Beat=60f/BPM, Duration=82f;
  public readonly List<Stroke> strokes=new List<Stroke>();
  public readonly List<Syllable> syllables=new List<Syllable>();
  public readonly string[] phrases={"토끼는 바다로 가자", "거북이 등에 올라", "달빛 따라 둥실둥실", "용궁 잔치 열렸네", "용왕님이 말씀하길", "토끼 간이 필요하오", "토끼 눈이 동그래져", "간은 집에 두었지요", "거북이는 깜짝 놀라", "다시 육지 향했네", "바위 위로 폴짝폴짝", "토끼 웃음 터졌네", "간을 두고 다닌다니", "그런 말이 어디 있소", "거북이는 멍하니", "바다만 바라보네"};
  static readonly string[] initial={"r","R","s","e","E","f","a","q","Q","t","T","d","w","W","c","z","x","v","g"};
  static readonly string[] vowel={"k","o","i","O","j","p","u","P","h","hk","ho","hl","y","n","nj","np","nl","b","m","ml","l"};
  static readonly string[] final={"","r","R","rt","s","sw","sg","e","f","fr","fa","fq","ft","fx","fv","fg","a","q","qt","t","T","d","w","c","z","x","v","g"};
  const string physical="rsefaqtdwczxvgkoiOjpuPhynbml";
  public static string Jamo(char k){string keys="rRseEf aqQtTd wWczxvgkoiOjpuPhynbml".Replace(" ","");string[] names={"ㄱ","ㄲ","ㄴ","ㄷ","ㄸ","ㄹ","ㅁ","ㅂ","ㅃ","ㅅ","ㅆ","ㅇ","ㅈ","ㅉ","ㅊ","ㅋ","ㅌ","ㅍ","ㅎ","ㅏ","ㅐ","ㅑ","ㅒ","ㅓ","ㅔ","ㅕ","ㅖ","ㅗ","ㅛ","ㅜ","ㅠ","ㅡ","ㅣ"};int n=keys.IndexOf(k);return n>=0?names[n]:k.ToString();}
  public static string Keys(char c){int n=c-0xAC00;if(n<0||n>=11172)return "";return initial[n/588]+vowel[(n%588)/28]+final[n%28];}
  public RhythmChart(){
   // Two-beat count-in, then phrase blocks with breathing room. Each physical key owns a note.
   int note=0;
   for(int p=0;p<phrases.Length;p++){
    float phraseStart=4*Beat+p*11*Beat;
    var chars=phrases[p].ToCharArray();int count=0;foreach(char c in chars)count+=Keys(c).Length;
    float step=Beat*.5f;
    // Long phrases use eighth notes; short ones leave a longer musical response.
    for(int c=0;c<chars.Length;c++){
     string ks=Keys(chars[c]);if(ks.Length==0)continue;
     int index=syllables.Count;var sy=new Syllable{glyph=chars[c].ToString(),phrase=p,space=c>0&&chars[c-1]==' ',firstStroke=strokes.Count};syllables.Add(sy);
     foreach(char k in ks){strokes.Add(new Stroke{key=k.ToString(),jamo=Jamo(k),time=phraseStart+note*step,syllable=index,phrase=p});note++;}
     sy.lastStroke=strokes.Count-1;strokes[sy.lastStroke].last=true;
    }
    // Keep adjacent phrases from overlapping: advance next phrase's grid if necessary.
    note=0;
   }
   // Schedule phrases on a musical grid, never overlap if a phrase exceeds 11 beats.
   float next=4*Beat;
   for(int p=0;p<phrases.Length;p++){
    float start=next;int n=0;foreach(var s in strokes)if(s.phrase==p){s.time=start+n*Beat*.5f;n++;}
    next=start+Mathf.Max(11,Mathf.Ceil(n*.5f)+1)*Beat;
   }
  }
  public float EndTime=>strokes.Count==0?0:strokes[strokes.Count-1].time;
 }
 public static class Judgement {
  public const float Perfect=.065f,Good=.125f,Window=.185f;
  public static int Grade(float delta){float d=Mathf.Abs(delta);return d<=Perfect?2:d<=Good?1:d<=Window?0:-1;}
  public static int Sales(int score,int maximum){if(maximum<=0)return 0;float rate=Mathf.Clamp01((float)score/maximum);return Mathf.RoundToInt(15+985*rate*rate);}
 }
}
