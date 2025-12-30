using System.Collections.Generic;

namespace Env0.Terminal.Config.Pocos
{
    public class Filesystem
    {
        public Dictionary<string, FileEntry> Root { get; set; } = new Dictionary<string, FileEntry>();
    }
}
