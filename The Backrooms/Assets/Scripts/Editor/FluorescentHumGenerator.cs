using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Backrooms.EditorTools
{
    /// <summary>
    /// 백룸 특유의 형광등 "웅―" 소리를 합성해 WAV 파일로 저장한다. (외부 음원 라이선스 걱정 없음)
    /// 전원 주파수 60Hz의 2배인 120Hz와 그 배음을 섞고, 아주 느린 떨림을 준다.
    /// 모든 주파수가 길이(초)의 정수배라 루프 이음새에서 끊김이 없다.
    /// </summary>
    public static class FluorescentHumGenerator
    {
        const int SampleRate = 44100;
        const int LengthSeconds = 8;

        public static AudioClip CreateClip(string assetPath)
        {
            int count = SampleRate * LengthSeconds;
            var samples = new float[count];

            // (주파수 Hz, 세기) — 모두 1/LengthSeconds Hz의 정수배여야 루프가 매끄럽다
            var partials = new (float freq, float amp)[]
            {
                (120f, 0.32f), (240f, 0.16f), (360f, 0.08f), (480f, 0.05f),
                (600f, 0.025f), (1080f, 0.012f), (2160f, 0.006f),
            };

            float peak = 0f;
            for (int n = 0; n < count; n++)
            {
                double t = (double)n / SampleRate;
                double s = 0;
                for (int p = 0; p < partials.Length; p++)
                    s += partials[p].amp * Math.Sin(2 * Math.PI * partials[p].freq * t + p * 0.7);

                // 느린 맥동(0.25Hz, 0.5Hz)으로 살아있는 듯한 웅웅거림
                double wobble = 1.0 + 0.06 * Math.Sin(2 * Math.PI * 0.25 * t) + 0.03 * Math.Sin(2 * Math.PI * 0.5 * t + 1.3);
                samples[n] = (float)(s * wobble);
                peak = Mathf.Max(peak, Mathf.Abs(samples[n]));
            }

            float gain = 0.6f / Mathf.Max(peak, 1e-4f);
            WriteWav(assetPath, samples, gain);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = (AudioImporter)AssetImporter.GetAtPath(assetPath);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        }

        static void WriteWav(string assetPath, float[] samples, float gain)
        {
            var fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", assetPath);
            using var writer = new BinaryWriter(File.Create(fullPath));
            int dataSize = samples.Length * 2;

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataSize);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);                 // fmt chunk size
            writer.Write((short)1);           // PCM
            writer.Write((short)1);           // mono
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);     // byte rate
            writer.Write((short)2);           // block align
            writer.Write((short)16);          // bits per sample
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataSize);
            foreach (var s in samples)
                writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * gain * short.MaxValue), short.MinValue, short.MaxValue));
        }
    }
}
