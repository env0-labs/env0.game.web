using System.Collections.Generic;
using Env0.Terminal.Config.Pocos;
using Env0.Terminal.Filesystem;
using Env0.Terminal.Terminal;
using Env0.Terminal.Terminal.Commands;

namespace Env0.Terminal.Tests.StandardTests.CommandsTests
{
    public class CatCommandTests
    {
        /// <summary>
        /// CatCommand should return an error for missing or directory target.
        /// </summary>
        [Fact]
        public void CatCommand_MissingOrDir_ReturnsError()
        {
            var root = new FileEntry() { Name = "/", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new CatCommand();

            var result = cmd.Execute(session, new string[0]);
            Assert.True(result.IsError);

            result = cmd.Execute(session, new[] { "/" });
            Assert.True(result.IsError);
        }

        /// <summary>
        /// CatCommand returns file contents when present.
        /// </summary>
        [Fact]
        public void CatCommand_File_ReturnsContents()
        {
            var file = new FileEntry
            {
                Name = "foo.txt",
                Type = "file",
                Content = "hello"
            };
            var root = new FileEntry
            {
                Name = "/",
                Type = "dir", // or just "" if you prefer
                Children = new Dictionary<string, FileEntry>()
            };
            root.Children.Add("foo.txt", file);
            file.Parent = root;

            var session = new SessionState { FilesystemManager = new FilesystemManager(root) };
            var cmd = new CatCommand();

            var result = cmd.Execute(session, new[] { "foo.txt" });
            Assert.False(result.IsError);
            Assert.Equal("hello", result.Output);
        }
    }
}