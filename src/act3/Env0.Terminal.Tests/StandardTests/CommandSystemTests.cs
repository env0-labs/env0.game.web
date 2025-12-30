using System;
using System.Collections.Generic;
using Xunit;
using Env0.Terminal.Terminal;
using Env0.Terminal.Terminal.Commands;
using Env0.Terminal.Filesystem;
using Env0.Terminal.Config.Pocos;


namespace Env0.Terminal.Tests
{
    /// <summary>
    /// Tests for ICommand, CommandHandler, and CommandResult core contract compliance.
    /// </summary>
    public class CommandSystemTests
    {
        /// <summary>
        /// Verifies that any ICommand implementation returns a CommandResult and does not throw.
        /// </summary>
        [Fact]
        public void ICommand_Execute_ReturnsCommandResult()
        {
            // Arrange
            var dummyCommand = new DummyCommand();
            var dummySession = new SessionState();
            string[] args = { "arg1", "arg2" };

            // Act
            var result = dummyCommand.Execute(dummySession, args);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<CommandResult>(result);
            Assert.Equal("Dummy executed.", result.Output);
        }

        /// <summary>
        /// Checks that CommandHandler returns the canonical error for unknown commands.
        /// </summary>
        [Fact]
        public void CommandHandler_UnknownCommand_ReturnsCanonicalError()
        {
            var handler = new CommandHandler(debugMode: false);
            var dummySession = new SessionState();

            var result = handler.Execute("nonexistentcommand", dummySession);

            Assert.True(result.IsError);
            Assert.Contains("command not found", result.Output);
        }

        /// <summary>
        /// Checks that CommandHandler can successfully dispatch a registered command and get correct output.
        /// </summary>
        [Fact]
        public void CommandHandler_DispatchesRegisteredCommand()
        {
            var handler = new CommandHandler(debugMode: false);

            var rootEntry = new FileEntry
            {
                Name = "/",
                Type = "dir",
                Children = new Dictionary<string, FileEntry>() // ← critical
            };

            var dummySession = new SessionState
            {
                FilesystemManager = new FilesystemManager(rootEntry)
            };

            var result = handler.Execute("ls", dummySession);

            Assert.NotNull(result);
            Assert.IsType<CommandResult>(result);
        }



        /// <summary>
        /// Ensures that CommandResult properties can be set and retrieved.
        /// </summary>
        [Fact]
        public void CommandResult_Properties_AreSettable()
        {
            var session = new SessionState();
            var result = new CommandResult();
            result.AddLine("output", OutputType.Error);
            result.RequiresPaging = true;
            result.StateChanged = true;
            result.UpdatedSession = session;

            Assert.Equal("output", result.Output);
            Assert.True(result.IsError);
            Assert.True(result.RequiresPaging);
            Assert.True(result.StateChanged);
            Assert.Equal(session, result.UpdatedSession);
        }

        // --- Minimal dummy ICommand implementation for test ---
        private class DummyCommand : ICommand
        {
            public CommandResult Execute(SessionState session, string[] args)
            {
                return new CommandResult("Dummy executed.");
            }
        }
    }
}
