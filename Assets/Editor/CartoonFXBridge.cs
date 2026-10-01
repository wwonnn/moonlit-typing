using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class CartoonFXBridge {
 // Run after importing the publisher's original free unitypackage through Package Manager.
 [MenuItem("Moonlit/Prepare imported Cartoon FX")]
 public static void Prepare(){
  var candidates=AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
   .Where(p=>!p.StartsWith("Assets/Resources/")&&(p.Contains("CFXR")||p.Contains("Cartoon FX")))
   .Where(p=>p.IndexOf("Hit",StringComparison.OrdinalIgnoreCase)>=0||p.IndexOf("Impact",StringComparison.OrdinalIgnoreCase)>=0||p.IndexOf("Magic Poof",StringComparison.OrdinalIgnoreCase)>=0)
   .Where(p=>p.IndexOf("Blood",StringComparison.OrdinalIgnoreCase)<0&&p.IndexOf("Text",StringComparison.OrdinalIgnoreCase)<0)
   .OrderBy(p=>p.IndexOf("Hit",StringComparison.OrdinalIgnoreCase)>=0?0:1).ThenBy(p=>p).Take(3).ToArray();
  if(candidates.Length==0)throw new Exception("Import the official Cartoon FX Remaster Free package first.");
  Directory.CreateDirectory("Assets/Resources/MoonlitFX");
  foreach(var path in candidates){var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);var clone=UnityEngine.Object.Instantiate(source);clone.name=source.name;
   foreach(var behavior in clone.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(behavior);
   foreach(var light in clone.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(light);
   foreach(var ps in clone.GetComponentsInChildren<ParticleSystem>(true)){var main=ps.main;main.playOnAwake=false;main.loop=false;main.stopAction=ParticleSystemStopAction.None;main.simulationSpeed=1.5f;}
   PrefabUtility.SaveAsPrefabAsset(clone,"Assets/Resources/MoonlitFX/"+source.name+".prefab");UnityEngine.Object.DestroyImmediate(clone);
  }
  File.WriteAllText("cartoon-fx-import.txt",string.Join("\n",candidates)+"\nCamera shake and effect lights removed; particle shaders preserved.\n");AssetDatabase.SaveAssets();Debug.Log("MOONLIT_CARTOON_FX_IMPORTED "+candidates.Length);
 }
}
