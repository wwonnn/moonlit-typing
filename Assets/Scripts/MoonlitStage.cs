using UnityEngine;
using UnityEngine.Rendering;
namespace Moonlit {
 public class MoonlitStage : MonoBehaviour {
  public Animator actor; public Camera view; public Light lantern; public Transform brush; public float pulse;
  Vector3 cameraBase; Quaternion cameraRot; Transform hand,paperSheet; bool paperPlaced; Material glow;
  public void Build(){
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.13f,.17f,.23f);RenderSettings.ambientEquatorColor=new Color(.08f,.10f,.13f);RenderSettings.ambientGroundColor=new Color(.06f,.055f,.05f);
   QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=15;
   view=new GameObject("Main Camera").AddComponent<Camera>();view.tag="MainCamera";view.transform.position=new Vector3(2.5f,1.85f,3.3f);view.transform.LookAt(new Vector3(0,.85f,.25f));view.fieldOfView=34;view.nearClipPlane=.08f;view.farClipPlane=50;view.clearFlags=CameraClearFlags.Depth;view.depth=1;view.cullingMask=~((1<<8)|(1<<9));var projection=view.projectionMatrix;projection.m02=.5f;view.projectionMatrix=projection;view.backgroundColor=new Color(.025f,.045f,.08f);view.allowHDR=true;view.gameObject.AddComponent<AudioListener>();cameraBase=view.transform.position;cameraRot=view.transform.rotation;
   var bgCamera=new GameObject("Backdrop Camera").AddComponent<Camera>();bgCamera.transform.position=new Vector3(0,0,-30);bgCamera.orthographic=true;bgCamera.orthographicSize=4.5f;bgCamera.cullingMask=1<<8;bgCamera.depth=0;bgCamera.clearFlags=CameraClearFlags.SolidColor;bgCamera.backgroundColor=new Color(.025f,.045f,.08f);
   var bg=Resources.Load<Texture2D>("Art/NightStudy");if(bg){var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Painted night architecture";quad.layer=8;quad.transform.position=new Vector3(0,0,-20);quad.transform.localScale=new Vector3(16,9,1);var m=new Material(Shader.Find("Unlit/Texture"));m.mainTexture=bg;quad.GetComponent<Renderer>().material=m;Destroy(quad.GetComponent<Collider>());}
   var key=Light("Warm desk key",LightType.Directional,new Color(1,.85f,.68f),.92f,new Vector3(-2,3,3));key.transform.rotation=Quaternion.Euler(32,-32,0);key.shadows=LightShadows.Soft;key.shadowBias=.045f;
   var rim=Light("Moon blue rim",LightType.Directional,new Color(.47f,.63f,1),.38f,new Vector3(1,3,-2));rim.transform.rotation=Quaternion.Euler(28,145,0);
   lantern=Light("Lantern flicker",LightType.Point,new Color(1,.66f,.36f),.75f,new Vector3(-.68f,1.03f,.85f));lantern.range=2.3f;
   var prefab=Resources.Load<GameObject>("Scholar");if(prefab){var model=Instantiate(prefab);model.name="Approved Scholar";actor=model.GetComponent<Animator>();actor.applyRootMotion=false;actor.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    foreach(var mesh in model.GetComponentsInChildren<SkinnedMeshRenderer>()){mesh.updateWhenOffscreen=true;var copy=Instantiate(mesh.sharedMesh);var weights=copy.boneWeights;var colors=new Color[copy.vertexCount];for(int i=0;i<colors.Length;i++){float w=0;if(i<weights.Length){var bw=weights[i];int[] ids={bw.boneIndex0,bw.boneIndex1,bw.boneIndex2,bw.boneIndex3};float[] ws={bw.weight0,bw.weight1,bw.weight2,bw.weight3};for(int j=0;j<4;j++){string n=mesh.bones[ids[j]].name;if(n.Contains("Hips")||n.Contains("Leg")||n.Contains("Foot")||n.Contains("Toe"))w+=ws[j];}}colors[i]=new Color(w>=.7f?1:0,0,0,1);}copy.colors=colors;mesh.sharedMesh=copy;}
    hand=actor.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);actor.Play("SeatedIdle");
   }
   // Desk coordinates match the corrected writing pose and lower-body shader mask.
   Box("Desktop",new Vector3(1.28f,.075f,.72f),new Vector3(0,.72f,.68f),new Color(.23f,.135f,.085f));
   for(int i=0;i<7;i++)Box("Desk grain",new Vector3(1.24f,.002f,.003f),new Vector3(0,.759f,.39f+i*.09f),new Color(.12f,.08f,.06f));
   foreach(float x in new[]{-.52f,.52f})foreach(float z in new[]{.4f,.96f})Box("Desk leg",new Vector3(.06f,.69f,.06f),new Vector3(x,.35f,z),new Color(.16f,.10f,.075f));
   Box("Chair",new Vector3(.49f,.06f,.44f),new Vector3(.05f,.57f,.14f),new Color(.17f,.11f,.075f));
   paperSheet=Box("Writing paper",new Vector3(.34f,.013f,.25f),new Vector3(.015f,.768f,.76f),new Color(.82f,.78f,.65f)).transform;
   Box("Ink stone",new Vector3(.13f,.024f,.085f),new Vector3(.37f,.775f,.68f),new Color(.025f,.03f,.033f));
   for(int i=0;i<3;i++){Box("Bound books",new Vector3(.23f,.025f,.16f),new Vector3(-.42f,.78f+i*.028f,.67f),new Color(.3f+i*.055f,.34f+i*.03f,.34f));}
   Box("Lantern base",new Vector3(.12f,.025f,.12f),new Vector3(-.53f,.78f,.46f),new Color(.1f,.065f,.04f));
   var paper=Box("Lantern paper",new Vector3(.095f,.22f,.095f),new Vector3(-.53f,.9f,.46f),new Color(.95f,.65f,.28f));glow=paper.GetComponent<Renderer>().material;glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.49f,.14f)*1.6f);
   foreach(float x in new[]{-.585f,-.475f})foreach(float z in new[]{.405f,.515f})Box("Lantern frame",new Vector3(.012f,.24f,.012f),new Vector3(x,.9f,z),new Color(.12f,.07f,.04f));
   var go=new GameObject("Hand-held writing brush");brush=go.transform;var shaft=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shaft.transform.SetParent(brush,false);shaft.transform.localScale=new Vector3(.009f,.115f,.009f);shaft.transform.localPosition=new Vector3(0,.035f,0);shaft.GetComponent<Renderer>().material=Mat(new Color(.29f,.14f,.065f));Destroy(shaft.GetComponent<Collider>());var tip=GameObject.CreatePrimitive(PrimitiveType.Sphere);tip.transform.SetParent(brush,false);tip.transform.localScale=new Vector3(.012f,.033f,.012f);tip.transform.localPosition=new Vector3(0,-.093f,0);tip.GetComponent<Renderer>().material=Mat(new Color(.025f,.03f,.035f));Destroy(tip.GetComponent<Collider>());
  }
  Material Mat(Color c){var m=new Material(Shader.Find("Standard"));m.color=c;m.SetFloat("_Glossiness",.12f);return m;}
  GameObject Box(string n,Vector3 scale,Vector3 pos,Color color){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.position=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().material=Mat(color);o.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.On;Destroy(o.GetComponent<Collider>());return o;}
  Light Light(string n,LightType type,Color color,float intensity,Vector3 pos){var l=new GameObject(n).AddComponent<Light>();l.type=type;l.color=color;l.intensity=intensity;l.transform.position=pos;return l;}
  public void Motion(string name,float fade=.12f){if(actor)actor.CrossFade(name,fade);if(brush)brush.gameObject.SetActive(name=="Writing"||name=="SeatedIdle");}
  void LateUpdate(){if(lantern)lantern.intensity=.75f+Mathf.Sin(Time.unscaledTime*7.3f)*.045f;if(hand&&brush){if(!paperPlaced&&actor.GetCurrentAnimatorStateInfo(0).IsName("Writing")&&actor.GetCurrentAnimatorStateInfo(0).normalizedTime>.12f){paperSheet.position=new Vector3(hand.position.x,.778f,Mathf.Clamp(hand.position.z,.48f,.86f));paperPlaced=true;}brush.position=hand.position;brush.rotation=Quaternion.Euler(0,0,-8);}if(view){view.transform.position=cameraBase;view.transform.rotation=cameraRot;}}
 }
}
