
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
    /// 文書ファイルの読み込み、保存、および終了確認を行うクラスです。
    /// </summary>
    /// <remarks>
    /// RTF形式およびプレーンテキスト形式のファイルをサポートします。
    /// ファイル選択にはWindowsのファイルピッカーを使用し、
    /// 文書の終了時には必要に応じて保存確認ダイアログを表示します。
    /// </remarks>
    internal class FileService
    {
        /// <summary>
        /// ファイル選択ダイアログを表示し、選択された文書をRichEditBoxに読み込みます。
        /// </summary>
        /// <param name="editor">
        /// 読み込んだ文書を表示する対象のRichEditBox。
        /// </param>
        /// <param name="pageSettings">
        /// RTF文書に含まれる用紙サイズや余白情報を反映するページ設定。
        /// </param>
        /// <param name="windowHandle">
        /// ファイル選択ダイアログの親ウィンドウを示すハンドル。
        /// </param>
        /// <returns>
        /// 読み込みに成功した場合は選択されたStorageFile。
        /// ファイル選択がキャンセルされた場合、または
        /// 対応していないファイル形式の場合はnull。
        /// </returns>
        /// <remarks>
        /// RTF形式の場合は、文書の内容とページ設定を読み込みます。
        /// TXT形式の場合は、プレーンテキストとして文書を読み込みます。
        /// ファイルの読み込みは非同期で実行されます。
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
        /// RichEditBoxの文書を、指定されたファイルに保存します。
        /// </summary>
        /// <param name="editor">
        /// 保存対象の文書を保持するRichEditBox。
        /// </param>
        /// <param name="pageSettings">
        /// RTF形式で保存する際に文書へ反映するページ設定。
        /// </param>
        /// <param name="windowHandle">
        /// 呼び出し元のウィンドウハンドル。
        /// 現在の実装では使用されません。
        /// </param>
        /// <param name="file">
        /// 保存先となるStorageFile。
        /// </param>
        /// <returns>
        /// 保存に成功した場合は保存先のStorageFile。
        /// 対応していないファイル形式の場合はnull。
        /// </returns>
        /// <remarks>
        /// RTF形式の場合は、ページ設定を含むRTF文字列を生成して保存します。
        /// TXT形式の場合は、書式情報を含まないプレーンテキストとして保存します。
        /// 保存先に既存の内容がある場合は上書きされます。
        /// ファイルへの書き込みは非同期で実行されます。
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
        /// 名前を付けて保存ダイアログを表示し、
        /// RichEditBoxの文書を指定されたファイルに保存します。
        /// </summary>
        /// <param name="editor">
        /// 保存対象の文書を保持するRichEditBox。
        /// </param>
        /// <param name="pageSettings">
        /// RTF形式で保存する際に反映するページ設定。
        /// </param>
        /// <param name="windowHandle">
        /// ファイル保存ダイアログの親ウィンドウを示すハンドル。
        /// </param>
        /// <returns>
        /// 保存処理を実行した場合は保存先のStorageFile。
        /// ファイル選択がキャンセルされた場合はnull。
        /// </returns>
        /// <remarks>
        /// 保存形式としてRTF形式またはTXT形式を選択できます。
        /// 初期保存先にはドキュメントライブラリを指定し、
        /// 既定のファイル名には「New Document」を使用します。
        /// 実際のファイル書き込みにはSaveFileAsyncメソッドを使用します。
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
        /// 文書を閉じる際に未保存の変更があるかを確認し、
        /// 必要に応じて保存確認ダイアログを表示します。
        /// </summary>
        /// <param name="xamlRoot">
        /// 保存確認ダイアログを表示するためのXamlRoot。
        /// </param>
        /// <param name="editor">
        /// 終了対象の文書を保持するRichEditBox。
        /// </param>
        /// <param name="isFileSaved">
        /// 文書が保存済みかどうかを示す値。
        /// trueの場合は保存確認を行わず終了を許可します。
        /// </param>
        /// <param name="currentFile">
        /// 現在編集中のファイル。
        /// 現在の実装では使用されません。
        /// </param>
        /// <param name="pageSettings">
        /// 文書を保存する際に使用するページ設定。
        /// </param>
        /// <param name="windowHandle">
        /// 保存ダイアログの親ウィンドウを示すハンドル。
        /// </param>
        /// <returns>
        /// 文書を閉じてもよい場合はtrue。
        /// 保存ダイアログがキャンセルされた場合や、
        /// 保存先が選択されなかった場合はfalse。
        /// </returns>
        /// <remarks>
        /// 未保存の変更がある場合は、保存、保存しない、
        /// キャンセルの3つの選択肢を表示します。
        ///
        /// 「保存」を選択した場合はSaveAsAsyncを呼び出し、
        /// 保存先が選択されると終了を許可します。
        ///
        /// 「保存しない」を選択した場合は、
        /// 変更内容を保存せずに終了を許可します。
        ///
        /// 「キャンセル」を選択した場合は終了を中止します。
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
                    Title = "作業内容を保存しますか?",
                    Content = "変更内容を保存しない場合、編集内容は失われます。",
                    PrimaryButtonText = "保存",
                    SecondaryButtonText = "保存しない",
                    CloseButtonText = "キャンセル",
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
