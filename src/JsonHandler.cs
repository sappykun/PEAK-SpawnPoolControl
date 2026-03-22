using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using BepInEx;

public class SpawnPoolConfig
{
    [JsonProperty("clear_default_weights")]
    public bool ClearDefaultWeights { get; set; }

    [JsonProperty("force_allow_duplicates")]
    public bool ForceAllowDuplicates { get; set; }

    [JsonProperty("items")]
    public Dictionary<string, Dictionary<string, int>>? SpawnPoolItems { get; set; }

    [JsonProperty("replacements")]
    public Dictionary<string, string>? SpawnPoolReplacements { get; set; }
}

class JsonHandler
{
    public static void Save(SpawnPoolConfig data, string configName)
    {
        try
        {
            SpawnPoolControlPlugin.Log?.LogInfo("Creating folder");
            string configFolder = Path.Combine(Paths.ConfigPath, "SpawnPoolControl");
            Directory.CreateDirectory(configFolder);
            SpawnPoolControlPlugin.Log?.LogInfo("Created folder");

            string outputFullPath = Path.Combine(configFolder, configName + ".json");
            string outputJson = JsonConvert.SerializeObject(
                data,
                Formatting.Indented
            );

            SpawnPoolControlPlugin.Log?.LogInfo("Writing file...");
            File.WriteAllText(outputFullPath, outputJson);

            SpawnPoolControlPlugin.Log?.LogInfo($"Saved JSON to: {outputFullPath}");
        }
        catch (Exception ex)
        {
            SpawnPoolControlPlugin.Log?.LogWarning("Error: " + ex.Message);
        }
    }

    public static SpawnPoolConfig? Load(string configName)
    {
        try
        {
            string configFolder = Path.Combine(Paths.ConfigPath, "SpawnPoolControl");
            string path = Path.Combine(configFolder, configName + ".json");

            if (!File.Exists(path))
            {
                SpawnPoolControlPlugin.Log?.LogWarning("Config not found: " + path);
                return null;
            }

            string json = File.ReadAllText(path);
            SpawnPoolConfig? ret = JsonConvert.DeserializeObject<SpawnPoolConfig>(json);
            SpawnPoolControlPlugin.Log?.LogInfo("Updated pools from config: " + configName + ".json");
            return ret;
        }
        catch (Exception ex)
        {
            SpawnPoolControlPlugin.Log?.LogError("Error loading JSON: " + ex.Message);
            return null;
        }
    }
}
