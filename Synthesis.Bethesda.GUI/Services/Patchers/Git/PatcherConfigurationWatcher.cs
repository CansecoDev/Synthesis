﻿using System;
using Path = System.IO.Path;
using System.IO.Abstractions;
using System.Reactive;
using System.Reactive.Linq;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.Json;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IPatcherConfigurationWatcher
    {
        IObservable<PatcherCustomization?> Customization { get; }
    }

    public class PatcherConfigurationWatcher : IPatcherConfigurationWatcher
    {
        private readonly IRunnableStateProvider _runnableStateProvider;

        public IObservable<PatcherCustomization?> Customization { get; }

        public PatcherConfigurationWatcher(
            IFileSystem fileSystem,
            IPatcherCustomizationImporter customizationImporter,
            IRunnableStateProvider runnableStateProvider)
        {
            _runnableStateProvider = runnableStateProvider;
            Customization = runnableStateProvider.State
                .Select(x =>
                {
                    if (x.RunnableState.Failed) return Observable.Return(default(PatcherCustomization?));
                    var confPath = Path.Combine(Path.GetDirectoryName(x.Item.ProjPath)!, Constants.MetaFileName);
                    return Noggog.ObservableExt.WatchFile(confPath)
                        .StartWith(Unit.Default)
                        .Select(_ =>
                        {
                            try
                            {
                                if (!fileSystem.File.Exists(confPath)) return default;
                                return customizationImporter.Import(confPath);
                            }
                            catch (Exception)
                            {
                                return default(PatcherCustomization?);
                            }
                        });
                })
                .Switch()
                .Replay(1)
                .RefCount();
        }
    }
}