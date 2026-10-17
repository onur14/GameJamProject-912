using System.Collections.Generic;
using UnityEngine;

public sealed class PreparationSpriteCutout
{
    private readonly Texture2D source;
    private readonly Color32[] sourcePixels;
    private readonly List<Texture2D> textures = new List<Texture2D>();
    private readonly List<Sprite> sprites = new List<Sprite>();

    public PreparationSpriteCutout(Texture2D source)
    {
        this.source = source;
        sourcePixels = source.GetPixels32();
    }

    public Sprite Cut(int x, int top, int width, int height, bool deskOnly = false, bool keepLargest = false, bool removeBright = false)
    {
        var pixels = new Color32[width * height];
        int bottom = source.height - top - height;
        for (int row = 0; row < height; row++)
            System.Array.Copy(sourcePixels, (bottom + row) * source.width + x, pixels, row * width, width);

        var removed = new bool[pixels.Length];
        var queue = new int[pixels.Length];
        int front = 0, back = 0;
        void Enqueue(int index)
        {
            if (removed[index] || !IsBackground(pixels[index])) return;
            removed[index] = true;
            queue[back++] = index;
        }
        for (int column = 0; column < width; column++)
        {
            Enqueue(column);
            Enqueue((height - 1) * width + column);
        }
        for (int row = 0; row < height; row++)
        {
            Enqueue(row * width);
            Enqueue(row * width + width - 1);
        }
        while (front < back)
        {
            int index = queue[front++], column = index % width, row = index / width;
            if (column > 0) Enqueue(index - 1);
            if (column + 1 < width) Enqueue(index + 1);
            if (row > 0) Enqueue(index - width);
            if (row + 1 < height) Enqueue(index + width);
        }
        for (int row = 0; row < height; row++)
            for (int column = 0; column < width; column++)
            {
                int index = row * width + column;
                Color32 color = pixels[index];
                if (removed[index] || removeBright && IsBrightBackground(color) || deskOnly && column > 21 && column < 159 && height - 1 - row > 18)
                    color.a = 0;
                pixels[index] = color;
            }
        if (keepLargest) KeepLargestComponent(pixels, width, height);

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height),
            new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        textures.Add(texture);
        sprites.Add(sprite);
        return sprite;
    }

    private static bool IsBackground(Color32 color)
    {
        int maximum = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        int minimum = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        return minimum >= 205 && maximum - minimum <= 18;
    }
    private static bool IsBrightBackground(Color32 color)
    {
        int maximum = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        int minimum = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        return minimum >= 160 && maximum - minimum <= 90;
    }

    private static void KeepLargestComponent(Color32[] pixels, int width, int height)
    {
        var component = new int[pixels.Length];
        var queue = new int[pixels.Length];
        int current = 0, largest = 0, largestSize = 0;
        for (int start = 0; start < pixels.Length; start++)
        {
            if (pixels[start].a == 0 || component[start] != 0) continue;
            current++;
            int front = 0, back = 0;
            queue[back++] = start;
            component[start] = current;
            while (front < back)
            {
                int index = queue[front++], column = index % width, row = index / width;
                for (int vertical = -1; vertical <= 1; vertical++)
                    for (int horizontal = -1; horizontal <= 1; horizontal++)
                    {
                        int nextColumn = column + horizontal, nextRow = row + vertical;
                        if (nextColumn < 0 || nextColumn >= width || nextRow < 0 || nextRow >= height) continue;
                        int next = nextRow * width + nextColumn;
                        if (pixels[next].a == 0 || component[next] != 0) continue;
                        component[next] = current;
                        queue[back++] = next;
                    }
            }
            if (back > largestSize) { largestSize = back; largest = current; }
        }
        for (int index = 0; index < pixels.Length; index++)
            if (component[index] != largest)
            {
                var color = pixels[index];
                color.a = 0;
                pixels[index] = color;
            }
    }

    public void Dispose()
    {
        foreach (var sprite in sprites) if (sprite) Object.Destroy(sprite);
        foreach (var texture in textures) if (texture) Object.Destroy(texture);
    }
}
