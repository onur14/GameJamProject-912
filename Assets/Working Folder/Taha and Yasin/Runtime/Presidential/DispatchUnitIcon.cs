using UnityEngine;
using UnityEngine.UI;

// Small original 16x16 pixel symbols, drawn once as UI geometry (no texture allocation).
public sealed class DispatchUnitIcon : MaskableGraphic
{
    public int UnitIndex { get; set; }
    private static readonly string[][] Pixels = {
        new[]{"................","................",".....##.........","....####........",".....##.........","................",".###########....",".#.......#.##...",".#.......#..##..",".#########...#..",".#############..",".#############..","...##.....##....","..####...####...","...##.....##....","................"},
        new[]{".......#........","......##........",".....###........",".....####.......","....#####.......","...#######......","..#########.....","..####.#####....",".####..#####....",".###...######...",".###....#####...",".####...#####...","..####.#####....","...########.....","....######......","................"},
        new[]{"................","....########....","...##########...","..###......###..",".###........###.",".##..........##.",".##..........##.",".##..........##.",".###........###.","..############..",".##############.",".##############.","....########....",".....######.....","................","................"}
    };
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var pattern=Pixels[Mathf.Clamp(UnitIndex,0,2)];Rect r=rectTransform.rect;
        float cell=Mathf.Min(r.width,r.height)/16;Vector2 origin=r.center-new Vector2(cell*8,cell*8);
        for(int y=0;y<16;y++)for(int x=0;x<16;x++)if(pattern[y][x]=='#')
        {
            float left=origin.x+x*cell,bottom=origin.y+(15-y)*cell;int v=mesh.currentVertCount;
            mesh.AddVert(new Vector3(left,bottom),color,Vector2.zero);mesh.AddVert(new Vector3(left+cell,bottom),color,Vector2.zero);
            mesh.AddVert(new Vector3(left+cell,bottom+cell),color,Vector2.zero);mesh.AddVert(new Vector3(left,bottom+cell),color,Vector2.zero);
            mesh.AddTriangle(v,v+1,v+2);mesh.AddTriangle(v,v+2,v+3);
        }
    }
}
