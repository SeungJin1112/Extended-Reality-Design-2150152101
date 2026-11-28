using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Backrooms.EditorTools
{
    // Play 모드를 잠깐 켜서 실제 실행 상태를 점검하고 자동으로 끈다. (EditorJobBridge로 PlayTestRunner.Run 호출)
    // 결과: Temp/PlayTest/report.txt, 화면: Temp/PlayTest/play_XXs.png
    [InitializeOnLoad]
    public static class PlayTestRunner
    {
        const string KeyStart = "Backrooms.PlayTest.Start";
        const float Duration = 24f;
        static readonly float[] Shots = { 6f, 12f, 20f };
        static readonly string Dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "Temp", "PlayTest");

        static PlayTestRunner() => EditorApplication.update += Tick;

        static void Run()
        {
            Directory.CreateDirectory(Dir);
            foreach (var f in Directory.GetFiles(Dir)) File.Delete(f);
            SessionState.SetFloat(KeyStart, -1f);
            SessionState.SetInt("Backrooms.PlayTest.Shot", 0);
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            float start = SessionState.GetFloat(KeyStart, -2f);
            if (start <= -2f || !EditorApplication.isPlaying) return;
            if (start < 0f) { SessionState.SetFloat(KeyStart, Time.realtimeSinceStartup); return; }

            float t = Time.realtimeSinceStartup - start;
            int shot = SessionState.GetInt("Backrooms.PlayTest.Shot", 0);
            if (shot < Shots.Length && t >= Shots[shot])
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(Dir, $"play_{Shots[shot]:00}s.png"));
                SessionState.SetInt("Backrooms.PlayTest.Shot", shot + 1);
            }
            if (t < Duration) return;

            WriteReport(t);
            SessionState.EraseFloat(KeyStart);
            EditorApplication.ExitPlaymode();
        }

        static void WriteReport(float t)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"elapsed {t:0.0}s");
            sb.AppendLine("scenes:");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                sb.AppendLine($"  {SceneManager.GetSceneAt(i).name}");
            foreach (var g in Object.FindObjectsOfType<NarrationSequence>().GroupBy(n => n.name))
                sb.AppendLine($"narration {g.Key}: copies={g.Count()} finished={string.Join(",", g.Select(n => n.Finished))}");
            foreach (var ra in Object.FindObjectsOfType<ResearcherAnimation>().OrderBy(r => r.name))
            {
                var a = ra.GetComponentInChildren<Animator>();
                var info = a != null ? a.GetCurrentAnimatorStateInfo(0) : default;
                string state = a != null && a.runtimeAnimatorController != null
                    ? a.runtimeAnimatorController.animationClips.Select(c => c.name).FirstOrDefault(n => info.IsName(n) || info.IsName(n.Replace("Researcher", ""))) ?? info.shortNameHash.ToString()
                    : "none";
                sb.AppendLine($"researcher {ra.name}: enabled={a?.enabled} state={state} t={info.normalizedTime:0.00}");
            }
            File.WriteAllText(Path.Combine(Dir, "report.txt"), sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[Backrooms] PlayTest report written");
        }
    }
}
