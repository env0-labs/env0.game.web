namespace env0.records.Runtime;

public sealed class GameState
{
    public string CurrentSceneId { get; private set; }
    public Dictionary<string, bool> Flags { get; } = new();

    public GameState(string startSceneId)
    {
        CurrentSceneId = startSceneId;
    }

    public void SetScene(string sceneId)
    {
        CurrentSceneId = sceneId;
    }

    public bool HasFlag(string flag)
    {
        return Flags.TryGetValue(flag, out var value) && value;
    }

    public void SetFlag(string flag)
    {
        Flags[flag] = true;
    }

    public void ClearFlag(string flag)
    {
        Flags[flag] = false;
    }
}

