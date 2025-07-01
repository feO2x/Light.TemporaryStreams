namespace Light.TemporaryStreams.CallTracking;

public interface ICallTracker
{
    string Name { get; }
    int NumberOfCalls { get; }
}
