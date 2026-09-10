using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StarBuilder
{
    [MenuItem("Star Catcher/Prepare scene")]
    public static void Setup()
    {
        if (File.Exists("Assets/Scenes/StarCatcher.unity")) {
            EditorSceneManager.OpenScene("Assets/Scenes/StarCatcher.unity");
            Debug.Log("STAR_CATCHER_SETUP_OK existing scene preserved");
            return;
        }
        Directory.CreateDirectory("Assets/Resources"); Directory.CreateDirectory("Assets/Scenes");
        string[] names={"Ship","Glass","Star","Meteor","Dust"};
        Color[] colors={new Color(.87f,.95f,1),new Color(.22f,.7f,.85f),new Color(.83f,.98f,.46f),new Color(1,.4f,.3f),new Color(.38f,.55f,.67f)};
        for(int i=0;i<names.Length;i++) {
            string path="Assets/Resources/"+names[i]+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Unlit/Color"));AssetDatabase.CreateAsset(material,path);}
            material.color=colors[i]; EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var game=new GameObject("Game").AddComponent<StarGame>(); game.BuildWorld();
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/StarCatcher.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/StarCatcher.unity",true)};
        PlayerSettings.companyName="Local Games"; PlayerSettings.productName="Star Catcher";
        PlayerSettings.defaultScreenWidth=1000; PlayerSettings.defaultScreenHeight=610;
        PlayerSettings.runInBackground=false;
        PlayerSettings.WebGL.template="PROJECT:StarCatcher";
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback=false;
        PlayerSettings.WebGL.initialMemorySize=64;
        PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        QualitySettings.vSyncCount=0; QualitySettings.antiAliasing=2;
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=game.gameObject;
        Debug.Log("STAR_CATCHER_SETUP_OK");
    }
    [MenuItem("Star Catcher/Build Web")]
    public static void BuildWeb()
    {
        EditorNumberFormat.Apply();
        Setup();
        PlayerSettings.bundleVersion="0.1.0";
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new Exception("Web build support module is missing.");
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL,BuildTarget.WebGL);
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/StarCatcher.unity"},locationPathName="Web",target=BuildTarget.WebGL,options=BuildOptions.None});
        if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Web build failed: "+result.summary.result);
        Debug.Log("STAR_CATCHER_WEB_BUILD_OK bytes="+result.summary.totalSize);
    }
}
