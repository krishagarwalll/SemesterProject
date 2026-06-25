using System;
using System.IO;
using UnityEngine;

public static class StreamingAssetsVideoUrl
{
    public static bool TryBuild(string fileName, out string url)
    {
        url = string.Empty;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        string trimmedName = fileName.Trim().Replace('\\', '/');
        string safeName = Path.GetFileName(trimmedName);
        if (string.IsNullOrWhiteSpace(safeName) ||
            !string.Equals(trimmedName, safeName, StringComparison.Ordinal))
        {
            return false;
        }

        string basePath = Application.streamingAssetsPath.Replace('\\', '/').TrimEnd('/');
        url = $"{basePath}/Cutscenes/{Uri.EscapeDataString(safeName)}";
        return true;
    }
}
