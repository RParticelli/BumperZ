using System;
using UnityEngine;

[Serializable]
public sealed class BumperLiveContentData
{
    public int revision = 1;
    public string seasonName = "TEMPORADA 1";
    public string eventName = "ARENA INFERNO";
    public long eventEndUnix;
    public bool eventEnabled = true;
}

public static class BumperLiveContent
{
    const string Key = "BumperZ.LiveContent.v1";
    static BumperLiveContentData data;
    public static BumperLiveContentData Data => data ??= Load();

    static BumperLiveContentData Load()
    {
        string json = PlayerPrefs.GetString(Key, "");
        BumperLiveContentData value = string.IsNullOrEmpty(json) ? new BumperLiveContentData() : JsonUtility.FromJson<BumperLiveContentData>(json);
        value ??= new BumperLiveContentData();
        if (value.eventEndUnix <= 0) value.eventEndUnix = DateTimeOffset.UtcNow.AddDays(3).ToUnixTimeSeconds();
        Save(value); return value;
    }

    public static bool ApplyServerJson(string json)
    {
        try
        {
            var incoming = JsonUtility.FromJson<BumperLiveContentData>(json);
            if (incoming == null || incoming.revision < Data.revision) return false;
            data = incoming; Save(data); return true;
        }
        catch { return false; }
    }

    public static string EventRemaining()
    {
        long seconds = Math.Max(0, Data.eventEndUnix - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var span = TimeSpan.FromSeconds(seconds);
        return $"{(int)span.TotalDays}d {span.Hours}h {span.Minutes}m";
    }

    static void Save(BumperLiveContentData value) { PlayerPrefs.SetString(Key, JsonUtility.ToJson(value)); PlayerPrefs.Save(); }
}
