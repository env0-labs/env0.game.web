using Xunit;
using Env0.Terminal.Terminal;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Env0.Terminal.Tests
{
    [Trait("TestType", "Psychotic")]
    public class PsychoticTerminalStateManagerTests
    {
        [Fact]
        public void AllEnumValues_AreValidStates()
        {
            var manager = new TerminalStateManager();
            foreach (TerminalState state in Enum.GetValues(typeof(TerminalState)))
            {
                manager.TransitionTo(state);
                Assert.Equal(state, manager.CurrentState);
            }
        }

        [Fact]
        public void InvalidEnumValue_ShouldFailOrSilentlySet()
        {
            var manager = new TerminalStateManager();
            var bogus = (TerminalState)666;
            manager.TransitionTo(bogus);
            // Should be equal (since no validation, but this is where you'd want a validation check)
            Assert.Equal(bogus, manager.CurrentState);
        }

        [Fact]
        public void RapidFireStateChanges_ShouldNotCorruptState()
        {
            var manager = new TerminalStateManager();
            var sequence = new[] { TerminalState.Boot, TerminalState.Login, TerminalState.Shell, TerminalState.Login, TerminalState.Boot };
            for (int i = 0; i < 1000; i++)
            {
                foreach (var state in sequence)
                {
                    manager.TransitionTo(state);
                }
            }
            Assert.True(Enum.IsDefined(typeof(TerminalState), manager.CurrentState));
        }

        [Fact]
        public void MultiThreadedStateTransitions_ShouldNotDeadlockOrCorrupt()
        {
            var manager = new TerminalStateManager();
            var transitions = new[] { TerminalState.Boot, TerminalState.Login, TerminalState.Shell };
            Exception caught = null;

            Parallel.For(0, 100, i =>
            {
                try
                {
                    manager.TransitionTo(transitions[i % transitions.Length]);
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref caught, ex, null);
                }
            });

            Assert.Null(caught);
            Assert.True(Enum.IsDefined(typeof(TerminalState), manager.CurrentState));
        }

        [Fact]
        public void SettingSameStateManyTimes_DoesNotCorrupt()
        {
            var manager = new TerminalStateManager();
            for (int i = 0; i < 1000; i++)
            {
                manager.TransitionTo(TerminalState.Boot);
            }
            Assert.Equal(TerminalState.Boot, manager.CurrentState);
        }

        [Fact]
        public void Reflection_NukeCurrentStateField()
        {
            var manager = new TerminalStateManager();
            var field = typeof(TerminalStateManager).GetField("CurrentState", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null && field.FieldType.IsValueType)
            {
                field.SetValue(manager, null);
                // Try to use it after nuking - expect exception or default behavior
                Assert.ThrowsAny<Exception>(() => manager.TransitionTo(TerminalState.Login));
            }
        }
    }
}
