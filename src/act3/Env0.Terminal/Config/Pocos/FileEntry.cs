using Newtonsoft.Json;
using System.Collections.Generic;

namespace Env0.Terminal.Config.Pocos
{
    [JsonConverter(typeof(FileEntryConverter))]
    public class FileEntry
    {
        public string Type { get; set; } = ""; // "file" or "" (directory)
        public string Content { get; set; }
        public Dictionary<string, FileEntry> Children { get; set; }

        // ---- PATCH: RUNTIME ONLY ----
        [JsonIgnore]
        public string Name { get; set; }

        [JsonIgnore]
        public FileEntry Parent { get; set; }

        public bool IsDirectory => string.IsNullOrEmpty(Type) || Type.ToLower() == "dir";
    }
}