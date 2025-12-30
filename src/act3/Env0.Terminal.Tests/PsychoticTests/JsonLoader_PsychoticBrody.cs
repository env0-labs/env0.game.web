using System.IO;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Env0.Terminal.Config;
using Env0.Terminal.Config.Pocos;
using System.Text;

namespace Env0.Terminal.Tests
{
    [Trait("TestType", "Psychotic")]
    public class PsychoticJsonLoaderTests
    {
        private const string TestDataDir = "TestData_Psychotic";

        public PsychoticJsonLoaderTests()
        {
            if (!Directory.Exists(TestDataDir))
                Directory.CreateDirectory(TestDataDir);
        }

        [Fact]
        public void LoadBootConfig_EmptyFile_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "BootConfig_emptyfile.json");
            File.WriteAllText(path, "");
            var config = JsonLoader.LoadBootConfig(path, out var errors);

            Assert.Null(config);
            Assert.Contains(errors, e => e.ToLower().Contains("empty"));
        }

        [Fact]
        public void LoadBootConfig_NullFile_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "BootConfig_nullfile.json");
            File.WriteAllText(path, "");
            var config = JsonLoader.LoadBootConfig(path, out var errors);

            Assert.Null(config);
            Assert.Contains(errors, e => e.ToLower().Contains("empty"));
        }

        [Fact]
        public void LoadBootConfig_DeeplyNestedJson_ParsesOrFailsGracefully()
        {
            var sb = new StringBuilder();
            sb.Append("{ \"BootText\": [");
            for (int i = 0; i < 10000; i++)
            {
                sb.Append("\"Line" + i + "\",");
            }
            sb.Append("\"LastLine\"] }");
            var path = Path.Combine(TestDataDir, "BootConfig_deeplynested.json");
            File.WriteAllText(path, sb.ToString());

            var config = JsonLoader.LoadBootConfig(path, out var errors);
            Assert.NotNull(config);
            Assert.True(config.BootText.Count >= 10000);
            Assert.Empty(errors);
        }

