using System;
using FluentAssertions;
using Xunit;

namespace Light.TemporaryStreams.Tests;

public static class ArrayAllocationTests
{
    [Fact]
    public static void ByteArray_WithSize84975_AllocatesInSmallObjectHeap()
    {
        const int sohArraySize = 84_975;
        var result = GC.TryStartNoGCRegion(100_000, disallowFullBlockingGC: true);
        result.Should().BeTrue("GC.TryStartNoGCRegion was executed successfully");

        try
        {
            var sohArray = new byte[sohArraySize];

            GC.GetGeneration(sohArray).Should().Be(0);
        }
        finally
        {
            GC.EndNoGCRegion();
        }
    }

    [Fact]
    public static void ByteArray_WithSize85000_AllocatesInLargeObjectHeap()
    {
        const int lohArraySize = 84_976;

        var result = GC.TryStartNoGCRegion(100_000, disallowFullBlockingGC: true);
        result.Should().BeTrue("GC.TryStartNoGCRegion was executed successfully");

        try
        {
            var lohArray = new byte[lohArraySize];

            GC.GetGeneration(lohArray).Should().Be(2);
        }
        finally
        {
            GC.EndNoGCRegion();
        }
    }
}
