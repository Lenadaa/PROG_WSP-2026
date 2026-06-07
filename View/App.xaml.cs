using System.Configuration;
using System.Data;
using System.Windows;
using Data;

namespace View;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Instance.Dispose();
        base.OnExit(e);
    }
}