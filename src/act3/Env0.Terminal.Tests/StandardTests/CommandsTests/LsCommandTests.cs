using System.Collections.Generic;
using Env0.Terminal.Config.Pocos;
using Xunit;
using Env0.Terminal.Terminal;
using Env0.Terminal.Terminal.Commands;
using Env0.Terminal.Filesystem;

namespace Env0.Terminal.Tests.Commands
{
    public class LsCommandTests
    {
        /// <summary>
        /// LsCommand should return empty output for an empty directory.
        /// </summary>
        [Fact]
        public void LsCommand_EmptyDirectory_ReturnsEmpty()
        {
            var root = new FileEntry
            {
                Name = "/",
                Type = "dir", // or just "" if you prefer
                Children = new Dictionary<string, FileEntry>()
            };
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new LsCommand();

            var result = cmd.Execute(session, new string[0]);
            Assert.NotNull(result);
            Assert.Equal("", result.Output);
        }

        /// <summary>
        /// LsCommand should return an error for an invalid path.
        /// </summary>
        [Fact]
        public void LsCommand_InvalidPath_ReturnsError()
        {
            var root = new FileEntry
            {
                Name = "/",
                Type = "dir", // or just "" if you prefer
                Children = new Dictionary<string, FileEntry>()
            };
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new LsCommand();

            var result = cmd.Execute(session, new[] { "/notadir" });
            Assert.True(result.IsError);
            Assert.Contains("bash: ls: No such file or directory\n\n", result.Output);
        }
    }
}