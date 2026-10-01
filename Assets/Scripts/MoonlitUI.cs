using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Moonlit {
 public class MoonlitUI:MonoBehaviour {
  public RectTransform root,book,overlay,fxRoot; public Text combo,judge,title,progressText,phraseText,subline,scoreText;public Image progress;public Font font,serif;public Action onStart,onDemo,onPause,onRetry,onResume,onMenu;public Action<bool> onDifficulty;public MoonlitVFX effects;public Action<float> onOffset,onVolume;public Action<bool> onReduced;
  public Color ink=new Color(.12f,.15f,.17f),paper=new Color(.91f,.86f,.75f),gold=new Color(.86f,.71f,.41f),blue=new Color(.53f,.66f,.72f);
  readonly List<GameObject> tiles=new List<GameObject>(); readonly List<Text> tileTexts=new List<Text>();readonly List<Image> tileImages=new List<Image>();readonly List<RectTransform> notes=new List<RectTransform>();readonly List<Text> noteTexts=new List<Text>();readonly List<Image> noteImages=new List<Image>();
  readonly List<Text> groupLabels=new List<Text>();readonly List<Image> groupLines=new List<Image>();
  readonly List<Text> bookGlyphs=new List<Text>();readonly List<Vector2> slots=new List<Vector2>();readonly List<Particle> particles=new List<Particle>();readonly List<Flight> flights=new List<Flight>(); readonly List<Text> bookNumbers=new List<Text>();
  class Particle{public RectTransform rt;public Image image;public Vector2 vel;public float life,max;public float spin;}
  class Flight{public Text text,target;public Vector2 start,end;public float time,duration;public Action arrived;public bool phraseEnd;}
  Image flash;Text beatLabel,saleCounter;RectTransform sellingBook;CanvasGroup sellingBookAlpha;int saleTarget;float saleAge;float judgementAge;int currentPhrase=-1;bool reduced;Sprite dot,diamond;float beatPulse;float[] tileBounce=new float[0];
  public void Build(){
   font=Resources.Load<Font>("Fonts/NanumGothic");serif=Resources.Load<Font>("Fonts/NanumMyeongjo");string glyphs=string.Join("",RhythmChart.EasyPhrases)+string.Join("",RhythmChart.ChallengePhrases);font.RequestCharactersInTexture(glyphs,40,FontStyle.Bold);serif.RequestCharactersInTexture(glyphs,27,FontStyle.Normal);
   var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;gameObject.AddComponent<GraphicRaycaster>();root=GetComponent<RectTransform>();
   var es=new GameObject("EventSystem").AddComponent<EventSystem>();es.gameObject.AddComponent<StandaloneInputModule>();
   dot=CircleSprite();diamond=dot;
   Panel(root,"Top shade",0,0,1600,104,new Color(.035f,.055f,.085f,.88f));
   Text(root,"MOONLIT PRESS",38,22,260,22,15,new Color(.72f,.76f,.77f));title=Text(root,"달빛 인쇄소",36,45,340,48,32,paper,true);
   Text(root,"첫 번째 이야기  /  별주부전",400,38,350,30,18,paper);
   Panel(root,"Track rail",795,42,370,5,new Color(1,1,1,.15f));progress=Panel(root,"Song progress",795,42,0,5,gold);progressText=Text(root,"144 BPM  ·  달빛 따라 둥실",795,57,390,25,13,new Color(.72f,.77f,.8f));
   combo=Text(root,"0 연타",1215,25,235,58,36,paper,true);Button(root,"II",1475,27,80,49,()=>onPause?.Invoke());
   var art=Resources.Load<Texture2D>("Art/InkUI");book=Panel(root,"Open book",666,143,905,526,new Color(.93f,.89f,.80f)).rectTransform;
   if(art){var img=book.GetComponent<Image>();img.sprite=Sprite.Create(art,new Rect(0,art.height*(1-518f/887),art.width*(880f/1774),art.height*(518f/887)),new Vector2(.5f,.5f),100);img.color=new Color(.97f,.92f,.81f);}
   else{Panel(book,"Spine",444,9,3,508,new Color(.36f,.27f,.17f,.5f));}
   bookNumbers.Add(Text(root,"一",863,626,70,20,14,new Color(.4f,.32f,.24f)));bookNumbers.Add(Text(root,"二",1301,626,70,20,14,new Color(.4f,.32f,.24f)));
   // Book content is separately placed at measured final glyph slots; no prewritten text.
   var inputPaper=Panel(root,"Input backdrop",190,707,1220,174,new Color(.96f,.91f,.81f,.98f));if(art){inputPaper.sprite=Sprite.Create(art,new Rect(art.width*(875f/1774),art.height*(517f/887),art.width*(895f/1774),art.height*(200f/887)),new Vector2(.5f,.5f),100);}
   phraseText=Text(root,"붓을 들고, 박자에 맞춰",340,731,920,22,16,ink);
   subline=Text(root,"음악에 맞춰 키를 누르면, 글자가 책으로",340,833,920,24,16,ink);
   phraseText.alignment=TextAnchor.MiddleCenter;subline.alignment=TextAnchor.MiddleCenter;
   judge=Text(root,"",200,588,385,65,42,gold,true);scoreText=Text(root,"000000",44,111,300,32,22,paper);
   Panel(root,"Rhythm track",220,667,1160,32,new Color(.035f,.06f,.09f,.85f));Panel(root,"Judgement line",797,660,6,46,gold);beatLabel=Text(root,"박자",749,637,100,25,14,paper);beatLabel.alignment=TextAnchor.MiddleCenter;
   for(int i=0;i<18;i++){var n=Panel(root,"Key beat",0,663,38,38,paper).rectTransform;var t=Text(n,"",0,0,38,38,20,ink,true);t.alignment=TextAnchor.MiddleCenter;notes.Add(n);noteTexts.Add(t);noteImages.Add(n.GetComponent<Image>());n.gameObject.SetActive(false);}
   for(int i=0;i<6;i++){var label=Text(root,"",0,619,100,32,22,paper,true);label.alignment=TextAnchor.MiddleCenter;groupLabels.Add(label);groupLines.Add(Panel(root,"Syllable note group",0,653,100,3,gold));}
   fxRoot=Panel(root,"Effects",0,0,1600,900,Color.clear).rectTransform;fxRoot.GetComponent<Image>().raycastTarget=false;
   flash=Panel(fxRoot,"Soft hit light",0,0,1600,900,Color.clear);flash.raycastTarget=false;
   for(int i=0;i<110;i++){var im=Panel(fxRoot,"Spark",0,0,6,6,gold);im.sprite=dot;im.raycastTarget=false;im.gameObject.SetActive(false);particles.Add(new Particle{rt=im.rectTransform,image=im});}
   effects=gameObject.AddComponent<MoonlitVFX>();effects.Build(this);
   overlay=Panel(root,"Modal",0,0,1600,900,new Color(.025f,.04f,.06f,.74f)).rectTransform;
  }
  public static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h){var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  public Image Panel(Transform parent,string n,float x,float y,float w,float h,Color color){var r=Rect(parent,n,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;return im;}
  public Text Text(Transform parent,string value,float x,float y,float w,float h,int size,Color color,bool bold=false){var r=Rect(parent,value,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;return t;}
  public Button Button(Transform parent,string label,float x,float y,float w,float h,Action action){var im=Panel(parent,label,x,y,w,h,new Color(.85f,.77f,.6f));im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;var c=b.colors;c.highlightedColor=new Color(1,.96f,.85f);c.pressedColor=new Color(.72f,.8f,.86f);b.colors=c;var t=Text(im.transform,label,0,0,w,h,21,ink,true);t.alignment=TextAnchor.MiddleCenter;b.onClick.AddListener(()=>{EventSystem.current.SetSelectedGameObject(null);action?.Invoke();});return b;}
  Sprite CircleSprite(){var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);for(int y=0;y<32;y++)for(int x=0;x<32;x++){float a=Mathf.Clamp01((15-Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f)))*.6f);tex.SetPixel(x,y,new Color(1,1,1,a));}tex.Apply();return Sprite.Create(tex,new Rect(0,0,32,32),new Vector2(.5f,.5f));}
  void ClearModal(){saleCounter=null;sellingBook=null;overlay.GetComponent<Image>().raycastTarget=true;foreach(Transform t in overlay)Destroy(t.gameObject);overlay.gameObject.SetActive(true);}
  public void Menu(bool easy){ClearModal();Panel(overlay,"Card",365,100,870,715,new Color(.065f,.09f,.13f,.98f));Text(overlay,"달빛 인쇄소",424,137,750,73,52,paper,true);
   Text(overlay,"한 글자씩, 신나게 써 내려가는 달밤",428,221,770,37,23,gold);
   var a=Button(overlay,easy?"선택됨 · 쉬움 120 BPM":"쉬움 · 달빛 잔치",425,279,358,73,()=>onDifficulty?.Invoke(true));
   var b=Button(overlay,easy?"도전 · 달빛 인쇄소":"선택됨 · 도전 144 BPM",804,279,360,73,()=>onDifficulty?.Invoke(false));
   a.GetComponent<Image>().color=easy?gold:new Color(.58f,.64f,.67f);b.GetComponent<Image>().color=easy?new Color(.58f,.64f,.67f):gold;
   Text(overlay,easy?"달빛 잔치 74초 · 28글자 · 67번 입력\n키 간격 최소 0.5초 · 멜로디를 따라 톡톡!":"달빛 인쇄소 84초 · 122글자 · 301번 입력\n곡의 연주에 맞춘 연속 입력",432,382,740,82,24,paper);
   Text(overlay,"한 글자 = 하나의 리듬 묶음\n예: 아  →  D(ㅇ), K(ㅏ)를 각각 박자에 맞춰 입력\n완성된 글자만 책의 다음 칸으로 날아갑니다.",432,475,740,105,21,new Color(.73f,.8f,.82f));
   Button(overlay,"집필 시작",425,608,358,68,()=>onStart?.Invoke());Button(overlay,"자동 연주 보기",804,608,360,68,()=>onDemo?.Invoke());
   Text(overlay,"ENTER 시작 · F2 자동 연주 · ESC 일시정지",432,715,740,30,17,new Color(.6f,.68f,.73f));
  }
  public void HideModal(){overlay.gameObject.SetActive(false);}
  public void Pause(float offset,float volume,bool reduce){ClearModal();Panel(overlay,"Pause card",440,153,720,595,new Color(.065f,.09f,.13f,.98f));Text(overlay,"잠시 붓을 내려놓고",488,192,670,64,38,paper,true);
   Text(overlay,"입력 판정 보정",490,280,450,32,22,paper);Button(overlay,"- 10 ms",490,325,178,46,()=>onOffset?.Invoke(-.01f));Text(overlay,Mathf.RoundToInt(offset*1000)+" ms",696,330,185,38,23,gold);Button(overlay,"+ 10 ms",920,325,178,46,()=>onOffset?.Invoke(.01f));
   Text(overlay,"음량  "+Mathf.RoundToInt(volume*100)+"%",490,401,310,36,22,paper);Button(overlay,"-",840,396,114,42,()=>onVolume?.Invoke(-.1f));Button(overlay,"+",980,396,114,42,()=>onVolume?.Invoke(.1f));
   Button(overlay,reduce?"효과 강도: 절제":"효과 강도: 풍성",490,468,600,48,()=>onReduced?.Invoke(!reduce));
   Button(overlay,"계속 쓰기",490,568,284,63,()=>onResume?.Invoke());Button(overlay,"처음부터",800,568,284,63,()=>onRetry?.Invoke());Button(overlay,"곡 선택으로",490,657,600,43,()=>onMenu?.Invoke());}
  public void Result(int score,int perfect,int good,int miss,int maxCombo,int sales,bool demo,int best){ClearModal();Panel(overlay,"Results card",356,91,888,737,new Color(.065f,.09f,.13f,.98f));Text(overlay,demo?"자동 연주 · 완본":"별주부전 · 완본",412,122,780,67,43,paper,true);Text(overlay,"달빛 책방, 오늘의 신간이 나왔습니다",416,201,780,35,22,gold);
   saleCounter=Text(overlay,"0 권 판매",414,267,800,91,66,paper,true);saleTarget=sales;saleAge=0;Text(overlay,"책값 30냥  ·  수입 "+(sales*30).ToString("N0")+"냥",418,365,780,36,26,gold);
   Text(overlay,$"찰떡 {perfect}     좋아 {good}     놓침 {miss}\n최대 연타 {maxCombo}     점수 {score:N0}\n최고 기록 {best:N0}",418,433,780,133,25,paper);
   Text(overlay,sales>=800?"장터에서 줄을 섭니다! 다음 권도 써 주세요!":sales>=400?"소문이 돌기 시작했어요. 제법 읽을 만한 이야기!":"첫 독자가 찾아왔어요. 다음 책은 더 잘 팔릴 거예요.",418,588,790,47,21,new Color(.67f,.78f,.76f));
   Panel(overlay,"Bookshop counter",949,555,229,13,new Color(.38f,.25f,.17f));
   for(int i=0;i<3;i++){float x=1020+i*56;var face=Panel(overlay,"Reader",x,456+i*10,35,38,new Color(.8f,.74f,.63f));face.sprite=dot;Panel(overlay,"Reader hair",x,457+i*10,35,9,ink);Panel(overlay,"Reader robe",x-5,491+i*10,45,55,new Color(.30f+i*.025f,.39f,.43f));Panel(overlay,"Eye",x+9,471+i*10,3,3,ink);Panel(overlay,"Eye",x+23,471+i*10,3,3,ink);}
   sellingBook=Panel(overlay,"New book",956,447,61,82,new Color(.27f,.36f,.42f)).rectTransform;sellingBookAlpha=sellingBook.gameObject.AddComponent<CanvasGroup>();Panel(sellingBook,"Book binding",5,0,3,82,gold);Text(sellingBook,"별주\n부전",17,11,38,56,16,paper,true);
   Button(overlay,"다시 집필하기",416,688,356,65,()=>onRetry?.Invoke());Button(overlay,"자동 연주",802,688,356,65,()=>onDemo?.Invoke());Button(overlay,"곡 선택",1030,765,128,40,()=>onMenu?.Invoke());fxRoot.SetAsLastSibling();}
  public void ResetBook(RhythmChart chart){effects.Clear();foreach(var p in particles){p.life=0;p.rt.gameObject.SetActive(false);}judge.text="";combo.text="0 연타";foreach(var t in bookGlyphs)if(t)Destroy(t.gameObject);bookGlyphs.Clear();slots.Clear();foreach(var f in flights)if(f.text)Destroy(f.text.gameObject);flights.Clear();int col=0,line=0;for(int i=0;i<chart.syllables.Count;i++){var sy=chart.syllables[i];if(sy.space)col++;if(i>0&&sy.phrase!=chart.syllables[i-1].phrase){line++;col=0;}if(col>=12){col=0;line++;}int page=line/11;int row=line%11;Vector2 pos=new Vector2((page%2==0?790:1180)+col*27,245+row*29);slots.Add(pos);var t=Text(root,sy.glyph,pos.x,pos.y,32,39,27,ink);t.font=serif;t.gameObject.SetActive(false);bookGlyphs.Add(t);col++;}fxRoot.SetAsLastSibling();overlay.SetAsLastSibling();currentPhrase=-1;}
  public void SetPhrase(RhythmChart chart,int p,int activeSyllable,int strokeIndex){if(currentPhrase!=p){currentPhrase=p;foreach(var o in tiles)Destroy(o);tiles.Clear();tileTexts.Clear();tileImages.Clear();var sy=chart.syllables.FindAll(s=>s.phrase==p);float width=Mathf.Min(88,1110f/sy.Count);float x=800-sy.Count*width*.5f;tileBounce=new float[sy.Count];for(int i=0;i<sy.Count;i++){var im=Panel(root,"Syllable",x+i*width,752,width-8,76,new Color(.83f,.8f,.72f));var border=im.gameObject.AddComponent<Outline>();border.effectColor=new Color(.23f,.25f,.25f,.65f);border.effectDistance=new Vector2(2,-2);tiles.Add(im.gameObject);tileImages.Add(im);var t=Text(im.transform,sy[i].glyph,0,0,width-8,76,40,ink,true);t.alignment=TextAnchor.MiddleCenter;tileTexts.Add(t);}fxRoot.SetAsLastSibling();overlay.SetAsLastSibling();phraseText.text=$"{p+1:00} / {chart.phrases.Length:00}   {chart.phrases[p]}";}
   int j=0;for(int i=0;i<chart.syllables.Count;i++)if(chart.syllables[i].phrase==p){tileImages[j].color=i==activeSyllable?gold:i<activeSyllable?new Color(.72f,.72f,.67f):new Color(.87f,.83f,.75f);tileTexts[j].color=i<activeSyllable?new Color(.4f,.44f,.43f):ink;j++;}
   if(strokeIndex<chart.strokes.Count){var s=chart.strokes[strokeIndex];subline.text=$"다음 키  {s.jamo}  [ {s.key.ToUpper()} ]"+(char.IsUpper(s.key[0])?" + SHIFT":"")+"      ·      키마다 한 번씩, 박자선에 맞춰";}
  }
  public void Rhythm(RhythmChart chart,int cursor,float time){
   for(int i=0;i<notes.Count;i++){int idx=cursor+i;if(idx>=chart.strokes.Count){notes[i].gameObject.SetActive(false);continue;}var s=chart.strokes[idx];float x=800+(s.time-time)*380;bool shown=x>230&&x<1360;notes[i].gameObject.SetActive(shown);if(shown){notes[i].anchoredPosition=new Vector2(x-19,-663);noteTexts[i].text=s.jamo;noteImages[i].color=i==0?gold:paper;}}
   int g=0;
   if(cursor<chart.strokes.Count){int first=chart.strokes[cursor].syllable;for(int index=first;index<chart.syllables.Count&&g<groupLabels.Count;index++){
    var sy=chart.syllables[index];float x1=800+(chart.strokes[sy.firstStroke].time-time)*380,x2=800+(chart.strokes[sy.lastStroke].time-time)*380;if(x1>1340)break;if(x2<240)continue;
    float left=Mathf.Max(235,x1-22),right=Mathf.Min(1360,x2+22);var label=groupLabels[g];label.gameObject.SetActive(true);label.text=sy.glyph;label.rectTransform.anchoredPosition=new Vector2((left+right)*.5f-50,-618);var line=groupLines[g];line.gameObject.SetActive(true);line.rectTransform.anchoredPosition=new Vector2(left,-653);line.rectTransform.sizeDelta=new Vector2(right-left,3);g++;
   }
   var n=chart.strokes[cursor];float wait=n.time-time;subline.text=wait>.9f?"쉬는 박자 · 다음 글자  "+chart.syllables[n.syllable].glyph:$"다음 키  {n.jamo} [ {n.key.ToUpper()} ]"+(char.IsUpper(n.key[0])?" + SHIFT":"")+" · 박자선에 맞춰 톡!";
   }
   for(;g<groupLabels.Count;g++){groupLabels[g].gameObject.SetActive(false);groupLines[g].gameObject.SetActive(false);}
  }
  public void Feedback(string word,Color color,int chain,Vector2 source,bool rich){judge.text=word;judge.color=color;judgementAge=0;combo.text=chain+" 연타";if(chain>0){int closest=-1;float distance=10000;for(int i=0;i<tileImages.Count;i++){float d=Mathf.Abs(tileImages[i].rectTransform.anchoredPosition.x+20-source.x);if(d<distance){distance=d;closest=i;}}if(closest>=0)tileBounce[closest]=1;}Burst(source,color,rich?14:4,rich?180:70);beatPulse=1;}
  public void Celebrate(int chain){judge.text="신바람!";judge.color=gold;judgementAge=0;Burst(new Vector2(430,360),gold,reduced?5:38,250);if(!reduced)effects.Particle(new Vector2(430,330),1.3f);}
  public void Beat(){beatPulse=Mathf.Max(beatPulse,.45f);}
  public Vector2 SyllableSource(RhythmChart chart,int index){var p=chart.syllables[index].phrase;int local=0,count=0;for(int i=0;i<chart.syllables.Count;i++)if(chart.syllables[i].phrase==p){if(i<index)local++;count++;}float width=Mathf.Min(88,1110f/count);return new Vector2(800-count*width*.5f+local*width+20,767);}
  public void Fly(int index,Vector2 source,Action arrived,bool phraseEnd=false){var dest=slots[index];var t=Text(fxRoot,bookGlyphs[index].text,source.x,source.y,52,55,40,ink);t.font=serif;flights.Add(new Flight{text=t,target=bookGlyphs[index],start=source,end=dest,duration=reduced?.25f:.46f,arrived=arrived,phraseEnd=phraseEnd});}
  public void MarkMissing(int index){bookGlyphs[index].gameObject.SetActive(false);}
  public void Burst(Vector2 at,Color color,int count,float speed){int k=0;foreach(var p in particles){if(p.life>0)continue;p.life=p.max=UnityEngine.Random.Range(.25f,.58f);p.rt.anchoredPosition=new Vector2(at.x,-at.y);float a=UnityEngine.Random.value*Mathf.PI*2;p.vel=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*UnityEngine.Random.Range(speed*.3f,speed);p.spin=UnityEngine.Random.Range(-240,240);p.rt.sizeDelta=Vector2.one*UnityEngine.Random.Range(3,8);p.image.color=color;p.rt.gameObject.SetActive(true);if(++k>=count)break;}}
  public void Reduced(bool b){reduced=b;}
  public void Tick(float dt){effects.Tick(dt);for(int j=0;j<tileBounce.Length;j++){tileBounce[j]=Mathf.MoveTowards(tileBounce[j],0,dt*5);float amount=Mathf.Sin(tileBounce[j]*Mathf.PI);tileTexts[j].rectTransform.localScale=new Vector3(1-amount*.10f,1+amount*.22f,1);tileTexts[j].rectTransform.localRotation=Quaternion.Euler(0,0,amount*5);}judgementAge+=dt;var jc=judge.color;jc.a=Mathf.Clamp01(1-(judgementAge-.4f)*3);judge.color=jc;judge.rectTransform.localScale=Vector3.one*(1+Mathf.Exp(-judgementAge*18)*.12f);beatPulse=Mathf.MoveTowards(beatPulse,0,dt*5);beatLabel.color=Color.Lerp(paper,gold,beatPulse);
   if(saleCounter){saleAge+=dt;float p=Mathf.Clamp01(saleAge/2.5f);saleCounter.text=Mathf.RoundToInt(saleTarget*(1-Mathf.Pow(1-p,3))).ToString("N0")+" 권 판매";if(sellingBook){float u=(saleAge%1.1f)/1.1f;sellingBook.anchoredPosition=new Vector2(958+u*170,-447+Mathf.Sin(u*Mathf.PI)*55);sellingBook.localRotation=Quaternion.Euler(0,0,Mathf.Sin(u*Mathf.PI)*-14);sellingBookAlpha.alpha=Mathf.Clamp01((1-u)*4);}}
   for(int i=flights.Count-1;i>=0;i--){var f=flights[i];f.time+=dt;float u=Mathf.Clamp01(f.time/f.duration);float travel=reduced?u:Mathf.Clamp01((u-.12f)/.88f);float e=travel*travel;Vector2 pos=Vector2.Lerp(f.start,f.end,e);
    pos.y-=Mathf.Sin(travel*Mathf.PI)*(reduced?10:135);if(!reduced&&u<.12f)pos.y+=u/.12f*12;
    f.text.rectTransform.anchoredPosition=new Vector2(pos.x,-pos.y);f.text.fontSize=Mathf.RoundToInt(Mathf.Lerp(40,27,e));
    float swell=reduced?1:1+Mathf.Sin(travel*Mathf.PI)*.55f;f.text.rectTransform.localScale=new Vector3(swell,1/swell+.15f*(1-e),1);
    f.text.rectTransform.localRotation=Quaternion.Euler(0,0,reduced?0:Mathf.Sin(travel*Mathf.PI)*-22);
    if(u>=1){f.target.gameObject.SetActive(true);Destroy(f.text.gameObject);effects.Ink(f.end+new Vector2(13,17),f.phraseEnd,reduced);Burst(f.end+new Vector2(13,17),gold,reduced?3:f.phraseEnd?28:16,reduced?50:f.phraseEnd?260:180);f.arrived?.Invoke();flights.RemoveAt(i);}
   }
   foreach(var p in particles){if(p.life<=0)continue;p.life-=dt;p.rt.anchoredPosition+=p.vel*dt;p.vel.y-=120*dt;p.rt.Rotate(0,0,p.spin*dt);var c=p.image.color;c.a=Mathf.Clamp01(p.life/p.max);p.image.color=c;if(p.life<=0)p.rt.gameObject.SetActive(false);}
  }
 }
}
