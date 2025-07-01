using System;
using FluentAssertions;
using Xunit;

namespace Light.TemporaryStreams;

[Trait("Category", "Triangulation")]
public sealed class ArrayAllocationTests
{
    private readonly ITestOutputHelper _output;

    public ArrayAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ByteArray_WithSize84975_AllocatesInSmallObjectHeap()
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
            // When an exception occurs within a No-GC region, the region is immediately ended.
            // This is why we wrap GC.EndNoGCRegion() in a dedicated try-catch block to avoid
            // that an exception thrown during GC.EndNoGCRegion() hides an exception from the assertion
            // in the try block.
            try
            {
                GC.EndNoGCRegion();
            }
            catch (Exception ex)
            {
                _output.WriteLine("Caught exception during GC.EndNoGCRegion");
                _output.WriteLine(ex.ToString());
            }
        }
    }

    [Fact]
    public void ByteArray_WithSize85000_AllocatesInLargeObjectHeap()
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
            try
            {
                GC.EndNoGCRegion();
            }
            catch (Exception ex)
            {
                _output.WriteLine("Caught exception during GC.EndNoGCRegion");
                _output.WriteLine(ex.ToString());
            }
        }
    }

    [Fact]
    public static void ArrayMaxLength_IsLessThanInt32MaxValue() => Array.MaxLength.Should().BeLessThan(int.MaxValue);
}
