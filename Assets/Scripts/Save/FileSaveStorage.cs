using System.IO;
using UnityEngine;

public class FileSaveStorage : ISaveStorage
{
    public void Write(string key, string json)
        => File.WriteAllText(GetPath(key), json);

    public bool TryRead(string key, out string json)
    {
        string path = GetPath(key);
        if (!File.Exists(path)) { json = null; return false; }
        json = File.ReadAllText(path);
        return true;
    }

    public bool Exists(string key) => File.Exists(GetPath(key));

    public void Delete(string key)
    {
        string path = GetPath(key);
        if (File.Exists(path)) File.Delete(path);
    }

    private static string GetPath(string key)
        => Path.Combine(Application.persistentDataPath, key + ".json");
}
