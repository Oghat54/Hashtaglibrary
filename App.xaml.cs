using System.Windows;
using System.Windows.Threading;

namespace HashtagCopier;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            MessageBox.Show(e.Exception.ToString(), "Необработанная ошибка");
            e.Handled = true;
        };
    }
}