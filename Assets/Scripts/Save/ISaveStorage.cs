public interface ISaveStorage
{
    void Write(string key, string json);
    bool TryRead(string key, out string json);
    bool Exists(string key);
    void Delete(string key);
}
