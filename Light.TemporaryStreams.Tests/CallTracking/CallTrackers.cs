using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Light.GuardClauses;
using Xunit.Sdk;

namespace Light.TemporaryStreams.Tests.CallTracking;

public sealed class CallTrackers
{
    private readonly Dictionary<string, ICallTracker> _callTrackers = new ();

    public void TrackCall([CallerMemberName] string name = "") =>
        GetOrCreateCallTracker(name, () => new CallTracker(name)).TrackCall();

    public void TrackCall<T>(T parameter, [CallerMemberName] string name = "")
    {
        var callTracker = GetOrCreateCallTracker(name, () => new CallTracker<T>(name));
        callTracker.TrackCall(parameter);
    }

    public void TrackCall<T1, T2>(T1 parameter1, T2 parameter2, [CallerMemberName] string name = "")
    {
        var callTracker = GetOrCreateCallTracker(name, () => new CallTracker<T1, T2>(name));
        callTracker.TrackCall(parameter1, parameter2);
    }

    public void TrackCall<T1, T2, T3>(T1 parameter1, T2 parameter2, T3 parameter3, [CallerMemberName] string name = "")
    {
        var callTracker = GetOrCreateCallTracker(name, () => new CallTracker<T1, T2, T3>(name));
        callTracker.TrackCall(parameter1, parameter2, parameter3);
    }

    public void TrackCall<T1, T2, T3, T4>(
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        [CallerMemberName] string name = ""
    )
    {
        var callTracker = GetOrCreateCallTracker(name, () => new CallTracker<T1, T2, T3, T4>(name));
        callTracker.TrackCall(parameter1, parameter2, parameter3, parameter4);
    }

    public void TrackCall<T1, T2, T3, T4, T5>(
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        [CallerMemberName] string name = ""
    )
    {
        var callTracker = GetOrCreateCallTracker(name, () => new CallTracker<T1, T2, T3, T4, T5>(name));
        callTracker.TrackCall(parameter1, parameter2, parameter3, parameter4, parameter5);
    }

    public void TrackCall<T1, T2, T3, T4, T5, T6>(
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6,
        [CallerMemberName] string name = ""
    )
    {
        var callTracker = GetOrCreateCallTracker(name, () => new CallTracker<T1, T2, T3, T4, T5, T6>(name));
        callTracker.TrackCall(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6);
    }

    public void TrackCall<T1, T2, T3, T4, T5, T6, T7>(
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6,
        T7 parameter7,
        [CallerMemberName] string name = ""
    )
    {
        var callTracker = GetOrCreateCallTracker(name, () => new CallTracker<T1, T2, T3, T4, T5, T6, T7>(name));
        callTracker.TrackCall(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7);
    }

    public void TrackCall<T1, T2, T3, T4, T5, T6, T7, T8>(
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6,
        T7 parameter7,
        T8 parameter8,
        [CallerMemberName] string name = ""
    )
    {
        var callTracker = GetOrCreateCallTracker(name, () => new CallTracker<T1, T2, T3, T4, T5, T6, T7, T8>(name));
        callTracker.TrackCall(
            parameter1,
            parameter2,
            parameter3,
            parameter4,
            parameter5,
            parameter6,
            parameter7,
            parameter8
        );
    }

    public void MustHaveBeenCalled(string name, int times = 1)
    {
        times.MustBeGreaterThan(0);
        GetRequiredCallTracker<CallTracker>(name).MustHaveBeenCalled(times);
    }

    public void MustHaveBeenCalledWith<T>(string name, T parameter)
    {
        GetRequiredCallTracker<CallTracker<T>>(name).MustHaveBeenCalledWith(parameter);
    }

    public void MustHaveBeenCalledWith<T1, T2>(string name, T1 parameter1, T2 parameter2)
    {
        GetRequiredCallTracker<CallTracker<T1, T2>>(name).MustHaveBeenCalledWith(parameter1, parameter2);
    }

    public void MustHaveBeenCalledWith<T1, T2, T3>(string name, T1 parameter1, T2 parameter2, T3 parameter3)
    {
        GetRequiredCallTracker<CallTracker<T1, T2, T3>>(name)
           .MustHaveBeenCalledWith(parameter1, parameter2, parameter3);
    }

    public void MustHaveBeenCalledWith<T1, T2, T3, T4>(
        string name,
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4
    )
    {
        GetRequiredCallTracker<CallTracker<T1, T2, T3, T4>>(name)
           .MustHaveBeenCalledWith(parameter1, parameter2, parameter3, parameter4);
    }

    public void MustHaveBeenCalledWith<T1, T2, T3, T4, T5>(
        string name,
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5
    )
    {
        GetRequiredCallTracker<CallTracker<T1, T2, T3, T4, T5>>(name)
           .MustHaveBeenCalledWith(parameter1, parameter2, parameter3, parameter4, parameter5);
    }

    public void MustHaveBeenCalledWith<T1, T2, T3, T4, T5, T6>(
        string name,
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6
    )
    {
        GetRequiredCallTracker<CallTracker<T1, T2, T3, T4, T5, T6>>(name)
           .MustHaveBeenCalledWith(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6);
    }

