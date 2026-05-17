using System.Windows;
namespace DiskCleaner;
public partial class App : Application {
    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);
        Helpers.PrivilegeHelper.EnableAllPrivileges();
    }
}
