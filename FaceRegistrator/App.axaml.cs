using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using FaceRegistrator.ViewModels;
using FaceRegistrator.Views;

namespace FaceRegistrator
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);
                var vm = new MainWindowViewModel();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = vm,
                };
                desktop.MainWindow.Loaded += async (sender, args) =>
                {
                    await vm.RefreshDevicesCommand.ExecuteAsync(null);
                    await vm.RequestGroupsCommand.ExecuteAsync(null);
                };
                desktop.MainWindow.Closed += async (sender, args) =>
                {
                    await vm.DisposeWebCamera();
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}