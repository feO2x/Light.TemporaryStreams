using System.Collections.Generic;
using FluentAssertions;

namespace Light.TemporaryStreams.Tests.CallTracking;

public sealed class CallTracker : BaseCallTracker, ICallTracker
{
    public CallTracker(string name) : base(name) { }
    public int NumberOfCalls { get; private set; }

    public void TrackCall() => NumberOfCalls++;

    public void MustHaveBeenCalled(int times = 1) => NumberOfCalls.Should().Be(times);
}

public sealed class CallTracker<T> : BaseCallTracker, ICallTracker
{
    public CallTracker(string name) : base(name) { }
    public List<T> CapturedParameters { get; } = new ();

    public int NumberOfCalls => CapturedParameters.Count;

    public void TrackCall(T parameter) => CapturedParameters.Add(parameter);

    public void MustHaveBeenCalledWith(T parameter) =>
        CapturedParameters.Should().ContainSingle().Which.Should().Be(parameter);
}

public sealed class CallTracker<T1, T2> : BaseCallTracker, ICallTracker
{
    public CallTracker(string name) : base(name) { }
    public List<(T1, T2)> CapturedParameters { get; } = new ();

    public int NumberOfCalls => CapturedParameters.Count;

    public void TrackCall(T1 parameter1, T2 parameter2) => CapturedParameters.Add((parameter1, parameter2));

    public void MustHaveBeenCalledWith(T1 parameter1, T2 parameter2) =>
        CapturedParameters.Should().ContainSingle().Which.Should().Be((parameter1, parameter2));
}

public sealed class CallTracker<T1, T2, T3> : BaseCallTracker, ICallTracker
{
    public CallTracker(string name) : base(name) { }
    public List<(T1, T2, T3)> CapturedParameters { get; } = new ();

    public int NumberOfCalls => CapturedParameters.Count;

    public void TrackCall(T1 parameter1, T2 parameter2, T3 parameter3) =>
        CapturedParameters.Add((parameter1, parameter2, parameter3));

    public void MustHaveBeenCalledWith(T1 parameter1, T2 parameter2, T3 parameter3) =>
        CapturedParameters.Should().ContainSingle().Which.Should().Be((parameter1, parameter2, parameter3));
}

public sealed class CallTracker<T1, T2, T3, T4> : BaseCallTracker, ICallTracker
{
    public CallTracker(string name) : base(name) { }
    public List<(T1, T2, T3, T4)> CapturedParameters { get; } = new ();

    public int NumberOfCalls => CapturedParameters.Count;

    public void TrackCall(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4) =>
        CapturedParameters.Add((parameter1, parameter2, parameter3, parameter4));

    public void MustHaveBeenCalledWith(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4) =>
        CapturedParameters.Should().ContainSingle().Which.Should().Be((parameter1, parameter2, parameter3, parameter4));
}

public sealed class CallTracker<T1, T2, T3, T4, T5> : BaseCallTracker, ICallTracker
{
    public CallTracker(string name) : base(name) { }
    public List<(T1, T2, T3, T4, T5)> CapturedParameters { get; } = new ();

    public int NumberOfCalls => CapturedParameters.Count;

    public void TrackCall(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5) =>
        CapturedParameters.Add((parameter1, parameter2, parameter3, parameter4, parameter5));

    public void MustHaveBeenCalledWith(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5) =>
        CapturedParameters
           .Should().ContainSingle().Which.Should()
           .Be((parameter1, parameter2, parameter3, parameter4, parameter5));
}

public sealed class CallTracker<T1, T2, T3, T4, T5, T6> : BaseCallTracker, ICallTracker
{
    public CallTracker(string name) : base(name) { }
    public List<(T1, T2, T3, T4, T5, T6)> CapturedParameters { get; } = new ();

    public int NumberOfCalls => CapturedParameters.Count;

    public void TrackCall(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6) =>
        CapturedParameters.Add((parameter1, parameter2, parameter3, parameter4, parameter5, parameter6));

    public void MustHaveBeenCalledWith(
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6
    ) =>
        CapturedParameters
           .Should().ContainSingle().Which.Should()
           .Be((parameter1, parameter2, parameter3, parameter4, parameter5, parameter6));
}

public sealed class CallTracker<T1, T2, T3, T4, T5, T6, T7> : BaseCallTracker, ICallTracker
{
    public CallTracker(string name) : base(name) { }
    public List<(T1, T2, T3, T4, T5, T6, T7)> CapturedParameters { get; } = new ();

    public int NumberOfCalls => CapturedParameters.Count;

    public void TrackCall(
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6,
        T7 parameter7
    ) =>
        CapturedParameters.Add((parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7));

    public void MustHaveBeenCalledWith(
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6,
        T7 parameter7
    ) =>
        CapturedParameters
           .Should().ContainSingle().Which.Should()
           .Be((parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7));
}

public sealed class CallTracker<T1, T2, T3, T4, T5, T6, T7, T8> : BaseCallTracker, ICallTracker
{
    public CallTracker(string name) : base(name) { }
    public List<(T1, T2, T3, T4, T5, T6, T7, T8)> CapturedParameters { get; } = new ();

    public int NumberOfCalls => CapturedParameters.Count;

    public void TrackCall(
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6,
        T7 parameter7,
        T8 parameter8
    ) =>
        CapturedParameters.Add(
            (parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8)
        );

    public void MustHaveBeenCalledWith(
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6,
        T7 parameter7,
        T8 parameter8
    ) =>
        CapturedParameters
           .Should().ContainSingle().Which.Should()
           .Be((parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8));
}
