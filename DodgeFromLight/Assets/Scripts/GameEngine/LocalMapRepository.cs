using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Persists authored puzzle maps locally without any server dependency.
/// </summary>
public static class LocalMapRepository
{
    public static string RootFolder => Path.Combine(Application.persistentDataPath, "Maps");

    /// <summary>
    /// Saves the current editor workspace into the local map library.
    /// </summary>
    public static void Save(FullMap map, byte[] preview)
    {
        // Write the canonical workspace first, then mirror it into the map library.
        if (map == null || map.Grid == null || map.Map == null)
            throw new ArgumentNullException(nameof(map));

        GridManager.SaveFullMap(map, preview ?? Array.Empty<byte>());
        var destination = GetMapFolder(map.Map.ID);
        CopyWorkspace(destination);
    }

    /// <summary>
    /// Copies a local map into the editor and gameplay workspace.
    /// </summary>
    public static bool Open(string mapId)
    {
        // Replace the temporary workspace only when a complete local map exists.
        var source = GetMapFolder(mapId);
        if (!File.Exists(GridManager.GetGridPath(source)) ||
            !File.Exists(GridManager.GetMapPath(source)))
        {
            return false;
        }

        GridManager.CleanFolder();
        CopyDirectoryFiles(source, GridManager.Folder);
        return true;
    }

    /// <summary>
    /// Loads all valid local map metadata ordered by most recent save.
    /// </summary>
    public static IReadOnlyList<Map> GetAll()
    {
        // Ignore incomplete folders so one broken map cannot block the library.
        Directory.CreateDirectory(RootFolder);
        var maps = new List<Map>();
        foreach (var directory in Directory.GetDirectories(RootFolder))
        {
            var map = GridManager.GetMap(directory);
            if (map != null && !string.IsNullOrWhiteSpace(map.ID))
                maps.Add(map);
        }

        return maps
            .OrderByDescending(map => GetLastWriteTimeUtc(map.ID))
            .ToList();
    }

    /// <summary>
    /// Gets the most recently saved local map.
    /// </summary>
    public static Map GetMostRecent()
    {
        // The ordered library exposes the last edited map first.
        return GetAll().FirstOrDefault();
    }

    /// <summary>
    /// Persists metadata changes made while playing a local map.
    /// </summary>
    public static void UpdateMetadata(Map map)
    {
        // Update both the active workspace and its durable library entry.
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        GridManager.SaveMap(map);
        var destination = GetMapFolder(map.ID);
        Directory.CreateDirectory(destination);
        File.Copy(
            GridManager.GetMapPath(),
            GridManager.GetMapPath(destination),
            true);
    }

    /// <summary>
    /// Deletes one authored map from the local library.
    /// </summary>
    public static bool Delete(string mapId)
    {
        // Restrict deletion to the exact child folder derived from the map identifier.
        var folder = GetMapFolder(mapId);
        if (!Directory.Exists(folder))
            return false;

        Directory.Delete(folder, true);
        return true;
    }

    /// <summary>
    /// Resolves the storage folder for a stable map identifier.
    /// </summary>
    private static string GetMapFolder(string mapId)
    {
        // Reject path syntax so authored identifiers cannot escape the map library.
        if (string.IsNullOrWhiteSpace(mapId) ||
            mapId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            mapId.Contains(".."))
        {
            throw new ArgumentException("Invalid local map identifier.", nameof(mapId));
        }

        return Path.Combine(RootFolder, mapId);
    }

    /// <summary>
    /// Copies the canonical workspace into a map library folder.
    /// </summary>
    private static void CopyWorkspace(string destination)
    {
        // Replace stale files while preserving the exact authored workspace contents.
        Directory.CreateDirectory(destination);
        CopyDirectoryFiles(GridManager.Folder, destination);
    }

    /// <summary>
    /// Copies every file directly contained in one map folder.
    /// </summary>
    private static void CopyDirectoryFiles(string source, string destination)
    {
        // Map packages are intentionally flat: grid, metadata, and preview.
        Directory.CreateDirectory(destination);
        foreach (var sourceFile in Directory.GetFiles(source))
        {
            var destinationFile = Path.Combine(destination, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, destinationFile, true);
        }
    }

    /// <summary>
    /// Reads the most useful timestamp for ordering local maps.
    /// </summary>
    private static DateTime GetLastWriteTimeUtc(string mapId)
    {
        // Metadata changes on every editor save and is stable across platforms.
        var metadataPath = GridManager.GetMapPath(GetMapFolder(mapId));
        return File.Exists(metadataPath)
            ? File.GetLastWriteTimeUtc(metadataPath)
            : DateTime.MinValue;
    }
}
