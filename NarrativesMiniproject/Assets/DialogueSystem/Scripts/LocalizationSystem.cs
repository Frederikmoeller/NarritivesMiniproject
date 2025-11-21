using UnityEngine;

public class LocalizationSystem
{
    public static LocalizationDatabase Db { get; private set; }

    public static void Load(string csvFileName)
    {
        Db = CSVLoader.LoadCSV(csvFileName);
    }

    public static void SetLanguage(string lang)
    {
        Db.SetLanguage(lang);
    }

    public static string Get(string key)
    {
        return Db.Get(key);
    }
}
