using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LevelAssetCreator
{
    [MenuItem("Tools/Create Level Assets")]
    public static void CreateLevelAssets()
    {
        // Create the levels folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Levels"))
        {
            AssetDatabase.CreateFolder("Assets", "Levels");
        }

        // Create three level assets
        for (int i = 1; i <= 3; i++)
        {
            string assetPath = $"Assets/Levels/Level{i}.asset";
            
            // Check if asset already exists
            if (AssetDatabase.LoadAssetAtPath<LevelData>(assetPath) != null)
            {
                Debug.Log($"Level{i}.asset already exists!");
                continue;
            }

            // Create new LevelData asset
            LevelData levelData = ScriptableObject.CreateInstance<LevelData>();
            
            // Initialize with default values if needed
            levelData.Row = 5;  // Default size, adjust as needed
            levelData.Column = 5;
            levelData.Data = new List<int>(new int[25]); // 5x5 grid of empty cells

            // Save the asset
            AssetDatabase.CreateAsset(levelData, assetPath);
            Debug.Log($"Created Level{i}.asset");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}