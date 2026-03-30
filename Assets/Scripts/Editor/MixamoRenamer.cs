using UnityEditor;
using UnityEngine;
using System.IO;

public class MixamoRenamer : Editor
{
    [MenuItem("Tools/Rename Selected Mixamo Clips")]
    public static void RenameClips()
    {
        // This grabs all files you have highlighted in your Project window
        Object[] selectedAssets = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
        int count = 0;

        foreach (Object asset in selectedAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            
            // Make sure we only process FBX files
            if (assetPath.ToLower().EndsWith(".fbx"))
            {
                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                
                if (importer != null)
                {
                    // Mixamo defaults to storing things here before they are tweaked
                    ModelImporterClipAnimation[] currentClips = importer.clipAnimations;
                    
                    if (currentClips == null || currentClips.Length == 0)
                    {
                        currentClips = importer.defaultClipAnimations;
                    }

                    if (currentClips != null && currentClips.Length > 0)
                    {
                        // Use the precise name of the FBX file (without the .fbx at the end)
                        string fileName = Path.GetFileNameWithoutExtension(assetPath);
                        bool changed = false;

                        for (int i = 0; i < currentClips.Length; i++)
                        {
                            // Target "mixamo.com" specifically
                            if (currentClips[i].name.Contains("mixamo.com") || currentClips[i].name == "Take 001")
                            {
                                currentClips[i].name = fileName;
                                changed = true;
                            }
                        }

                        // Only reimport files that actually needed changing
                        if (changed)
                        {
                            importer.clipAnimations = currentClips;
                            importer.SaveAndReimport();
                            count++;
                        }
                    }
                }
            }
        }
        
        Debug.Log($"Successfully renamed {count} Mixamo FBX animations to match their parent file names!");
    }
}
