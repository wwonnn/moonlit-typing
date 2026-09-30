using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Moonlit;
public static class BuildMoonlit {
 public static void BuildAll(){Prepare();BuildWeb();}
 public static void Prepare(){
  Directory.CreateDirectory("Assets/Resources/Motions");Directory.CreateDirectory("Assets/Scenes");
  AssetDatabase.Refresh();
  var bodyImporter=(ModelImporter)AssetImporter.GetAtPath("Assets/Scholar/Models/Scholar-Writing.fbx");bodyImporter.isReadable=true;bodyImporter.SaveAndReimport();
  foreach(var path in Directory.GetFiles("Assets/Scholar/Textures","*.png")){var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Compressed;if(path.Contains("Normal"))ti.textureType=TextureImporterType.NormalMap;ti.SaveAndReimport();}
  foreach(var path in Directory.GetFiles("Assets/Resources/Art","*.png")){var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.maxTextureSize=2048;ti.alphaIsTransparency=true;ti.npotScale=TextureImporterNPOTScale.None;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();}
  foreach(var path in Directory.GetFiles("Assets/Resources/Audio","*.wav")){var ai=(AudioImporter)AssetImporter.GetAtPath(path);var s=ai.defaultSampleSettings;s.loadType=AudioClipLoadType.DecompressOnLoad;s.compressionFormat=AudioCompressionFormat.Vorbis;s.quality=path.Contains("MoonlitPress")?.7f:.9f;s.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;s.preloadAudioData=true;ai.defaultSampleSettings=s;ai.SaveAndReimport();}
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
  var chart=new RhythmChart();for(int i=1;i<chart.strokes.Count;i++)Assert(chart.strokes[i].time>chart.strokes[i-1].time,"Non-monotonic chart");
  var music=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/MoonlitPress.wav");Assert(music&&chart.EndTime+1<music.length,"Chart fits song ending");
  for(int i=1;i<=100;i++)Assert(Judgement.Sales(i,100)>=Judgement.Sales(i-1,100),"Monotonic sales");
  var avatar=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Scholar.prefab").GetComponent<Animator>().avatar;Assert(avatar.isHuman&&avatar.isValid,"Humanoid");
  string report=$"Unity {Application.unityVersion}\nStrokes: {chart.strokes.Count}\nSyllables: {chart.syllables.Count}\nPhrases: {chart.phrases.Length}\nLast note: {chart.EndTime:F3}s\nMusic: {music.length:F3}s\nHangul decomposition, timing boundaries, sequence, sales, Humanoid: PASS\n";File.WriteAllText("validation.txt",report);Debug.Log(report);
 }
 public static void BuildWeb(){Validate();Directory.CreateDirectory("docs");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/MoonlitStudy.unity"},locationPathName="docs",target=BuildTarget.WebGL,options=BuildOptions.None});File.WriteAllText("build-report.txt",report.summary.result+"\n"+report.summary.totalSize+" bytes\n"+report.summary.totalTime);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Web build failed");File.WriteAllText("docs/.nojekyll","");Debug.Log("MOONLIT_WEB_OK");}
 public static void BuildDesktop(){var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/MoonlitStudy.unity"},locationPathName="Builds/Windows/Moonlit.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(r.summary.result!=BuildResult.Succeeded)throw new Exception("Desktop build failed");}
}
