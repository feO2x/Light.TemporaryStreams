using System;
using System.Threading;

namespace Light.TemporaryStreams.Tests;

public sealed class AsyncResultNullObject : IAsyncResult
{
    public object? AsyncState => null;
    public WaitHandle AsyncWaitHandle => null!;
    public bool CompletedSynchronously => true;
    public bool IsCompleted => true;
}
