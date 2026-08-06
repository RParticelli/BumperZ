using System;
using UnityEngine;

[Serializable]
public sealed class BumperProfileData
{
    public string playerName = "RacerX";
    public int level = 1;
    public int experience;
    public int coins = 1500;
    public int gems = 50;
    public int trophies;
    public int passPoints;
    public int wins;
    public int matches;
    public int unreadMessages = 3;
    public int eventRewards = 1;
}

public static class BumperProfile
{
    const string SaveKey = "BumperZ.Profile.v1";
    static BumperProfileData data;
    public static BumperProfileData Data => data ??= Load();

    static BumperProfileData Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return new BumperProfileData();
        try { return JsonUtility.FromJson<BumperProfileData>(json) ?? new BumperProfileData(); }
        catch { return new BumperProfileData(); }
    }

    public static void RecordMatch(bool won)
    {
        var p = Data;
        p.matches++;
        p.coins += won ? 350 : 90;
        p.trophies = Mathf.Max(0, p.trophies + (won ? 30 : -8));
        p.experience += won ? 140 : 55;
        p.passPoints += won ? 45 : 18;
        if (won) p.wins++;
        while (p.experience >= ExperienceForLevel(p.level))
        {
            p.experience -= ExperienceForLevel(p.level);
            p.level++;
            p.gems += 10;
        }
        Save();
    }

    public static int ExperienceForLevel(int level) => 250 + Mathf.Max(0, level - 1) * 50;
    public static string RankName(int trophies) => trophies >= 2500 ? "DIAMANTE" : trophies >= 1500 ? "OURO III" : trophies >= 800 ? "PRATA II" : trophies >= 300 ? "BRONZE I" : "NOVATO";

    public static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
    }
}
