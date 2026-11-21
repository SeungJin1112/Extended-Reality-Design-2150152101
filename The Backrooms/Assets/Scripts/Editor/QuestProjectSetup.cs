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
    /// <summary>
    /// Meta Quest(OpenXR) + XR Interaction Toolkit 개발을 위한 프로젝트 설정을 한 번에 적용한다.
    /// 메뉴: Backrooms > 1. Setup Quest Project
    /// 여러 번 실행해도 안전하다(이미 있는 설정은 유지/갱신).
    /// </summary>
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
            Debug.Log("[Backrooms] Quest 프로젝트 설정 완료. 다음: Backrooms > 2. Build Backrooms Scene");
        }

        // ---------------------------------------------------------------- URP

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
            rendererData.postProcessData = null; // Quest: 포스트 프로세싱 비활성
            EditorUtility.SetDirty(rendererData);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RenderPipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, RenderPipelinePath);
            }

            // Quest 권장값: MSAA 4x, HDR 끔, 실시간 그림자 끔(조명은 라이트맵으로 베이크)
            pipeline.msaaSampleCount = 4;
            pipeline.supportsHDR = false;
            pipeline.renderScale = 1f;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.maxAdditionalLightsCount = 4;
            pipeline.shadowDistance = 0f;

            // URP 14에서 setter가 internal인 항목은 SerializedObject로 설정
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

        // ---------------------------------------------------------------- Player

        static void SetupPlayerSettings()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // Android (Meta Quest)
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.DefaultCompany.TheBackrooms");
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32; // Quest 요구: API 32 이상
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        }

        // ---------------------------------------------------------------- XR Plug-in Management

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
            // 패키지 내부(GetOrCreate)와 같은 경로/방식을 쓰기 위해 리플렉션으로 호출
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

        // ---------------------------------------------------------------- OpenXR

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
                    // Quest 컨트롤러 프로필은 PC(Quest Link)와 Android 모두 사용
                    if (feature is OculusTouchControllerProfile)
                        feature.enabled = true;
                    // Meta Quest 지원 기능은 Android 전용
                    if (group == BuildTargetGroup.Android && feature is MetaQuestFeature)
                        feature.enabled = true;
                    EditorUtility.SetDirty(feature);
                }
                EditorUtility.SetDirty(settings);
            }
        }

        // ---------------------------------------------------------------- XRI Samples

        /// <summary>XR Origin 프리팹이 들어있는 Starter Assets와 에디터 테스트용 XR Interaction Simulator를 임포트.</summary>
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

        /// <summary>헤드셋 없이 에디터 Play 모드에서 키보드/마우스로 XR을 테스트할 수 있게 한다(에디터 전용).</summary>
        static void SetupInteractionSimulator()
        {
            var prefabPath = FindSamplePrefab("XR Interaction Simulator", "XR Interaction Simulator.prefab");
            if (prefabPath == null)
                return;

            var type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings, Unity.XR.Interaction.Toolkit");
            var instanceProp = type?.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (!(instanceProp?.GetValue(null) is ScriptableObject settings))
                return;

            var so = new SerializedObject(settings);
            so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = true;
            so.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue = true;
            so.FindProperty("m_UseClassic").boolValue = false;
            so.FindProperty("m_SimulatorPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        // ---------------------------------------------------------------- Util

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
