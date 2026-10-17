using UnityEngine;
using UnityEngine.UI;

// Pixel-stepped contour traced against the approved 1280x720 room composition.
// Only this overlay is drawn; neither the baked artwork nor the hit polygon changes.
public sealed class PhoneHoverGraphic : MaskableGraphic
{
    public Vector2[] Polygon { get; set; }
    private static readonly Vector2Int[] Contour = {
        new Vector2Int(45,546), new Vector2Int(58,535), new Vector2Int(65,522),
        new Vector2Int(76,514), new Vector2Int(108,511), new Vector2Int(119,518),
        new Vector2Int(231,512), new Vector2Int(240,514), new Vector2Int(247,534),
        new Vector2Int(281,634), new Vector2Int(280,642), new Vector2Int(269,647),
        new Vector2Int(224,659), new Vector2Int(167,665), new Vector2Int(77,665),
        new Vector2Int(58,657), new Vector2Int(51,641), new Vector2Int(44,602),
        new Vector2Int(40,582), new Vector2Int(43,569)
    };
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        float sx = r.width / 230.4f, sy = r.height / 163.44f;
        for (int i = 0; i < Contour.Length; i++)
        {
            Vector2 a = Contour[i], b = Contour[(i + 1) % Contour.Length];
            int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(b.x-a.x), Mathf.Abs(b.y-a.y)) / 2);
            for (int j = 0; j < steps; j++)
            {
                Vector2 p = Vector2.Lerp(a,b,(float)j/steps);
                float x = r.xMin + (Mathf.Round(p.x / 2)*2 - 51.2f)*sx;
                float y = r.yMin + (673.2f - Mathf.Round(p.y / 2)*2)*sy;
                int v = mesh.currentVertCount;
                mesh.AddVert(new Vector3(x,y),color,Vector2.zero);
                mesh.AddVert(new Vector3(x+2*sx,y),color,Vector2.zero);
                mesh.AddVert(new Vector3(x+2*sx,y+2*sy),color,Vector2.zero);
                mesh.AddVert(new Vector3(x,y+2*sy),color,Vector2.zero);
                mesh.AddTriangle(v,v+1,v+2);mesh.AddTriangle(v,v+2,v+3);
            }
        }
    }
}
