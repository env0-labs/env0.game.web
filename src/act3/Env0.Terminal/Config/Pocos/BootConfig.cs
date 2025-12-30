using System.Collections.Generic;

namespace Env0.Terminal.Config.Pocos
{
    /// <summary>
    /// Represents the boot sequence text loaded from BootConfig.json.
    /// </summary>
    public class BootConfig
    {
        /// <summary>
        /// Lines of boot text to display during the boot sequence.
        /// </summary>
        public List<string> BootText { get; set; }
    }
}
