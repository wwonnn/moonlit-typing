using System;
using UnityEngine;

namespace Moonlit {
 [Serializable] public class NoteDirection {
  public int index;
  public float time=-1, endTime;
  public string articulation="tap";
  public bool syncopated;
 }
 [Serializable] public class MusicPhrase {public float start,end;public string label;}
 [Serializable] public class PerformanceData {
  public string revision,audioSha256,analysisAudioSha256,tailSource;
  public NoteDirection[] notes;
  public MusicPhrase[] sections;
 }
 public static class NotePerformance {
  public const string Revision="brush-performance-v1";
  public static void Apply(RhythmChart chart) {
   if(chart.easy)return;
   var asset=Resources.Load<TextAsset>("Performances/"+chart.MusicName);
   if(!asset)throw new InvalidOperationException("Missing music arrangement: "+chart.MusicName);
   var data=JsonUtility.FromJson<PerformanceData>(asset.text);
   if(data.revision!=Revision||data.audioSha256!=chart.timeline.audioSha256||data.analysisAudioSha256!=chart.timeline.analysisAudioSha256||data.tailSource!="audio-energy-release")throw new InvalidOperationException("Arrangement belongs to a different song/revision");
   chart.performance=data;
   var used=new bool[chart.strokes.Count];
   foreach(var direction in data.notes){
    if(direction.index<0||direction.index>=used.Length||used[direction.index])throw new InvalidOperationException("Duplicate/invalid arrangement note");
    used[direction.index]=true;var n=chart.strokes[direction.index];
    if(direction.time>=0){var e=Array.Find(chart.timeline.events,x=>Mathf.Abs(x.time-direction.time)<.0001f);if(e==null)throw new InvalidOperationException("Arrangement invented an inaudible timestamp");n.time=e.time;n.strength=e.strength;n.eventKind=e.kind;}
    n.articulation=direction.articulation;n.endTime=direction.endTime;n.syncopated=direction.syncopated;
   }
   for(int i=0;i<chart.strokes.Count;i++){
    var n=chart.strokes[i];
    if(n.articulation!="tap"&&n.articulation!="hold"&&n.articulation!="accent")throw new InvalidOperationException("Unknown note articulation");
    if(i>0&&n.time-chart.strokes[i-1].time+.0001f<SongChartGenerator.Gap(chart.strokes[i-1],n,false,chart.Beat,chart.stage.difficulty))throw new InvalidOperationException("Arrangement violates typing budget");
    if(n.articulation=="hold"){
     if(n.last||i+1>=chart.strokes.Count||n.endTime-n.time<.449f||chart.strokes[i+1].time-n.endTime<.32f)throw new InvalidOperationException("Unsafe hold/letter completion overlap");
     if(float.IsNaN(n.endTime)||float.IsInfinity(n.endTime)||n.endTime>=chart.timeline.duration)throw new InvalidOperationException("Invalid audio release time");
    }
   }
  }
  public static string Phrase(RhythmChart chart,float time){if(chart.performance?.sections==null)return "";foreach(var s in chart.performance.sections)if(time>=s.start&&time<s.end)return s.label;return "";}
 }
 // One physical key remains one scored note. Holding never creates repeated letters.
 public sealed class HoldJudgement {
  public readonly string key;
  public readonly float endTime,releaseWindow;
  public readonly int headGrade;
  public bool needsGrip;
  public HoldJudgement(Stroke note,int grade,float releaseWindow){key=note.key;endTime=note.endTime;headGrade=grade;this.releaseWindow=releaseWindow;}
  public bool Matches(string releasedKey)=>string.Equals(key,releasedKey,StringComparison.OrdinalIgnoreCase);
  public int Release(float time){float d=Mathf.Abs(time-endTime);if(needsGrip||d>releaseWindow)return -1;return Mathf.Min(headGrade,d<=.11f?2:d<=.20f?1:0);}
  public bool Expired(float time)=>time>endTime+releaseWindow;
 }
}
