using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public class WebOptimizer
{
    [MenuItem("Tools/Apply Web Release Settings")]
    public static void ApplyWebReleaseSettings()
    {
        var target = NamedBuildTarget.WebGL;

        // Code and binary size optimization
        PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.High);
        PlayerSettings.stripUnusedMeshComponents = true;

        // WebGL specific settings
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true; // Essential for itch.io hosting
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.WebGL.initialMemorySize = 64; // 64 MB starting heap to prevent immediate resize hitches
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;

        // Persist to disk
        AssetDatabase.SaveAssets();

        Debug.Log("[WebOptimizer] Web release settings applied and saved successfully:\n"
            + $"  IL2CPP Code Gen: {PlayerSettings.GetIl2CppCodeGeneration(target)}\n"
            + $"  Managed Stripping: {PlayerSettings.GetManagedStrippingLevel(target)}\n"
            + $"  Compression: {PlayerSettings.WebGL.compressionFormat} (Fallback: {PlayerSettings.WebGL.decompressionFallback})\n"
            + $"  Exceptions: {PlayerSettings.WebGL.exceptionSupport}\n"
            + $"  Initial Memory: {PlayerSettings.WebGL.initialMemorySize} MB\n"
            + $"  Data Caching: {PlayerSettings.WebGL.dataCaching}");
    }

    [MenuItem("Tools/Build WebGL for Itch")]
    public static void BuildWebGLForItch()
    {
        // 1. Ensure settings are optimized
        ApplyWebReleaseSettings();

        // 2. Collect enabled scenes
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[WebOptimizer] No scenes enabled in EditorBuildSettings!");
            return;
        }

        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "WebGL");
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        Debug.Log($"[WebOptimizer] Starting WebGL build to: {outputPath}\nScenes: {string.Join(", ", scenes)}");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[WebOptimizer] WebGL Build SUCCEEDED! Total size: {summary.totalSize / (1024 * 1024f):F2} MB. Output: {outputPath}");
        }
        else
        {
            Debug.LogError($"[WebOptimizer] WebGL Build FAILED with result: {summary.result}. Errors: {summary.totalErrors}");
        }
    }
}
