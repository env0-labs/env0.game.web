using System.Collections.Generic;

namespace Env0.Core;

public interface IContextModule
{
    IEnumerable<OutputLine> Handle(string input, SessionState state);
}

