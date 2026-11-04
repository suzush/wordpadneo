using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using wordpadneo.Models;

namespace wordpadneo.Helpers
{
    /// <summary>
    /// RichEdit の RTF 文書を Windows の GDI 印刷機能で印刷するためのヘルパークラスです。
    /// </summary>
    public static class PrintHelper
    {
        /// <summary>
        /// 矩形領域の座標を保持する Win32 構造体です。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int left, top, right, bottom; }

        /// <summary>
        /// RichEdit の文字範囲を保持する構造体です。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct CHARRANGE { public int cpMin, cpMax; }

        /// <summary>
        /// RichEdit のページ描画範囲と描画先 DC を指定する構造体です。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct FORMATRANGE
        {
            public IntPtr hdc, hdcTarget;
            public RECT rc, rcPage;
            public CHARRANGE chrg;
        }

        /// <summary>
        /// GDI 印刷ジョブの文書情報を指定する構造体です。
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DOCINFO
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszDocName;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszOutput;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszDatatype;
            public int fwType;
        }

        /// <summary>
        /// RichEdit へのテキスト設定方法とコードページを指定する構造体です。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SETTEXTEX
        {
            public uint flags;
            public uint codepage;
        }

        /// <summary>
        /// 指定された DLL を現在のプロセスに読み込みます。
        /// </summary>
        /// <param name="name">読み込む DLL の名前またはパス。</param>
        /// <returns>成功時はモジュールハンドル、失敗時は <see cref="IntPtr.Zero"/>。</returns>
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string name);

        /// <summary>
        /// 読み込んだ DLL の参照カウントを減らし、必要に応じて解放します。
        /// </summary>
        /// <param name="module">解放するモジュールのハンドル。</param>
        /// <returns>成功した場合は <see langword="true"/>。</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr module);

        /// <summary>
        /// 指定したウィンドウクラスの Win32 ウィンドウを作成します。
        /// </summary>
        /// <param name="exStyle">拡張ウィンドウスタイル。</param>
        /// <param name="className">登録済みウィンドウクラス名。</param>
        /// <param name="windowName">ウィンドウ名。</param>
        /// <param name="style">ウィンドウスタイル。</param>
        /// <param name="x">左上の X 座標。</param>
        /// <param name="y">左上の Y 座標。</param>
        /// <param name="width">ウィンドウの幅。</param>
        /// <param name="height">ウィンドウの高さ。</param>
        /// <param name="parent">親ウィンドウのハンドル。</param>
        /// <param name="menu">メニューまたは子ウィンドウ ID。</param>
        /// <param name="instance">ウィンドウクラスに対応するモジュールハンドル。</param>
        /// <param name="param">作成時に渡す追加データへのポインター。</param>
        /// <returns>成功時はウィンドウハンドル、失敗時は <see cref="IntPtr.Zero"/>。</returns>
        [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            int exStyle, string className, string windowName, int style,
            int x, int y, int width, int height,
            IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

        /// <summary>
        /// Win32 ウィンドウを破棄します。
        /// </summary>
        /// <param name="hwnd">破棄するウィンドウのハンドル。</param>
        /// <returns>成功した場合は <see langword="true"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hwnd);

        /// <summary>
        /// RichEdit に EM_SETTEXTEX メッセージを送信して RTF データを設定します。
        /// </summary>
        /// <param name="hwnd">RichEdit ウィンドウのハンドル。</param>
        /// <param name="message">送信するメッセージ番号。</param>
        /// <param name="options">テキスト設定オプション。</param>
        /// <param name="text">読み込ませるデータへのポインター。</param>
        /// <returns>ウィンドウプロシージャが返す処理結果。</returns>
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr SendMessageSetText(IntPtr hwnd, uint message,
            ref SETTEXTEX options, IntPtr text);

        /// <summary>
        /// RichEdit に EM_FORMATRANGE メッセージを送信し、指定範囲を組版・描画します。
        /// </summary>
        /// <param name="hwnd">RichEdit ウィンドウのハンドル。</param>
        /// <param name="message">送信するメッセージ番号。</param>
        /// <param name="render">描画する場合は 0 以外、計測のみの場合は 0。</param>
        /// <param name="formatRange">描画先および文字範囲の設定。</param>
        /// <returns>処理された最後の文字の次の位置。</returns>
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr SendMessageFormatRange(IntPtr hwnd, uint message,
            IntPtr render, ref FORMATRANGE formatRange);

        /// <summary>
        /// 指定したウィンドウに汎用の Win32 メッセージを同期送信します。
        /// </summary>
        /// <param name="hwnd">送信先ウィンドウのハンドル。</param>
        /// <param name="message">メッセージ番号。</param>
        /// <param name="wParam">メッセージ固有の第 1 パラメーター。</param>
        /// <param name="lParam">メッセージ固有の第 2 パラメーター。</param>
        /// <returns>ウィンドウプロシージャからの戻り値。</returns>
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr SendMessageGeneral(IntPtr hwnd, uint message,
            IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// RichEdit の選択範囲を CHARRANGE 構造体として取得します。
        /// </summary>
        /// <param name="hwnd">RichEdit ウィンドウのハンドル。</param>
        /// <param name="message">送信するメッセージ番号。</param>
        /// <param name="wParam">このメッセージでは使用しないパラメーター。</param>
        /// <param name="range">取得した選択範囲の出力先。</param>
        /// <returns>ウィンドウプロシージャからの戻り値。</returns>
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr SendMessageGetSelection(IntPtr hwnd, uint message,
            IntPtr wParam, out CHARRANGE range);

        /// <summary>
        /// プリンター DC の DPI や印刷可能領域などのデバイス情報を取得します。
        /// </summary>
        /// <param name="hdc">プリンターのデバイスコンテキスト。</param>
        /// <param name="index">取得するデバイス能力の識別子。</param>
        /// <returns>指定した項目の値。項目によって戻り値の意味が異なります。</returns>
        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int index);
        /// <summary>
        /// GDI の印刷ジョブを開始します。
        /// </summary>
        /// <param name="hdc">プリンターのデバイスコンテキスト。</param>
        /// <param name="di">印刷ジョブの文書情報。</param>
        /// <returns>成功時は正のジョブ ID、失敗時は 0 以下の値。</returns>
        [DllImport("gdi32.dll", EntryPoint = "StartDocW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int StartDoc(IntPtr hdc, ref DOCINFO di);
        /// <summary>
        /// 現在の GDI 印刷ジョブを正常終了します。
        /// </summary>
        /// <param name="hdc">プリンターのデバイスコンテキスト。</param>
        /// <returns>成功時は正の値、失敗時は 0 以下の値。</returns>
        [DllImport("gdi32.dll", SetLastError = true)] private static extern int EndDoc(IntPtr hdc);
        /// <summary>
        /// 現在の GDI 印刷ジョブを中止します。
        /// </summary>
        /// <param name="hdc">プリンターのデバイスコンテキスト。</param>
        /// <returns>成功時は正の値、失敗時は 0 以下の値。</returns>
        [DllImport("gdi32.dll", SetLastError = true)] private static extern int AbortDoc(IntPtr hdc);
        /// <summary>
        /// 印刷ジョブ内の新しいページを開始します。
        /// </summary>
        /// <param name="hdc">プリンターのデバイスコンテキスト。</param>
        /// <returns>成功時は正の値、失敗時は 0 以下の値。</returns>
        [DllImport("gdi32.dll", SetLastError = true)] private static extern int StartPage(IntPtr hdc);
        /// <summary>
        /// 現在の印刷ページを終了します。
        /// </summary>
        /// <param name="hdc">プリンターのデバイスコンテキスト。</param>
        /// <returns>成功時は正の値、失敗時は 0 以下の値。</returns>
        [DllImport("gdi32.dll", SetLastError = true)] private static extern int EndPage(IntPtr hdc);
        /// <summary>
        /// デバイスコンテキストの現在の描画状態を保存します。
        /// </summary>
        /// <param name="hdc">状態を保存するデバイスコンテキスト。</param>
        /// <returns>成功時は保存状態を表す正の識別子、失敗時は 0。</returns>
        [DllImport("gdi32.dll", SetLastError = true)] private static extern int SaveDC(IntPtr hdc);
        /// <summary>
        /// 保存したデバイスコンテキストの描画状態を復元します。
        /// </summary>
        /// <param name="hdc">復元対象のデバイスコンテキスト。</param>
        /// <param name="savedDc"><see cref="SaveDC"/> が返した保存状態の識別子。</param>
        /// <returns>成功した場合は <see langword="true"/>。</returns>
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RestoreDC(IntPtr hdc, int savedDc);
        /// <summary>
        /// デバイスコンテキストのビューポート原点を変更します。
        /// </summary>
        /// <param name="hdc">変更対象のデバイスコンテキスト。</param>
        /// <param name="x">新しい原点の X 座標。</param>
        /// <param name="y">新しい原点の Y 座標。</param>
        /// <param name="oldPoint">元の原点を受け取る領域へのポインター。不要な場合はゼロ。</param>
        /// <returns>成功した場合は <see langword="true"/>。</returns>
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetViewportOrgEx(IntPtr hdc, int x, int y, IntPtr oldPoint);

        private const uint WM_USER = 0x400;
        private const uint WM_GETTEXTLENGTH = 0x000E;
        private const uint EM_SETSEL = 0x00B1;
        private const uint EM_EXGETSEL = WM_USER + 52;
        private const uint EM_FORMATRANGE = WM_USER + 57;
        private const uint EM_SETTARGETDEVICE = WM_USER + 72;
        private const uint EM_SETTEXTEX = WM_USER + 97;

        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;
        private const int PHYSICALOFFSETX = 112;
        private const int PHYSICALOFFSETY = 113;
        private const int HORZRES = 8;
        private const int VERTRES = 10;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int ES_MULTILINE = 0x0004;
        private const int ES_AUTOVSCROLL = 0x0040;

        /// <summary>
        /// センチメートル単位の長さを twip（1/1440 インチ）へ変換します。
        /// </summary>
        /// <param name="cm">変換する長さ（cm）。</param>
        /// <returns>四捨五入して整数化した twip 値。</returns>
        private static int ToTwips(double cm) => checked((int)Math.Round(cm * 1440.0 / 2.54));
        /// <summary>
        /// 指定 DPI に基づきピクセル数を twip に変換します。
        /// </summary>
        /// <param name="pixels">変換するピクセル数。</param>
        /// <param name="dpi">変換元の解像度（1 インチ当たりのピクセル数）。</param>
        /// <returns>四捨五入して整数化した twip 値。</returns>
        private static int PixelsToTwips(int pixels, int dpi) =>
            checked((int)Math.Round((double)pixels * 1440.0 / dpi));

        /// <summary>
        /// RichEditBox の RTF 文書を、指定のプリンター DC とページ設定を使って印刷します。
        /// </summary>
        /// <param name="editor">印刷元の RichEditBox。</param>
        /// <param name="hdc">呼び出し元が所有するプリンターのデバイスコンテキスト。</param>
        /// <param name="settings">用紙サイズおよび余白の設定。</param>
        /// <param name="docName">印刷キュー上で使用する文書名。</param>
        /// <returns>すべてのページを正常に印刷した場合は <see langword="true"/>、入力不正または印刷エラー時は <see langword="false"/>。</returns>
        /// <remarks>
        /// このメソッドは <paramref name="hdc"/> を破棄しません。印刷開始後に失敗した場合は印刷ジョブの中止と一時リソースの解放を試みます。
        /// </remarks>
        public static bool PrintRTF(RichEditBox editor, IntPtr hdc, PageSettings settings,
            string docName = "RichEdit Document")
        {
            if (editor == null || settings == null || hdc == IntPtr.Zero) return false;

            IntPtr module = IntPtr.Zero;
            IntPtr hwnd = IntPtr.Zero;
            IntPtr rtfBuffer = IntPtr.Zero;
            bool docStarted = false;
            string step = "initialization";

            try
            {
                step = "get RTF";
                editor.Document.GetRange(0, int.MaxValue)
                    .GetText(TextGetOptions.FormatRtf, out string rtf);
                if (string.IsNullOrWhiteSpace(rtf)) return false;

                step = "load msftedit.dll";
                module = LoadLibrary("msftedit.dll");
                if (module == IntPtr.Zero) { LogWin32(step); return false; }

                step = "create RichEdit";
                hwnd = CreateWindowEx(0, "RICHEDIT50W", "",
                    WS_POPUP | ES_MULTILINE | ES_AUTOVSCROLL,
                    0, 0, 1000, 1000, IntPtr.Zero, IntPtr.Zero, module, IntPtr.Zero);
                if (hwnd == IntPtr.Zero) { LogWin32(step); return false; }

                step = "load RTF into RichEdit";
                rtfBuffer = Marshal.StringToHGlobalUni(rtf);
                SETTEXTEX options = new SETTEXTEX { flags = 0, codepage = 1200 };
                IntPtr loadResult = SendMessageSetText(hwnd, EM_SETTEXTEX, ref options, rtfBuffer);
                SendMessageGeneral(hwnd, EM_SETSEL, new IntPtr(-1), new IntPtr(-1));
                SendMessageGetSelection(hwnd, EM_EXGETSEL, IntPtr.Zero, out CHARRANGE endRange);
                int textLength = endRange.cpMax;
                Debug.WriteLine($"[Print] EM_SETTEXTEX={loadResult}, characters={textLength}");
                if (loadResult == IntPtr.Zero || textLength <= 0) return false;

                Marshal.FreeHGlobal(rtfBuffer);
                rtfBuffer = IntPtr.Zero;

                step = "printer capabilities";
                int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
                int dpiY = GetDeviceCaps(hdc, LOGPIXELSY);
                int printablePixelsX = GetDeviceCaps(hdc, HORZRES);
                int printablePixelsY = GetDeviceCaps(hdc, VERTRES);
                if (dpiX <= 0 || dpiY <= 0 || printablePixelsX <= 0 || printablePixelsY <= 0)
                    return false;

                int offsetPixelsX = GetDeviceCaps(hdc, PHYSICALOFFSETX);
                int offsetPixelsY = GetDeviceCaps(hdc, PHYSICALOFFSETY);
                int pageWidth = ToTwips(settings.PageWidthCm);
                int pageHeight = ToTwips(settings.PageHeightCm);
                int left = Math.Max(ToTwips(settings.MarginLeftCm), PixelsToTwips(offsetPixelsX, dpiX));
                int top = Math.Max(ToTwips(settings.MarginTopCm), PixelsToTwips(offsetPixelsY, dpiY));
                int right = Math.Min(pageWidth - ToTwips(settings.MarginRightCm),
                    PixelsToTwips(offsetPixelsX + printablePixelsX, dpiX));
                int bottom = Math.Min(pageHeight - ToTwips(settings.MarginBottomCm),
                    PixelsToTwips(offsetPixelsY + printablePixelsY, dpiY));
                if (left < 0 || top < 0 || right <= left || bottom <= top) return false;

                RECT page = new RECT { left = 0, top = 0, right = pageWidth, bottom = pageHeight };
                RECT printable = new RECT { left = left, top = top, right = right, bottom = bottom };

                SendMessageGeneral(hwnd, EM_SETTARGETDEVICE, IntPtr.Zero,
                    (IntPtr)(right - left));

                FORMATRANGE fr = new FORMATRANGE
                {
                    hdc = hdc,
                    hdcTarget = hdc,
                    rcPage = page,
                    rc = printable,
                    chrg = new CHARRANGE { cpMin = 0, cpMax = textLength }
                };

                step = "StartDoc";
                DOCINFO info = new DOCINFO
                {
                    cbSize = Marshal.SizeOf<DOCINFO>(),
                    lpszDocName = docName
                };
                if (StartDoc(hdc, ref info) <= 0) { LogWin32(step); return false; }
                docStarted = true;

                int pageNumber = 0;
                while (fr.chrg.cpMin < textLength)
                {
                    int current = fr.chrg.cpMin;
                    step = $"StartPage {pageNumber + 1}";
                    if (StartPage(hdc) <= 0) { LogWin32(step); return false; }

                    int saved = SaveDC(hdc);
                    if (saved == 0) { LogWin32("SaveDC"); return false; }
                    int next;
                    try
                    {
                        step = $"set origin page {pageNumber + 1}";
                        if (!SetViewportOrgEx(hdc, -offsetPixelsX, -offsetPixelsY, IntPtr.Zero))
                        { LogWin32(step); return false; }

                        step = $"render page {pageNumber + 1}";
                        fr.rc = printable;
                        next = SendMessageFormatRange(hwnd, EM_FORMATRANGE,
                            (IntPtr)1, ref fr).ToInt32();
                    }
                    finally
                    {
                        if (!RestoreDC(hdc, saved)) LogWin32("RestoreDC");
                    }

                    Debug.WriteLine($"[Print] Page {pageNumber + 1}: {current} -> {next}/{textLength}");
                    if (next <= current || next > textLength)
                    {
                        Debug.WriteLine("[Print] Pagination failed");
                        return false;
                    }
                    step = $"EndPage {pageNumber + 1}";
                    if (EndPage(hdc) <= 0) { LogWin32(step); return false; }
                    fr.chrg.cpMin = next;
                    pageNumber++;
                }

                step = "EndDoc";
                int ended = EndDoc(hdc);
                docStarted = false;
                if (ended <= 0) { LogWin32(step); return false; }
                Debug.WriteLine($"[Print] Completed: {pageNumber} pages");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Print] Failure at {step}: {ex}");
                return false;
            }
            finally
            {
                if (hwnd != IntPtr.Zero)
                {
                    try { SendMessageGeneral(hwnd, EM_FORMATRANGE, IntPtr.Zero, IntPtr.Zero); }
                    catch (Exception ex) { Debug.WriteLine($"[Print] format-cache cleanup: {ex}"); }
                }
                if (docStarted)
                {
                    try { if (AbortDoc(hdc) <= 0) LogWin32("AbortDoc"); }
                    catch (Exception ex) { Debug.WriteLine($"[Print] AbortDoc cleanup: {ex}"); }
                }
                if (rtfBuffer != IntPtr.Zero)
                {
                    try { Marshal.FreeHGlobal(rtfBuffer); }
                    catch (Exception ex) { Debug.WriteLine($"[Print] buffer cleanup: {ex}"); }
                }
                if (hwnd != IntPtr.Zero)
                {
                    try { if (!DestroyWindow(hwnd)) LogWin32("DestroyWindow"); }
                    catch (Exception ex) { Debug.WriteLine($"[Print] HWND cleanup: {ex}"); }
                }
                if (module != IntPtr.Zero)
                {
                    try { if (!FreeLibrary(module)) LogWin32("FreeLibrary"); }
                    catch (Exception ex) { Debug.WriteLine($"[Print] DLL cleanup: {ex}"); }
                }
            }
        }

        /// <summary>
        /// 直前の Win32 エラーコードと失敗した処理名をデバッグ出力に記録します。
        /// </summary>
        /// <param name="step">失敗した処理を示す文字列。</param>
        private static void LogWin32(string step)
        {
            Debug.WriteLine($"[Print] {step} failed (Win32={Marshal.GetLastWin32Error()})");
        }
    }
}
