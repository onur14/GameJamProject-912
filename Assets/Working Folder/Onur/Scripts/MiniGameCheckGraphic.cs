using UnityEngine;
using UnityEngine.UI;

// A font-independent check mark for minigame buttons and completed interactions.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MiniGameCheckGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var r = rectTransform.rect;
        float size = Mathf.Min(r.width, r.height);
        Vector2 origin = r.center - Vector2.one * (size * .5f);
        Segment(mesh, origin + new Vector2(.1f, .45f) * size,
            origin + new Vector2(.38f, .17f) * size, size * .12f);
        Segment(mesh, origin + new Vector2(.38f, .17f) * size,
            origin + new Vector2(.9f, .84f) * size, size * .12f);
    }

    private void Segment(VertexHelper mesh, Vector2 a, Vector2 b, float width)
    {
        Vector2 direction = (b - a).normalized;
        Vector2 offset = new Vector2(-direction.y, direction.x) * (width * .5f);
        int index = mesh.currentVertCount;
        mesh.AddVert(a - offset, color, Vector2.zero);
        mesh.AddVert(a + offset, color, Vector2.zero);
        mesh.AddVert(b + offset, color, Vector2.zero);
        mesh.AddVert(b - offset, color, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }
}
