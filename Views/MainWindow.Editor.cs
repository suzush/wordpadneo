using wordpadneo.Constants;
using wordpadneo.Helpers;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.System;
using WinRT.Interop;

namespace wordpadneo.Views
{
    /// <summary>
    /// メインウィンドウの文字書式、編集、検索・置換、印刷機能を実装する部分クラスです。
    /// </summary>
    partial class MainWindow
    {
        /// <summary>
        /// システムにインストールされているフォントファミリーを取得し、フォント選択欄に名前順で設定します。
        /// </summary>
        private void PopulateFontList()
        {
            string[] fontFamilies = CanvasTextFormat.GetSystemFontFamilies();
            if (fontComboBox != null)
            {
                fontComboBox.ItemsSource = fontFamilies.OrderBy(f => f).ToList();
            }
        }

        /// <summary>
        /// あらかじめ定義されたフォントサイズの一覧をフォントサイズ選択欄に設定します。
        /// </summary>
        private void PopulateFontSizeList()
        {
            List<string> fontSizes = new List<string>
            {
                "8", "9", "10", "10.5", "11", "12", "14", "16", "18", "20", "22", "24", "26", "28", "36", "48", "72"
            };
            if (fontSizeComboBox != null)
            {
                fontSizeComboBox.ItemsSource = fontSizes;
            }
        }

        /// <summary>
        /// ページ設定に従ってエディターとルーラーの幅、余白、描画位置を更新します。
        /// </summary>
        /// <remarks>ページ寸法と左右余白をセンチメートルからピクセルに変換し、最後にルーラーを再描画します。</remarks>
        private void UpdatePageLayout()
        {
            if (editor == null || rulerCanvas == null) return;

            double pageWidthPx = _pageSettings.PageWidthCm * PageConstants.PxPerCm;

            rulerCanvas.Width = pageWidthPx;
            editor.Width = pageWidthPx;

            double paddingLeftPx = _pageSettings.MarginLeftCm * PageConstants.PxPerCm;
            double paddingRightPx = _pageSettings.MarginRightCm * PageConstants.PxPerCm;
            editor.Padding = new Thickness(paddingLeftPx, 12, paddingRightPx, 12);

            _rulerLeft = paddingLeftPx;
            _rulerRight = pageWidthPx - paddingRightPx;

            DrawRuler();
        }

        /// <summary>
        /// エディター読み込み時に既定の文字書式、ページレイアウト、元に戻す設定およびウィンドウタイトルを初期化します。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void Editor_Loaded(object sender, RoutedEventArgs e)
        {
            ITextCharacterFormat defaultFormat = editor.Document.GetDefaultCharacterFormat();
            defaultFormat.Name = "Yu Gothic";
            defaultFormat.Size = 10.5F;
            editor.Document.SetDefaultCharacterFormat(defaultFormat);

            if (fontComboBox != null)
            {
                fontComboBox.SelectedItem = defaultFormat.Name;
            }

            UpdatePageLayout();
            ResetUndoLimit();
            _isFileSaved = true;
            UpdateWindowTitle();
        }

        /// <summary>
        /// 選択範囲の文字書式と段落の配置を取得し、対応するUIの選択状態とページレイアウトを更新します。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        /// <remarks>更新処理中を示すフラグが有効な場合は処理しません。</remarks>
        private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdating) return;

            ITextSelection selectedText = editor.Document.Selection;
            if (selectedText == null) return;

            ITextCharacterFormat characterFormat = selectedText.CharacterFormat;
            if (boldButton != null) boldButton.IsChecked = (characterFormat.Bold == FormatEffect.On);
            if (italicButton != null) italicButton.IsChecked = (characterFormat.Italic == FormatEffect.On);
            if (underlineButton != null) underlineButton.IsChecked = (characterFormat.Underline != UnderlineType.None);
            if (strikethroughButton != null) strikethroughButton.IsChecked = (characterFormat.Strikethrough == FormatEffect.On);

            if (fontComboBox != null && fontComboBox.SelectedItem?.ToString() != characterFormat.Name)
            {
                fontComboBox.SelectedItem = characterFormat.Name;
            }

            if (fontSizeComboBox != null && fontSizeComboBox.SelectedItem?.ToString() != characterFormat.Size.ToString())
            {
                fontSizeComboBox.SelectedItem = characterFormat.Size.ToString();
            }

