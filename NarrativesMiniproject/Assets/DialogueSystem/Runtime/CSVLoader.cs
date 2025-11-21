using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class CSVLoader
{
    public static LocalizationDatabase LoadCSV(string path)
    {
        var db = new LocalizationDatabase();

        string fullPath = Path.Combine(Application.streamingAssetsPath, path);
        string[] lines = File.ReadAllLines(fullPath);

        if (lines.Length < 2)
        {
            Debug.LogError("Csv has no content!");
            return db;
        }

        string[] header = lines[0].Split(',');

        string[] languages = header.Skip(1).ToArray();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split(',');

            if (cols.Length < 2 || string.IsNullOrWhiteSpace(cols[0])) continue;

            string key = cols[0].Trim();
            db.data[key] = new Dictionary<string, string>();

            for (int langIndex = 0; langIndex < languages.Length; langIndex++)
            {
                string lang = languages[langIndex];

                if (langIndex + 1 < cols.Length)
                {
                    db.data[key][lang] = cols[langIndex + 1].Trim();
                }
                else
                {
                    db.data[key][lang] = "";
                }
            }
        }
        return db;
    }
}
