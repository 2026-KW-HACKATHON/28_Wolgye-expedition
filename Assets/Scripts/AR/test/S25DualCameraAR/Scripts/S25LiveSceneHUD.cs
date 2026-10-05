using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// A single explicit overlay owns the experiment, including errors and camera transitions.
public sealed class S25LiveSceneHUD : IDisposable
{
    GameObject root,ownedEvents;RectTransform panel;RawImage video,actor;Text status,switchLabel;
    public Button StartButton {get;private set;} public Button StopButton {get;private set;}
    public Button SwitchButton {get;private set;} public Button PlaceButton {get;private set;} public Button PhotoButton {get;private set;}
    Font font;
    public S25LiveSceneHUD(Transform parent){
        root=new GameObject("S25 experiment overlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(parent,false);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32767;canvas.overrideSorting=true;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(620,1300);scaler.matchWidthOrHeight=0;
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var bg=New("Background",root.transform).gameObject.AddComponent<Image>();bg.color=Color.black;Stretch(bg.rectTransform);bg.raycastTarget=false;
        video=New("Camera",root.transform).gameObject.AddComponent<RawImage>();video.raycastTarget=false;
        actor=New("Actor",root.transform).gameObject.AddComponent<RawImage>();actor.raycastTarget=false;actor.enabled=false;
        panel=New("Controls",root.transform);var fill=panel.gameObject.AddComponent<Image>();fill.color=new Color(.04f,.05f,.07f,.97f);
        StartButton=ButtonAt("Start / Rescan",8,8,296);StopButton=ButtonAt("Stop + save log",314,8,296);
        PlaceButton=ButtonAt("Place monster",8,54,196);SwitchButton=ButtonAt("Show FRONT",212,54,196);PhotoButton=ButtonAt("Take photo",416,54,194);switchLabel=SwitchButton.GetComponentInChildren<Text>();
        status=New("Status",panel).gameObject.AddComponent<Text>();status.font=font;status.fontSize=18;status.color=Color.white;status.alignment=TextAnchor.UpperLeft;status.horizontalOverflow=HorizontalWrapMode.Wrap;status.verticalOverflow=VerticalWrapMode.Truncate;status.raycastTarget=false;Box(status.rectTransform,10,104,600,205);
        if(EventSystem.current==null){
            ownedEvents=new GameObject("S25 UI events",typeof(EventSystem));
            Type input=Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if(input!=null){var module=ownedEvents.AddComponent(input);input.GetMethod("AssignDefaultActions")?.Invoke(module,null);}
            else ownedEvents.AddComponent<StandaloneInputModule>();
        }
    }
    static RectTransform New(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go.GetComponent<RectTransform>();}
    static void Stretch(RectTransform t){t.anchorMin=Vector2.zero;t.anchorMax=Vector2.one;t.offsetMin=t.offsetMax=Vector2.zero;}
    static void Box(RectTransform t,float x,float y,float w,float h){t.anchorMin=t.anchorMax=new Vector2(0,1);t.pivot=new Vector2(0,1);t.anchoredPosition=new Vector2(x,-y);t.sizeDelta=new Vector2(w,h);}
    Button ButtonAt(string label,float x,float y,float w){var t=New(label,panel);Box(t,x,y,w,40);var image=t.gameObject.AddComponent<Image>();image.color=new Color(.23f,.28f,.34f,1);var button=t.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var text=New("Label",t).gameObject.AddComponent<Text>();Stretch(text.rectTransform);text.font=font;text.fontSize=19;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.text=label;text.raycastTarget=false;return button;}
    public void SetSwitchLabel(string text){switchLabel.text=text;}
    public void UpdateView(Texture2D frame,Texture2D overlay,bool fullOverlay,Rect target,string text){
        status.text=text;float height=Screen.height*620f/Math.Max(1,Screen.width);const float top=320;Box(panel,0,0,620,top);
        video.texture=frame;video.enabled=frame!=null;actor.texture=overlay;actor.enabled=frame!=null&&overlay!=null;
        if(frame==null)return;float scale=Math.Min(620f/frame.width,Math.Max(1,height-top)/frame.height);float x=(620-frame.width*scale)*.5f,y=top;
        Box(video.rectTransform,x,y,frame.width*scale,frame.height*scale);
        if(fullOverlay)Box(actor.rectTransform,x,y,frame.width*scale,frame.height*scale);
        else Box(actor.rectTransform,x+target.x*scale,y+target.y*scale,target.width*scale,target.height*scale);
    }
    public void Dispose(){if(root!=null)UnityEngine.Object.Destroy(root);if(ownedEvents!=null)UnityEngine.Object.Destroy(ownedEvents);root=null;}
}
