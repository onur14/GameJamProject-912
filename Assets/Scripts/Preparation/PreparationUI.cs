using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PreparationUI
{
    public static readonly Color Navy=new Color(.035f,.063f,.09f), PanelColor=new Color(.065f,.105f,.14f);
    public static readonly Color Amber=new Color(.94f,.66f,.25f), White=new Color(.83f,.9f,.91f), Green=new Color(.53f,.77f,.69f), Red=new Color(.88f,.32f,.29f);
    public static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent,false); r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero; return r;
    }
    public static Image Panel(string name,Transform parent,Vector2 min,Vector2 max,Color color)
    {
        var image=Rect(name,parent,min,max).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    public static TMP_Text Label(string name,Transform parent,Vector2 min,Vector2 max,string text,TMP_FontAsset font,float size=24)
    {
        var t=Rect(name,parent,min,max).gameObject.AddComponent<TextMeshProUGUI>();
        t.font=font?font:TMP_Settings.defaultFontAsset;t.text=text;t.fontSize=size;t.fontStyle=FontStyles.Bold;
        t.color=White;t.alignment=TextAlignmentOptions.Center;t.enableAutoSizing=true;t.fontSizeMin=16;t.fontSizeMax=size;t.raycastTarget=false;
        return t;
    }
    public static Button Button(string name,Transform parent,Vector2 min,Vector2 max,string caption,TMP_FontAsset font)
    {
        var image=Panel(name,parent,min,max,PanelColor);image.raycastTarget=true;
        var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;GameAudioFeedback.Bind(b);
        var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.72f,.91f,1);
        colors.pressedColor=new Color(.48f,.68f,.76f);colors.selectedColor=colors.highlightedColor;
        colors.disabledColor=new Color(.52f,.57f,.61f);colors.fadeDuration=.09f;b.colors=colors;
        var n=b.navigation;n.mode=Navigation.Mode.None;b.navigation=n;
        if(!string.IsNullOrEmpty(caption))Label("Yazı",image.transform,new Vector2(.06f,.07f),new Vector2(.94f,.93f),caption,font,22);
        return b;
    }
    public static Sprite Sprite(Texture2D texture,Rect normalized)
    {
        return UnityEngine.Sprite.Create(texture,new Rect(normalized.x*texture.width,normalized.y*texture.height,normalized.width*texture.width,normalized.height*texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
    }
    public static void Fit(RectTransform r,Transform parent)
    {r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
}
