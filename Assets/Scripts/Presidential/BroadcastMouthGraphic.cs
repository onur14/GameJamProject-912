using UnityEngine;
using UnityEngine.UI;

// Two restrained open-mouth states registered to the approved portrait's pixels.
// Idle draws nothing, exposing the original closed mouth exactly.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class BroadcastMouthGraphic : MaskableGraphic
{
    public Image Portrait { get; set; }
    private int frame;
    public int Frame { get => frame; set { if(frame==value)return;frame=value;SetVerticesDirty(); } }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();if(frame==0||!Portrait||!Portrait.sprite)return;
        Rect area=rectTransform.rect;float aspect=Portrait.sprite.rect.width/Portrait.sprite.rect.height;
        float width=Mathf.Min(area.width,area.height*aspect),height=width/aspect;
        Vector2 origin=area.center-new Vector2(width,height)/2;
        // Source coordinates use the 1672x941 reference image; preserveAspect letterboxing is included.
        float sx=width/1672,sy=height/941;
        // Stepped pixel edges follow the original lips, rather than a rectangular face patch.
        var shadow=new Color(.12f,.075f,.07f);
        Add(mesh,origin,sx,sy,804,507,56,frame==1?9:10,shadow);
        Add(mesh,origin,sx,sy,796,514,72,frame==1?6:10,shadow);
        if(frame==2)Add(mesh,origin,sx,sy,804,524,56,7,shadow);
        Add(mesh,origin,sx,sy,812,frame==1?520:531,40,4,new Color(.46f,.29f,.22f));
    }
    private static void Add(VertexHelper mesh,Vector2 o,float sx,float sy,float x,float y,float w,float h,Color c)
    {
        float l=o.x+x*sx,b=o.y+(941-y-h)*sy;int v=mesh.currentVertCount;
        mesh.AddVert(new Vector3(l,b),c,Vector2.zero);mesh.AddVert(new Vector3(l+w*sx,b),c,Vector2.zero);
        mesh.AddVert(new Vector3(l+w*sx,b+h*sy),c,Vector2.zero);mesh.AddVert(new Vector3(l,b+h*sy),c,Vector2.zero);
        mesh.AddTriangle(v,v+1,v+2);mesh.AddTriangle(v,v+2,v+3);
    }
}
