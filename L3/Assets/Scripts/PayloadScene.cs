using System;
using UnityEngine;

public sealed class PayloadScene : MonoBehaviour
{
    private void Awake()
    {
        var payload = new LessonPayload();
        for (int index = 0; index < payload.Data.Length; index += 4096)
            payload.Data[index] = 123;
        PayloadCache.Retained.Add(payload);
        PayloadCache.LastData = new WeakReference(payload.Data);
    }
}
