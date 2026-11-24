using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace Backrooms.EditorTools
{
    public static class QuestProjectSetup
    {
        const string SettingsFolder = "Assets/Settings";
        const string RenderPipelinePath = SettingsFolder + "/QuestRenderPipeline.asset";
        const string RendererPath = SettingsFolder + "/QuestRenderer.asset";
        const string XriPackage = "com.unity.xr.interaction.toolkit";
        const string OpenXRLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";

        [MenuItem("Backrooms/1. Setup Quest Project", priority = 1)]
        public static void Run()
        {
            SetupRenderPipeline();
            SetupPlayerSettings();
            SetupXRManagement();
            SetupOpenXRFeatures();
            ImportXRInteractionSamples();
            SetupInteractionSimulator();

            AssetDatabase.SaveAssets();
            Debug.Log("[Backrooms] Quest 프로젝트 설정 완료. 다음: Backrooms > 2. Build World");
        }

        static void SetupRenderPipeline()
        {
            EnsureFolder(SettingsFolder);

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }
            rendererData.renderingMode = RenderingMode.Forward;
            rendererData.depthPrimingMode = DepthPrimingMode.Disabled;
            rendererData.postProcessData = null;
            EditorUtility.SetDirty(rendererData);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RenderPipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, RenderPipelinePath);
            }

            pipeline.msaaSampleCount = 4;
            pipeline.supportsHDR = false;
            pipeline.renderScale = 1f;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.maxAdditionalLightsCount = 4;
            pipeline.shadowDistance = 0f;

            var so = new SerializedObject(pipeline);
            so.FindProperty("m_MainLightRenderingMode").intValue = (int)LightRenderingMode.PerPixel;
            so.FindProperty("m_AdditionalLightsRenderingMode").intValue = (int)LightRenderingMode.PerPixel;
            so.FindProperty("m_MainLightShadowsSupported").boolValue = false;
            so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        static void SetupPlayerSettings()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.DefaultCompany.TheBackrooms");
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        }

        static void SetupXRManagement()
        {
            var perBuildTarget = GetOrCreateXRGeneralSettings();
            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                if (!perBuildTarget.HasSettingsForBuildTarget(group))
                    perBuildTarget.CreateDefaultSettingsForBuildTarget(group);
                if (!perBuildTarget.HasManagerSettingsForBuildTarget(group))
                    perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(group);

                var settings = perBuildTarget.SettingsForBuildTarget(group);
                settings.InitManagerOnStart = true;
                XRPackageMetadataStore.AssignLoader(settings.Manager, OpenXRLoader, group);
                EditorUtility.SetDirty(settings);
            }
            EditorUtility.SetDirty(perBuildTarget);
        }

        static XRGeneralSettingsPerBuildTarget GetOrCreateXRGeneralSettings()
        {
            var method = typeof(XRGeneralSettingsPerBuildTarget).GetMethod(
                "GetOrCreate", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (method != null && method.Invoke(null, null) is XRGeneralSettingsPerBuildTarget found)
                return found;

            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
                    out XRGeneralSettingsPerBuildTarget existing) && existing != null)
                return existing;

            EnsureFolder("Assets/XR");
            var created = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(created, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, created, true);
            return created;
        }

        static void SetupOpenXRFeatures()
        {
            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                FeatureHelpers.RefreshFeatures(group);
                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                if (settings == null)
                {
                    Debug.LogWarning($"[Backrooms] OpenXR 설정을 찾을 수 없습니다: {group}");
                    continue;
                }

                settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;

                foreach (var feature in settings.GetFeatures())
                {
                    if (feature is OculusTouchControllerProfile)
                        feature.enabled = true;
                    if (group == BuildTargetGroup.Android && feature is MetaQuestFeature)
                        feature.enabled = true;
                    EditorUtility.SetDirty(feature);
                }
                EditorUtility.SetDirty(settings);
            }
        }

        static void ImportXRInteractionSamples()
        {
            var samples = Sample.FindByPackage(XriPackage, null).ToList();
            foreach (var name in new[] { "Starter Assets", "XR Interaction Simulator" })
            {
                var sample = samples.FirstOrDefault(s => s.displayName == name);
                if (string.IsNullOrEmpty(sample.displayName))
                {
                    Debug.LogWarning($"[Backrooms] XRI 샘플을 찾을 수 없습니다: {name}");
                    continue;
                }
                if (!sample.isImported)
                    sample.Import(Sample.ImportOptions.HideImportWindow);
            }
            AssetDatabase.Refresh();
        }

        public static string FindSamplePrefab(string sampleName, string prefabFileName)
        {
            var sample = Sample.FindByPackage(XriPackage, null).FirstOrDefault(s => s.displayName == sampleName);
            if (string.IsNullOrEmpty(sample.importPath))
                return null;

            var full = Path.Combine(sample.importPath, prefabFileName).Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');
            var relative = full.StartsWith(dataPath) ? "Assets" + full.Substring(dataPath.Length) : full;
            return File.Exists(relative) ? relative : null;
        }

        public static void SetupInteractionSimulator() => SetupInteractionSimulator(false);

        public static void SetupInteractionSimulator(bool enable)
        {
            var prefabPath = FindSamplePrefab("XR Interaction Simulator", "XR Interaction Simulator.prefab");
            if (prefabPath == null)
                return;

            var type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings, Unity.XR.Interaction.Toolkit");
            var instanceProp = type?.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (!(instanceProp?.GetValue(null) is ScriptableObject settings))
                return;

            var so = new SerializedObject(settings);
            so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = enable;
            so.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue = true;
            so.FindProperty("m_UseClassic").boolValue = false;
            so.FindProperty("m_SimulatorPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
