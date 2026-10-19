using System;
using System.Collections.Generic;

[Serializable]
public sealed class LessonPayload
{
    public string Name = "Lesson3_Retained_16MiB";
    public byte[] Data = new byte[16 * 1024 * 1024];
}

public static class PayloadCache
{
    public static readonly List<LessonPayload> Retained = new List<LessonPayload>();
    public static WeakReference LastData;

    public static long RetainedBytes
    {
        get
        {
            long bytes = 0;
            foreach (LessonPayload payload in Retained)
                bytes += payload.Data.LongLength;
            return bytes;
        }
    }
}
