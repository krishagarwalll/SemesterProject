using UnityEngine;

public class PlayerPrefsSaveStorage : ISaveStorage
{
    public void Write(string key, string json)
    {
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    public bool TryRead(string key, out string json)
    {
        if (!PlayerPrefs.HasKey(key)) { json = null; return false; }
        json = PlayerPrefs.GetString(key);
        return true;
    }

    public bool Exists(string key) => PlayerPrefs.HasKey(key);

    public void Delete(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
}
