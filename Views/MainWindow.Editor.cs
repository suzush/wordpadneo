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
    /// Partial class implementing text formatting, editing, find and replace, and printing features for the main window.
    /// </summary>
    partial class MainWindow
    {
        /// <summary>
        /// Retrieves system-installed font families and populates the font selector in alphabetical order.
        /// </summary>
        private void PopulateFontList()
        {
            string[] fontFamilies = CanvasTextFormat.GetSystemFontFamilies();
            if (fontComboBox != null)
            {
                fontComboBox.Items.Clear();
                foreach (string family in fontFamilies
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase))
                {
                    fontComboBox.Items.Add(family);
                }
            }
        }

        /// <summary>
        /// Populates the font size selector with a predefined list of font sizes.
        /// </summary>
        private void PopulateFontSizeList()
        {
            List<string> fontSizes = new List<string>
            {
                "8", "9", "10", "10.5", "11", "12", "14",
                "16", "18", "20", "22", "24", "26", "28",
                "36", "48", "72"
            };

            if (fontSizeComboBox != null)
            {
                fontSizeComboBox.Items.Clear();

                foreach (string fontSize in fontSizes)
                {
                    fontSizeComboBox.Items.Add(fontSize);
                }
            }
        }

        /// <summary>
        /// Updates the editor and ruler widths, margins, and drawing positions based on the page settings.
        /// </summary>
        /// <remarks>Converts page dimensions and horizontal margins from centimeters to pixels, then redraws the ruler.</remarks>
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
        /// Initializes the default character formatting, page layout, undo settings, and window title when the editor loads.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
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
        /// Reads the selected text formatting and paragraph alignment, then updates the corresponding UI controls and page layout.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        /// <remarks>Skips processing while the update-in-progress flag is set.</remarks>
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
        /// Checks whether the document has been modified and updates the saved-state flag and window title when necessary.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        /// <remarks>The current implementation determines the modified state based on whether an undo operation is available.</remarks>
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
        /// Applies the font chosen in the font selector to the current selection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
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
        /// Converts the selected font size to a numeric value and applies it to the current selection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
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
        /// Toggles bold formatting for the selected text according to the bold button state.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
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
        /// Toggles italic formatting for the selected text according to the italic button state.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
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
        /// Applies or removes a single underline on the selected text according to the underline button state.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
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
        /// Toggles strikethrough formatting for the selected text according to the strikethrough button state.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
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
        /// Applies the color selected in the font color picker to the selected text.
        /// </summary>
        /// <param name="sender">The ColorPicker whose color changed.</param>
        /// <param name="args">Event arguments containing the new color.</param>
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
        /// Applies the color selected in the background color picker to the background of the selected text.
        /// </summary>
        /// <param name="sender">The ColorPicker whose color changed.</param>
        /// <param name="args">Event arguments containing the new color.</param>
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
        /// Sets the paragraph alignment of the current selection and updates the alignment buttons.
        /// </summary>
        /// <param name="alignment">The paragraph alignment to apply.</param>
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
        /// Updates the left, center, right, and justify button states to match the specified paragraph alignment.
        /// </summary>
        /// <param name="alignment">The paragraph alignment to reflect in the button states.</param>
        private void UpdateAlignmentButtonStates(ParagraphAlignment alignment)
        {
            alignLeftButton.IsChecked = (alignment == ParagraphAlignment.Left);
            alignCenterButton.IsChecked = (alignment == ParagraphAlignment.Center);
            alignRightButton.IsChecked = (alignment == ParagraphAlignment.Right);
            alignJustifyButton.IsChecked = (alignment == ParagraphAlignment.Justify);
        }

        /// <summary>
        /// Left-aligns the selected paragraphs when the left-align button is clicked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        private void AlignLeftButton_Click(object sender, RoutedEventArgs e)
        {
            SetParagraphAlignment(ParagraphAlignment.Left);
        }

        /// <summary>
        /// Centers the selected paragraphs when the center-align button is clicked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        private void AlignCenterButton_Click(object sender, RoutedEventArgs e)
        {
            SetParagraphAlignment(ParagraphAlignment.Center);
        }

        /// <summary>
        /// Right-aligns the selected paragraphs when the right-align button is clicked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        private void AlignRightButton_Click(object sender, RoutedEventArgs e)
        {
            SetParagraphAlignment(ParagraphAlignment.Right);
        }

        /// <summary>
        /// Justifies the selected paragraphs when the justify button is clicked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        private void AlignJustifyButton_Click(object sender, RoutedEventArgs e)
        {
            SetParagraphAlignment(ParagraphAlignment.Justify);
        }

        /// <summary>
        /// Inserts a tab character when Tab is pressed in the editor and marks the key event as handled.
        /// </summary>
        /// <param name="sender">The source of the keyboard event.</param>
        /// <param name="e">Event arguments containing the pressed key and handled state.</param>
        private void Editor_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                editor.Document.Selection.TypeText("\t");
                e.Handled = true;
            }
        }

        /// <summary>
        /// Undoes the most recent edit if undo is available.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (editor.Document.CanUndo())
            {
                editor.Document.Undo();
            }
        }

        /// <summary>
        /// Redoes the previously undone edit if redo is available.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            if (editor.Document.CanRedo())
            {
                editor.Document.Redo();
            }
        }

        /// <summary>
        /// Cuts the current selection to the clipboard.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        private void CutButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.Cut();
        }

        /// <summary>
        /// Copies the current selection to the clipboard.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.Copy();
        }

        /// <summary>
        /// Retrieves text from the clipboard, if available, and pastes it into the current selection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        /// <remarks>Retrieves clipboard text and sets it using the FormatRtf option, as in the existing implementation.</remarks>
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
        /// Displays the Find dialog and searches forward or backward for the entered text.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        /// <remarks>Stores the search term so it can be reused the next time the dialog is opened.</remarks>
        private async void FindButton_Click(object sender, RoutedEventArgs e)

        {
            TextBox searchTextBox = new TextBox
            {
                PlaceholderText = LocalizationHelper.GetString("FindDialog_SearchPlaceholder"),
                Width = 280,
                Margin = new Thickness(0, 10, 0, 0),
                Text = _lastSearchKeyword
            };

            ContentDialog searchDialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = LocalizationHelper.GetString("FindDialog_Title"),
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = LocalizationHelper.GetString("FindDialog_Prompt") },
                        searchTextBox
                    }
                },
                PrimaryButtonText = LocalizationHelper.GetString("FindDialog_NextButton"),
                SecondaryButtonText = LocalizationHelper.GetString("FindDialog_PreviousButton"),
                CloseButtonText = LocalizationHelper.GetString("Common_Cancel"),
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
        /// Searches case-insensitively for the specified text starting after the end of the selection.
        /// </summary>
        /// <param name="keyword">The text to search for.</param>
        /// <remarks>If no match is found before the end of the document, wraps to the beginning and selects the match.</remarks>
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
                System.Diagnostics.Debug.WriteLine("No matching text was found.");
            }
        }

        /// <summary>
        /// Searches backward, case-insensitively, for the specified text before the start of the selection.
        /// </summary>
        /// <param name="keyword">The text to search for.</param>
        /// <remarks>If no earlier match is found, wraps to the end of the document and selects the match.</remarks>
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
                System.Diagnostics.Debug.WriteLine("No matching text was found.");
            }
        }

        /// <summary>
        /// Displays the Replace dialog and replaces one occurrence or all occurrences of the specified text.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        /// <remarks>Replace changes the matching range; Replace All resets the entire document as plain text.</remarks>
        private async void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox findBox = new TextBox { PlaceholderText = LocalizationHelper.GetString("ReplaceDialog_FindPlaceholder"), Width = 280, Margin = new Thickness(0, 5, 0, 10), Text = _lastSearchKeyword };
            TextBox replaceBox = new TextBox { PlaceholderText = LocalizationHelper.GetString("ReplaceDialog_ReplacePlaceholder"), Width = 280, Margin = new Thickness(0, 5, 0, 0), Text = _lastReplaceKeyword };

            ContentDialog replaceDialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = LocalizationHelper.GetString("ReplaceDialog_Title"),
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = LocalizationHelper.GetString("ReplaceDialog_FindLabel") },
                        findBox,
                        new TextBlock { Text = LocalizationHelper.GetString("ReplaceDialog_ReplaceLabel") },
                        replaceBox
                    }
                },
                PrimaryButtonText = LocalizationHelper.GetString("ReplaceDialog_ReplaceButton"),
                SecondaryButtonText = LocalizationHelper.GetString("ReplaceDialog_ReplaceAllButton"),
                CloseButtonText = LocalizationHelper.GetString("Common_Cancel"),
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
        /// Holds the settings passed to the Windows extended print dialog and the resulting print selection.
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
        /// Displays the Windows extended print dialog.
        /// </summary>
        /// <param name="pdex">Structure containing the print dialog settings and results.</param>
        /// <returns>The HRESULT value returned by the Windows API.</returns>
        [DllImport("comdlg32.dll", EntryPoint = "PrintDlgExW", CharSet = CharSet.Unicode)]
        private static extern int PrintDlgEx(ref PRINTDLGEX pdex);

        /// <summary>
        /// Releases the specified GDI device context.
        /// </summary>
        /// <param name="hdc">Handle to the device context to release.</param>
        /// <returns>True if the device context was released successfully; otherwise, false.</returns>
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr hdc);

        /// <summary>
        /// Frees memory allocated as global memory.
        /// </summary>
        /// <param name="memory">Handle to the global memory block to free.</param>
        /// <returns>IntPtr.Zero on success; otherwise, the original handle.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr memory);

        private const uint PD_RETURNDC = 0x00000100;
        private const uint PD_NOPAGENUMS = 0x00000008;
        private const uint PD_RESULT_PRINT = 1;

        /// <summary>
        /// Displays the extended print dialog and prints the document using the selected printer.
        /// </summary>
        /// <remarks>Logs the print result and releases device contexts and memory returned by the dialog in the finally block.</remarks>
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
                    editor, pdex.hDC, _pageSettings, LocalizationHelper.GetString("MainWindow_PrintJobName"));
                System.Diagnostics.Debug.WriteLine(printed ? "Printing completed" : "Printing failed");
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
        /// Runs the print operation when the Print button is clicked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing information about the event.</param>
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            ExecutePrint();
        }
    }
}
