using System.Collections.Generic;
using UnityEngine;
using DialogueSystem;

public class LocalizationDatabase
{
    public Dictionary<string, Dictionary<string, string>> data = new Dictionary<string, Dictionary<string, string>>();

    public string CurrentLanguage { get; private set; } = "EN";

    public void SetLanguage(string lang)
    {
        CurrentLanguage = lang;
    }

    public string Get(string key)
    {
        if (!data.ContainsKey(key)) return $"Missing Key: {key}";
        if (!data[key].ContainsKey(CurrentLanguage)) return $"[No {CurrentLanguage} For {key}";
        return data[key][CurrentLanguage];
    }
}
