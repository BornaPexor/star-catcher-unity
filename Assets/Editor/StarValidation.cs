using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StarValidation
{
    [MenuItem("Star Catcher/Validate scene and editor colors")]
    public static void Run()
    {
        EditorNumberFormat.Apply();
        EditorSceneManager.OpenScene("Assets/Scenes/StarCatcher.unity");
        var game = UnityEngine.Object.FindFirstObjectByType<StarGame>();
        Require(game && game.player && game.gameCamera, "Game, player and camera references");
        foreach (var name in new[] {"Ship","Glass","Star","Meteor","Dust"})
            Require(Resources.Load<Material>(name), "Material: " + name);
        Require(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL), "Web module");
        // Force the editor to read the color settings that failed in the report.
        Color[] colors = { Handles.centerColor, Handles.xAxisColor, Handles.yAxisColor, Handles.zAxisColor, Handles.selectedColor };
        foreach (var color in colors) Require(!float.IsNaN(color.r), "Valid parsed editor color");
        Debug.Log("STAR_CATCHER_VALIDATION_OK scene references, materials, Web module and editor colors");
    }
    static void Require(bool condition, string label)
    {
        if (!condition) throw new Exception("Validation failed: " + label);
    }
}
