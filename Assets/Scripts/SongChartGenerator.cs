using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonlit {
 [Serializable] public class MusicEvent {
  public float time, strength;
  public string kind, section;
 }
 [Serializable] public class SongTimeline {
  public int schemaVersion, sampleRate;
  public string song, source, audioSha256, analysisAudioSha256;
  public float bpm, duration;
  public MusicEvent[] events;
  public static SongTimeline Load(string song) {
   var asset=Resources.Load<TextAsset>("Charts/"+song);
   if(!asset)throw new InvalidOperationException("Missing song events: "+song+". Regenerate the composition or analyze its audio.");
   var data=JsonUtility.FromJson<SongTimeline>(asset.text);
   Validate(data);
   if(data.song!=song)throw new InvalidOperationException("Wrong song event file: "+song);
   return data;
  }
  public static void Validate(SongTimeline data) {
   if(data==null||data.schemaVersion!=1||!Finite(data.bpm)||data.bpm<=0||!Finite(data.duration)||data.duration<=3||data.events==null||data.events.Length==0)
    throw new InvalidOperationException("Invalid or empty song timeline");
   foreach(var e in data.events)if(e==null||!Finite(e.time)||!Finite(e.strength)||e.time<0||e.time>=data.duration||e.strength<0||e.strength>1||string.IsNullOrEmpty(e.kind))
    throw new InvalidOperationException("Invalid music event");
  }
  static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
 }

 public static class SongChartGenerator {
  public const string Version="music-events-v1";
  // BPM is only a tempo reference. Every selected time must exist in the song events.
  public static float MinimumGap(bool easy,float beat)=>easy?.5f:Mathf.Max(.16f,beat*.5f);
  public static float Gap(Stroke before,Stroke after,bool easy,float beat) {
   if(before.phrase!=after.phrase)return easy?1.75f:2*beat;
   if(before.syllable!=after.syllable&&easy)return .75f;
   return MinimumGap(easy,beat);
  }
  public static List<MusicEvent> Candidates(SongTimeline song) {
   SongTimeline.Validate(song);
   var sorted=new List<MusicEvent>();
   foreach(var e in song.events)if(e.time>=4*60/song.bpm-.0001f&&e.time<=song.duration-3&&e.strength>=.6f)sorted.Add(e);
   sorted.Sort((a,b)=>a.time.CompareTo(b.time));
   var result=new List<MusicEvent>();
   foreach(var e in sorted) {
    if(result.Count>0&&e.time-result[result.Count-1].time<.001f) {
     if(MusicalValue(e)>MusicalValue(result[result.Count-1]))result[result.Count-1]=e;
    }else result.Add(e);
   }
   return result;
  }
  static float MusicalValue(MusicEvent e) {
   float role=e.kind=="melody"?1f:e.kind=="snare"?.7f:e.kind=="tom"?.4f:e.kind=="kick"?.2f:0;
   return e.strength*1.1f+role;
  }
  public static void Schedule(List<Stroke> strokes,SongTimeline song,bool easy) {
   if(strokes.Count==0)throw new InvalidOperationException("The story has no playable keys");
   var events=Candidates(song);int n=strokes.Count,m=events.Count;float beat=60/song.bpm;
   if(m<n)throw new InvalidOperationException("Not enough musical attacks for this text. Shorten the story or use a longer song.");
   float[] cumulative=new float[n];
   for(int i=1;i<n;i++)cumulative[i]=cumulative[i-1]+Gap(strokes[i-1],strokes[i],easy,beat);
   float first=events[0].time,last=events[m-1].time,available=last-first;
   if(cumulative[n-1]>available+.001f)throw new InvalidOperationException("Story exceeds the song's typing budget at this difficulty");
   float[] target=new float[n];
   for(int i=0;i<n;i++)target[i]=first+(n==1?0:cumulative[i]/cumulative[n-1]*available);
   var parent=new int[n,m];var previous=new float[m];
   for(int j=0;j<m;j++){previous[j]=Cost(events[j],strokes[0],target[0],easy);parent[0,j]=-1;}
   for(int i=1;i<n;i++) {
    var current=new float[m];
    float minGap=Gap(strokes[i-1],strokes[i],easy,beat);
    bool phrase=strokes[i-1].phrase!=strokes[i].phrase;
    bool syllable=strokes[i-1].syllable!=strokes[i].syllable;
    bool analyzedAudio=song.source!=null&&song.source.StartsWith("audio-");
    float maxGap=phrase?(easy?8:analyzedAudio?10:4):syllable?(easy?4.5f:analyzedAudio?4:2):easy||analyzedAudio?2.5f:1.25f;
    for(int j=0;j<m;j++) {
     float best=float.PositiveInfinity;int bestIndex=-1;
     for(int k=j-1;k>=0;k--) {
      float gap=events[j].time-events[k].time;
      if(gap>maxGap+.0001f)break;
      if(gap+.0001f<minGap||float.IsPositiveInfinity(previous[k]))continue;
      // Favor recovery between phrases without inventing timestamps in silence.
      float recovery=phrase?Mathf.Max(0,(easy?2.5f:1.2f)-gap)*.5f:0;
      float grouping=easy&&!syllable?(gap-.65f)*(gap-.65f)*2:0;
      float value=previous[k]+recovery+grouping;
      if(value<best){best=value;bestIndex=k;}
     }
     current[j]=best+Cost(events[j],strokes[i],target[i],easy);parent[i,j]=bestIndex;
    }
    previous=current;
   }
   int end=-1;float bestEnd=float.PositiveInfinity;
   for(int j=0;j<m;j++)if(previous[j]<bestEnd){bestEnd=previous[j];end=j;}
   if(end<0)throw new InvalidOperationException("No playable musical path for this story. Adjust text length or event selection.");
   for(int i=n-1;i>=0;i--) {
    var e=events[end];var s=strokes[i];s.time=e.time;s.eventKind=e.kind;s.strength=e.strength;s.section=e.section;
    end=parent[i,end];
   }
  }
  static float Cost(MusicEvent e,Stroke stroke,float target,bool easy) {
   float displacement=e.time-target;
   float importance=stroke.last?1.8f:1;
   float accent=stroke.last&&e.strength>=.95f?.6f:0;
   // Dynamic programming balances audible attacks, completion accents, and full-song coverage.
   float melodyFocus=easy&&e.kind=="melody"?1.1f:0;
   return displacement*displacement*(easy?.3f:.65f)-(MusicalValue(e)+melodyFocus)*importance-accent;
  }
 }
}
