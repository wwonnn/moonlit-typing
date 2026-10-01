using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Moonlit {
 // Actual Kenney sprites plus actual imported Cartoon FX particle prefabs.
 // A dedicated transparent render texture places particles over uGUI without moving the stage.
 public class MoonlitVFX : MonoBehaviour {
  class Mark { public Image image;public float age,life,size;public Color color; }
  class Effect { public GameObject root;public ParticleSystem[] systems;public float life;public Vector3 scale; }
  readonly List<Mark> marks=new List<Mark>();readonly List<Effect> effects=new List<Effect>();
  readonly List<Sprite> splats=new List<Sprite>();
  RenderTexture texture;Camera fxCamera;bool paused;int sequence,effectSequence;
  public bool CartoonReady=>effects.Count>0;
  public void Build(MoonlitUI ui) {
   foreach(var tex in Resources.LoadAll<Texture2D>("Art/Splat"))splats.Add(Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),Vector2.one*.5f,100));
   for(int i=0;i<24;i++){
    var im=ui.Panel(ui.fxRoot,"Kenney ink splash",0,0,80,80,Color.clear);im.rectTransform.pivot=Vector2.one*.5f;im.gameObject.SetActive(false);marks.Add(new Mark{image=im});
   }
   var prefabs=Resources.LoadAll<GameObject>("MoonlitFX");
   if(prefabs.Length==0){Debug.LogWarning("Cartoon FX not yet imported. Kenney splats active.");return;}
   texture=new RenderTexture(1280,720,16,RenderTextureFormat.ARGB32);texture.name="CartoonFX UI composite";texture.Create();
   fxCamera=new GameObject("Cartoon FX Camera").AddComponent<Camera>();fxCamera.transform.position=new Vector3(0,0,-10);fxCamera.orthographic=true;fxCamera.orthographicSize=4.5f;fxCamera.aspect=16f/9;fxCamera.cullingMask=1<<9;fxCamera.clearFlags=CameraClearFlags.SolidColor;fxCamera.backgroundColor=Color.clear;fxCamera.targetTexture=texture;fxCamera.allowHDR=false;fxCamera.depth=3;
   var rt=MoonlitUI.Rect(ui.fxRoot,"Cartoon FX overlay",0,0,1600,900);var display=rt.gameObject.AddComponent<RawImage>();display.texture=texture;display.raycastTarget=false;
   for(int i=0;i<12;i++){
    var go=Instantiate(prefabs[i%prefabs.Length]);go.name="Pooled "+prefabs[i%prefabs.Length].name;
    foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=9;
    // Defensive: imported effects cannot inject camera shake, lights or destroy the pool.
    foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
    foreach(var l in go.GetComponentsInChildren<Light>(true))l.enabled=false;
    var systems=go.GetComponentsInChildren<ParticleSystem>(true);
    foreach(var ps in systems){var main=ps.main;main.playOnAwake=false;main.loop=false;main.stopAction=ParticleSystemStopAction.None;main.useUnscaledTime=true;}
    go.SetActive(false);effects.Add(new Effect{root=go,systems=systems,scale=go.transform.localScale});
   }
   Debug.Log("MOONLIT_CARTOON_FX_READY "+prefabs.Length);
  }
  public void Ink(Vector2 point,bool large,bool reduced) {
   if(splats.Count==0)return;
   foreach(var m in marks){if(m.life>0)continue;m.age=0;m.life=reduced?.2f:.38f;m.size=large?125:74;m.color=new Color(.075f,.13f,.16f,large?.38f:.30f);m.image.sprite=splats[sequence++%splats.Count];m.image.color=m.color;m.image.rectTransform.anchoredPosition=new Vector2(point.x,-point.y);m.image.rectTransform.localRotation=Quaternion.Euler(0,0,sequence*71);m.image.gameObject.SetActive(true);break;}
   if(!reduced)Particle(point,large?1.05f:.65f);
  }
  public void Particle(Vector2 point,float scale) {
   for(int i=0;i<effects.Count;i++){int index=(effectSequence+i)%effects.Count;var fx=effects[index];if(fx.life>0)continue;effectSequence=(index+1)%effects.Count;fx.root.transform.position=new Vector3((point.x-800)/100,(450-point.y)/100,0);fx.root.transform.localScale=fx.scale*scale;fx.root.SetActive(true);fx.life=1.25f;foreach(var ps in fx.systems){ps.Clear(false);ps.Play(false);if(paused)ps.Pause(false);}break;}
  }
  public void Tick(float dt) {
   foreach(var m in marks){if(m.life<=0)continue;m.age+=dt;float u=Mathf.Clamp01(m.age/m.life);m.image.rectTransform.sizeDelta=Vector2.one*m.size*Mathf.Lerp(.12f,1.2f,1-Mathf.Pow(1-u,4));var c=m.color;c.a*=Mathf.Pow(1-u,1.5f);m.image.color=c;if(u>=1){m.life=0;m.image.gameObject.SetActive(false);}}
   foreach(var fx in effects){if(fx.life<=0)continue;fx.life-=dt;if(fx.life<=0)fx.root.SetActive(false);}
  }
  public void SetPaused(bool value){paused=value;foreach(var fx in effects)if(fx.life>0)foreach(var ps in fx.systems){if(value)ps.Pause(false);else ps.Play(false);}}
  public void Clear(){paused=false;foreach(var m in marks){m.life=0;m.image.gameObject.SetActive(false);}foreach(var fx in effects){fx.life=0;fx.root.SetActive(false);}}
  void OnDestroy(){if(texture){texture.Release();Destroy(texture);}if(fxCamera)Destroy(fxCamera.gameObject);foreach(var fx in effects)if(fx.root)Destroy(fx.root);foreach(var s in splats)Destroy(s);}
 }
}
