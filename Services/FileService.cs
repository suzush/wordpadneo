using wordpadneo.Helpers;
using wordpadneo.Models;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace wordpadneo.Services
{
    /// <summary>
    /// Provides functionality for opening, saving, and confirming
    /// the closure of document files.
    /// </summary>
    /// <remarks>
    /// Supports both RTF and plain text file formats.
    /// Uses Windows file pickers for file selection and displays
    /// a save confirmation dialog when closing a document, if necessary.
    /// </remarks>
    internal class FileService
    {
        /// <summary>
        /// Displays a file picker dialog and loads the selected document
        /// into the RichEditBox.
        /// </summary>
        /// <param name="editor">
        /// The RichEditBox in which the loaded document is displayed.
        /// </param>
        /// <param name="pageSettings">
        /// The page settings to update with paper size and margin
        /// information from an RTF document.
        /// </param>
        /// <param name="windowHandle">
        /// The handle of the parent window for the file picker dialog.
        /// </param>
        /// <returns>
        /// The selected StorageFile if loading succeeds.
        /// Null if file selection is canceled or the file format
        /// is not supported.
        /// </returns>
        /// <remarks>
        /// For RTF files, loads the document content and page settings.
        /// For TXT files, loads the document as plain text.
        /// File loading is performed asynchronously.
        /// </remarks>
        public static async Task<StorageFile> OpenFileAsync(
            RichEditBox editor,
            PageSettings pageSettings,
            IntPtr windowHandle)
        {
            FileOpenPicker openPicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            openPicker.FileTypeFilter.Add(".rtf");
            openPicker.FileTypeFilter.Add(".txt");

            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, windowHandle);

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file == null) return null;

            string contentToLoad = await FileIO.ReadTextAsync(file);

            switch (file.FileType.ToLower())
            {
                case ".rtf":
                    RtfHelper.SetFormattedRtf(editor, contentToLoad, pageSettings);
                    break;

                case ".txt":
                    editor.Document.SetText(TextSetOptions.None, contentToLoad);
                    break;

                default:
                    return null;
            }

            return file;
        }

        /// <summary>
        /// Saves the document contained in the RichEditBox
        /// to the specified file.
        /// </summary>
        /// <param name="editor">
        /// The RichEditBox containing the document to save.
        /// </param>
        /// <param name="pageSettings">
        /// The page settings to include when saving in RTF format.
        /// </param>
        /// <param name="windowHandle">
        /// The handle of the calling window.
        /// This parameter is not used in the current implementation.
        /// </param>
        /// <param name="file">
        /// The StorageFile representing the save destination.
        /// </param>
        /// <returns>
        /// The destination StorageFile if saving succeeds.
        /// Null if the file format is not supported.
        /// </returns>
        /// <remarks>
        /// For RTF files, generates and saves an RTF string
        /// containing the page settings.
        /// For TXT files, saves the document as plain text
        /// without formatting information.
        /// Any existing content in the destination file is overwritten.
        /// File writing is performed asynchronously.
        /// </remarks>
        public static async Task<StorageFile> SaveFileAsync(
            RichEditBox editor,
            PageSettings pageSettings,
            IntPtr windowHandle,
            StorageFile file)
        {
            string contentToSave = string.Empty;

            switch (file.FileType.ToLower())
            {
                case ".rtf":
                    contentToSave = RtfHelper.GetFormattedRtf(editor, pageSettings);
                    break;

                case ".txt":
                    editor.Document.GetText(TextGetOptions.None, out contentToSave);
                    break;

                default:
                    return null;
            }

            await FileIO.WriteTextAsync(file, contentToSave);

            return file;
        }

        /// <summary>
        /// Displays the Save As dialog and saves the document
        /// contained in the RichEditBox to the specified file.
        /// </summary>
        /// <param name="editor">
        /// The RichEditBox containing the document to save.
        /// </param>
        /// <param name="pageSettings">
        /// The page settings to apply when saving in RTF format.
        /// </param>
        /// <param name="windowHandle">
        /// The handle of the parent window for the file save dialog.
        /// </param>
        /// <returns>
        /// The destination StorageFile if the save operation is performed.
        /// Null if file selection is canceled.
        /// </returns>
        /// <remarks>
        /// Allows the user to select either RTF or TXT format.
        /// Uses the Documents library as the initial save location
        /// and "New Document" as the default file name.
        /// Uses the SaveFileAsync method to perform the actual file writing.
        /// </remarks>
        public static async Task<StorageFile> SaveAsAsync(
            RichEditBox editor,
            PageSettings pageSettings,
            IntPtr windowHandle)
        {
            FileSavePicker savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = "New Document"
            };

            savePicker.FileTypeChoices.Add("Rich Text Document", new List<string>() { ".rtf" });
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });

            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file == null) return null;

            await SaveFileAsync(editor, pageSettings, windowHandle, file);

            return file;
        }

        /// <summary>
        /// Checks whether the document has unsaved changes before closing
        /// and displays a save confirmation dialog if necessary.
        /// </summary>
        /// <param name="xamlRoot">
        /// The XamlRoot used to display the save confirmation dialog.
        /// </param>
        /// <param name="editor">
        /// The RichEditBox containing the document to close.
        /// </param>
        /// <param name="isFileSaved">
        /// Indicates whether the document has been saved.
        /// If true, closing is permitted without prompting for confirmation.
        /// </param>
        /// <param name="currentFile">
        /// The file currently being edited.
        /// This parameter is not used in the current implementation.
        /// </param>
        /// <param name="pageSettings">
        /// The page settings used when saving the document.
        /// </param>
        /// <param name="windowHandle">
        /// The handle of the parent window for the save dialog.
        /// </param>
        /// <returns>
        /// True if the document can be closed.
        /// False if the save dialog is canceled or no save destination
        /// is selected.
        /// </returns>
        /// <remarks>
        /// If unsaved changes exist, displays three options:
        /// Save, Don't Save, and Cancel.
        ///
        /// If Save is selected, calls SaveAsAsync and permits closing
        /// when a save destination is selected.
        ///
        /// If Don't Save is selected, permits closing without saving
        /// the changes.
        ///
        /// If Cancel is selected, aborts the closing operation.
        /// </remarks>
        public static async Task<bool> CloseDocumentAsync(
            XamlRoot xamlRoot,
            RichEditBox editor,
            bool isFileSaved,
            StorageFile currentFile,
            PageSettings pageSettings,
            IntPtr windowHandle)
        {
            editor.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string text);
            if (!isFileSaved)
            {
                ContentDialog exitDialog = new ContentDialog
                {
                    XamlRoot = xamlRoot,
                    Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                    Title = LocalizationHelper.GetString("CloseDocumentDialog_Title"),
                    Content = LocalizationHelper.GetString("CloseDocumentDialog_Content"),
                    PrimaryButtonText = LocalizationHelper.GetString("CloseDocumentDialog_SaveButton"),
                    SecondaryButtonText = LocalizationHelper.GetString("CloseDocumentDialog_DontSaveButton"),
                    CloseButtonText = LocalizationHelper.GetString("Common_Cancel"),
                    DefaultButton = ContentDialogButton.Primary
                };

                ContentDialogResult result = await exitDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    StorageFile file = await FileService.SaveAsAsync(editor, pageSettings, windowHandle);
                    if (file != null)
                    {
                        return true;
                    }
                    return false;
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}