// These tests are intentionally commented out. 
// Why: The loader never returns null for a failed UserConfig, it always supplies a default config.
// This matches our security and usability spec, and avoids locking the user out for bad configs.
// Test is left for reference/documentation—do not re-enable unless loader logic changes.


        [Fact]
        public void LoadUserConfig_BinaryFile_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "UserConfig_binary.json");
            File.WriteAllBytes(path, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 }); // JPEG magic number
            var config = JsonLoader.LoadUserConfig(path, out var errors);

            Assert.NotNull(config);
            Assert.Equal("player", config.Username);
            Assert.Equal("password", config.Password);
            Assert.Contains(errors, e => e.ToLower().Contains("failed to load") || e.ToLower().Contains("binary") || e.ToLower().Contains("defaulting"));
        }

        [Fact]
        public void LoadUserConfig_InvalidEncoding_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "UserConfig_invalidencoding.json");
            File.WriteAllBytes(path, Encoding.UTF32.GetBytes(@"{ ""Username"": ""player"", ""Password"": ""password"" }"));
            var config = JsonLoader.LoadUserConfig(path, out var errors);

            Assert.NotNull(config);
            Assert.Equal("player", config.Username);
            Assert.Equal("password", config.Password);
            Assert.Contains(errors, e => e.ToLower().Contains("encoding") || e.ToLower().Contains("defaulting"));
        }

        [Fact]
        public void LoadUserConfig_NestedArrayInsteadOfObject_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "UserConfig_array.json");
            File.WriteAllText(path, @"[ { ""Username"": ""player"" }, { ""Password"": ""password"" } ]");
            var config = JsonLoader.LoadUserConfig(path, out var errors);

            Assert.NotNull(config);
            Assert.Equal("player", config.Username);
            Assert.Equal("password", config.Password);
            Assert.Contains(errors, e => e.ToLower().Contains("object") || e.ToLower().Contains("defaulting"));
        }

        [Fact]
        public void LoadDevices_HugeList_PerformanceTest()
        {
            var path = Path.Combine(TestDataDir, "Devices_huge.json");
            var sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < 10000; i++)
            {
                sb.Append($@"{{""ip"":""10.10.10.{i}"",""hostname"":""host{i}"",""mac"":""00:11:22:33:44:{i:X2}"",""username"":""admin"",""password"":""pass"",""filesystem"":""fs{i}.json"",""motd"":""hi"",""description"":"""",""ports"":[""22""],""interfaces"":[{{""name"":""eth0"",""ip"":""10.10.10.{i}"",""subnet"":""255.255.255.0"",""mac"":""00:11:22:33:44:{i:X2}""}}]}}");
                if (i < 9999) sb.Append(",");
            }
            sb.Append("]");
            File.WriteAllText(path, sb.ToString());

            var devices = JsonLoader.LoadDevices(path, out var errors);

            Assert.NotNull(devices);
            Assert.Equal(10000, devices.Count);
            Assert.True(errors.Count == 0 || errors.Count < 100); // Accept up to 1% errors
        }

        [Fact]
        public void LoadDevices_DuplicateKeys_ReportsErrorOrHandles()
        {
            var path = Path.Combine(TestDataDir, "Devices_duplicatekeys.json");
            File.WriteAllText(path, @"
            [
                { ""ip"": ""10.10.10.1"", ""hostname"": ""host"", ""mac"": ""00:11:22:33:44:55"", ""username"": ""admin"", ""password"": ""pass"", ""filesystem"": ""fs.json"", ""motd"": ""hi"", ""description"": """", ""ports"": [""22""], ""interfaces"": [ { ""name"": ""eth0"", ""ip"": ""10.10.10.1"", ""subnet"": ""255.255.255.0"", ""mac"": ""00:11:22:33:44:55"" }, { ""name"": ""eth0"", ""ip"": ""10.10.10.2"", ""subnet"": ""255.255.255.0"", ""mac"": ""00:11:22:33:44:56"" } ] }
            ]");
            var devices = JsonLoader.LoadDevices(path, out var errors);

            Assert.NotNull(devices);
            Assert.True(devices[0].Interfaces.Count == 2); // Either keep both or only one, but no crash
            Assert.True(errors.Count == 0 || errors.Count < 10);
        }

        [Fact]
        public void LoadFilesystem_UnicodeKeys_HandlesOrFailsGracefully()
        {
            var path = Path.Combine(TestDataDir, "Filesystem_unicode.json");
            File.WriteAllText(path, @"
            {
              ""root"": {
                ""hømè"": {
                  ""usér"": {
                    ""ファイル.txt"": {
                      ""type"": ""file"",
                      ""content"": ""ユニコード!""
                    }
                  }
                }
              }
            }");
            var fs = JsonLoader.LoadFilesystem(path, out var errors);

            Assert.NotNull(fs);
            Assert.NotNull(fs.Root);
            Assert.True(errors.Count == 0 || errors.Count < 10);
        }

        [Fact]
        public void LoadFilesystem_CircularReference_ReportsErrorOrPreventsStackOverflow()
        {
            // JSON can't natively express circular refs, but we can try to reference a parent via a key
            var path = Path.Combine(TestDataDir, "Filesystem_circular.json");
            File.WriteAllText(path, @"
            {
              ""root"": {
                ""loop"": {
                  ""parent"": ""root""
                }
              }
            }");
            var fs = JsonLoader.LoadFilesystem(path, out var errors);

            Assert.NotNull(fs);
            Assert.True(errors.Count == 0 || errors.Any(e => e.ToLower().Contains("circular")));
        }

        [Fact]
        public void LoadFilesystem_WeirdTypesInsteadOfFiles_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "Filesystem_weirdtypes.json");
            File.WriteAllText(path, @"
            {
              ""root"": {
                ""home"": [""should"", ""not"", ""be"", ""an"", ""array""]
              }
            }");
            var fs = JsonLoader.LoadFilesystem(path, out var errors);

            Assert.NotNull(fs);
            Assert.Contains(errors,(e => e.ToLower().Contains("type")));
        }

        [Fact]
        public void LoadFilesystem_BOMAndIllegalUnicode_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "Filesystem_bom.json");
            var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(@"{ ""root"": {} }")).ToArray();
            File.WriteAllBytes(path, content);

            var fs = JsonLoader.LoadFilesystem(path, out var errors);

            Assert.NotNull(fs);
            Assert.Contains(errors,(e => e.ToLower().Contains("unicode") || e.ToLower().Contains("bom") || e.ToLower().Contains("empty")));
        }

        [Fact]
        public void LoadFilesystem_NonJsonText_ReportsError()
        {
            var path = Path.Combine(TestDataDir, "Filesystem_notjson.txt");
            File.WriteAllText(path, @"This is not JSON, it is plain text.");
            var fs = JsonLoader.LoadFilesystem(path, out var errors);

            Assert.NotNull(fs);
            Assert.Contains(errors,(e => e.ToLower().Contains("json")));
        }

        [Fact]
        public void LoadFilesystem_NestedToMaxDepth_HandlesOrFailsGracefully()
        {
            var path = Path.Combine(TestDataDir, "Filesystem_maxdepth.json");
            var sb = new StringBuilder();
            sb.Append("{ \"root\": ");
            int depth = 200;
            for (int i = 0; i < depth; i++)
                sb.Append("{ \"deep\": ");
            sb.Append(@"{ ""type"": ""file"", ""content"": ""max depth"" }");
            for (int i = 0; i < depth; i++)
                sb.Append("}");
            sb.Append("}");
            File.WriteAllText(path, sb.ToString());

            var fs = JsonLoader.LoadFilesystem(path, out var errors);

            Assert.NotNull(fs);
            Assert.True(errors.Count == 0 || errors.Any(e => e.ToLower().Contains("depth")));
        }
    }
}
