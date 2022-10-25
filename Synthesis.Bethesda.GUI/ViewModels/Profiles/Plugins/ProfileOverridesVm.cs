using System.IO.Abstractions;
using System.Reactive;
using System.Reactive.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Installs.DI;
using Noggog;
using Noggog.Reactive;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

public interface IProfileOverridesVm
{
    string? DataPathOverride { get; set; }
    GetResponse<DirectoryPath> DataFolderResult { get; }
    GameInstallMode? InstallModeOverride { get; set; }
    GameInstallMode InstallMode { get; }
}

public class ProfileOverridesVm : ViewModel,
    IProfileOverridesVm, 
    IDataDirectoryProvider,
    IGameInstallModeProvider
{
    public IFileSystem FileSystem { get; }

    [Reactive]
    public string? DataPathOverride { get; set; }

    private readonly ObservableAsPropertyHelper<GetResponse<DirectoryPath>> _dataFolderResult;
    public GetResponse<DirectoryPath> DataFolderResult => _dataFolderResult.Value;
    
    public DirectoryPath Path => _dataFolderResult.Value.Value;

    [Reactive]
    public GameInstallMode? InstallModeOverride { get; set; }

    private readonly ObservableAsPropertyHelper<GameInstallMode> _installMode;
    public GameInstallMode InstallMode => _installMode.Value;

    public ProfileOverridesVm(
        ILogger logger,
        ISchedulerProvider schedulerProvider,
        IWatchDirectory watchDirectory,
        IFileSystem fileSystem,
        IDataDirectoryLookup dataDirLookup,
        IGameInstallLookup gameInstallModeLookup,
        IProfileIdentifier ident)
    {
        FileSystem = fileSystem;
        
        _installMode = this.WhenAnyValue(x => x.InstallModeOverride)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(x =>
            {
                if (x != null) return x.Value;
                var installs = gameInstallModeLookup.GetInstallMode(ident.Release);

                foreach (var mode in Enums<GameInstallMode>.Values)
                {
                    if (installs.HasFlag(mode)) return mode;
                }

                return default(GameInstallMode);
            })
            .ToProperty(this, nameof(InstallMode), GameInstallMode.Steam, scheduler: schedulerProvider.MainThread, deferSubscription: false);
        
        _dataFolderResult = this.WhenAnyValue(x => x.DataPathOverride)
            .Select(path =>
            {
                if (path != null) return Observable.Return(GetResponse<DirectoryPath>.Succeed(path));
                logger.Information("Starting to locate data folder");
                return this.WhenAnyValue(x => x.InstallMode)
                    .ObserveOn(schedulerProvider.TaskPool)
                    .Select(installMode =>
                    {
                        try
                        {
                            if (!dataDirLookup.TryGet(ident.Release, installMode, out var dataFolder))
                            {
                                return GetResponse<DirectoryPath>.Fail(
                                    $"Could not automatically locate Data folder.  Run {installMode} once to properly register things.");
                            }

                            return GetResponse<DirectoryPath>.Succeed(dataFolder);
                        }
                        catch (Exception ex)
                        {
                            return GetResponse<DirectoryPath>.Fail(string.Empty, ex);
                        }
                    });
            })
            .Switch()
            // Watch folder for existence
            .Select(x =>
            {
                if (x.Failed) return Observable.Return(x);
                return watchDirectory.Watch(x.Value)
                    .StartWith(Unit.Default)
                    .Select(_ =>
                    {
                        try
                        {
                            if (fileSystem.Directory.Exists(x.Value)) return x;
                            return GetResponse<DirectoryPath>.Fail($"Data folder did not exist: {x.Value}");
                        }
                        catch (Exception ex)
                        {
                            return GetResponse<DirectoryPath>.Fail(string.Empty, ex);
                        }
                    });
            })
            .Switch()
            .Do(d =>
            {
                if (d.Failed)
                {
                    logger.Error("Could not locate data folder: {Reason}", d.Reason);
                }
                else
                {
                    logger.Information("Data Folder: {DataFolderPath}", d.Value);
                }
            })
            .ToProperty(this, nameof(DataFolderResult), scheduler: schedulerProvider.MainThread, deferSubscription: true);
    }
}