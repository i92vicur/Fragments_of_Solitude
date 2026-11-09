// [LAT] temporal: instrumentación de latencia para las pruebas. Eliminar este archivo y las líneas marcadas con [LAT] al terminar.
// Las muestras se guardan en memoria sin registrar nada por muestra. F9 exporta LatencyResults.txt a Application.persistentDataPath
// y al cerrar la aplicación se imprime el resumen en el log.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class LatencyProbe
{
    public const string ResultsFileName = "LatencyResults.txt";

    private static readonly string[] KnownChannels =
    {
        "special_input_to_projectile",
        "auto_aim_to_shot",
        "auto_cooldown_at_aim",
        "jump_release_to_impulse",
        "dash_phase1_input_to_displacement",
        "dash_phase2_input_to_displacement",
    };

    private static readonly Dictionary<string, long> pending = new Dictionary<string, long>();
    private static readonly Dictionary<string, List<double>> samples = new Dictionary<string, List<double>>();
    private static readonly Dictionary<string, int> blocked = new Dictionary<string, int>();
    private static readonly List<string> channels = new List<string>();

    public static long Now => Stopwatch.GetTimestamp();

    public static void Begin(string channel)
    {
        Begin(channel, Now);
    }

    public static void Begin(string channel, long startTimestamp)
    {
        Ensure(channel);
        pending[channel] = startTimestamp;
    }

    public static void End(string channel)
    {
        if (!pending.TryGetValue(channel, out long startTimestamp))
            return;

        pending.Remove(channel);
        samples[channel].Add(ToMilliseconds(Now - startTimestamp));
    }

    // Intento válido que no llegó a ejecutarse. Solo cuenta si había una medición iniciada.
    public static void Cancel(string channel)
    {
        if (!pending.Remove(channel))
            return;

        Ensure(channel);
        blocked[channel]++;
    }

    public static void Blocked(string channel)
    {
        Ensure(channel);
        blocked[channel]++;
    }

    public static bool IsPending(string channel)
    {
        return pending.ContainsKey(channel);
    }

    // Para valores ya calculados fuera de un par inicio/fin.
    public static void Record(string channel, double milliseconds)
    {
        Ensure(channel);
        samples[channel].Add(milliseconds);
    }

    public static void Clear()
    {
        pending.Clear();
        channels.Clear();
        samples.Clear();
        blocked.Clear();
    }

    public static string BuildSummary()
    {
        var builder = new StringBuilder();
        builder.AppendLine("[LAT] Resumen de latencias (ms). Canal | válidas | bloqueadas | media | mediana | mín | máx");

        foreach (string channel in channels)
        {
            List<double> values = samples[channel];
            int valid = values.Count;
            string prefix = $"{channel} | {valid} | {blocked[channel]}";

            if (valid == 0)
            {
                builder.AppendLine($"{prefix} | - | - | - | -");
                continue;
            }

            var sorted = new List<double>(values);
            sorted.Sort();

            double sum = 0;
            foreach (double value in sorted)
                sum += value;

            int middle = valid / 2;
            double median = valid % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2.0;

            builder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0} | {1:F3} | {2:F3} | {3:F3} | {4:F3}",
                prefix, sum / valid, median, sorted[0], sorted[valid - 1]));
        }

        return builder.ToString();
    }

    public static void PrintSummary()
    {
        Debug.Log(BuildSummary());
    }

    public static bool HasSamples()
    {
        foreach (List<double> values in samples.Values)
        {
            if (values.Count > 0)
                return true;
        }

        return false;
    }

    public static string BuildReport()
    {
        foreach (string channel in KnownChannels)
            Ensure(channel);

        var builder = new StringBuilder();
        builder.AppendLine("Fragments of Solitude - resultados de latencia [LAT] temporal");
        builder.AppendLine("Generado: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        builder.AppendLine("Origen: " + (Application.isEditor ? "Editor" : "Build") + " | " + Application.platform);
        builder.AppendLine();

        if (!HasSamples())
            builder.AppendLine("SIN MUESTRAS: ninguna acción válida se ha medido en esta sesión.");

        builder.Append(BuildSummary());
        return builder.ToString();
    }

    // Sobrescribe el archivo anterior en cada exportación.
    public static bool TryExport(out string path, out string error)
    {
        path = Path.Combine(Application.persistentDataPath, ResultsFileName);
        error = null;

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(path, BuildReport(), new UTF8Encoding(false));
            return true;
        }
        catch (System.Exception e)
        {
            error = e.Message;
            return false;
        }
    }

    private static void Ensure(string channel)
    {
        if (samples.ContainsKey(channel))
            return;

        samples[channel] = new List<double>();
        blocked[channel] = 0;
        channels.Add(channel);
    }

    private static double ToMilliseconds(long ticks)
    {
        return ticks * 1000.0 / Stopwatch.Frequency;
    }
}

// Objeto oculto que escucha F9, muestra un aviso breve y exporta los resultados. Se crea solo, sin tocar escenas.
public sealed class LatencyProbeHost : MonoBehaviour
{
    private const float MessageSeconds = 6f;

    private static LatencyProbeHost instance;

    private string message;
    private float messageUntil;
    private GUIStyle messageStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (instance != null)
            return;

        var go = new GameObject("[LatencyProbe]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<LatencyProbeHost>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
            Export();
    }

    private void Export()
    {
        bool saved = LatencyProbe.TryExport(out string path, out string error);

        if (!saved)
            message = "[LAT] ERROR al guardar los resultados: " + error;
        else if (!LatencyProbe.HasSamples())
            message = "[LAT] Sin muestras: no hay acciones válidas medidas.\nArchivo guardado (sin estadísticas): " + path;
        else
            message = "[LAT] Resultados guardados correctamente.\n" + path;

        messageUntil = Time.unscaledTime + MessageSeconds;
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(message) || Time.unscaledTime > messageUntil)
            return;

        if (messageStyle == null)
        {
            messageStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                padding = new RectOffset(12, 12, 8, 8),
            };
            messageStyle.normal.textColor = Color.white;
        }

        GUI.Box(new Rect(20, 20, Mathf.Min(Screen.width - 40, 900), 80), message, messageStyle);
    }

    private void OnApplicationQuit()
    {
        LatencyProbe.PrintSummary();
    }
}
