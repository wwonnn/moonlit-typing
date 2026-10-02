using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Moonlit;
public static class BuildMoonlit {
 [Serializable] public class ChartReport {public string generator;public ChartSong[] songs;}
 [Serializable] public class ChartSong {public string song,source,stageId,title,storyTitle,performanceRevision;public int difficulty;public bool easy;public float bpm,duration;public string[] phrases;public Stroke[] strokes;public Syllable[] syllables;
  public ChartSong(RhythmChart chart){difficulty=chart.stage.difficulty;performanceRevision=chart.performance?.revision??"tap-only";stageId=chart.stage.id;title=chart.stage.title;storyTitle=chart.stage.storyTitle;song=chart.MusicName;source=chart.timeline.source;easy=chart.easy;bpm=chart.BPM;duration=chart.timeline.duration;phrases=chart.phrases;strokes=chart.strokes.ToArray();syllables=chart.syllables.ToArray();}}
 public static void ExportCharts(){AssetDatabase.Refresh();var modes=Enumerable.Range(0,StageCatalog.All.Length).Select(i=>new RhythmChart(i)).ToArray();File.WriteAllText("chart-report.json",JsonUtility.ToJson(new ChartReport{generator=SongChartGenerator.Version,songs=modes.Select(c=>new ChartSong(c)).ToArray()},true));}
 public static void BuildAll(){Prepare();BuildWeb();}
 public static void Prepare(){
  Directory.CreateDirectory("Assets/Resources/Motions");Directory.CreateDirectory("Assets/Scenes");
  AssetDatabase.Refresh();
  var bodyImporter=(ModelImporter)AssetImporter.GetAtPath("Assets/Scholar/Models/Scholar-Writing.fbx");bodyImporter.isReadable=true;bodyImporter.SaveAndReimport();
  foreach(var path in Directory.GetFiles("Assets/Scholar/Textures","*.png")){var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Compressed;if(path.Contains("Normal"))ti.textureType=TextureImporterType.NormalMap;ti.SaveAndReimport();}
  foreach(var path in Directory.GetFiles("Assets/Resources/Art","*.png")){var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.maxTextureSize=2048;ti.alphaIsTransparency=true;ti.npotScale=TextureImporterNPOTScale.None;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();}
  foreach(var path in Directory.GetFiles("Assets/Resources/Audio","*.wav")){var ai=(AudioImporter)AssetImporter.GetAtPath(path);var s=ai.defaultSampleSettings;s.loadType=AudioClipLoadType.DecompressOnLoad;s.compressionFormat=AudioCompressionFormat.Vorbis;s.quality=.9f;s.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;s.preloadAudioData=true;ai.defaultSampleSettings=s;ai.SaveAndReimport();}
  var mat=new Material(Shader.Find("Moonlit/ScholarToon"));mat.name="Scholar Moonlight Toon";mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scholar/Textures/BaseColor.png");mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scholar/Textures/Normal.png"));SaveAsset(mat,"Assets/Resources/ScholarMaterial.mat");
  string controllerPath="Assets/Resources/Scholar.controller";if(File.Exists(controllerPath))AssetDatabase.DeleteAsset(controllerPath);var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);var machine=controller.layers[0].stateMachine;
  foreach(var path in Directory.GetFiles("Assets/Scholar/Models","*.fbx").OrderBy(p=>p.Contains("Writing")?0:1)){
   string name=Path.GetFileNameWithoutExtension(path).Replace("Scholar-","");var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));var copy=UnityEngine.Object.Instantiate(clip);copy.name=name;SaveAsset(copy,"Assets/Resources/Motions/"+name+".anim");var state=machine.AddState(name);state.motion=copy;if(name=="Writing")machine.defaultState=state;
  }
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scholar/Models/Scholar-Writing.fbx");var model=UnityEngine.Object.Instantiate(source);model.name="Scholar";var animator=model.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;if(animator.avatar==null||!animator.avatar.isValid||!animator.avatar.isHuman)throw new Exception("Invalid approved scholar humanoid");foreach(var renderer in model.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=mat;PrefabUtility.SaveAsPrefabAsset(model,"Assets/Resources/Scholar.prefab");UnityEngine.Object.DestroyImmediate(model);
  foreach(string shader in new[]{"Standard","Unlit/Texture","UI/Default"}){var keep=new Material(Shader.Find(shader));SaveAsset(keep,"Assets/Resources/Keep_"+shader.Replace('/','_')+".mat");}
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("MoonlitGame").AddComponent<MoonlitGame>();EditorSceneManager.SaveScene(scene,"Assets/Scenes/MoonlitStudy.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/MoonlitStudy.unity",true)};
  PlayerSettings.companyName="Moonlit Press";PlayerSettings.productName="달빛 인쇄소";PlayerSettings.bundleVersion="0.1.0";PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.runInBackground=false;
  PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;PlayerSettings.WebGL.dataCaching=true;PlayerSettings.WebGL.memorySize=256;PlayerSettings.WebGL.template="PROJECT:Moonlit";
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL,ManagedStrippingLevel.Low);
  var so=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=so.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;so.ApplyModifiedPropertiesWithoutUndo();}
  AssetDatabase.SaveAssets();Validate();Debug.Log("MOONLIT_PREPARE_OK");
 }
 static void SaveAsset(UnityEngine.Object obj,string path){if(File.Exists(path))AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(obj,path);}
 public static void Validate(){
  void Assert(bool ok,string message){if(!ok)throw new Exception(message);}
  Assert(RhythmChart.Keys('아')=="dk","Ah is two physical strokes");Assert(RhythmChart.Keys('값')=="rkqt","Compound final decomposition");Assert(RhythmChart.Keys('왜')=="dho","Compound vowel decomposition");Assert(RhythmChart.Keys('꼬')=="Rh","Shift consonant decomposition");
  Assert(Judgement.Grade(.064f)==2&&Judgement.Grade(.124f)==1&&Judgement.Grade(.184f)==0&&Judgement.Grade(.19f)==-1,"Judgment boundaries");
  var chart=new RhythmChart();var hard=new RhythmChart(1);var door=new RhythmChart(2);var modes=new[]{chart,hard,door};
  var export=new ChartReport{generator=SongChartGenerator.Version,songs=modes.Select(c=>new ChartSong(c)).ToArray()};
  File.WriteAllText("chart-report.json",JsonUtility.ToJson(export,true));
  string details="";
  foreach(var mode in modes){
   Assert(mode.stage.difficulty>=1&&mode.stage.difficulty<=5&&mode.stage.Stars.Length==5,"Five-star song difficulty display");
   for(int i=1;i<mode.strokes.Count;i++)Assert(mode.strokes[i].time>mode.strokes[i-1].time,"Non-monotonic chart");
   var song=AssetDatabase.LoadAssetAtPath<AudioClip>(mode.stage.AudioAssetPath);Assert(song&&mode.EndTime+1<song.length,"Chart fits song ending: "+mode.Title);
   foreach(var sy in mode.syllables)Assert(mode.strokes[sy.lastStroke].last&&sy.lastStroke-sy.firstStroke+1==RhythmChart.Keys(sy.glyph[0]).Length,"Physical key group matches syllable");
   var timeline=mode.timeline;
   Assert(Mathf.Abs(timeline.duration-song.length)<.01f,"Song event duration matches imported audio");
   using(var sha=SHA256.Create()){
    string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(mode.stage.AudioAssetPath))).Replace("-","").ToLowerInvariant();
    Assert(hash==timeline.audioSha256,"Music event file belongs to this exact audio: "+mode.MusicName);
   }
   var candidates=SongChartGenerator.Candidates(timeline);
   foreach(var stroke in mode.strokes)Assert(candidates.Any(e=>Mathf.Abs(e.time-stroke.time)<.0001f&&e.kind==stroke.eventKind),"Every note is an actual instrument attack");
   for(int i=1;i<mode.strokes.Count;i++){
    float gap=mode.strokes[i].time-mode.strokes[i-1].time;
    Assert(gap+.0001f>=SongChartGenerator.Gap(mode.strokes[i-1],mode.strokes[i],mode.easy,mode.Beat,mode.stage.difficulty),"Typing/rest budget respected");
    Assert(mode.LateWindow(i-1)+mode.EarlyWindow(i)<gap,"Adjacent judgement windows never overlap");
   }
   var repeated=new RhythmChart(mode.stageIndex);
   Assert(repeated.strokes.Select(x=>x.time).SequenceEqual(mode.strokes.Select(x=>x.time)),"Same song produces a deterministic chart");
   details+=$"{mode.Title}: {mode.BPM} BPM, {mode.strokes.Count} keys, {mode.syllables.Count} syllables, last {mode.EndTime:F3}s / song {song.length:F3}s\n";
  }
  Assert(chart.strokes.All(n=>n.articulation=="tap"),"Tutorial remains tap-only");
  var holds=hard.strokes.Where(n=>n.articulation=="hold").ToArray();
  Assert(holds.Length==13&&hard.strokes.Count(n=>n.articulation=="accent")==18,"Authored full-song hold/accent arrangement");
  Assert(hard.strokes[0].articulation=="hold"&&hard.strokes[1].syncopated&&hard.strokes[2].syncopated,"Opening held brush answers with offbeat pair");
  foreach(var mode in modes.Where(m=>!m.easy))for(int i=0;i<mode.strokes.Count;i++){var n=mode.strokes[i];if(n.articulation!="hold")continue;
   Assert(!n.last&&n.endTime>n.time&&n.endTime+mode.ReleaseWindow(i)<mode.strokes[i+1].time-mode.EarlyWindow(i+1),"Hold tail and next key windows are disjoint");
   var held=new HoldJudgement(n,2,mode.ReleaseWindow(i));
   Assert(held.Matches(n.key.ToLower())&&held.Release(n.endTime)==2,"Physical key identity and exact release");
   Assert(held.Release(n.time+.05f)==-1,"A tap cannot clear a hold");
   Assert(held.Release(n.endTime+held.releaseWindow+.01f)==-1&&held.Expired(n.endTime+held.releaseWindow+.01f),"Late or missing release fails");
   var imperfect=new HoldJudgement(n,1,mode.ReleaseWindow(i));Assert(imperfect.Release(n.endTime)==1,"Tail cannot improve an inaccurate head");
   held.needsGrip=true;Assert(held.Release(n.endTime)==-1,"Focus loss cannot leave a phantom held key");held.needsGrip=false;Assert(held.Release(n.endTime)==2,"Fresh grip can resume a paused hold");
  }
  Assert(Mathf.Approximately(MoonlitGame.SfxGain,1.12f),"Sound effects raised 12 percent");
  var brushClip=Resources.Load<AudioClip>("Audio/BrushFriction");Assert(brushClip&&Mathf.Abs(brushClip.length-2)<.01f,"Loopable brush friction imported");
  details+="Hold heads, release bounds, early break, timeout, Shift identity, paused grip, 13 holds / 18 accents / opening offbeats, SFX +12%: PASS\n";
  var gaps=chart.strokes.Skip(1).Select((n,i)=>n.time-chart.strokes[i].time).ToArray();
  Assert(chart.strokes.Count<hard.strokes.Count*.3f,"Easy has substantially fewer notes");
  Assert(gaps.Min()>=.499f&&gaps.Count(g=>g>=.99f)>=15,"Easy has real rests and a safe key rate");
  Assert(gaps.Select(g=>Mathf.RoundToInt(g*1000)).Distinct().Count()>=4,"Easy rhythm has varied note intervals");
  Assert(chart.strokes.Count(s=>Mathf.Abs(s.time/chart.Beat-Mathf.Round(s.time/chart.Beat))>.1f)>=8,"Easy follows audible offbeats, not only a BPM grid");
  Assert(chart.Window*2<gaps.Min(),"Easy judgement windows do not overlap");
  Assert(chart.Grade(.099f)==2&&chart.Grade(.179f)==1&&chart.Grade(.239f)==0&&chart.Grade(.241f)==-1,"Easy judgement boundaries");
  Assert(chart.strokes.Count==67&&chart.syllables.Count==28,"Tutorial retains its original text and key count");
  Assert(hard.strokes.Count==hard.phrases.Sum(p=>p.Sum(c=>RhythmChart.Keys(c).Length)),"New stage keeps every physical key in the story");
  Assert(hard.stage.id=="shining-moment"&&hard.timeline.source=="audio-spectral-flux-v2","New stage uses provided audio analysis");
  Assert(!File.Exists("Assets/Resources/Audio/MoonlitPress.wav")&&!File.Exists("Assets/Resources/Charts/MoonlitPress.json"),"Removed challenge song is absent");
  Assert(chart.timeline.audioSha256=="4bfab71ec6e2657d7537a03d6952ccf60eb3718ec99bd06e254840065e667cb3","Tutorial music remains byte-identical");
  Assert(hard.timeline.audioSha256=="e83792b84488f9cdac039295a9c6c4e8111bd47dbd24e52f17e10769f903cd9f","Provided song remains byte-identical");
  Assert(door.stage.id=="door-clicks"&&door.timeline.source=="audio-spectral-flux-v2","Second stage uses supplied song analysis");
  Assert(door.strokes.Count/door.timeline.duration>hard.strokes.Count/hard.timeline.duration*1.15f,"Second stage has higher average key density");
  Assert(door.strokes.Count(n=>n.articulation=="hold")>=4&&door.strokes.Count(n=>n.articulation=="accent")>=18&&door.strokes.Count(n=>n.syncopated)>=8,"Second stage has held brush, accents and offbeats");
  Assert(door.strokes.Skip(1).Where((n,i)=>n.phrase==door.strokes[i].phrase&&n.time-door.strokes[i].time<.45f).Count()>=15,"Second stage has rhythmic short bursts");
  Assert(door.stage.difficulty>hard.stage.difficulty&&hard.stage.difficulty>chart.stage.difficulty,"Song difficulty increases by stars");
  details+=$"Second-stage density {door.strokes.Count/door.timeline.duration:F2} keys/s; {door.strokes.Count(n=>n.articulation=="hold")} holds / {door.strokes.Count(n=>n.articulation=="accent")} accents / {door.strokes.Count(n=>n.syncopated)} offbeats; stars 1/3/4: PASS\n";
  foreach(var mode in modes){int col=0,line=0;for(int i=0;i<mode.syllables.Count;i++){var sy=mode.syllables[i];if(sy.space)col++;if(i>0&&sy.phrase!=mode.syllables[i-1].phrase){line++;col=0;}if(col>=12){col=0;line++;}Assert(line<22,"Story fits two book pages");col++;}}
  var changed=new SongTimeline{schemaVersion=1,bpm=chart.BPM,duration=chart.timeline.duration+.125f,source="test-shifted-audio",song=chart.MusicName,sampleRate=32000,
   events=chart.timeline.events.Select(e=>new MusicEvent{time=e.time+.125f,strength=e.strength,kind=e.kind,section=e.section}).ToArray()};
  var shifted=new RhythmChart(0,changed);
  Assert(shifted.BPM==chart.BPM&&shifted.strokes.Where((s,i)=>Mathf.Abs(s.time-chart.strokes[i].time)>.01f).Count()>chart.strokes.Count/2,"Changing music events changes notes even at the same BPM");
  bool rejected=false;try{var impossible=new SongTimeline{schemaVersion=1,bpm=120,duration=5,events=new[]{new MusicEvent{time=2,strength=1,kind="melody"}}};new RhythmChart(0,impossible);}catch(InvalidOperationException){rejected=true;}
  Assert(rejected,"An impossible song/text budget is rejected; no fixed-grid fallback");
  details+=$"Music-driven chart: easy {gaps.Select(g=>Mathf.RoundToInt(g*1000)).Distinct().Count()} gap lengths, {chart.strokes.Count(s=>Mathf.Abs(s.time/chart.Beat-Mathf.Round(s.time/chart.Beat))>.1f)} offbeat keys; exact audio hashes and same-BPM mutation: PASS\n";
  foreach(var path in Directory.GetFiles("Assets/Resources/Audio/Impacts","*.ogg")){var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);Assert(clip&&clip.length<2,"Impact sound short and imported: "+path);}
  for(int i=1;i<=100;i++)Assert(Judgement.Sales(i,100)>=Judgement.Sales(i-1,100),"Monotonic sales");
  var avatar=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Scholar.prefab").GetComponent<Animator>().avatar;Assert(avatar.isHuman&&avatar.isValid,"Humanoid");
  var fxPrefabs=Resources.LoadAll<GameObject>("MoonlitFX");Assert(fxPrefabs.Length==3,"Import official Cartoon FX, then run CartoonFXBridge.Prepare");
  foreach(var prefab in fxPrefabs){Assert(prefab.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"No shake/destroy scripts in pooled FX");Assert(prefab.GetComponentsInChildren<Light>(true).Length==0,"No effect light changes to scholar stage");var systems=prefab.GetComponentsInChildren<ParticleSystem>(true);Assert(systems.Length>0,"Actual particle systems present");foreach(var ps in systems){Assert(!ps.main.loop,"FX must be one-shot");var renderer=ps.GetComponent<ParticleSystemRenderer>();if(renderer.enabled&&renderer.renderMode!=ParticleSystemRenderMode.None)Assert(renderer.sharedMaterial&&renderer.sharedMaterial.shader,"Particle material resolved: "+prefab.name+"/"+ps.name);if(ps.trails.enabled)Assert(renderer.trailMaterial&&renderer.trailMaterial.shader,"Particle trail material resolved: "+ps.name);}}
  details+="Cartoon FX: 3 official particle prefabs; materials resolved; no stage shake/light scripts: PASS\n";
  string report=$"Unity {Application.unityVersion}\n"+details+"Hangul groups, all three songs, easy rests and spacing, judgement windows, sales, Humanoid, impact imports: PASS\n";File.WriteAllText("validation.txt",report);Debug.Log(report);
 }
 public static void PrepareUpdate(){
  AssetDatabase.Refresh();
  foreach(var path in Directory.GetFiles("Assets/Resources/Art/Splat","*.png")){var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.maxTextureSize=256;ti.alphaIsTransparency=true;ti.npotScale=TextureImporterNPOTScale.None;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();}
  foreach(var path in Directory.GetFiles("Assets/Resources/Audio","*",SearchOption.AllDirectories).Where(p=>p.EndsWith(".wav")||p.EndsWith(".ogg")||p.EndsWith(".mp3"))){var ai=(AudioImporter)AssetImporter.GetAtPath(path);var sample=ai.defaultSampleSettings;sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=path.Contains("Moonlit")?.75f:.95f;sample.preloadAudioData=true;ai.defaultSampleSettings=sample;ai.SaveAndReimport();}
  PlayerSettings.bundleVersion="0.6.0";AssetDatabase.SaveAssets();BuildWeb();
 }
 public static void BuildWeb(){PlayerSettings.bundleVersion="0.6.0";Validate();Directory.CreateDirectory("docs");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/MoonlitStudy.unity"},locationPathName="docs",target=BuildTarget.WebGL,options=BuildOptions.None});File.WriteAllText("build-report.txt",report.summary.result+"\n"+report.summary.totalSize+" bytes\n"+report.summary.totalTime);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Web build failed");File.WriteAllText("docs/.nojekyll","");Debug.Log("MOONLIT_WEB_OK");}
 public static void BuildDesktop(){var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/MoonlitStudy.unity"},locationPathName="Builds/Windows/Moonlit.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(r.summary.result!=BuildResult.Succeeded)throw new Exception("Desktop build failed");}
}
