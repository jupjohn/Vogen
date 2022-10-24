﻿using System;
using System.Collections.Immutable;
using FluentAssertions.Execution;
using MediumTests.DiagnosticsTests;
using Microsoft.CodeAnalysis;

namespace AnalyzerTests
{
    public class TestRunner<T> where T : IIncrementalGenerator, new()
    {
        private readonly TargetFramework[] _allFrameworks = new[]
        {
            TargetFramework.Net6_0,
            TargetFramework.Net7_0,
        };

        private Action<ImmutableArray<Diagnostic>>? _validationMethod;
        private string? _source;

        public void RunOnAllFrameworks()
        {
            RunOn(_allFrameworks);
        }

        public TestRunner<T> WithSource(string source)
        {
            _source = source;
            return this;
        }

        public TestRunner<T> ValidateWith(Action<ImmutableArray<Diagnostic>> method)
        {
            _validationMethod = method;

            return this;
        }

        public void RunOn(params TargetFramework[] frameworks)
        {
            _ = _source ?? throw new InvalidOperationException("No source!");
            _ = _validationMethod ?? throw new InvalidOperationException("No validation method!");
        
            foreach (var eachFramework in frameworks)
            {
                using var scope = new AssertionScope();

                var (diagnostics, _) = TestHelper.GetGeneratedOutput<T>(_source, eachFramework);

                _validationMethod(diagnostics);
            }
        }
    }
}