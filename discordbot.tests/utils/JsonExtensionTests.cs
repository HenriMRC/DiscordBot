using discordbot.utils;
using System.Dynamic;
using System.Text.Json;

namespace DiscordBot.Tests;

public class JsonExtensionTests
{
    [Test]
    public void ToExpandoObject_ParsesNestedObjectArrayAndPrimitiveValues()
    {
        const string json = """
            {
              "name": "bot",
              "enabled": true,
              "count": 3,
              "ratio": 1.5,
              "meta": {
                "active": false
              },
              "items": [1, "a", null]
            }
            """;

        using JsonDocument document = JsonDocument.Parse(json);
        ExpandoObject expando = document.ToExpandoObject();
        IDictionary<string, object?> root = expando;

        IDictionary<string, object?> meta = (IDictionary<string, object?>)root["meta"]!;
        List<object?> items = (List<object?>)root["items"]!;

        Assert.Multiple(() =>
        {
            Assert.That(root["name"], Is.EqualTo("bot"));
            Assert.That(root["enabled"], Is.EqualTo(true));
            Assert.That(root["count"], Is.EqualTo(3L));
            Assert.That(root["ratio"], Is.EqualTo(1.5d));
            Assert.That(meta["active"], Is.EqualTo(false));
            Assert.That(items[0], Is.EqualTo(1L));
            Assert.That(items[1], Is.EqualTo("a"));
            Assert.That(items[2], Is.Null);
        });
    }
}
