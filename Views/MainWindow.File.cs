
using wordpadneo.Constants;
using wordpadneo.Dialogs;
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
    /// メインウィンドウのファイル操作、ページ設定、
    /// アプリケーション終了処理を管理します。
    /// </summary>
    partial class MainWindow
    {
        /// <summary>
        /// メインウィンドウのネイティブウィンドウハンドルを取得します。
        /// </summary>
        /// <value>
        /// メインウィンドウを表すウィンドウハンドル。
        /// </value>
        /// <remarks>
        /// ファイル選択ダイアログなどの親ウィンドウを
        /// 指定する際に使用します。
        /// </remarks>
        private IntPtr WindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(this);

        /// <summary>
        /// 現在編集中のファイル名に基づいてウィンドウタイトルを更新します。
        /// </summary>
        /// <remarks>
        /// ファイルが開かれている場合はそのファイル名を使用します。
        /// 新規文書の場合は「ドキュメント」を使用し、
        /// アプリケーション名「WordPad Neo」を付加して表示します。
        /// </remarks>
        public void UpdateWindowTitle()
        {
            string fileName = _currentFile != null ? _currentFile.Name : "ドキュメント";
            Title = $"{fileName} - WordPad Neo";
        }

        /// <summary>
        /// RichEditBoxの元に戻す操作の履歴を初期化します。
        /// </summary>
        /// <remarks>
        /// UndoLimitを一度0に設定して履歴をクリアした後、
        /// 最大500回分の操作履歴を保持できるように設定します。
        /// エディターまたは文書が存在しない場合は処理を行いません。
        /// </remarks>
        private void ResetUndoLimit()
        {
            if (editor?.Document == null) return;

            editor.Document.UndoLimit = 0;
            editor.Document.UndoLimit = 500;
        }

        /// <summary>
        /// 新規文書を作成するボタンのクリックイベントを処理します。
        /// </summary>
        /// <param name="sender">
        /// イベントを発生させたオブジェクト。
        /// </param>
        /// <param name="e">
        /// クリックイベントのデータ。
        /// </param>
        /// <remarks>
        /// 現在の文書に未保存の変更がある場合は、
        /// 新規文書を作成する前に保存確認を行います。
        /// 終了が許可された場合は文書を初期化し、
        /// ファイル情報、操作履歴、保存状態および
        /// ウィンドウタイトルを更新します。
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
        /// ファイルを開くボタンのクリックイベントを処理します。
        /// </summary>
        /// <param name="sender">
        /// イベントを発生させたオブジェクト。
        /// </param>
        /// <param name="e">
        /// クリックイベントのデータ。
        /// </param>
        /// <remarks>
        /// ファイル選択ダイアログを表示し、
        /// 選択されたRTFまたはTXT形式の文書を読み込みます。
        /// 読み込みに成功した場合は現在のファイル情報を更新し、
        /// 操作履歴と保存状態を初期化します。
        /// 最後にページレイアウトを更新します。
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
        /// 上書き保存ボタンのクリックイベントを処理します。
        /// </summary>
        /// <param name="sender">
        /// イベントを発生させたオブジェクト。
        /// </param>
        /// <param name="e">
        /// クリックイベントのデータ。
        /// </param>
        /// <remarks>
        /// 現在のファイルが存在する場合は上書き保存します。
        /// 新規文書の場合は名前を付けて保存ダイアログを表示します。
        /// 保存に成功した場合はファイル情報を更新し、
        /// 操作履歴を初期化して保存済み状態に変更します。
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
        /// 名前を付けて保存ボタンのクリックイベントを処理します。
        /// </summary>
        /// <param name="sender">
        /// イベントを発生させたオブジェクト。
        /// </param>
        /// <param name="e">
        /// クリックイベントのデータ。
        /// </param>
        /// <remarks>
        /// ファイル保存ダイアログを表示し、
        /// 保存先とファイル形式を選択して文書を保存します。
        /// 保存に成功した場合は現在のファイル情報、
        /// 操作履歴、保存状態およびウィンドウタイトルを更新します。
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
        /// メインウィンドウが閉じられる際のイベントを処理します。
        /// </summary>
        /// <param name="sender">
        /// 終了処理を開始したAppWindow。
        /// </param>
        /// <param name="args">
        /// ウィンドウ終了処理を制御するイベントデータ。
        /// </param>
        /// <remarks>
        /// いったんウィンドウの終了をキャンセルし、
        /// 未保存の変更がある場合は保存確認を行います。
        /// 終了が許可された場合は保存済み状態に変更し、
        /// Closingイベントの登録を解除してウィンドウを閉じます。
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
        /// アプリケーション終了ボタンのクリックイベントを処理します。
        /// </summary>
        /// <param name="sender">
        /// イベントを発生させたオブジェクト。
        /// </param>
        /// <param name="e">
        /// クリックイベントのデータ。
        /// </param>
        /// <remarks>
        /// ウィンドウを閉じる前に文書の保存状態を確認します。
        /// 未保存の変更がある場合は保存確認ダイアログを表示します。
        /// 終了が許可された場合はClosingイベントの登録を解除し、
        /// メインウィンドウを閉じます。
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
        /// ページ設定ボタンのクリックイベントを処理します。
        /// </summary>
        /// <param name="sender">
        /// イベントを発生させたオブジェクト。
        /// </param>
        /// <param name="e">
        /// クリックイベントのデータ。
        /// </param>
        /// <remarks>
        /// ページ設定ダイアログを表示し、
        /// 用紙サイズや余白の設定変更を受け付けます。
        /// 設定が確定された場合は左右の余白をピクセル単位に変換し、
        /// RichEditBoxのPaddingへ反映します。
        /// その後、ルーラーとページレイアウトを更新します。
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
        /// アプリケーション情報ボタンのクリックイベントを処理します。
        /// </summary>
        /// <param name="sender">
        /// イベントを発生させたオブジェクト。
        /// </param>
        /// <param name="e">
        /// クリックイベントのデータ。
        /// </param>
        /// <remarks>
        /// AboutDialogを表示し、
        /// アプリケーションに関する情報をユーザーに提示します。
        /// ダイアログが閉じられるまで非同期で待機します。
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
