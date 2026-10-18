using UnityEditor;
using UnityEngine;

// Import-only cleanup of the existing cutouts. Internal white details stay intact.
public sealed class BagSpriteTransparency : AssetPostprocessor
{
    private bool IsCutout()
    {
        if(!assetPath.Contains("/MiniGames/Cutouts/"))return false;
        string name=System.IO.Path.GetFileNameWithoutExtension(assetPath);
        return name=="bag"||name=="water"||name=="radio"||name=="flashlight"||name=="first_aid"||name=="ball"||name=="controller";
    }
    public override uint GetVersion()=>3;
    private void OnPreprocessTexture()
    {
        if(!IsCutout())return;
        var importer=(TextureImporter)assetImporter;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Point;
        importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.wrapMode=TextureWrapMode.Clamp;
    }
    private void OnPostprocessTexture(Texture2D texture)
    {
        if(!IsCutout())return;
        var pixels=texture.GetPixels32();int width=texture.width,height=texture.height;
        var seen=new bool[pixels.Length];var queue=new int[pixels.Length];int front=0,back=0;
        void Add(int index)
        {
            if(seen[index])return;
            var c=pixels[index];int min=Mathf.Min(c.r,Mathf.Min(c.g,c.b)),max=Mathf.Max(c.r,Mathf.Max(c.g,c.b));
            if(c.a!=0&&(min<165||max-min>65))return;
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
        if(System.IO.Path.GetFileNameWithoutExtension(assetPath)=="bag" && width==326 && height==390)
        {
            ClearBrightRegion(75+(height-1-85)*width);
            ClearBrightRegion(180+(height-1-30)*width);
        }
        texture.SetPixels32(pixels);texture.Apply(false,false);

        void ClearBrightRegion(int seed)
        {
            var visited=new bool[pixels.Length];int head=0,tail=0;
            void AddBright(int index)
            {
                if(visited[index])return;
                var color=pixels[index];int low=Mathf.Min(color.r,Mathf.Min(color.g,color.b));
                int high=Mathf.Max(color.r,Mathf.Max(color.g,color.b));
                if(color.a==0||low<165||high-low>65)return;
                visited[index]=true;queue[tail++]=index;
            }
            AddBright(seed);
            while(head<tail)
            {
                int index=queue[head++],column=index%width,row=index/width;
                var color=pixels[index];color.a=0;pixels[index]=color;
                if(column>0)AddBright(index-1);if(column+1<width)AddBright(index+1);
                if(row>0)AddBright(index-width);if(row+1<height)AddBright(index+width);
            }
        }
    }
}
