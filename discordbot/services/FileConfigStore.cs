using discordbot.models;
using System;
using System.IO;
using System.Text.Json;
using Range = discordbot.models.Range;

namespace discordbot.services;

internal sealed class FileConfigStore : IConfigStore
{
    private const string DefaultChannelName = "bot-cambio";
    private const string DefaultConfigFile = "./config.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _configFilePath;

    public FileConfigStore() : this(DefaultConfigFile) { }

    internal FileConfigStore(string configFilePath)
    {
        _configFilePath = Path.GetFullPath(configFilePath);
    }

    public Config Current { get; private set; } = new();
    public string ChannelName => Current.ChannelName;

    public void Load()
    {
        Current = ReadConfigFile() ?? new Config();
        if (string.IsNullOrWhiteSpace(Current.ChannelName))
        {
            Current.ChannelName = DefaultChannelName;
        }
    }

    public void Save()
    {
        WriteConfigToFile(Current);
    }

    public Range GetOrCreateRange(ulong guildId, decimal defaultMin, decimal defaultMax)
    {
        if (!Current.Channels.TryGetValue(guildId, out Range? range))
        {
            range = new Range(defaultMin, defaultMax);
            Current.Channels[guildId] = range;
        }

        return range;
    }

    private Config? ReadConfigFile()
    {
        if (!File.Exists(_configFilePath))
        {
            return null;
        }

        using FileStream stream = File.OpenRead(_configFilePath);
        return JsonSerializer.Deserialize<Config>(stream, SerializerOptions);
    }

    private void WriteConfigToFile(Config config)
    {
        ArgumentNullException.ThrowIfNull(config);

        string? directory = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string tempFilePath = $"{_configFilePath}.tmp";
        using (FileStream stream = File.Create(tempFilePath))
        {
            JsonSerializer.Serialize(stream, config, SerializerOptions);
        }

        File.Move(tempFilePath, _configFilePath, true);
    }
}
