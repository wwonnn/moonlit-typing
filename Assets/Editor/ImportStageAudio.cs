using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// Decode with Unity itself so analysis shares the exact MP3 trimming/playback timeline.
public static class ImportStageAudio {
 [Serializable] public class AudioReport {
  public string assetPath, audioSha256;
  public int sampleRate, channels, samples;
  public float duration, peak, rms;
 }
 public static void ExportShiningMoment() {
  const string path="Assets/Resources/Audio/ShiningMoment.mp3";
  AssetDatabase.Refresh();
  var importer=(AudioImporter)AssetImporter.GetAtPath(path);
  if(!importer)throw new InvalidOperationException("Copy the supplied MP3 to "+path);
  var settings=importer.defaultSampleSettings;
  settings.loadType=AudioClipLoadType.DecompressOnLoad;
  settings.compressionFormat=AudioCompressionFormat.PCM;
  settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
  settings.preloadAudioData=true;
  importer.defaultSampleSettings=settings;importer.loadInBackground=false;importer.SaveAndReimport();
  var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
  clip.LoadAudioData();var data=new float[clip.samples*clip.channels];
  if(!clip.GetData(data,0))throw new InvalidOperationException("Could not decode supplied MP3");
  Directory.CreateDirectory(".local");
  // Mono 16-bit analysis WAV; the original stereo MP3 remains the game soundtrack.
  using(var writer=new BinaryWriter(File.Create(".local/ShiningMoment-analysis.wav"))) {
   int bytes=clip.samples*2;
   writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+bytes);
   writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);
   writer.Write((short)1);writer.Write((short)1);writer.Write(clip.frequency);writer.Write(clip.frequency*2);
   writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(bytes);
   for(int i=0;i<clip.samples;i++) {
    float mono=0;for(int c=0;c<clip.channels;c++)mono+=data[i*clip.channels+c];mono/=clip.channels;
    writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(mono,-1,1)*32767));
   }
  }
  float peak=0;double energy=0;foreach(float sample in data){peak=Mathf.Max(peak,Mathf.Abs(sample));energy+=sample*sample;}
  string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
  var report=new AudioReport{assetPath=path,audioSha256=hash,sampleRate=clip.frequency,channels=clip.channels,samples=clip.samples,duration=clip.length,peak=peak,rms=(float)Math.Sqrt(energy/data.Length)};
  File.WriteAllText(".local/ShiningMoment-audio.json",JsonUtility.ToJson(report,true));
  // Compress for the playable build after the exact analysis PCM has been exported.
  settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
  Debug.Log("MOONLIT_MP3_DECODE_OK "+JsonUtility.ToJson(report));
 }
}
