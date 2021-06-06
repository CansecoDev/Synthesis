using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI
{
    public class ConfigurationVM : ViewModel
    {
        private ISelectedProfileControllerVm _SelectedProfileController;

        public SourceCache<ProfileVM, string> Profiles { get; } = new SourceCache<ProfileVM, string>(p => p.ID);

        public IObservableCollection<ProfileVM> ProfilesDisplay { get; }
        public IObservableCollection<PatcherVM> PatchersDisplay { get; }

        public ICommand CompleteConfiguration { get; }
        public ICommand CancelConfiguration { get; }
        public ICommand ShowHelpToggleCommand { get; }

        public ReactiveCommandBase<Unit, Unit> RunPatchers { get; }

        private readonly ObservableAsPropertyHelper<ProfileVM?> _SelectedProfile;
        public ProfileVM? SelectedProfile => _SelectedProfile.Value;

        [Reactive]
        public PatcherInitVM? NewPatcher { get; set; }

        private readonly ObservableAsPropertyHelper<object?> _DisplayedObject;
        public object? DisplayedObject => _DisplayedObject.Value;

        private readonly ObservableAsPropertyHelper<PatchersRunVM?> _CurrentRun;
        public PatchersRunVM? CurrentRun => _CurrentRun.Value;

        [Reactive]
        public bool ShowHelp { get; set; }

        public ConfigurationVM(
            ISelectedProfileControllerVm selectedProfile,
            ISaveSignal saveSignal)
        {
            _SelectedProfileController = selectedProfile;
            _SelectedProfile = _SelectedProfileController.WhenAnyValue(x => x.SelectedProfile)
                .ToGuiProperty(this, nameof(SelectedProfile), default);
            
            ProfilesDisplay = Profiles.Connect().ToObservableCollection(this);
            PatchersDisplay = this.WhenAnyValue(x => x.SelectedProfile)
                .Select(p => p?.Patchers.Connect() ?? Observable.Empty<IChangeSet<PatcherVM>>())
                .Switch()
                .ToObservableCollection(this);

            CompleteConfiguration = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    var initializer = this.NewPatcher;
                    if (initializer == null) return;
                    AddNewPatchers(await initializer.Construct().ToListAsync());
                },
                canExecute: this.WhenAnyValue(x => x.NewPatcher)
                    .Select(patcher =>
                    {
                        if (patcher == null) return Observable.Return(false);
                        return patcher.WhenAnyValue(x => x.CanCompleteConfiguration)
                            .Select(e => e.Succeeded);
                    })
                    .Switch());

            CancelConfiguration = ReactiveCommand.Create(
                () =>
                {
                    NewPatcher?.Cancel();
                    NewPatcher = null;
                });

            // Dispose any old patcher initializations
            this.WhenAnyValue(x => x.NewPatcher)
                .DisposePrevious()
                .Subscribe()
                .DisposeWith(this);

            _DisplayedObject = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.SelectedProfile!.DisplayedObject),
                    this.WhenAnyValue(x => x.NewPatcher),
                    (selected, newConfig) => (newConfig as object) ?? selected)
                .ToGuiProperty(this, nameof(DisplayedObject), default);

            RunPatchers = NoggogCommand.CreateFromJob(
                extraInput: this.WhenAnyValue(x => x.SelectedProfile),
                jobCreator: (profile) =>
                {
                    if (SelectedProfile == null)
                    {
                        return (default(PatchersRunVM?), Observable.Return(Unit.Default));
                    }
                    var ret = new PatchersRunVM(this, SelectedProfile);
                    var completeSignal = ret.WhenAnyValue(x => x.Running)
                        .TurnedOff()
                        .FirstAsync();
                    return (ret, completeSignal);
                },
                createdJobs: out var createdRuns,
                canExecute: this.WhenAnyFallback(x => x.SelectedProfile!.BlockingError, fallback: ErrorResponse.Failure)
                    .Select(err => err.Succeeded))
                .DisposeWith(this);

            _CurrentRun = createdRuns
                .ToGuiProperty(this, nameof(CurrentRun), default);

            var activePanelController = Inject.Scope.GetInstance<IActivePanelControllerVm>();
            this.WhenAnyValue(x => x.CurrentRun)
                .NotNull()
                .Do(run => activePanelController.ActivePanel = run)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Subscribe(r => r.Run())
                .DisposeWith(this);

            ShowHelpToggleCommand = ReactiveCommand.Create(() => ShowHelp = !ShowHelp);

            saveSignal.Saving
                .Subscribe(x => Save(x.Gui, x.Pipe))
                .DisposeWith(this);
        }

        public void Load(SynthesisGuiSettings settings, PipelineSettings pipeSettings)
        {
            Profiles.Clear();
            Profiles.AddOrUpdate(pipeSettings.Profiles.Select(p =>
            {
                return new ProfileVM(this, p);
            }));
            if (Profiles.TryGetValue(settings.SelectedProfile, out var profile))
            {
                _SelectedProfileController.SelectedProfile = profile;
            }
            ShowHelp = settings.ShowHelp;
        }

        private void Save(SynthesisGuiSettings guiSettings, PipelineSettings pipeSettings)
        {
            guiSettings.ShowHelp = ShowHelp;
            guiSettings.SelectedProfile = SelectedProfile?.ID ?? string.Empty;
            pipeSettings.Profiles = Profiles.Items.Select(p => p.Save()).ToList();
        }

        public void AddNewPatchers(List<PatcherVM> patchersToAdd)
        {
            NewPatcher = null;
            if (patchersToAdd.Count == 0) return;
            if (SelectedProfile == null)
            {
                throw new ArgumentNullException("Selected profile unexpectedly null");
            }
            patchersToAdd.ForEach(p => p.IsOn = true);
            SelectedProfile.Patchers.AddRange(patchersToAdd);
            SelectedProfile.DisplayedObject = patchersToAdd.First();
        }

        public override void Dispose()
        {
            base.Dispose();
            Profiles.Items.ForEach(p => p.Dispose());
        }
    }
}
