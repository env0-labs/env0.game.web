using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Env0.Terminal.Config.Pocos;

namespace Env0.Terminal.Config.Pocos
{
    public class FileEntryConverter : JsonConverter<FileEntry>
    {
        public override FileEntry ReadJson(JsonReader reader, Type objectType, FileEntry existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            var entry = new FileEntry();

            var typeToken = obj["type"];
            var contentToken = obj["content"];

            if (typeToken != null && typeToken.Type == JTokenType.String && (string)typeToken == "file")
            {
                // --- FILE NODE ---
                entry.Type = "file";

                if (contentToken == null)
                    throw new JsonSerializationException($"File node missing 'content' property.");

                entry.Content = (string)contentToken ?? "";

                // Disallow any extra properties on file nodes
                foreach (var prop in obj.Properties())
                {
                    if (prop.Name != "type" && prop.Name != "content")
                        throw new JsonSerializationException($"File node has unexpected property '{prop.Name}' (only 'type' and 'content' allowed).");
                }

                entry.Children = null;
            }
            else
            {
                // --- DIRECTORY NODE ---
                if (typeToken != null)
                    throw new JsonSerializationException($"Directory node has forbidden 'type' property.");
                if (contentToken != null)
                    throw new JsonSerializationException($"Directory node has forbidden 'content' property.");

                var children = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in obj.Properties())
                {
                    children[prop.Name] = prop.Value.ToObject<FileEntry>(serializer)!;
                }

                entry.Children = children;
                entry.Type = "";
                entry.Content = null;
            }

            return entry;
        }

        public override void WriteJson(JsonWriter writer, FileEntry value, JsonSerializer serializer)
        {
            // Not needed unless you want to serialize back out.
            throw new NotImplementedException();
        }
    }
}
