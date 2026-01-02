using System;
using System.Collections.Generic;
using Env0.Terminal.API_DTOs;
using Env0.Core;

namespace Env0.Terminal
{
    public sealed class TerminalModule : IContextModule
    {
        private readonly TerminalEngineAPI _api = new TerminalEngineAPI();
        private bool _initialized;
        private bool _bootSequenceComplete;
        private string _requestedFilesystem;

        public IEnumerable<OutputLine> Handle(string input, SessionState state)
        {
            var trimmed = (input ?? string.Empty).Trim();
            if (trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                var exitOutput = new List<OutputLine>
                {
                    new OutputLine(OutputType.Standard, "Exiting...")
                };
                var returnContext = state.TerminalReturnContext == ContextRoute.None
                    ? ContextRoute.Maintenance
                    : state.TerminalReturnContext;
                state.NextContext = returnContext;
                if (returnContext == ContextRoute.Records)
                    state.ResumeRecords = true;
                else
                {
                    state.RecordsReturnSceneId = null;
                    state.ResumeRecords = false;
                }
                state.IsComplete = true;
                state.TerminalReturnContext = ContextRoute.None;
                state.TerminalStartFilesystem = null;
                state.MaintenanceMachineId = null;
                state.MaintenanceFilesystem = null;
                return exitOutput;
            }

            if (!_initialized)
            {
                if (_requestedFilesystem == null && !string.IsNullOrWhiteSpace(state.TerminalStartFilesystem))
                    _requestedFilesystem = state.TerminalStartFilesystem;
                _api.Initialize(_requestedFilesystem);
                _initialized = true;
                state.TerminalStartFilesystem = null;
            }

            var output = new List<OutputLine>();

            if (!_bootSequenceComplete)
            {
                var bootState = _api.Execute(string.Empty);
                AppendOutputLines(output, bootState);

                var loginState = _api.Execute(string.Empty);
                AppendOutputLines(output, loginState);
                AppendPrompt(output, loginState);

                _bootSequenceComplete = true;
                return output;
            }

            var renderState = _api.Execute(input ?? string.Empty);
            AppendOutputLines(output, renderState);
            AppendPrompt(output, renderState);
            return output;
        }

        private static void AppendOutputLines(List<OutputLine> output, TerminalRenderState state)
        {
            if (state.OutputLines == null || state.OutputLines.Count == 0)
                return;

            foreach (var line in state.OutputLines)
            {
                output.Add(new OutputLine(MapOutputType(line.Type), line.Text ?? string.Empty));
            }
        }

        private static void AppendPrompt(List<OutputLine> output, TerminalRenderState state)
        {
            if (state.Phase == TerminalPhase.Login)
            {
                if (state.IsLoginPrompt || state.IsPasswordPrompt)
                    output.Add(new OutputLine(OutputType.System, state.Prompt ?? string.Empty, newLine: false));
                return;
            }

            if (state.Phase == TerminalPhase.Terminal && !string.IsNullOrEmpty(state.Prompt))
                output.Add(new OutputLine(OutputType.System, state.Prompt, newLine: false));
        }

        private static OutputType MapOutputType(Env0.Terminal.Terminal.OutputType type)
        {
            switch (type)
            {
                case Env0.Terminal.Terminal.OutputType.Error:
                    return OutputType.Error;
                case Env0.Terminal.Terminal.OutputType.System:
                    return OutputType.System;
                default:
                    return OutputType.Standard;
            }
        }
    }
}


