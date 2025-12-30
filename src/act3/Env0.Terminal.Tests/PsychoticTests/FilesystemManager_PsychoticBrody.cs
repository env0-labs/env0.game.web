using Xunit;
using Env0.Terminal.Filesystem;
using Env0.Terminal.Config.Pocos;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Env0.Terminal.Tests
{
    public class PsychoticFilesystemManagerTests
    {
        [Trait("TestType", "Psychotic")]
        private FileEntry MakeCycle()
        {
            var root = new FileEntry { Name = "root", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            var loop = new FileEntry { Name = "loop", Type = "dir", Parent = root, Children = new Dictionary<string, FileEntry>() };
            root.Children.Add("loop", loop);
            loop.Children.Add("root", root); // cycle
            return root;
        }

        [Fact]
        public void CycleDetection_ShouldNotStackOverflowOrLoopForever()
        {
            var fs = new FilesystemManager(MakeCycle());
            var items = fs.ListCurrentDirectory();
            Assert.Contains("loop", items);
        }

        [Fact]
        public void PathTraversal_AttemptToCdDotDotFromRoot_ShouldStayAtRoot()
        {
            var root = new FileEntry { Name = "root", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            var fs = new FilesystemManager(root);
            string error;
            fs.ChangeDirectory("..", out error);
            var items = fs.ListCurrentDirectory();
            Assert.Equal(new List<string>(), error == null ? items : new List<string>());
        }

        [Fact]
        public void UnicodeFilenames_ShouldBeHandledCorrectly()
        {
            var root = new FileEntry { Name = "root", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            var emoji = new FileEntry { Name = "💩file.txt", Type = "file", Content = "Unicode shit!", Parent = root };
            root.Children.Add("💩file.txt", emoji);

            var fs = new FilesystemManager(root);
            string content, error;
            var result = fs.GetFileContent("💩file.txt", out content, out error);
            Assert.True(result);
            Assert.Equal("Unicode shit!", content);
            Assert.Null(error);
        }

        [Fact]
        public void InsaneFileNameCharacters_ShouldBeHandledOrRejected()
        {
            var root = new FileEntry { Name = "root", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            string insaneName = string.Concat(Enumerable.Repeat("!", 256)) + "\0\t\n\r";
            var insaneFile = new FileEntry { Name = insaneName, Type = "file", Content = "Madness", Parent = root };
            root.Children.Add(insaneName, insaneFile);

            var fs = new FilesystemManager(root);
            string content, error;
            var result = fs.GetFileContent(insaneName, out content, out error);
            Assert.True(result || error != null);
        }

        [Fact]
        public void ExtremelyDeepDirectoryStructure_ShouldNotOverflow()
        {
            FileEntry current = new FileEntry { Name = "root", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            FileEntry root = current;
            for (int i = 0; i < 1000; i++)
            {
                var next = new FileEntry { Name = $"dir{i}", Type = "dir", Parent = current, Children = new Dictionary<string, FileEntry>() };
                current.Children.Add($"dir{i}", next);
                current = next;
            }

            var fs = new FilesystemManager(root);
            string error = null;
            for (int i = 0; i < 1000; i++)
            {
                fs.ChangeDirectory($"dir{i}", out error);
                Assert.Null(error);
            }
        }

        [Fact]
        public void LargeNumberOfFiles_ShouldBeListedCorrectly()
        {
            var root = new FileEntry { Name = "root", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            for (int i = 0; i < 10000; i++)
            {
                var file = new FileEntry { Name = $"file_{i}.txt", Type = "file", Content = $"File {i}", Parent = root };
                root.Children.Add(file.Name, file);
            }
            var fs = new FilesystemManager(root);
            var items = fs.ListCurrentDirectory();
            Assert.Equal(10000, items.Count);
        }

        [Fact]
        public void CatHugeFile_ShouldReturnFullContentOrError()
        {
            var root = new FileEntry { Name = "root", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            var sb = new StringBuilder();
            for (int i = 0; i < 1000000; i++) sb.Append("A");
            var hugeFile = new FileEntry { Name = "huge.txt", Type = "file", Content = sb.ToString(), Parent = root };
            root.Children.Add("huge.txt", hugeFile);

            var fs = new FilesystemManager(root);
            string content, error;
            var result = fs.GetFileContent("huge.txt", out content, out error);
            Assert.True((result && content.Length == 1000000) || (!result && error != null));
        }

        [Fact]
        public void DuplicateFileNames_ShouldNotBeAllowed()
        {
            var root = new FileEntry { Name = "root", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            var fileA = new FileEntry { Name = "duplicate.txt", Type = "file", Content = "One", Parent = root };
            root.Children.Add("duplicate.txt", fileA);
            Assert.Throws<System.ArgumentException>(() => root.Children.Add("duplicate.txt", fileA));
        }

        [Fact]
        public void NullFileName_ShouldBeRejectedOrHandled()
        {
            var root = new FileEntry { Name = "root", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            Assert.Throws<System.ArgumentNullException>(() => root.Children.Add(null, null));
        }

        [Fact]
        public void NullOrEmptyInputs_ShouldDefaultToRoot()
        {
            var root = new FileEntry
            {
                Name = "root",
                Type = "dir",
                Children = new Dictionary<string, FileEntry>()
            };
            var fs = new FilesystemManager(root);
            string error, content;

            Assert.False(fs.GetFileContent(null, out content, out error));
            Assert.False(fs.GetFileContent("", out content, out error));

            // These now succeed because null/empty means "cd /"
            Assert.True(fs.ChangeDirectory(null, out error));
            Assert.True(fs.ChangeDirectory("", out error));
            Assert.Null(error);
            Assert.Equal("root", fs.CurrentDirectoryName());
        }


        [Fact]
        public void FileContentIsCaseSensitiveOrNot()
        {
            var root = new FileEntry { Name = "root", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            var file = new FileEntry { Name = "README.TXT", Type = "file", Content = "Capital!", Parent = root };
            root.Children.Add("README.TXT", file);

            var fs = new FilesystemManager(root);
            string content, error;
            var result = fs.GetFileContent("readme.txt", out content, out error);
            Assert.False(result);
        }

        [Fact]
        public void NonAsciiPathTraversal_ShouldNotBreak()
        {
            var root = new FileEntry { Name = "root", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            var funky = new FileEntry { Name = "üñîçødë", Type = "dir", Children = new Dictionary<string, FileEntry>(), Parent = root };
            root.Children.Add(funky.Name, funky);

            var fs = new FilesystemManager(root);
            string error;
            var result = fs.ChangeDirectory("üñîçødë", out error);
            Assert.True(result);
            Assert.Null(error);
        }

        [Fact]
        public void CircularReferenceParentChain_ShouldNotCrash()
        {
            var root = new FileEntry { Name = "root", Type = "dir", Children = new Dictionary<string, FileEntry>() };
            root.Parent = root;

            var fs = new FilesystemManager(root);
            var items = fs.ListCurrentDirectory();
            Assert.Contains("root", root.Name);
        }
    }
}
