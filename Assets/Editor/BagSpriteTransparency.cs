using UnityEditor;
using UnityEngine;

// Import-only cleanup of the existing cutouts. Internal white details stay intact.
public sealed class BagSpriteTransparency : AssetPostprocessor
{
    private bool IsBagCutout()
    {
        if(!assetPath.Contains("/MiniGames/Cutouts/"))return false;
        string name=System.IO.Path.GetFileNameWithoutExtension(assetPath);
        return name=="bag"||name=="water"||name=="radio"||name=="flashlight"||name=="first_aid"||name=="ball"||name=="controller";
    }
    public override uint GetVersion()=>1;
    private void OnPreprocessTexture()
    {
        if(!IsBagCutout())return;
        var importer=(TextureImporter)assetImporter;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Point;
        importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.wrapMode=TextureWrapMode.Clamp;
    }
    private void OnPostprocessTexture(Texture2D texture)
    {
        if(!IsBagCutout())return;
        var pixels=texture.GetPixels32();int width=texture.width,height=texture.height;
        var seen=new bool[pixels.Length];var queue=new int[pixels.Length];int front=0,back=0;
        void Add(int index)
        {
            if(seen[index])return;
            var c=pixels[index];int min=Mathf.Min(c.r,Mathf.Min(c.g,c.b)),max=Mathf.Max(c.r,Mathf.Max(c.g,c.b));
            if(c.a!=0&&(min<185||max-min>40))return;
            seen[index]=true;queue[back++]=index;
        }
        // Flood only from the transparent outside, never from highlights inside an item.
        for(int x=0;x<width;x++){Add(x);Add((height-1)*width+x);}
        for(int y=0;y<height;y++){Add(y*width);Add(y*width+width-1);}
        while(front<back)
        {
            int i=queue[front++],x=i%width,y=i/width;
            var c=pixels[i];c.a=0;pixels[i]=c;
            if(x>0)Add(i-1);if(x+1<width)Add(i+1);
            if(y>0)Add(i-width);if(y+1<height)Add(i+width);
        }
        texture.SetPixels32(pixels);texture.Apply(false,false);
    }
}
