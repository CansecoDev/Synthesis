using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using DynamicData;
using Synthesis.Bethesda.DTO;
using Mutagen.Bethesda.Synthesis.WPF;

namespace Synthesis.Bethesda.GUI
{
    public class PatcherSettingsVM : ViewModel
    {
        public ILogger Logger { get; }

        public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }

        private readonly ObservableAsPropertyHelper<SettingsConfiguration> _SettingsConfiguration;
        public SettingsConfiguration SettingsConfiguration => _SettingsConfiguration.Value;

        private readonly ObservableAsPropertyHelper<bool> _SettingsOpen;
        public bool SettingsOpen => _SettingsOpen.Value;

        private bool _hasBeenRetrieved = false;
        private readonly ObservableAsPropertyHelper<AutogeneratedSettingsVM?> _ReflectionSettings;
        public AutogeneratedSettingsVM? ReflectionSettings
        {
            get
            {
                _hasBeenRetrieved = true;
                return _ReflectionSettings.Value;
            }
        }

        public PatcherSettingsVM(
            ILogger logger,
            PatcherVM parent,
            IObservable<(GetResponse<FilePath> ProjPath, string? SynthVersion)> source,
            bool needBuild)
        {
            Logger = logger;
            _SettingsConfiguration = source
                .Select(i =>
                {
                    return Observable.Create<SettingsConfiguration>(async (observer, cancel) =>
                    {
                        observer.OnNext(new SettingsConfiguration(SettingsStyle.None, Array.Empty<ReflectionSettingsConfig
                            >()));
                        if (i.ProjPath.Failed) return;

                        try
                        {
                            var result = await Synthesis.Bethesda.Execution.CLI.Commands.GetSettingsStyle(
                                i.ProjPath.Value,
                                directExe: false,
                                cancel: cancel,
                                build: needBuild,
                                logger.Information);
                            // Turn on if Host systems needed
                            //if (result.Style == SettingsStyle.SpecifiedClass
                            //    && parent is GitPatcherVM gitPatcher
                            //    && Version.TryParse(i.SynthVersion, out var vers)
                            //    && vers <= new Version(0, 16, 9))
                            //{
                            //    result = new SettingsConfiguration(SettingsStyle.Host, result.Targets);
                            //}
                            logger.Information($"Settings type: {result}");
                            observer.OnNext(result);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error checking if patcher can open settings: {ex}");
                        }
                        observer.OnCompleted();
                    });
                })
                .Switch()
                .ToGuiProperty(this, nameof(SettingsConfiguration), new SettingsConfiguration(SettingsStyle.None, Array.Empty<ReflectionSettingsConfig>()));

            OpenSettingsCommand = NoggogCommand.CreateFromObject(
                objectSource: Observable.CombineLatest(
                        source.Select(x => x.ProjPath),
                        this.WhenAnyValue(x => x.SettingsConfiguration),
                        (Proj, Conf) => (Proj, Conf)),
                canExecute: x => x.Proj.Succeeded 
                    && (x.Conf.Style == SettingsStyle.Open || x.Conf.Style == SettingsStyle.Host),
                execute: async (o) =>
                {
                    if (o.Conf.Style == SettingsStyle.Open)
                    {
                        await Synthesis.Bethesda.Execution.CLI.Commands.OpenForSettings(
                            o.Proj.Value,
                            directExe: false,
                            rect: parent.Profile.Config.MainVM.Rectangle,
                            cancel: CancellationToken.None,
                            release: parent.Profile.Release,
                            dataFolderPath: parent.Profile.DataFolder,
                            loadOrder: parent.Profile.LoadOrder.Items.Select(lvm => lvm.Listing));
                    }
                    else
                    {
                        await Synthesis.Bethesda.Execution.CLI.Commands.OpenSettingHost(
                            patcherName: parent.DisplayName,
                            path: o.Proj.Value,
                            rect: parent.Profile.Config.MainVM.Rectangle,
                            cancel: CancellationToken.None,
                            release: parent.Profile.Release,
                            dataFolderPath: parent.Profile.DataFolder,
                            loadOrder: parent.Profile.LoadOrder.Items.Select(lvm => lvm.Listing));
                    }
                },
                disposable: this.CompositeDisposable);

            _SettingsOpen = OpenSettingsCommand.IsExecuting
                .ToGuiProperty(this, nameof(SettingsOpen));

            _ReflectionSettings = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.SettingsConfiguration),
                    source.Select(x => x.ProjPath),
                    (SettingsConfig, ProjPath) => (SettingsConfig, ProjPath))
                .Select(x =>
                {
                    if (x.ProjPath.Failed
                        || x.SettingsConfig.Style != SettingsStyle.SpecifiedClass
                        || x.SettingsConfig.Targets.Length == 0)
                    {
                        return default(AutogeneratedSettingsVM?);
                    }
                    return new AutogeneratedSettingsVM(
                        x.SettingsConfig,
                        projPath: x.ProjPath.Value,
                        displayName: parent.DisplayName,
                        loadOrder: parent.Profile.LoadOrder.Connect(),
                        linkCache: parent.Profile.SimpleLinkCache,
                        log: Log.Logger.Information);
                })
                .ToGuiProperty<AutogeneratedSettingsVM?>(this, nameof(ReflectionSettings), initialValue: null, deferSubscription: true);
        }

        public void Persist(Action<string> logger)
        {
            if (!_hasBeenRetrieved) return;
            ReflectionSettings?.Bundle?.Settings?.ForEach(vm => vm.Persist(logger));
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_hasBeenRetrieved)
            {
                ReflectionSettings?.Dispose();
            }
        }
    }
}
