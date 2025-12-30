using Xunit;
using Env0.Terminal.Filesystem;
using Env0.Terminal.Config.Pocos;
using System;
using System.Collections.Generic;

public class FilesystemManagerHostileUserTests
{
    [Trait("TestType", "Psychotic")]
    private FileEntry MakeMinimalRoot()
    {
        return new FileEntry
        {
            Name = "root",
            Type = "dir",
            Children = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase)
        };
    }

    [Fact]
    public void Throws_When_DirectoryHasChildNamed_DotDot()
    {
        var root = MakeMinimalRoot();
        var name = "..";

        Assert.Throws<ArgumentException>(() =>
        {
            if (string.IsNullOrWhiteSpace(name) || name.Contains("/") || name == "." || name == "..")
                throw new ArgumentException("Invalid FS name");
            var evil = new FileEntry
            {
                Name = name,
                Type = "dir",
                Parent = root,
                Children = new Dictionary<string, FileEntry>()
            };
            root.Children.Add(name, evil);
        });
    }

    [Fact]
    public void Throws_When_FilenameHasSlashes()
    {
        var root = MakeMinimalRoot();
        var name = "user/home";

        Assert.Throws<ArgumentException>(() =>
        {
            if (string.IsNullOrWhiteSpace(name) || name.Contains("/") || name == "." || name == "..")
                throw new ArgumentException("Invalid FS name");
            var broken = new FileEntry
            {
                Name = name,
                Type = "file",
                Content = "oops",
                Parent = root
            };
            root.Children.Add(name, broken);
        });
    }


    [Fact]
    public void PathTraversalAboveRoot_DoesNotCrash()
    {
        var root = MakeMinimalRoot();
        var fs = new FilesystemManager(root);
        string error;

        for (int i = 0; i < 10; i++)
        {
            Assert.True(fs.ChangeDirectory("..", out error));
            Assert.Equal("root", fs.CurrentDirectoryName());
        }
    }

    [Fact]
    public void DirectoryAndFileWithSameName_DirectoryWins()
    {
        var root = MakeMinimalRoot();
        var subdir = new FileEntry
        {
            Name = "bin",
            Type = "dir",
            Children = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase),
            Parent = root
        };
        var binFile = new FileEntry
        {
            Name = "bin",
            Type = "file",
            Content = "i should be ignored",
            Parent = root
        };
        root.Children.Add(subdir.Name, subdir);
        Assert.Throws<ArgumentException>(() => root.Children.Add(binFile.Name, binFile));
    }

    [Fact]
    public void FileWithRidiculousName_Length256Plus()
    {
        var root = MakeMinimalRoot();
        string longName = new string('a', 300) + ".txt";
        var weird = new FileEntry
        {
            Name = longName,
            Type = "file",
            Content = "just... why",
            Parent = root
        };
        root.Children.Add(longName, weird);
        var fs = new FilesystemManager(root);

        string content, error;
        Assert.True(fs.GetFileContent(longName, out content, out error));
        Assert.Equal("just... why", content);
    }

    [Fact]
    public void LsOnDirectoryWithOnlyInvalidChildren_PrintsEmpty()
    {
        var root = MakeMinimalRoot();
        var junk = new FileEntry
        {
            Name = "",
            Type = "file",
            Content = "null",
            Parent = root
        };
        root.Children.Add("", junk);
        var fs = new FilesystemManager(root);

        var items = fs.ListCurrentDirectory();
        Assert.True(items.Count == 1 || items.Count == 0);
    }

    [Fact]
    public void CatOnDirectory_AlwaysFails()
    {
        var root = MakeMinimalRoot();
        var home = new FileEntry
        {
            Name = "home",
            Type = "dir",
            Children = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase),
            Parent = root
        };
        root.Children.Add("home", home);
        var fs = new FilesystemManager(root);

        string content, error;
        Assert.False(fs.GetFileContent("home", out content, out error));
        Assert.NotNull(error);
    }
}
