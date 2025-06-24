namespace Light.TemporaryStreams.Tests.CallTracking;

public interface ICallTracker
{
    string Name { get; }
    int NumberOfCalls { get; }
}
