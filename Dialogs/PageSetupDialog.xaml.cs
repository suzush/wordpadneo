using wordpadneo.Models;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace wordpadneo.Dialogs
{
    public sealed partial class PageSetupDialog : ContentDialog
    {
        private readonly PageSettings _pageSettings;
        public PageSetupDialog(PageSettings pageSettings)
        {
            InitializeComponent();
            _pageSettings = pageSettings;

            comboPaper.SelectedIndex = (_pageSettings.PageWidthCm, _pageSettings.PageHeightCm) switch
            {
                (29.7, 42.0) => 0,  // A3
                (21.0, 29.7) => 1,  // A4
                (14.8, 21.0) => 2,  // A5
                (10.5, 14.8) => 3,  // A6
                (25.7, 36.4) => 4,  // B4
                (18.2, 25.7) => 5,  // B5
                (12.8, 18.2) => 6,  // B6
                (21.59, 27.94) => 7,  // Letter
                (8.9, 12.7) => 8,  // L size
                (12.7, 17.8) => 9,  // 2L size
                (10.0, 14.8) => 10, // Postcard
                (12.0, 23.5) => 11, // Chokei 3
                (9.0, 20.5) => 12, // Chokei 4
                (11.4, 16.2) => 13, // Youkei 2
                (10.5, 23.5) => 14, // Youkei 4
                _ => 1   // Default (A4)
            };
            numLeftMargin.Value = _pageSettings.MarginLeftCm;
            numRightMargin.Value = _pageSettings.MarginRightCm;
            numTopMargin.Value = _pageSettings.MarginTopCm;
            numBottomMargin.Value = _pageSettings.MarginBottomCm;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            switch (comboPaper.SelectedIndex)
            {
                // A series
                case 0: _pageSettings.PageWidthCm = 29.7; _pageSettings.PageHeightCm = 42.0; break;  // A3
                case 1: _pageSettings.PageWidthCm = 21.0; _pageSettings.PageHeightCm = 29.7; break;  // A4
                case 2: _pageSettings.PageWidthCm = 14.8; _pageSettings.PageHeightCm = 21.0; break;  // A5
                case 3: _pageSettings.PageWidthCm = 10.5; _pageSettings.PageHeightCm = 14.8; break;  // A6

                // B series
                case 4: _pageSettings.PageWidthCm = 25.7; _pageSettings.PageHeightCm = 36.4; break;  // B4
                case 5: _pageSettings.PageWidthCm = 18.2; _pageSettings.PageHeightCm = 25.7; break;  // B5
                case 6: _pageSettings.PageWidthCm = 12.8; _pageSettings.PageHeightCm = 18.2; break;  // B6

                // Other / photo / envelope sizes
                case 7: _pageSettings.PageWidthCm = 21.59; _pageSettings.PageHeightCm = 27.94; break; // Letter
                case 8: _pageSettings.PageWidthCm = 8.9; _pageSettings.PageHeightCm = 12.7; break; // L size
                case 9: _pageSettings.PageWidthCm = 12.7; _pageSettings.PageHeightCm = 17.8; break; // 2L size
                case 10: _pageSettings.PageWidthCm = 10.0; _pageSettings.PageHeightCm = 14.8; break; // Postcard
                case 11: _pageSettings.PageWidthCm = 12.0; _pageSettings.PageHeightCm = 23.5; break; // Chokei 3
                case 12: _pageSettings.PageWidthCm = 9.0; _pageSettings.PageHeightCm = 20.5; break; // Chokei 4
                case 13: _pageSettings.PageWidthCm = 11.4; _pageSettings.PageHeightCm = 16.2; break; // Youkei 2
                case 14: _pageSettings.PageWidthCm = 10.5; _pageSettings.PageHeightCm = 23.5; break; // Youkei 4
            }

            _pageSettings.MarginLeftCm = numLeftMargin.Value;
            _pageSettings.MarginRightCm = numRightMargin.Value;
        }
    }
}
