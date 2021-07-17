using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Initialization;

namespace Synthesis.Bethesda.GUI.Views
{
    public class CliInitViewBase : NoggogUserControl<CliPatcherInitVm> { }

    /// <summary>
    /// Interaction logic for CliInitView.xaml
    /// </summary>
    public partial class CliInitView : CliInitViewBase
    {
        public CliInitView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.BindStrict(this.ViewModel, vm => vm.DisplayName, view => view.PatcherDetailName.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel)
                    .BindToStrict(this, x => x.PatcherIconDisplay.DataContext)
                    .DisposeWith(disposable);

                // Set up discard/confirm clicks
                this.WhenAnyValue(x => x.ViewModel!.Init.CancelConfiguration)
                    .BindToStrict(this, x => x.CancelAdditionButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Init.CompleteConfiguration)
                    .BindToStrict(this, x => x.ConfirmButton.ConfirmAdditionButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
