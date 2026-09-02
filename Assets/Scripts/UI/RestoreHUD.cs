using UnityEngine;
using UnityEngine.UI;

public sealed class RestoreHUD : MonoBehaviour
{
    private RestorationManager manager;
    private Text progress, instruction, current;
    private Image fill;
    private readonly Image[] cards = new Image[5];
    private readonly Text[] states = new Text[5];
    private GameObject overlay;
    private bool built;
    private static readonly Color Ink = new Color32(35,49,55,255);
    private static readonly Color Accent = new Color32(42,143,160,255);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot(){ GameObject go=new GameObject("Restore HUD Polish"); go.AddComponent<RestoreHUD>(); }
    private void Start(){ TryInitialise(); }
    private void TryInitialise(){ if(built)return; if(manager==null)manager=FindFirstObjectByType<RestorationManager>(); if(manager==null)return; HideLegacyUI(); Build(); built=true; }
    private void HideLegacyUI(){ GameObject old=GameObject.Find("UI Manager"); if(old==null)return; Canvas c=old.GetComponentInChildren<Canvas>(); if(c!=null)c.gameObject.SetActive(false); }

    private void Build(){
        GameObject go=new GameObject("HUD Canvas"); go.transform.SetParent(transform,false);
        Canvas canvas=go.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=20;
        CanvasScaler scaler=go.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1080,1920); scaler.matchWidthOrHeight=.5f; go.AddComponent<GraphicRaycaster>();
        Header(go.transform); Instruction(go.transform); Tools(go.transform); Completion(go.transform);
    }
    private void Header(Transform p){
        Image panel=Panel(p,new Vector2(.5f,.91f),new Vector2(900,180),new Color(1,1,1,.91f));
        progress=Text(panel.transform,"RESTORATION  0%",new Vector2(.5f,.70f),new Vector2(700,42),23,FontStyle.Bold,Ink);
        Text(panel.transform,"TAKE YOUR TIME",new Vector2(.5f,.45f),new Vector2(500,25),11,FontStyle.Bold,Accent);
        Image track=Panel(panel.transform,new Vector2(.5f,.18f),new Vector2(780,16),new Color(.78f,.84f,.85f,.8f));
        fill=Panel(track.transform,new Vector2(.5f,.5f),new Vector2(780,16),Accent); fill.type=Image.Type.Filled; fill.fillMethod=Image.FillMethod.Horizontal; fill.fillOrigin=0; fill.rectTransform.anchorMin=Vector2.zero; fill.rectTransform.anchorMax=Vector2.one; fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
    }
    private void Instruction(Transform p){ Image panel=Panel(p,new Vector2(.5f,.765f),new Vector2(850,76),new Color(.96f,.98f,.98f,.82f)); instruction=Text(panel.transform,"Drag across the muddy areas",new Vector2(.5f,.5f),new Vector2(780,65),18,FontStyle.Normal,Ink); }
    private void Tools(Transform p){
        Image dock=Panel(p,new Vector2(.5f,.105f),new Vector2(1000,205),new Color(.055f,.10f,.12f,.94f));
        Text(dock.transform,"CURRENT TOOL",new Vector2(.5f,.87f),new Vector2(300,24),10,FontStyle.Bold,new Color(1,1,1,.45f));
        current=Text(dock.transform,"WATER",new Vector2(.5f,.69f),new Vector2(500,32),15,FontStyle.Bold,Color.white);
        string[] names={"WATER","FOAM","BRUSH","RINSE","AIR DRY"};
        for(int i=0;i<5;i++){ int n=i; Image card=Panel(dock.transform,new Vector2(.5f,.36f),new Vector2(174,82),new Color(1,1,1,.10f)); card.rectTransform.anchoredPosition=new Vector2(-390+i*195,-5); cards[i]=card; Text(card.transform,names[i],new Vector2(.55f,.60f),new Vector2(130,30),12,FontStyle.Bold,Color.white); states[i]=Text(card.transform,"LOCKED",new Vector2(.55f,.28f),new Vector2(130,22),9,FontStyle.Bold,new Color(1,1,1,.3f)); Button b=card.gameObject.AddComponent<Button>(); b.targetGraphic=card; b.onClick.AddListener(()=>manager.SetTool((CleaningTool)n)); }
    }
    private void Completion(Transform p){ overlay=new GameObject("Completion"); overlay.transform.SetParent(p,false); RectTransform r=overlay.AddComponent<RectTransform>(); r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; Image dim=overlay.AddComponent<Image>(); dim.color=new Color(.02f,.07f,.08f,.62f); Image card=Panel(overlay.transform,new Vector2(.5f,.5f),new Vector2(820,500),new Color(.98f,1,1,.98f)); Text(card.transform,"PERFECT RESTORATION",new Vector2(.5f,.60f),new Vector2(720,65),32,FontStyle.Bold,Ink); Text(card.transform,"Take a breath.  You did it.",new Vector2(.5f,.40f),new Vector2(680,60),19,FontStyle.Normal,new Color32(94,110,114,255)); overlay.SetActive(false); }
    private void Update(){ TryInitialise(); if(!built)return; float p=Mathf.Clamp01(manager.Progress); progress.text="RESTORATION  "+Mathf.RoundToInt(p*100)+"%"; fill.fillAmount=p; current.text=manager.CurrentTool.Label(); instruction.text=GetInstruction(manager.CurrentTool); int stage=Mathf.Clamp(manager.StageIndex,0,4); for(int i=0;i<5;i++){ bool active=i==stage,unlocked=i<=manager.StageIndex; cards[i].color=active?Accent:unlocked?new Color(1,1,1,.15f):new Color(1,1,1,.07f); states[i].text=active?"ACTIVE":unlocked?"READY":"LOCKED"; cards[i].GetComponent<Button>().interactable=unlocked&&!manager.IsComplete; } if(manager.IsComplete&&!overlay.activeSelf)overlay.SetActive(true); }
    private static string GetInstruction(CleaningTool t){ switch(t){ case CleaningTool.Water:return "Drag across the muddy areas"; case CleaningTool.Foam:return "Spread a soft layer of foam"; case CleaningTool.Brush:return "Slowly scrub until the dirt melts away"; case CleaningTool.Rinse:return "Rinse the surface until it shines"; case CleaningTool.Dryer:return "Gently dry the restored sneaker"; default:return "Restore it at your own pace"; } }
    public static void ShowCompletion(){}
    private static Image Panel(Transform p,Vector2 a,Vector2 size,Color c){ GameObject go=new GameObject("Panel"); go.transform.SetParent(p,false); RectTransform r=go.AddComponent<RectTransform>(); r.anchorMin=r.anchorMax=a; r.anchoredPosition=Vector2.zero; r.sizeDelta=size; Image i=go.AddComponent<Image>(); i.color=c; return i; }
    private static Text Text(Transform p,string v,Vector2 a,Vector2 size,int f,FontStyle s,Color c){ GameObject go=new GameObject("Label"); go.transform.SetParent(p,false); RectTransform r=go.AddComponent<RectTransform>(); r.anchorMin=r.anchorMax=a; r.anchoredPosition=Vector2.zero; r.sizeDelta=size; Text t=go.AddComponent<Text>(); t.text=v; t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize=f; t.fontStyle=s; t.alignment=TextAnchor.MiddleCenter; t.color=c; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Overflow; return t; }
}
