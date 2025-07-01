namespace Light.TemporaryStreams.CallTracking;

public abstract class BaseCallTracker
{
    protected BaseCallTracker(string name) => Name = name;

    public string Name { get; }
}
