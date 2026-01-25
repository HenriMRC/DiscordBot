using DiscordBot.Models;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DiscordBot;

internal class JsonHandler
{
    internal const string CONFIG_FILE = "./Config.json";

    private readonly JsonSerializerOptions _options = new()
    {
        IncludeFields = true,
        WriteIndented = true
    };

    internal Config? ReadConfigFile()
    {
        FileInfo configFileInfo = new(CONFIG_FILE);
        if (configFileInfo.Exists)
        {
            using FileStream stream = configFileInfo.OpenRead();
            byte[] buffer = new byte[stream.Length];
            int position = 0;
            while (position < buffer.Length)
                position += stream.Read(buffer, position, buffer.Length);
            string json = Encoding.UTF8.GetString(buffer);
            Config? config = JsonSerializer.Deserialize<Config>(json, _options);
            return config;
        }
        else
            return null;
    }

    internal void WriteConfigToFile(Config config)
    {
        string json = JsonSerializer.Serialize(config, _options);
        byte[] buffer = Encoding.Default.GetBytes(json);
        FileInfo configFileInfo = new("./Config.json");
        using FileStream stream = configFileInfo.OpenWrite();
        stream.Write(buffer);
        stream.SetLength(buffer.Length);
    }
}
