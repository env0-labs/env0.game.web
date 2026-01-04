using Env0.Runner.Wpf;
using Xunit;

namespace Env0.Runner.Wpf.Tests
{
    public class InputHistoryTests
    {
        [Fact]
        public void Commit_ClearsBufferAndStoresTrimmedHistory()
        {
            var history = new InputHistory();
            history.Append("  status  ");

            var committed = history.Commit();

            Assert.Equal("  status  ", committed);
            Assert.Equal(string.Empty, history.Buffer);
            Assert.Single(history.History);
            Assert.Equal("status", history.History[0]);
            Assert.Equal(1, history.Index);
        }

        [Fact]
        public void Commit_SkipsConsecutiveDuplicates()
        {
            var history = new InputHistory();
            history.Append("process");
            history.Commit();
            history.Append("process");
            history.Commit();

            Assert.Single(history.History);
            Assert.Equal(1, history.Index);
        }

        [Fact]
        public void NavigateUpDown_WalksHistoryAndClearsAtEnd()
        {
            var history = new InputHistory();
            history.Append("status");
            history.Commit();
            history.Append("process");
            history.Commit();

            history.NavigateUp();
            Assert.Equal("process", history.Buffer);
            Assert.Equal(1, history.Index);

            history.NavigateUp();
            Assert.Equal("status", history.Buffer);
            Assert.Equal(0, history.Index);

            history.NavigateDown();
            Assert.Equal("process", history.Buffer);
            Assert.Equal(1, history.Index);

            history.NavigateDown();
            Assert.Equal(string.Empty, history.Buffer);
            Assert.Equal(2, history.Index);
        }

        [Fact]
        public void Backspace_RemovesLastCharacter()
        {
            var history = new InputHistory();
            history.Append("abc");

            history.Backspace();

            Assert.Equal("ab", history.Buffer);
        }
    }
}
