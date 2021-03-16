using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class AutogeneratedSettingsViewBase : NoggogUserControl<AutogeneratedSettingsVM> { }

    /// <summary>
    /// Interaction logic for AutogeneratedSettingsView.xaml
    /// </summary>
    public partial class AutogeneratedSettingsView : AutogeneratedSettingsViewBase
    {
        public AutogeneratedSettingsView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.SettingsLoading)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.ProcessingRing.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.Bundle!.Settings)
                    .BindToStrict(this, x => x.ReflectionSettingTabs.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel!.Bundle!.Settings!.Count)
                    .Select(x => x > 0 ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.ReflectionSettingTabs.Visibility)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.SelectedSettings, view => view.ReflectionSettingTabs.SelectedItem)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel!.Error),
                        this.WhenAnyValue(x => x.ViewModel!.SettingsLoading),
                        (err, loading) => !loading && err.Failed)
                    .Select(failed => failed ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.ErrorPanel.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.Error.Reason)
                    .BindToStrict(this, x => x.ErrorBox.Text)
                    .DisposeWith(disposable);
            });
        }
    }
}