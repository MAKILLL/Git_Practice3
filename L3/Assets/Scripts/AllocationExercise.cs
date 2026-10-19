using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public sealed class AllocationExercise : MonoBehaviour
{
    public const int ItemCount = 4096;
    public const long ExpectedChecksum = 25188352;
    private static readonly ProfilerMarker Marker = new ProfilerMarker("Lesson3.BuildValues");
    public long Checksum { get; private set; }
    public int AllocationCalls { get; private set; }
    public int Samples { get; private set; }
    public long TotalCalls { get; private set; }
    public bool Measuring { get; set; }
    private ProfilerRecorder allocationRecorder;
    private readonly List<int> values = new List<int>(ItemCount);

    private void Awake()
    {
        allocationRecorder = new ProfilerRecorder(ProfilerCategory.Memory, "GC.Alloc", 64,
            ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
    }

    private void OnDestroy() => allocationRecorder.Dispose();

    private void Update()
    {
        allocationRecorder.Reset();
        allocationRecorder.Start();
        using (Marker.Auto())
        {
            Checksum = BuildValues();
        }
        allocationRecorder.Stop();
        AllocationCalls = allocationRecorder.Count;
        if (Measuring)
        {
            Samples++;
            TotalCalls += AllocationCalls;
        }
    }

    private long BuildValues()
    {
        values.Clear();
        for (int index = 0; index < ItemCount; index++)
            values.Add(index * 3 + 7);
        long checksum = 0;
        for (int index = 0; index < values.Count; index++)
            checksum += values[index];
        return checksum;
    }
}