            ITextParagraphFormat paragraphFormat = selectedText.ParagraphFormat;
            if (alignLeftButton != null) alignLeftButton.IsChecked = (paragraphFormat.Alignment == ParagraphAlignment.Left);
            if (alignCenterButton != null) alignCenterButton.IsChecked = (paragraphFormat.Alignment == ParagraphAlignment.Center);
            if (alignRightButton != null) alignRightButton.IsChecked = (paragraphFormat.Alignment == ParagraphAlignment.Right);
            if (alignJustifyButton != null) alignJustifyButton.IsChecked = (paragraphFormat.Alignment == ParagraphAlignment.Justify);

            UpdatePageLayout();
        }

        /// <summary>
        /// 文書の変更状態を確認し、必要に応じて保存済みフラグとウィンドウタイトルを更新します。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        /// <remarks>現在の実装では、元に戻せる操作の有無を変更状態の判定に使用します。</remarks>
        private void Editor_TextChanged(object sender, RoutedEventArgs e)
        {
            bool isModified = editor.Document.CanUndo();

            if (_isFileSaved == isModified)
            {
                _isFileSaved = !isModified;
                UpdateWindowTitle();
            }
        }

        /// <summary>
        /// フォント選択欄で選ばれたフォントを現在の選択範囲に適用します。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fontComboBox.SelectedItem is string selectedFontName)
            {
                ITextSelection selectedText = editor.Document.Selection;
                if (selectedText != null)
                {
                    ITextCharacterFormat characterFormat = selectedText.CharacterFormat;
                    characterFormat.Name = selectedFontName;
                    selectedText.CharacterFormat = characterFormat;
                }
            }
        }

        /// <summary>
        /// フォントサイズ選択欄の値を数値に変換し、現在の選択範囲に適用します。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fontSizeComboBox.SelectedItem is string selectedFontSizeStr && float.TryParse(selectedFontSizeStr, out float selectedFontSize))
            {
                ITextSelection selectedText = editor.Document.Selection;
                if (selectedText != null)
                {
                    ITextCharacterFormat characterFormat = selectedText.CharacterFormat;
                    characterFormat.Size = selectedFontSize;
                    selectedText.CharacterFormat = characterFormat;
                }
            }
        }

        /// <summary>
        /// 太字ボタンの状態に応じて、選択範囲の太字設定を切り替えます。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                ITextCharacterFormat characterFormat = selectedText.CharacterFormat;
                characterFormat.Bold = boldButton.IsChecked == true ? FormatEffect.On : FormatEffect.Off;
                selectedText.CharacterFormat = characterFormat;
            }
        }

        /// <summary>
        /// 斜体ボタンの状態に応じて、選択範囲の斜体設定を切り替えます。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                ITextCharacterFormat characterFormat = selectedText.CharacterFormat;
                characterFormat.Italic = italicButton.IsChecked == true ? FormatEffect.On : FormatEffect.Off;
                selectedText.CharacterFormat = characterFormat;
            }
        }

        /// <summary>
        /// 下線ボタンの状態に応じて、選択範囲に単線の下線を設定または解除します。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void UnderlineButton_Click(object sender, RoutedEventArgs e)
        {
            ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                ITextCharacterFormat characterFormat = selectedText.CharacterFormat;
                characterFormat.Underline = (underlineButton.IsChecked == true)
                    ? UnderlineType.Single
                    : UnderlineType.None;
                selectedText.CharacterFormat = characterFormat;
            }
        }

        /// <summary>
        /// 取り消し線ボタンの状態に応じて、選択範囲の取り消し線を切り替えます。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void StrikethroughButton_Click(object sender, RoutedEventArgs e)
        {
            ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                ITextCharacterFormat characterFormat = selectedText.CharacterFormat;
                characterFormat.Strikethrough = strikethroughButton.IsChecked == true ? FormatEffect.On : FormatEffect.Off;
                selectedText.CharacterFormat = characterFormat;
            }
        }

        /// <summary>
        /// 文字色ピッカーで選ばれた色を現在の選択範囲の文字色に反映します。
        /// </summary>
        /// <param name="sender">色が変更されたColorPicker。</param>
        /// <param name="args">新しい色を含むイベント引数。</param>
        private void FontColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                ITextCharacterFormat characterFormat = selectedText.CharacterFormat;
                characterFormat.ForegroundColor = args.NewColor;
                selectedText.CharacterFormat = characterFormat;
            }
        }

        /// <summary>
        /// 背景色ピッカーで選ばれた色を現在の選択範囲の文字背景色に反映します。
        /// </summary>
        /// <param name="sender">色が変更されたColorPicker。</param>
        /// <param name="args">新しい色を含むイベント引数。</param>
        private void BackgroundColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                ITextCharacterFormat characterFormat = selectedText.CharacterFormat;
                characterFormat.BackgroundColor = args.NewColor;
                selectedText.CharacterFormat = characterFormat;
            }
        }

        /// <summary>
        /// 現在の選択範囲の段落配置を指定された配置に変更し、配置ボタンの状態を更新します。
        /// </summary>
        /// <param name="alignment">設定する段落配置。</param>
        private void SetParagraphAlignment(ParagraphAlignment alignment)
        {
            ITextSelection selectedText = editor.Document.Selection;
            if (selectedText != null)
            {
                ITextParagraphFormat paragraphFormat = selectedText.ParagraphFormat;
                paragraphFormat.Alignment = alignment;
                selectedText.ParagraphFormat = paragraphFormat;
                UpdateAlignmentButtonStates(alignment);
            }
        }

        /// <summary>
        /// 指定された段落配置に合わせて、左寄せ・中央揃え・右寄せ・両端揃えボタンの状態を更新します。
        /// </summary>
        /// <param name="alignment">選択状態を反映する段落配置。</param>
        private void UpdateAlignmentButtonStates(ParagraphAlignment alignment)
        {
            alignLeftButton.IsChecked = (alignment == ParagraphAlignment.Left);
            alignCenterButton.IsChecked = (alignment == ParagraphAlignment.Center);
            alignRightButton.IsChecked = (alignment == ParagraphAlignment.Right);
            alignJustifyButton.IsChecked = (alignment == ParagraphAlignment.Justify);
        }

        /// <summary>
        /// 左寄せボタンのクリックに応じて、選択範囲の段落を左寄せにします。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void AlignLeftButton_Click(object sender, RoutedEventArgs e)
        {
            SetParagraphAlignment(ParagraphAlignment.Left);
        }

        /// <summary>
        /// 中央揃えボタンのクリックに応じて、選択範囲の段落を中央揃えにします。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void AlignCenterButton_Click(object sender, RoutedEventArgs e)
        {
            SetParagraphAlignment(ParagraphAlignment.Center);
        }

        /// <summary>
        /// 右寄せボタンのクリックに応じて、選択範囲の段落を右寄せにします。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void AlignRightButton_Click(object sender, RoutedEventArgs e)
        {
            SetParagraphAlignment(ParagraphAlignment.Right);
        }

        /// <summary>
        /// 両端揃えボタンのクリックに応じて、選択範囲の段落を両端揃えにします。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void AlignJustifyButton_Click(object sender, RoutedEventArgs e)
        {
            SetParagraphAlignment(ParagraphAlignment.Justify);
        }

        /// <summary>
        /// エディターでTabキーが押された場合にタブ文字を挿入し、キー入力を処理済みにします。
        /// </summary>
        /// <param name="sender">キー入力イベントの発生元。</param>
        /// <param name="e">押されたキーと処理済み状態を保持するイベント引数。</param>
        private void Editor_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                editor.Document.Selection.TypeText("\t");
                e.Handled = true;
            }
        }

        /// <summary>
        /// 元に戻す操作が可能な場合、直前の編集操作を取り消します。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (editor.Document.CanUndo())
            {
                editor.Document.Undo();
            }
        }

        /// <summary>
        /// やり直し操作が可能な場合、取り消した編集操作を再実行します。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            if (editor.Document.CanRedo())
            {
                editor.Document.Redo();
            }
        }

        /// <summary>
        /// 現在の選択範囲を切り取ってクリップボードに移します。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void CutButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.Cut();
        }

        /// <summary>
        /// 現在の選択範囲をクリップボードにコピーします。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.Copy();
        }

        /// <summary>
        /// クリップボードにテキストがある場合、取得して現在の選択範囲に貼り付けます。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        /// <remarks>クリップボードのテキストを取得し、既存実装どおりFormatRtfオプションを指定して設定します。</remarks>
        private async void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Focus(FocusState.Programmatic);

            var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                string text = await dataPackageView.GetTextAsync();
                editor.Document.Selection.SetText(Microsoft.UI.Text.TextSetOptions.FormatRtf, text);
            }
        }

        /// <summary>
        /// 検索ダイアログを表示し、入力された語句を前方または後方へ検索します。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        /// <remarks>検索語は次回のダイアログ表示時に再利用できるように保存します。</remarks>
        private async void FindButton_Click(object sender, RoutedEventArgs e)

        {
            TextBox searchTextBox = new TextBox
            {
                PlaceholderText = "検索するワードを入力...",
                Width = 280,
                Margin = new Thickness(0, 10, 0, 0),
                Text = _lastSearchKeyword
            };

            ContentDialog searchDialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "検索",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "探したいテキストを入力してください：" },
                        searchTextBox
                    }
                },
                PrimaryButtonText = "次を検索",
                SecondaryButtonText = "前を検索",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await searchDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string keyword = searchTextBox.Text;
                _lastSearchKeyword = keyword;
                if (!string.IsNullOrEmpty(keyword))
                {
                    SearchTextNext(keyword);
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                string keyword = searchTextBox.Text;
                _lastSearchKeyword = keyword;
                if (!string.IsNullOrEmpty(keyword))
                {
                    SearchTextPrevious(keyword);
                }
            }
        }

        /// <summary>
        /// 選択範囲の末尾以降から指定語句を大文字・小文字を区別せずに検索します。
        /// </summary>
        /// <param name="keyword">検索する文字列。</param>
        /// <remarks>末尾まで見つからない場合は文書の先頭から再検索し、一致箇所を選択します。</remarks>
        private void SearchTextNext(string keyword)
        {
            var document = editor.Document;
            int startPos = document.Selection.EndPosition;

            document.GetText(TextGetOptions.None, out string fullText);

            int index = fullText.IndexOf(keyword, startPos, StringComparison.OrdinalIgnoreCase);

            if (index == -1)
            {
                index = fullText.IndexOf(keyword, 0, StringComparison.OrdinalIgnoreCase);
            }

            if (index != -1)
            {
                document.Selection.SetRange(index, index + keyword.Length);
                editor.Focus(FocusState.Programmatic);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("一致する文字列が見つかりませんでした。");
            }
        }

        /// <summary>
        /// 選択範囲の先頭より前から指定語句を大文字・小文字を区別せずに逆方向検索します。
        /// </summary>
        /// <param name="keyword">検索する文字列。</param>
        /// <remarks>前方に見つからない場合は文書の末尾側から再検索し、一致箇所を選択します。</remarks>
        private void SearchTextPrevious(string keyword)
        {
            var document = editor.Document;
            int startPos = document.Selection.StartPosition - 1;

            document.GetText(TextGetOptions.None, out string fullText);

            int index = -1;
            if (startPos > 0)
            {
                index = fullText.LastIndexOf(keyword, startPos, StringComparison.OrdinalIgnoreCase);
            }

            if (index == -1)
            {
                index = fullText.LastIndexOf(keyword, fullText.Length - 1, StringComparison.OrdinalIgnoreCase);
            }

            if (index != -1)
            {
                document.Selection.SetRange(index, index + keyword.Length);
                editor.Focus(FocusState.Programmatic);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("一致する文字列が見つかりませんでした。");
            }
        }

        /// <summary>
        /// 置換ダイアログを表示し、指定した文字列を1件または文書全体で置換します。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        /// <remarks>1件置換では該当する範囲の文字列を書き換えます。すべて置換では文書全体をプレーンテキストとして設定し直します。</remarks>
        private async void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox findBox = new TextBox { PlaceholderText = "検索する文字列", Width = 280, Margin = new Thickness(0, 5, 0, 10), Text = _lastSearchKeyword };
            TextBox replaceBox = new TextBox { PlaceholderText = "置換後の文字列", Width = 280, Margin = new Thickness(0, 5, 0, 0), Text = _lastReplaceKeyword };

            ContentDialog replaceDialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "置換",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "検索する文字列:" },
                        findBox,
                        new TextBlock { Text = "置換後の文字列:" },
                        replaceBox
                    }
                },
                PrimaryButtonText = "置換",
                SecondaryButtonText = "すべて置換",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await replaceDialog.ShowAsync();
            string findKey = findBox.Text;
            string replaceKey = replaceBox.Text;

            if (string.IsNullOrEmpty(findKey)) return;

            _lastSearchKeyword = findKey;
            _lastReplaceKeyword = replaceKey;
            var document = editor.Document;

            if (result == ContentDialogResult.Primary)
            {
                document.GetText(TextGetOptions.None, out string fullText);
                int startPos = document.Selection.StartPosition;

                int index = fullText.IndexOf(findKey, startPos, StringComparison.OrdinalIgnoreCase);
                if (index != -1)
                {
                    var range = document.GetRange(index, index + findKey.Length);
                    range.Text = replaceKey;
                    document.Selection.SetRange(index, index + replaceKey.Length);
                    editor.Focus(FocusState.Programmatic);
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                document.GetText(TextGetOptions.None, out string fullText);
                string newText = fullText.Replace(findKey, replaceKey, StringComparison.OrdinalIgnoreCase);

                document.SetText(TextSetOptions.None, newText);
                editor.Focus(FocusState.Programmatic);
            }
        }

        /// <summary>
        /// Windowsの拡張印刷ダイアログに渡す設定と、印刷選択結果を保持する構造体です。
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PRINTDLGEX
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hDevMode;
            public IntPtr hDevNames;
            public IntPtr hDC;
            public uint Flags;
            public uint Flags2;
            public uint ExclusionFlags;
            public uint nPageRanges;
            public uint nMaxPageRanges;
            public IntPtr lpPageRanges;
            public uint nMinPage;
            public uint nMaxPage;
            public uint nCopies;
            public IntPtr hInstance;
            public IntPtr lpPrintTemplateName;
            public IntPtr lpCallback;
            public uint nPropertyPages;
            public IntPtr lphPropertyPages;
            public uint nStartPage;
            public uint dwResultAction;
        }

        /// <summary>
        /// Windowsの拡張印刷ダイアログを表示します。
        /// </summary>
        /// <param name="pdex">印刷ダイアログの設定と結果を保持する構造体。</param>
        /// <returns>Windows APIのHRESULT値。</returns>
        [DllImport("comdlg32.dll", EntryPoint = "PrintDlgExW", CharSet = CharSet.Unicode)]
        private static extern int PrintDlgEx(ref PRINTDLGEX pdex);

        /// <summary>
        /// 指定されたGDIデバイスコンテキストを解放します。
        /// </summary>
        /// <param name="hdc">解放するデバイスコンテキストのハンドル。</param>
        /// <returns>解放に成功した場合はtrue、それ以外はfalse。</returns>
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr hdc);

        /// <summary>
        /// グローバルメモリとして割り当てた領域を解放します。
        /// </summary>
        /// <param name="memory">解放対象のグローバルメモリハンドル。</param>
        /// <returns>成功時はIntPtr.Zero、失敗時は元のハンドル。</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr memory);

        private const uint PD_RETURNDC = 0x00000100;
        private const uint PD_NOPAGENUMS = 0x00000008;
        private const uint PD_RESULT_PRINT = 1;

        /// <summary>
        /// 拡張印刷ダイアログを表示し、選択されたプリンターで文書を印刷します。
        /// </summary>
        /// <remarks>印刷の成否をデバッグ出力し、ダイアログが返したデバイスコンテキストやメモリをfinally節で解放します。</remarks>
        private void ExecutePrint()
        {
            IntPtr hwndOwner = WindowNative.GetWindowHandle(this);
            PRINTDLGEX pdex = new PRINTDLGEX
            {
                lStructSize = Marshal.SizeOf<PRINTDLGEX>(),
                hwndOwner = hwndOwner,
                Flags = PD_RETURNDC | PD_NOPAGENUMS,
                nMinPage = 1,
                nMaxPage = 9999,
                nCopies = 1,
                nStartPage = 0xFFFFFFFF // START_PAGE_GENERAL
            };

            try
            {
                int hr = PrintDlgEx(ref pdex);
                System.Diagnostics.Debug.WriteLine($"[PrintDlgEx] HRESULT=0x{hr:X8}, size={pdex.lStructSize}");
                if (hr != 0) return;
                if (pdex.dwResultAction != PD_RESULT_PRINT) return;
                if (pdex.hDC == IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine("[PrintDlgEx] Print selected, but no printer HDC");
                    return;
                }

                bool printed = PrintHelper.PrintRTF(
                    editor, pdex.hDC, _pageSettings, "ドキュメント印刷");
                System.Diagnostics.Debug.WriteLine(printed ? "印刷完了" : "印刷失敗");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PrintDlgEx] Exception: {ex}");
            }
            finally
            {
                if (pdex.hDC != IntPtr.Zero)
                {
                    if (!DeleteDC(pdex.hDC))
                        System.Diagnostics.Debug.WriteLine("[PrintDlgEx] DeleteDC failed");
                    pdex.hDC = IntPtr.Zero;
                }
                if (pdex.hDevMode != IntPtr.Zero)
                {
                    if (GlobalFree(pdex.hDevMode) != IntPtr.Zero)
                        System.Diagnostics.Debug.WriteLine("[PrintDlgEx] GlobalFree(hDevMode) failed");
                    pdex.hDevMode = IntPtr.Zero;
                }
                if (pdex.hDevNames != IntPtr.Zero)
                {
                    if (GlobalFree(pdex.hDevNames) != IntPtr.Zero)
                        System.Diagnostics.Debug.WriteLine("[PrintDlgEx] GlobalFree(hDevNames) failed");
                    pdex.hDevNames = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// 印刷ボタンのクリックに応じて印刷処理を実行します。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">イベントの情報を保持する引数。</param>
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            ExecutePrint();
        }
    }
}
