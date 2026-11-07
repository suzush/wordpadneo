using wordpadneo.Constants;
using wordpadneo.Dialogs;
using wordpadneo.Helpers;
using wordpadneo.Services;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Foundation;
using Windows.Storage;

namespace wordpadneo.Views
{
    /// <summary>
    /// Manages file operations, page settings, and application shutdown
    /// for the main window.
    /// </summary>
    partial class MainWindow
    {
        /// <summary>
        /// Gets the native window handle of the main window.
        /// </summary>
        /// <value>
        /// The window handle representing the main window.
        /// </value>
        /// <remarks>
        /// Used to specify the parent window for file picker dialogs
        /// and other native dialogs.
        /// </remarks>
        private IntPtr WindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(this);

        /// <summary>
        /// Updates the window title based on the name of the currently edited file.
        /// </summary>
        /// <remarks>
        /// Uses the file name if a file is open.
        /// For a new document, uses the default document name
        /// and appends the application name, "WordPad Neo".
        /// </remarks>
        public void UpdateWindowTitle()
        {
            string fileName = _currentFile != null
                ? _currentFile.Name
                : LocalizationHelper.GetString("MainWindow_DefaultDocumentName");
            string appName = LocalizationHelper.GetString("Common_AppName");
            Title = $"{fileName} - {appName}";
        }

        /// <summary>
        /// Resets the undo history of the RichEditBox.
        /// </summary>
        /// <remarks>
        /// Clears the undo history by temporarily setting UndoLimit to 0,
        /// then configures it to retain up to 500 undo operations.
        /// No action is taken if the editor or its document is unavailable.
        /// </remarks>
        private void ResetUndoLimit()
        {
            if (editor?.Document == null) return;

            editor.Document.UndoLimit = 0;
            editor.Document.UndoLimit = 500;
        }

        /// <summary>
        /// Handles the click event of the New Document button.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The click event data.
        /// </param>
        /// <remarks>
        /// Prompts the user to save any unsaved changes before creating
        /// a new document.
        /// If closing the current document is permitted, clears the document
        /// and resets the file information, undo history, save state,
        /// and window title.
        /// </remarks>
        private async void NewButton_Click(object sender, RoutedEventArgs e)
        {
            bool canClose = await FileService.CloseDocumentAsync(
                    this.Content.XamlRoot,
                    editor,
                    _isFileSaved,
                    _currentFile,
                    _pageSettings,
                    WindowHandle
            );

            if (canClose)
            {
                editor.Document.SetText(TextSetOptions.None, string.Empty);
                _currentFile = null;
                ResetUndoLimit();
                _isFileSaved = true;
                UpdateWindowTitle();
            }
        }

        /// <summary>
        /// Handles the click event of the Open File button.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The click event data.
        /// </param>
        /// <remarks>
        /// Displays a file picker dialog and loads the selected
        /// RTF or TXT document.
        /// If the file is loaded successfully, updates the current file
        /// information and resets the undo history and save state.
        /// Finally, updates the page layout.
        /// </remarks>
        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            StorageFile file = await FileService.OpenFileAsync(editor, _pageSettings, WindowHandle);
            if (file != null)
            {
                _currentFile = file;
                ResetUndoLimit();
                _isFileSaved = true;
                UpdateWindowTitle();
            }
            UpdatePageLayout();
        }

        /// <summary>
        /// Handles the click event of the Save button.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The click event data.
        /// </param>
        /// <remarks>
        /// Overwrites the current file if one exists.
        /// For a new document, displays the Save As dialog.
        /// If saving succeeds, updates the file information,
        /// resets the undo history, and marks the document as saved.
        /// </remarks>
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            StorageFile file;
            if (_currentFile == null)
            {
                file = await FileService.SaveAsAsync(editor, _pageSettings, WindowHandle);
            }
            else
            {
                file = await FileService.SaveFileAsync(editor, _pageSettings, WindowHandle, _currentFile);
            }
            if (file != null)
            {
                _currentFile = file;
                ResetUndoLimit();
                _isFileSaved = true;
                UpdateWindowTitle();
            }
        }

        /// <summary>
        /// Handles the click event of the Save As button.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The click event data.
        /// </param>
        /// <remarks>
        /// Displays a file save dialog that allows the user to select
        /// a destination and file format.
        /// If saving succeeds, updates the current file information,
        /// undo history, save state, and window title.
        /// </remarks>
        private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            StorageFile file = await FileService.SaveAsAsync(editor, _pageSettings, WindowHandle);
            if (file != null)
            {
                _currentFile = file;
                ResetUndoLimit();
                _isFileSaved = true;
                UpdateWindowTitle();
            }
        }

        /// <summary>
        /// Handles the event raised when the main window is closing.
        /// </summary>
        /// <param name="sender">
        /// The AppWindow that initiated the closing operation.
        /// </param>
        /// <param name="args">
        /// The event data used to control the window closing operation.
        /// </param>
        /// <remarks>
        /// Initially cancels the window closing operation and prompts
        /// the user to save any unsaved changes.
        /// If closing is permitted, marks the document as saved,
        /// unsubscribes from the Closing event, and closes the window.
        /// </remarks>
        private async void AppWindow_Closing(
            Microsoft.UI.Windowing.AppWindow sender,
            Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            args.Cancel = true;

            bool canClose = await FileService.CloseDocumentAsync(
                    this.Content.XamlRoot,
                    editor,
                    _isFileSaved,
                    _currentFile,
                    _pageSettings,
                    WindowHandle
            );

            if (canClose)
            {
                _isFileSaved = true;

                AppWindow.Closing -= AppWindow_Closing;
                Close();
            }
        }

        /// <summary>
        /// Handles the click event of the Exit Application button.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The click event data.
        /// </param>
        /// <remarks>
        /// Checks the document's save state before closing the window.
        /// Displays a confirmation dialog if unsaved changes exist.
        /// If closing is permitted, unsubscribes from the Closing event
        /// and closes the main window.
        /// </remarks>
        private async void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            bool canClose = await FileService.CloseDocumentAsync(
                    this.Content.XamlRoot,
                    editor,
                    _isFileSaved,
                    _currentFile,
                    _pageSettings,
                    WindowHandle
            );

            if (canClose)
            {
                _isFileSaved = true;

                AppWindow.Closing -= AppWindow_Closing;
                Close();
            }
        }

        /// <summary>
        /// Handles the click event of the Page Setup button.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The click event data.
        /// </param>
        /// <remarks>
        /// Displays the Page Setup dialog and allows the user
        /// to modify the paper size and margins.
        /// If the settings are confirmed, converts the left and right
        /// margins to pixels and applies them to the RichEditBox padding.
        /// Then updates the ruler and page layout.
        /// </remarks>
        private async void PageSetupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PageSetupDialog(_pageSettings)
            {
                XamlRoot = this.Content.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                double leftPaddingPx = _pageSettings.MarginLeftCm * PageConstants.PxPerCm;
                double rightPaddingPx = _pageSettings.MarginRightCm * PageConstants.PxPerCm;
                editor.Padding = new Thickness(leftPaddingPx, editor.Padding.Top, rightPaddingPx, editor.Padding.Bottom);

                DrawRuler();
                UpdatePageLayout();
            }
        }

        /// <summary>
        /// Handles the click event of the About button.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The click event data.
        /// </param>
        /// <remarks>
        /// Displays the AboutDialog to provide the user with
        /// information about the application.
        /// Asynchronously waits until the dialog is closed.
        /// </remarks>
        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AboutDialog
            {
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
