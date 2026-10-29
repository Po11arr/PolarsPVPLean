using System;
using System.IO;
using System.Xml.Serialization;
using VRage.FileSystem;

namespace PolarsPVPLean.Settings;

/// <summary>
/// Config in the game's Storage folder. Missing file: written with defaults. Unreadable file:
/// defaults are used and the file is left for the player to fix or delete. Unknown or missing
/// fields from other versions fall back to their defaults.
/// </summary>
public static class ConfigStorage
{
    private static readonly string ConfigFileName = string.Concat(Plugin.Name, ".cfg");
    private static string ConfigFilePath => Path.Combine(MyFileSystem.UserDataPath, "Storage", ConfigFileName);

    public static void Save(Config config)
    {
        try
        {
            var path = ConfigFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var text = File.CreateText(path))
                new XmlSerializer(typeof(Config)).Serialize(text, config);
        }
        catch (Exception e)
        {
            Log.Error("Could not save " + ConfigFileName, e);
        }
    }

    public static Config Load()
    {
        string path;
        try
        {
            path = ConfigFilePath;
            if (!File.Exists(path))
            {
                var defaults = new Config();
                Save(defaults);
                return defaults;
            }
        }
        catch (Exception e)
        {
            Log.Error("Could not find the config folder, using defaults", e);
            return new Config();
        }

        try
        {
            using (var streamReader = File.OpenText(path))
            {
                var config = (Config)new XmlSerializer(typeof(Config)).Deserialize(streamReader) ?? new Config();
                config.Validate();
                return config;
            }
        }
        catch (Exception e)
        {
            Log.Error("Could not read " + path + ", using defaults", e);
            return new Config();
        }
    }
}
