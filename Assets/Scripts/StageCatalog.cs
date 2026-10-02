using System;

namespace Moonlit {
 public sealed class StageDefinition {
  public readonly string id, title, musicName, audioExtension, storyTitle;
  public readonly bool tutorial;
  public readonly int difficulty;
  public string Stars=>new string('★',difficulty)+new string('☆',5-difficulty);
  public readonly string[] phrases;
  public string AudioAssetPath=>"Assets/Resources/Audio/"+musicName+audioExtension;
  public string CoverTitle=>storyTitle.Length>2?storyTitle.Substring(0,2)+"\n"+storyTitle.Substring(2):storyTitle;
  public StageDefinition(string id,string title,string musicName,string audioExtension,string storyTitle,bool tutorial,int difficulty,string[] phrases) {
   this.id=id;this.title=title;this.musicName=musicName;this.audioExtension=audioExtension;
   if(difficulty<1||difficulty>5)throw new ArgumentOutOfRangeException(nameof(difficulty));
   this.storyTitle=storyTitle;this.tutorial=tutorial;this.difficulty=difficulty;this.phrases=phrases;
  }
 }
 public static class StageCatalog {
  public static readonly StageDefinition[] All={
   new StageDefinition("tutorial","달빛 잔치","MoonlitFestival",".wav","별주부전",true,1,new[]{
    "토끼야 가자", "바다로 가자", "용궁에 도착", "간은 집에", "두고 왔소", "토끼는 폴짝"}),
   new StageDefinition("shining-moment","가장 빛나는 때","ShiningMoment",".mp3","흥부전",false,3,new[]{
    "흥부는 마음이 고와", "가난해도 웃었네", "제비 한 쌍 찾아와", "떨어진 작은 제비",
    "다친 다리 고쳐 주고", "따뜻하게 돌봤네", "박씨 하나 물고 왔네", "흥부가 박씨 심자",
    "커다란 박 열렸네", "슬근슬근 톱질하니", "금은보화 쏟아지네", "함께 나누어 살았네"}),
   new StageDefinition("door-clicks","When the Door Clicks In","DoorClicks",".mp3","옹고집전",false,4,new[]{
    "옹고집 심술쟁이", "욕심만 가득했네", "스님 한 분 찾아와", "볏짚 인형 뚝딱",
    "가짜가 나타나", "진짜라며 우겼네", "서로 다투다가", "원님 앞에 갔네",
    "집도 돈도 잃었네", "잘못 뉘우친 뒤", "마음 고쳐먹고", "이웃들과 나눴네"})
  };
  public static StageDefinition Get(int index) {
   if(index<0||index>=All.Length)throw new ArgumentOutOfRangeException(nameof(index));
   return All[index];
  }
 }
}
