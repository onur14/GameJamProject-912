using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

// Rebuild existing TMP assets in place, retaining their GUIDs and all scene/prefab references.
[InitializeOnLoad]
public static class TurkishFontRepair
{
    public static readonly string[] FontPaths={
        "Assets/Art/UI/Fonts/VCR_OSD_MONO_1.001 SDF.asset",
        "Assets/Art/UI/MainMenu/DispatchMenuPixel SDF.asset",
        "Assets/Working Folder/Taha and Yasin/Fonts/TY_VCR_OSD_MONO_SDF.asset",
        "Assets/Working Folder/Taha and Yasin/Fonts/TY_DispatchMenuPixel_SDF.asset"};
    public const string Characters="ÇçĞğİıÖöŞşÜü";
    static double next;
    static TurkishFontRepair(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.25;
        const string path="Library/UITypePolish/request.txt";if(!File.Exists(path))return;
        string action=File.ReadAllText(path).Trim();File.Delete(path);
        try{if(action!="repair")throw new Exception("Unknown font repair action");Repair();File.WriteAllText("Library/UITypePolish/status.txt","Rebuilt and verified four existing TMP font atlases");}
        catch(Exception e){File.WriteAllText("Library/UITypePolish/status.txt",e.ToString());}
    }
    [MenuItem("Tools/Onur/Rebuild Corrected Turkish Font Atlases")]
    public static void Repair()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        AssetDatabase.ImportAsset("Assets/Art/UI/Fonts/VCR_OSD_MONO_1.001.ttf",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        Directory.CreateDirectory("Logs/UITypePolish");File.WriteAllText("Logs/UITypePolish/fonts.txt","");
        foreach(string path in FontPaths)
        {
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(!font)throw new Exception("Missing font asset: "+path);
            var requested=font.characterTable.Select(c=>c.unicode).Where(c=>c>=32)
                .Concat(Enumerable.Range(32,95).Select(c=>(uint)c)).Concat(Characters.Select(c=>(uint)c)).Distinct().OrderBy(c=>c).ToArray();
            font.ClearFontAssetData();font.atlasPopulationMode=AtlasPopulationMode.Dynamic;
            font.TryAddCharacters(requested,out uint[] missing);
            var face=font.faceInfo;face.descentLine=-200f*face.pointSize/face.unitsPerEM;font.faceInfo=face;
            font.ReadFontAssetDefinition();
            foreach(char c in Characters)if(!font.HasCharacter(c,false,false))throw new Exception(path+" lacks native Turkish glyph "+c);
            if(font.characterLookupTable['Ç'].glyphIndex==font.characterLookupTable['9'].glyphIndex)throw new Exception("Ç must not alias 9");
            EditorUtility.SetDirty(font);foreach(var texture in font.atlasTextures)if(texture)EditorUtility.SetDirty(texture);
            File.AppendAllText("Logs/UITypePolish/fonts.txt",path+" — native Turkish glyphs verified; references preserved\n");
        }
        AssetDatabase.SaveAssets();
    }
}
