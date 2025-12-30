using System.Collections.Generic;
using Env0.Terminal.Config.Pocos;
using Xunit;
using Env0.Terminal.Terminal;
using Env0.Terminal.Terminal.Commands;
using Env0.Terminal.Filesystem;

namespace Env0.Terminal.Tests.Commands
{
    public class ReadCommandTests
    {
        /// <summary>
        /// ReadCommand should return error for missing file.
        /// </summary>
        [Fact]
        public void ReadCommand_MissingFile_ReturnsError()
        {
            var root = new FileEntry
            {
                Name = "/",
                Type = "dir", // or just "" if you prefer
                Children = new Dictionary<string, FileEntry>()
            };
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new ReadCommand();

            var result = cmd.Execute(session, new[] { "nofile.txt" });
            Assert.True(result.IsError);
        }
    }
}