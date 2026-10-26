using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using LosQueQuedan;

public static class BuildDemo
{
    public static void Prepare()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Los que quedan");var game=root.AddComponent<DemoGame>();
        game.inkShader=Shader.Find("Hidden/LosQueQuedan/Ink");
        game.worldTextShader=Shader.Find("LosQueQuedan/WorldText");
        const string materialPath="Assets/Scenes/SurfaceTemplate.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!material)
        {
            material=new Material(Shader.Find("Standard"));
            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);
            AssetDatabase.CreateAsset(material,materialPath);
        }
        game.surfaceTemplate=material;
        // Cut-out decals (blood, hands) need the alpha-test variant kept in the player: a saved material that uses it.
        const string cutoutPath="Assets/Scenes/CutoutTemplate.mat";
        var cutout=AssetDatabase.LoadAssetAtPath<Material>(cutoutPath);
        if(!cutout)
        {
            cutout=new Material(Shader.Find("Standard"));
            cutout.SetFloat("_Mode",1);cutout.SetFloat("_Cutoff",.5f);cutout.EnableKeyword("_ALPHATEST_ON");
            cutout.SetOverrideTag("RenderType","TransparentCutout");cutout.renderQueue=2450;
            AssetDatabase.CreateAsset(cutout,cutoutPath);
        }
        game.cutoutTemplate=cutout;
        // The same with its own light (windows of the burning city): alpha test and emission together.
        const string glowPath="Assets/Scenes/CutoutGlowTemplate.mat";
        var glow=AssetDatabase.LoadAssetAtPath<Material>(glowPath);
        if(!glow)
        {
            glow=new Material(cutout);glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",Color.black);
            AssetDatabase.CreateAsset(glow,glowPath);
        }
        game.cutoutGlowTemplate=glow;
        if(!game.inkShader)throw new System.Exception("No se encontro el shader de tinta");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/ElDespertar.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/ElDespertar.unity",true)};
        PlayerSettings.companyName="SpookyGameJam";PlayerSettings.productName="Hineni";
        PlayerSettings.bundleVersion="0.3.0";
        // Physical pixels: on a Retina Mac this is a 1440x900-point window rendered at full resolution.
        PlayerSettings.defaultScreenWidth=2880;PlayerSettings.defaultScreenHeight=1800;
        PlayerSettings.macRetinaSupport=true;PlayerSettings.resizableWindow=true;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.runInBackground=false;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.spookygamejam.losquequedan");
        QualitySettings.vSyncCount=1;QualitySettings.antiAliasing=2;
        AssetDatabase.SaveAssets();
        Debug.Log("SPOOKY_PREPARE_OK");
    }
    // Prints the size, pivot offset and materials of every runtime model, for laying out the hospital.
    public static void ReportModels()
    {
        foreach (var go in Resources.LoadAll<GameObject>(""))
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var mats = new System.Collections.Generic.HashSet<string>();
            foreach (var r in rs) foreach (var m in r.sharedMaterials) if (m) mats.Add(m.name + (m.mainTexture ? "*" : ""));
            Debug.Log($"MODEL {go.name} size={b.size:F2} center={b.center:F2} min={b.min:F2} mats={string.Join(",", mats)}");
        }
    }
    // Browser build for GitHub Pages: gzip with JS decompression fallback (Pages sends no Content-Encoding).
    public static void BuildWeb()
    {
        Prepare();
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback=true;
        PlayerSettings.WebGL.dataCaching=true;
        PlayerSettings.stripEngineCode=false;   // primitives add Sphere/Mesh colliders at runtime; keep those classes
        PlayerSettings.defaultWebScreenWidth=1280;PlayerSettings.defaultWebScreenHeight=800;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=new[]{"Assets/Scenes/ElDespertar.unity"},locationPathName="Builds/WebGL",target=BuildTarget.WebGL,options=BuildOptions.None});
        Debug.Log("SPOOKY_WEB_RESULT: "+report.summary.result+" errors="+report.summary.totalErrors+" size="+report.summary.totalSize);
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("La build web fallo");
    }
    public static void Build()
    {
        Prepare();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=new[]{"Assets/Scenes/ElDespertar.unity"},
            locationPathName="Builds/macOS/Hineni.app",target=BuildTarget.StandaloneOSX,
            options=BuildOptions.None});
        Debug.Log("SPOOKY_BUILD_RESULT: "+report.summary.result+" errors="+report.summary.totalErrors);
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("La build fallo");
    }
    // Windows and Linux (Mono, cross-built from the Mac): Builds/Windows/Hineni.exe, Builds/Linux/Hineni.x86_64.
    public static void BuildWindows()=>Desktop("Builds/Windows/Hineni.exe",BuildTarget.StandaloneWindows64);
    public static void BuildLinux()=>Desktop("Builds/Linux/Hineni.x86_64",BuildTarget.StandaloneLinux64);
    static void Desktop(string path,BuildTarget target)
    {
        Prepare();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/ElDespertar.unity"},locationPathName=path,target=target,options=BuildOptions.None});
        Debug.Log("SPOOKY_BUILD_RESULT: "+report.summary.result+" errors="+report.summary.totalErrors);
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("La build fallo");
    }
}
