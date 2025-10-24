using Bridge.Abstractions.Dto;
using Bridge.Runtime;
using Bridge.Runtime.Converters;
using FluentAssertions;
using Python.Runtime;
using Xunit;

namespace Bridge.Tests;

public class TypeConversionTests
{
    [Fact]
    public void ShouldConvertPrimitiveTypes()
    {
        PyHost.Initialize();

        using var gil = Py.GIL();

        var stringValue = "test";
        var pyString = PrimitiveConverter.ToPython(stringValue);
        var convertedString = PrimitiveConverter.FromPython<string>(pyString);
        convertedString.Should().Be(stringValue);

        var intValue = 42;
        var pyInt = PrimitiveConverter.ToPython(intValue);
        var convertedInt = PrimitiveConverter.FromPython<int>(pyInt);
        convertedInt.Should().Be(intValue);

        var boolValue = true;
        var pyBool = PrimitiveConverter.ToPython(boolValue);
        var convertedBool = PrimitiveConverter.FromPython<bool>(pyBool);
        convertedBool.Should().Be(boolValue);
    }

    [Fact]
    public void ShouldConvertJsonObjects()
    {
        PyHost.Initialize();

        using var gil = Py.GIL();

        var request = new SummaryRequest
        {
            Text = "Test text",
            MaxLength = 100,
            IncludeMetadata = true
        };

        var json = JsonConverter.ToPython(request);
        var convertedRequest = JsonConverter.FromPython(json, typeof(SummaryRequest)) as SummaryRequest;

        convertedRequest.Should().NotBeNull();
        convertedRequest!.Text.Should().Be(request.Text);
        convertedRequest.MaxLength.Should().Be(request.MaxLength);
        convertedRequest.IncludeMetadata.Should().Be(request.IncludeMetadata);
    }

    [Fact]
    public void ShouldHandleNullValues()
    {
        PyHost.Initialize();

        using var gil = Py.GIL();

        var pyNone = PrimitiveConverter.ToPython(null);
        pyNone.IsNone().Should().BeTrue();

        var convertedNull = PrimitiveConverter.FromPython<string>(pyNone);
        convertedNull.Should().BeNull();
    }
}
