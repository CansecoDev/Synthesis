﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class PrepPatchersForRunTests
    {
        [Theory, SynthAutoData]
        public async Task RunsPrepOnEachPatcher(
            IEnumerable<IPatcherRun> patchers,
            CancellationToken cancellation,
            PrepPatchersForRun sut)
        {
            await Task.WhenAll(
                sut.PrepPatchers(patchers, cancellation));

            foreach (var patcher in patchers)
            {
                await patcher.Received(1).Prep(cancellation);
            }
        }
        
        [Theory, SynthAutoData]
        public async Task ProperlyPreppedPatchersReturnNull(
            IEnumerable<IPatcherRun> patchers,
            CancellationToken cancellation,
            PrepPatchersForRun sut)
        {
            var results = sut.PrepPatchers(patchers, cancellation);
            foreach (var result in results)
            {
                (await result).Should().BeNull();
            }
        }
        
        [Theory, SynthAutoData]
        public async Task ThrowingPatchersReturnException(
            IEnumerable<IPatcherRun> patchers,
            CancellationToken cancellation,
            PrepPatchersForRun sut)
        {
            foreach (var item in patchers)
            {
                item.Prep(default!).ThrowsForAnyArgs<NotImplementedException>();
            }
            var results = sut.PrepPatchers(patchers, cancellation);
            foreach (var result in results)
            {
                (await result).Should().NotBeNull();
            }
        }
        
        [Theory, SynthAutoData]
        public async Task ThrowingPatchersReports(
            IEnumerable<IPatcherRun> patchers,
            CancellationToken cancellation,
            PrepPatchersForRun sut)
        {
            foreach (var item in patchers)
            {
                item.Prep(default!).ThrowsForAnyArgs<NotImplementedException>();
            }
            var results = sut.PrepPatchers(patchers, cancellation);
            await Task.WhenAll(results);
            sut.Reporter.ReceivedWithAnyArgs().ReportPrepProblem(default, default!, default!);
        }
        
        [Theory, SynthAutoData]
        public async Task CancellationReturnsNullExceptions(
            IEnumerable<IPatcherRun> patchers,
            CancellationToken cancelled,
            PrepPatchersForRun sut)
        {
            var results = sut.PrepPatchers(patchers, cancelled);
            foreach (var result in results)
            {
                (await result).Should().BeNull();
            }
        }
    }
}