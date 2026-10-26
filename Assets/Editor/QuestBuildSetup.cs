using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

// Reproducible Quest 3 setup for both supported development paths:
// - Android OpenXR for APKs that run directly on the headset.
// - Standalone OpenXR for Windows Editor Play Mode through Meta Horizon Link.
public static class QuestBuildSetup
{
    private const string ScenePath = "Assets/Scenes/ArcaneWorkshop.unity";
    private const string ApkPath = "Builds/ArcaneWorkshopReawakening.apk";
    private const string XRSettingsFolder = "Assets/XR";

    [MenuItem("MP2B/Configure Quest Settings")]
    public static void Configure()
    {
        ConfigureXR(BuildTargetGroup.Android);
        ConfigureXR(BuildTargetGroup.Standalone);
        ConfigureOpenXRFeatures(BuildTargetGroup.Android);
        ConfigureOpenXRFeatures(BuildTargetGroup.Standalone);
        ConfigurePlayer();
        AssetDatabase.SaveAssets();
        Debug.Log("MP2B: Quest OpenXR settings configured for Android APK and Windows Editor Link.");
    }

    [MenuItem("MP2B/Build Quest APK")]
    public static void BuildApk()
    {
        Configure();
        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("MP2B: APK build " + report.summary.result + " (" + report.summary.totalErrors + " errors) -> " + ApkPath);
    }

    private static void ConfigureXR(BuildTargetGroup group)
    {
        XRGeneralSettingsPerBuildTarget perTarget;
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out perTarget);
        if (perTarget == null)
        {
            Directory.CreateDirectory(XRSettingsFolder);
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget, XRSettingsFolder + "/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, perTarget, true);
        }

        if (!perTarget.HasSettingsForBuildTarget(group))
        {
            perTarget.CreateDefaultSettingsForBuildTarget(group);
        }

        XRGeneralSettings settings = perTarget.SettingsForBuildTarget(group);
        if (settings.Manager == null)
        {
            XRManagerSettings manager = ScriptableObject.CreateInstance<XRManagerSettings>();
            manager.name = group + " Providers";
            AssetDatabase.AddObjectToAsset(manager, AssetDatabase.GetAssetPath(perTarget));
            settings.Manager = manager;
        }
        settings.InitManagerOnStart = true;
        XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(settings.Manager);
        EditorUtility.SetDirty(perTarget);
    }

    private static void ConfigureOpenXRFeatures(BuildTargetGroup group)
    {
        OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
        if (settings == null)
        {
            Debug.LogWarning("MP2B: OpenXR settings for " + group + " were not found.");
            return;
        }

        settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        settings.latencyOptimization = OpenXRSettings.LatencyOptimization.PrioritizeInputPolling;
        if (group == BuildTargetGroup.Android)
        {
            Enable(settings, typeof(MetaQuestFeature));
        }
        Enable(settings, typeof(OculusTouchControllerProfile));
        Enable(settings, typeof(MetaQuestTouchPlusControllerProfile));
        EditorUtility.SetDirty(settings);
    }

    private static void Enable(OpenXRSettings settings, System.Type featureType)
    {
        OpenXRFeature feature = settings.GetFeature(featureType);
        if (feature == null)
        {
            Debug.LogWarning("MP2B: OpenXR feature not found: " + featureType.Name);
            return;
        }
        feature.enabled = true;
        EditorUtility.SetDirty(feature);
    }

    // 0 = legacy Input Manager, 1 = Input System package, 2 = both (unsupported on Android).
    private static void SetActiveInputHandling(int mode)
    {
        Object settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
        SerializedObject serialized = new SerializedObject(settings);
        SerializedProperty property = serialized.FindProperty("activeInputHandler");
        if (property != null && property.intValue != mode)
        {
            property.intValue = mode;
            serialized.ApplyModifiedProperties();
            Debug.Log("MP2B: Active Input Handling set to Input System only. Restart the editor for it to take full effect.");
        }
    }

    private static void ConfigurePlayer()
    {
        PlayerSettings.companyName = "CS417";
        PlayerSettings.productName = "Arcane Workshop Reawakening";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.cs417.arcaneworkshop.mp2b");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.runInBackground = true;
        SetActiveInputHandling(1);

        // OpenXR's Meta Quest validation reads the *selected* build target group and throws when
        // it is not Android, so keep the Build Settings selection on Android as well.
        EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }
}
