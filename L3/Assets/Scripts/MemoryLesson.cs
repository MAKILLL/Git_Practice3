using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;
using Unity.Profiling.Memory;

public sealed class MemoryLesson : MonoBehaviour
{
    private AllocationExercise exercise;
    private bool busy;
    private string status = "Готово: сначала измерьте выделения в Lesson3.BuildValues.";
    private GUIStyle textStyle;
    private bool automatic;
    private bool snapshots;
    private string evidencePath;
    private bool failed;

    private IEnumerator Start()
    {
        exercise = GetComponent<AllocationExercise>();
        Application.targetFrameRate = 60;
        string[] args = Environment.GetCommandLineArgs();
        automatic = Array.IndexOf(args, "--self-test") >= 0;
        snapshots = Array.IndexOf(args, "--snapshots") >= 0;
        int output = Array.IndexOf(args, "--evidence");
        evidencePath = output >= 0 ? args[output + 1] : Path.Combine(Application.persistentDataPath, "Lesson3");
        Directory.CreateDirectory(evidencePath);
        if (!automatic) yield break;
        busy = true;
        Profiler.logFile = Path.Combine(evidencePath, "allocations.raw");
        Profiler.enableBinaryLog = true;
        Profiler.enabled = true;
        for (int frame = 0; frame < 30; frame++) yield return null;
        exercise.Measuring = true;
        for (int frame = 0; frame < 120; frame++)
        {
            yield return null;
            Require(exercise.Checksum == AllocationExercise.ExpectedChecksum, "checksum");
        }
        exercise.Measuring = false;
        bool reuse = Array.IndexOf(args, "--expect-reuse") >= 0;
        Require(reuse ? exercise.TotalCalls == 0 : exercise.TotalCalls > 0, "allocation contract");
        Profiler.enabled = false;
        Profiler.enableBinaryLog = false;
        Debug.Log($"LESSON3 allocations samples={exercise.Samples} total_gc_alloc_calls={exercise.TotalCalls} last_gc_alloc_calls={exercise.AllocationCalls} checksum={exercise.Checksum} reuse={reuse}");
        exercise.enabled = false;
        yield return Cleanup();
        yield return Capture("01_baseline");
        yield return LoadPayload();
        yield return UnloadPayload();
        yield return Cleanup();
        Require(PayloadCache.RetainedBytes == 16777216 && PayloadCache.LastData.IsAlive, "retained after unload");
        Report("retained");
        yield return Capture("02_retained");
        PayloadCache.Retained.Clear();
        yield return Cleanup();
        Require(PayloadCache.RetainedBytes == 0 && !PayloadCache.LastData.IsAlive, "released after clear");
        Report("cleared");
        yield return Capture("03_cleared");
        Debug.Log(failed ? "LESSON3 SELF_TEST FAIL" : "LESSON3 SELF_TEST PASS");
        Application.Quit(failed ? 1 : 0);
    }

    private void Require(bool condition, string contract)
    {
        if (condition) return;
        failed = true;
        Debug.LogError($"LESSON3 failed contract={contract}");
    }

    private void Report(string phase)
    {
        Debug.Log($"LESSON3 phase={phase} scene_loaded={SceneManager.GetSceneByName("Payload").isLoaded} retained_count={PayloadCache.Retained.Count} retained_bytes={PayloadCache.RetainedBytes} weak_alive={PayloadCache.LastData.IsAlive} mono_used={Profiler.GetMonoUsedSizeLong()} mono_reserved={Profiler.GetMonoHeapSizeLong()}");
    }

    private IEnumerator Cleanup()
    {
        yield return Resources.UnloadUnusedAssets();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        yield return null;
    }

    private IEnumerator Capture(string name)
    {
        if (!snapshots) yield break;
        bool complete = false;
        bool success = false;
        string path = Path.Combine(evidencePath, name + ".snap");
        MemoryProfiler.TakeSnapshot(path, (savedPath, result) =>
        {
            success = result;
            complete = true;
            Debug.Log($"LESSON3 snapshot={savedPath} success={result}");
        }, CaptureFlags.ManagedObjects | CaptureFlags.NativeObjects | CaptureFlags.NativeAllocations);
        while (!complete) yield return null;
        Require(success, "snapshot " + name);
    }

    private IEnumerator LoadPayload()
    {
        status = "Загрузка Payload...";
        yield return SceneManager.LoadSceneAsync("Payload", LoadSceneMode.Additive);
        status = "Payload загружена. Теперь выгрузите сцену.";
    }

    private IEnumerator UnloadPayload()
    {
        status = "Ожидание выгрузки Payload...";
        yield return SceneManager.UnloadSceneAsync("Payload");
        status = "Сцена выгружена; static PayloadCache.Retained всё ещё удерживает byte[].";
    }

    private IEnumerator Action(IEnumerator operation)
    {
        busy = true;
        yield return operation;
        busy = false;
    }

    private IEnumerator ClearPayload()
    {
        PayloadCache.Retained.Clear();
        yield return Cleanup();
        status = "Ссылки очищены; сборка мусора выполнена вне измеряемого участка.";
    }

    private void OnGUI()
    {
        if (textStyle == null)
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
        GUI.Box(new Rect(20, 20, 950, 530), "Лекция 3: память Unity");
        GUILayout.BeginArea(new Rect(40, 60, 900, 470));
        GUILayout.Label("1. Аллокации: CPU Profiler / Hierarchy / Lesson3.BuildValues / GC Alloc", textStyle);
        GUILayout.Label($"Checksum: {exercise.Checksum} (ожидается {AllocationExercise.ExpectedChecksum})\nGC.Alloc вызовов за последний BuildValues: {exercise.AllocationCalls} (байты смотрите в Profiler)", textStyle);
        GUILayout.Label("Исправьте AllocationExercise.BuildValues: переиспользуйте List<int> с достаточной Capacity, сохранив данные и checksum. Сравните одинаковые участки после прогрева.", textStyle);
        GUILayout.Space(20);
        GUILayout.Label($"2. Удержание: PayloadCache.Retained -> LessonPayload -> Data (byte[])\nСцена: {SceneManager.GetSceneByName("Payload").isLoaded}; удерживается: {PayloadCache.RetainedBytes} байт", textStyle);
        GUI.enabled = !busy && !automatic;
        if (GUILayout.Button(exercise.enabled ? "Приостановить нагрузку для снимков" : "Продолжить нагрузку")) exercise.enabled = !exercise.enabled;
        GUI.enabled = !busy && !automatic && !SceneManager.GetSceneByName("Payload").isLoaded && PayloadCache.Retained.Count == 0;
        if (GUILayout.Button("Загрузить Payload (additive)")) StartCoroutine(Action(LoadPayload()));
        GUI.enabled = !busy && !automatic && SceneManager.GetSceneByName("Payload").isLoaded;
        if (GUILayout.Button("Выгрузить Payload и дождаться завершения")) StartCoroutine(Action(UnloadPayload()));
        GUI.enabled = !busy && !automatic && !SceneManager.GetSceneByName("Payload").isLoaded;
        if (GUILayout.Button("Очистить static ссылки + контролируемая очистка")) StartCoroutine(Action(ClearPayload()));
        GUI.enabled = true;
        GUILayout.Label(status, textStyle);
        GUILayout.EndArea();
    }
}