    public void MustHaveBeenCalledWith<T1, T2, T3, T4, T5, T6, T7>(
        string name,
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6,
        T7 parameter7
    )
    {
        GetRequiredCallTracker<CallTracker<T1, T2, T3, T4, T5, T6, T7>>(name)
           .MustHaveBeenCalledWith(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7);
    }

    public void MustHaveBeenCalledWith<T1, T2, T3, T4, T5, T6, T7, T8>(
        string name,
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6,
        T7 parameter7,
        T8 parameter8
    )
    {
        GetRequiredCallTracker<CallTracker<T1, T2, T3, T4, T5, T6, T7, T8>>(name)
           .MustHaveBeenCalledWith(
                parameter1,
                parameter2,
                parameter3,
                parameter4,
                parameter5,
                parameter6,
                parameter7,
                parameter8
            );
    }

    public void MustHaveNoOtherCallsExcept(string name)
    {
        _callTrackers.Keys.Should().OnlyContain(x => x == name);
    }

    public void MustHaveNoOtherCallsExcept(params string[] names)
    {
        _callTrackers.Keys.Should().OnlyContain(x => names.Contains(x));
    }

    public void MustHaveBeenOnlyMethodCalled(string name, int times = 1)
    {
        MustHaveBeenCalled(name, times);
        MustHaveNoOtherCallsExcept(name);
    }

    public void MustHaveBeenOnlyMethodCalledWith<T>(string name, T parameter)
    {
        MustHaveBeenCalledWith(name, parameter);
        MustHaveNoOtherCallsExcept(name);
    }

    public void MustHaveBeenOnlyMethodCalledWith<T1, T2>(string name, T1 parameter1, T2 parameter2)
    {
        MustHaveBeenCalledWith(name, parameter1, parameter2);
        MustHaveNoOtherCallsExcept(name);
    }

    public void MustHaveBeenOnlyMethodCalledWith<T1, T2, T3>(string name, T1 parameter1, T2 parameter2, T3 parameter3)
    {
        MustHaveBeenCalledWith(name, parameter1, parameter2, parameter3);
        MustHaveNoOtherCallsExcept(name);
    }

    public void MustHaveBeenOnlyMethodCalledWith<T1, T2, T3, T4>(
        string name,
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4
    )
    {
        MustHaveBeenCalledWith(name, parameter1, parameter2, parameter3, parameter4);
        MustHaveNoOtherCallsExcept(name);
    }

    public void MustHaveBeenOnlyMethodCalledWith<T1, T2, T3, T4, T5>(
        string name,
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5
    )
    {
        MustHaveBeenCalledWith(name, parameter1, parameter2, parameter3, parameter4, parameter5);
        MustHaveNoOtherCallsExcept(name);
    }

    public void MustHaveBeenOnlyMethodCalledWith<T1, T2, T3, T4, T5, T6>(
        string name,
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6
    )
    {
        MustHaveBeenCalledWith(name, parameter1, parameter2, parameter3, parameter4, parameter5, parameter6);
        MustHaveNoOtherCallsExcept(name);
    }

    public void MustHaveBeenOnlyMethodCalledWith<T1, T2, T3, T4, T5, T6, T7>(
        string name,
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6,
        T7 parameter7
    )
    {
        MustHaveBeenCalledWith(
            name,
            parameter1,
            parameter2,
            parameter3,
            parameter4,
            parameter5,
            parameter6,
            parameter7
        );
        MustHaveNoOtherCallsExcept(name);
    }

    public void MustHaveBeenOnlyMethodCalledWith<T1, T2, T3, T4, T5, T6, T7, T8>(
        string name,
        T1 parameter1,
        T2 parameter2,
        T3 parameter3,
        T4 parameter4,
        T5 parameter5,
        T6 parameter6,
        T7 parameter7,
        T8 parameter8
    )
    {
        MustHaveBeenCalledWith(
            name,
            parameter1,
            parameter2,
            parameter3,
            parameter4,
            parameter5,
            parameter6,
            parameter7,
            parameter8
        );
        MustHaveNoOtherCallsExcept(name);
    }

    private T GetOrCreateCallTracker<T>(string name, Func<T> createCallTracker)
        where T : ICallTracker
    {
        name.MustNotBeNullOrWhiteSpace();
        if (!_callTrackers.TryGetValue(name, out var callTracker))
        {
            callTracker = createCallTracker();
            _callTrackers.Add(name, callTracker);
        }

        if (callTracker is not T castCallTracker)
        {
            throw new InvalidOperationException($"Call tracker for '{name}' is not a {nameof(CallTracker)}.");
        }

        return castCallTracker;
    }

    public T GetRequiredCallTracker<T>(string name)
    {
        if (!_callTrackers.TryGetValue(name, out var callTracker))
        {
            throw new XunitException($"{name} has not been called.");
        }

        if (callTracker is not T castCallTracker)
        {
            throw new XunitException(
                $"There is a call tracker for '{name}', but it is not of type {typeof(T)}. Instead, it is of type {callTracker.GetType()}."
            );
        }

        return castCallTracker;
    }
}
