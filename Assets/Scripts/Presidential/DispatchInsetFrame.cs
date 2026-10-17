using UnityEngine;
using UnityEngine.UI;

// Flat pixel geometry: steel edge, small worn patches, no texture/animation/input dependency.
public sealed class DispatchInsetFrame : MaskableGraphic
{
    public static void Ensure(Transform card)
    {
        if (!card || card.Find("Console Inset Frame")) return;
        var frame = new GameObject("Console Inset Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(DispatchInsetFrame));
        frame.transform.SetParent(card, false);frame.transform.SetAsFirstSibling();
        var rect = (RectTransform)frame.transform;
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        frame.GetComponent<DispatchInsetFrame>().raycastTarget=false;
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;float inset=12;
        float left=Mathf.Round(r.xMin+inset),right=Mathf.Round(r.xMax-inset),bottom=Mathf.Round(r.yMin+inset),top=Mathf.Round(r.yMax-inset);
        var steel=new Color(.25f,.31f,.34f,.8f);var dark=new Color(.07f,.10f,.12f,.9f);var rust=new Color(.39f,.25f,.16f,.5f);
        Quad(vh,left,bottom,right, bottom+2,steel);Quad(vh,left,top-2,right,top,steel);
        Quad(vh,left,bottom,left+2,top,steel);Quad(vh,right-2,bottom,right,top,steel);
        Quad(vh,left+22,top-2,left+43,top,rust);Quad(vh,right-51,bottom,right-29,bottom+2,rust);
        Quad(vh,left,bottom+28,left+2,bottom+38,rust);Quad(vh,right-2,top-65,right,top-54,rust);
        Quad(vh,left+71,top-1,left+79,top,dark);Quad(vh,right-90,bottom,right-83,bottom+1,dark);
        foreach(float x in new[]{left+5,right-8})foreach(float y in new[]{bottom+5,top-8})
        {Quad(vh,x,y,x+3,y+3,steel);Quad(vh,x+1,y+1,x+2,y+2,dark);}
    }
    static void Quad(VertexHelper vh,float x0,float y0,float x1,float y1,Color color)
    {
        int i=vh.currentVertCount;vh.AddVert(new Vector3(x0,y0),color,Vector2.zero);vh.AddVert(new Vector3(x0,y1),color,Vector2.zero);
        vh.AddVert(new Vector3(x1,y1),color,Vector2.zero);vh.AddVert(new Vector3(x1,y0),color,Vector2.zero);
        vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
    }
}
