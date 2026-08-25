/*
CreateLevel3DefaultMapAsset.cs

Editor utility: create the Level 3 default LevelMapData asset and initialize its procedural prototype rows.
It is intentionally idempotent and avoids overwriting an existing asset.
*/

using System.IO;
using UnityEditor;
using UnityEngine;

public static class CreateLevel3DefaultMapAsset
{
    [MenuItem("Tools/Level3/Create Default Map Asset")]
    public static void Create()
    {
        string folder = "Assets/ScriptableObjects/Level3";
        Directory.CreateDirectory(folder);

        string assetPath = folder + "/Level3_DefaultMap.asset";
        LevelMapData asset = AssetDatabase.LoadAssetAtPath<LevelMapData>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<LevelMapData>();
            asset.InitializeDefaultLayout();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = asset;
        EditorUtility.DisplayDialog("Level 3 map asset", "Default Level 3 map asset created at " + assetPath + ".", "OK");
    }
}
