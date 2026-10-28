using System.Windows;

namespace ClinicApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var login = new LoginWindow();
        if (login.ShowDialog() == true && login.User != null)
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            MainWindow = new MainWindow(login.User);
            MainWindow.Show();
        }
        else Shutdown();
    }
}
