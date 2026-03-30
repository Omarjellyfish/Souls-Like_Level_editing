using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimationExtractor : Editor
{
    // Validates that we actually have something selected before enabling the button
    [MenuItem("Assets/Extract Animations", true)]
    private static bool ExtractAnimationsValidation()
    {
        return Selection.objects.Length > 0;
    }

    // Creates a new right-click option in your Project window
    [MenuItem("Assets/Extract Animations", false, 0)]
    private static void ExtractAnimations()
    {
        int count = 0;

        foreach (Object selected in Selection.objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(assetPath)) continue;

            // Load every single piece of data inside the FBX (Model, Bones, Animations)
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            foreach (Object subAsset in subAssets)
            {
                // Specifically look for AnimationClips (ignore the fake preview ones Unity adds)
                if (subAsset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    // 1. Create a brand new unconnected copy of the animation
                    AnimationClip newClip = Object.Instantiate(clip);
                    newClip.name = clip.name;

                    // 2. Put it in the exact same folder as the FBX
                    string folderPath = Path.GetDirectoryName(assetPath);
                    string savePath = Path.Combine(folderPath, $"{clip.name}.anim");

                    // 3. Prevent overwriting old animations safely
                    savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);

                    // 4. Save the file forever
                    AssetDatabase.CreateAsset(newClip, savePath);
                    count++;
                }
            }
        }

        if (count > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>SUCCESS:</color> Extracted {count} editable animation clips!");
        }
        else
        {
            Debug.LogWarning("No animation clips found inside the files you selected!");
        }
    }
}
