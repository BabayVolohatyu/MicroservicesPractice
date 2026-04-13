using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FinancialTracker.Dashboard.ViewModels;
using FinancialTracker.Dashboard.Views;

namespace FinancialTracker.Dashboard;

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
            var vm = new MainWindowViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };
            desktop.ShutdownRequested += (_, _) => vm.Cleanup();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
