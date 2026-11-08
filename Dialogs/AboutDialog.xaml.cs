using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel;
using wordpadneo.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace wordpadneo.Dialogs
{
    public sealed partial class AboutDialog : ContentDialog
    {
        public AboutDialog()
        {
            InitializeComponent();
            LoadVersion();
        }

        private void LoadVersion()
        {
            string format = LocalizationHelper.GetString("AboutDialog_VersionFormat");
            if (string.IsNullOrWhiteSpace(format))
            {
                format = "Version {0}.{1}.{2}";
            }

            try
            {
                var version = Package.Current.Id.Version;
                VersionText.Text = string.Format(format, version.Major, version.Minor, version.Build);
            }
            catch
            {
                VersionText.Text = string.Format(format, 1, 0, 0) + " (Debug)";
            }
        }
    }
}
