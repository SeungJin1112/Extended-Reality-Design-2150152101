using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Backrooms.EditorTools
{
    [InitializeOnLoad]
    public static class EditorJobBridge
    {
        static readonly string Dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "Temp", "BackroomsJobs");
        static double next;

        static EditorJobBridge()
        {
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            next = EditorApplication.timeSinceStartup + 1.0;
            var request = Path.Combine(Dir, "request.txt");
            if (!File.Exists(request))
                return;

            var lines = File.ReadAllLines(request);
            File.Delete(request);
            var log = new StringBuilder();
            void Capture(string msg, string stack, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || msg.StartsWith("[Backrooms]"))
                    log.AppendLine($"{type}: {msg}");
            }

            string status = "OK";
            Application.logMessageReceived += Capture;
            try
            {
                foreach (var line in lines)
                {
                    var l = line.Trim();
                    if (l.Length == 0) continue;
                    var eq = l.IndexOf('=');
                    if (eq > 0 && !l.Contains("("))
                    {
                        Environment.SetEnvironmentVariable(l.Substring(0, eq), l.Substring(eq + 1));
                        continue;
                    }
                    if (!l.StartsWith("Backrooms.EditorTools.")) throw new InvalidOperationException($"Not allowed: {l}");
                    var dot = l.LastIndexOf('.');
                    var type = typeof(EditorJobBridge).Assembly.GetType(l.Substring(0, dot));
                    var method = type?.GetMethod(l.Substring(dot + 1), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null);
                    if (method == null) throw new MissingMethodException(l);
                    method.Invoke(null, null);
                }
            }
            catch (Exception e)
            {
                status = "FAIL " + (e.InnerException ?? e);
            }
            finally
            {
                Application.logMessageReceived -= Capture;
            }
            Directory.CreateDirectory(Dir);
            File.WriteAllText(Path.Combine(Dir, "result.txt"), status + "\n" + log, new UTF8Encoding(false));
        }
    }
}
