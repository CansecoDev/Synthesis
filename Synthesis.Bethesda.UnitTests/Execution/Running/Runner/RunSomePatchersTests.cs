﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Mutagen.Bethesda.Plugins;
using Noggog;
using NSubstitute;
using NSubstitute.Core;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class RunSomePatchersTests
    {
        [Theory, SynthAutoData]
        public async Task PassesEachPatcherToRunComponent(
            IGroupRun groupRun,
            PatcherPrepBundle[] patchers,
            CancellationToken cancellation,
            FilePath? sourcePath,
            RunParameters runParameters,
            RunSomePatchers sut)
        {
            await sut.Run(
                groupRun,
                patchers,
                cancellation,
                sourcePath,
                runParameters);

            await sut.RunAPatcher.ReceivedWithAnyArgs().Run(
                default!, default!, default!,
                default!, default!);

            for (int i = 0; i < patchers.Length; i++)
            {
                await sut.RunAPatcher.Received(1)
                    .Run(groupRun,
                        patchers[i],
                        cancellation,
                        Arg.Any<FilePath?>(),
                        runParameters);
            }
        }
        
        [Theory, SynthAutoData]
        public async Task PassesPreviousPathToNext(
            IGroupRun groupRun,
            ModPath return1,
            ModPath return2,
            CancellationToken cancellation,
            FilePath? sourcePath,
            RunParameters runParameters,
            RunSomePatchers sut)
        {
            PatcherPrepBundle[] patchers = new[]
            {
                new PatcherPrepBundle(
                    Substitute.For<IPatcherRun>(),
                    Task.FromResult<Exception?>(null)),
                new PatcherPrepBundle(
                    Substitute.For<IPatcherRun>(),
                    Task.FromResult<Exception?>(null)),
            };
            
            sut.RunAPatcher.Run(
                    Arg.Any<IGroupRun>(),
                    patchers[0],
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<RunParameters>())
                .Returns(return1);
            
            sut.RunAPatcher.Run(
                    Arg.Any<IGroupRun>(),
                    patchers[1],
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<RunParameters>())
                .Returns(return2);
                
            await sut.Run(
                groupRun,
                patchers,
                cancellation,
                sourcePath,
                runParameters);

            await sut.RunAPatcher.Received(1)
                .Run(
                    Arg.Any<IGroupRun>(),
                    patchers[0],
                    Arg.Any<CancellationToken>(),
                    sourcePath,
                    Arg.Any<RunParameters>());

            await sut.RunAPatcher.Received(1)
                .Run(
                    Arg.Any<IGroupRun>(),
                    patchers[1],
                    Arg.Any<CancellationToken>(),
                    return1,
                    Arg.Any<RunParameters>());
        }
        
        [Theory, SynthAutoData]
        public async Task ReturnsFinalPatcherReturn(
            IGroupRun groupRun,
            FilePath return1,
            FilePath return2,
            CancellationToken cancellation,
            FilePath? sourcePath,
            RunParameters runParameters,
            RunSomePatchers sut)
        {
            PatcherPrepBundle[] patchers = new[]
            {
                new PatcherPrepBundle(
                    Substitute.For<IPatcherRun>(),
                    Task.FromResult<Exception?>(null)),
                new PatcherPrepBundle(
                    Substitute.For<IPatcherRun>(),
                    Task.FromResult<Exception?>(null)),
            };
            
            sut.RunAPatcher.Run(
                    Arg.Any<IGroupRun>(),
                    patchers[0],
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<RunParameters>())
                .Returns(return1);
            
            sut.RunAPatcher.Run(
                    Arg.Any<IGroupRun>(),
                    patchers[1],
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<RunParameters>())
                .Returns(return2);

            (await sut.Run(
                    groupRun,
                    patchers,
                    cancellation,
                    sourcePath,
                    runParameters))
                .Should().Be(return2);
        }
        
        [Theory, SynthAutoData]
        public async Task PatcherNullReturnDoesNotRunAnyMore(
            IGroupRun groupRun,
            ModPath return2,
            CancellationToken cancellation,
            FilePath? sourcePath,
            RunParameters runParameters,
            RunSomePatchers sut)
        {
            PatcherPrepBundle[] patchers = new[]
            {
                new PatcherPrepBundle(
                    Substitute.For<IPatcherRun>(),
                    Task.FromResult<Exception?>(null)),
                new PatcherPrepBundle(
                    Substitute.For<IPatcherRun>(),
                    Task.FromResult<Exception?>(null)),
            };
            
            sut.RunAPatcher.Run(
                    Arg.Any<IGroupRun>(),
                    patchers[0],
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<RunParameters>())
                .Returns(default(FilePath?));
            
            sut.RunAPatcher.Run(
                    Arg.Any<IGroupRun>(),
                    patchers[1],
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<RunParameters>())
                .Returns(return2);

            await sut.Run(
                groupRun,
                patchers,
                cancellation,
                sourcePath,
                runParameters);

            await sut.RunAPatcher.DidNotReceive()
                .Run(
                    Arg.Any<IGroupRun>(),
                    patchers[1],
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<RunParameters>());
        }
        
        [Theory, SynthAutoData]
        public async Task ThrowingPatcherRethrows(
            IGroupRun groupRun,
            ModPath return2,
            CancellationToken cancellation,
            FilePath? sourcePath,
            RunParameters runParameters,
            RunSomePatchers sut)
        {
            PatcherPrepBundle[] patchers = new[]
            {
                new PatcherPrepBundle(
                    Substitute.For<IPatcherRun>(),
                    Task.FromResult<Exception?>(null)),
                new PatcherPrepBundle(
                    Substitute.For<IPatcherRun>(),
                    Task.FromResult<Exception?>(null)),
            };

            sut.RunAPatcher.Run(
                    Arg.Any<IGroupRun>(),
                    patchers[0],
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<RunParameters>())
                .ThrowsForAnyArgs<NotImplementedException>();
            
            sut.RunAPatcher.Run(
                    Arg.Any<IGroupRun>(),
                    patchers[1],
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<RunParameters>())
                .Returns(return2);

            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                await sut.Run(
                    groupRun,
                    patchers,
                    cancellation,
                    sourcePath,
                    runParameters);
            });

            await sut.RunAPatcher.DidNotReceive()
                .Run(
                    Arg.Any<IGroupRun>(),
                    patchers[1],
                    Arg.Any<CancellationToken>(),
                    Arg.Any<FilePath?>(),
                    Arg.Any<RunParameters>());
        }
    }
}