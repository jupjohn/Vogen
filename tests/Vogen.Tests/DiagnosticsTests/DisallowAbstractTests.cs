﻿using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis;
using VerifyXunit;
using Xunit;

namespace Vogen.Tests.DiagnosticsTests;

[UsesVerify] 
public class DisallowAbstractTests
{
    [Theory]
    [InlineData("partial abstract class")]
    [InlineData("partial abstract record class")]
    public void Disallows_abstract_value_objects(string type)
    {
        var source = $@"using Vogen;

namespace Whatever;

[ValueObject]
public {type} CustomerId {{ }}
";
        
        var (diagnostics, _) = TestHelper.GetGeneratedOutput<ValueObjectGenerator>(source);

        using (new AssertionScope())
        {
            diagnostics.Should().HaveCount(1);
            Diagnostic diagnostic = diagnostics.Single();

            diagnostic.Id.Should().Be("VOG017");
            diagnostic.ToString().Should()
                .Match("* error VOG017: Type 'CustomerId' cannot be abstract");
        }
    }

    [Theory]
    [InlineData("partial abstract class")]
    [InlineData("partial abstract record class")]
    public void Disallows_nested_abstract_value_objects(string type)
    {
        var source = $@"using Vogen;

namespace Whatever;

public class MyContainer {{
    [ValueObject]
    public {type} CustomerId {{ }}
}}
";
        
        var (diagnostics, _) = TestHelper.GetGeneratedOutput<ValueObjectGenerator>(source);

        using (new AssertionScope())
        {
            diagnostics.Should().HaveCount(2);
            Diagnostic diagnostic = diagnostics.ElementAt(0);

            diagnostic.Id.Should().Be("VOG017");
            diagnostic.ToString().Should()
                .Match("*error VOG017: Type 'CustomerId' cannot be abstract");

            diagnostic = diagnostics.ElementAt(1);

            diagnostic.Id.Should().Be("VOG001");
            diagnostic.ToString().Should()
                .Match("*error VOG001: Type 'CustomerId' cannot be nested - remove it from inside MyContainer");
        }
    }
}