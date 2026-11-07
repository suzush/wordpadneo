using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using wordpadneo.Models;

namespace wordpadneo.Helpers
{
    /// <summary>
    /// Provides helper methods for printing RichEdit RTF documents using Windows GDI printing.
    /// </summary>
    public static class PrintHelper
    {
        /// <summary>
        /// A Win32 structure that stores the coordinates of a rectangle.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int left, top, right, bottom; }

        /// <summary>
        /// A structure that stores a RichEdit character range.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct CHARRANGE { public int cpMin, cpMax; }

        /// <summary>
        /// A structure that specifies the RichEdit page layout bounds and destination device context.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct FORMATRANGE
        {
            public IntPtr hdc, hdcTarget;
            public RECT rc, rcPage;
            public CHARRANGE chrg;
        }

        /// <summary>
        /// A structure that specifies document information for a GDI print job.
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
        /// A structure that specifies text-setting options and the code page for RichEdit.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SETTEXTEX
        {
            public uint flags;
            public uint codepage;
        }

        /// <summary>
        /// Loads the specified DLL into the current process.
        /// </summary>
        /// <param name="name">The name or path of the DLL to load.</param>
        /// <returns>The module handle on success; <see cref="IntPtr.Zero"/> on failure.</returns>
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string name);

        /// <summary>
        /// Decrements the reference count of a loaded DLL and unloads it when appropriate.
        /// </summary>
        /// <param name="module">The handle of the module to release.</param>
        /// <returns><see langword="true"/> if successful.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr module);

        /// <summary>
        /// Creates a Win32 window of the specified window class.
        /// </summary>
        /// <param name="exStyle">The extended window style.</param>
        /// <param name="className">The name of the registered window class.</param>
        /// <param name="windowName">The window name.</param>
        /// <param name="style">The window style.</param>
        /// <param name="x">The X coordinate of the upper-left corner.</param>
        /// <param name="y">The Y coordinate of the upper-left corner.</param>
        /// <param name="width">The window width.</param>
        /// <param name="height">The window height.</param>
        /// <param name="parent">The parent window handle.</param>
        /// <param name="menu">The menu handle or child window identifier.</param>
        /// <param name="instance">The module handle associated with the window class.</param>
        /// <param name="param">A pointer to additional data passed during window creation.</param>
        /// <returns>The window handle on success; <see cref="IntPtr.Zero"/> on failure.</returns>
        [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            int exStyle, string className, string windowName, int style,
            int x, int y, int width, int height,
            IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

        /// <summary>
        /// Destroys a Win32 window.
        /// </summary>
        /// <param name="hwnd">The handle of the window to destroy.</param>
        /// <returns><see langword="true"/> if successful.</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hwnd);

        /// <summary>
        /// Sends the EM_SETTEXTEX message to RichEdit to set RTF data.
        /// </summary>
        /// <param name="hwnd">The RichEdit window handle.</param>
        /// <param name="message">The message identifier to send.</param>
        /// <param name="options">The text-setting options.</param>
        /// <param name="text">A pointer to the data to load.</param>
        /// <returns>The result returned by the window procedure.</returns>
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr SendMessageSetText(IntPtr hwnd, uint message,
            ref SETTEXTEX options, IntPtr text);

        /// <summary>
        /// Sends EM_FORMATRANGE to RichEdit to format and render the specified range.
        /// </summary>
        /// <param name="hwnd">The RichEdit window handle.</param>
        /// <param name="message">The message identifier to send.</param>
        /// <param name="render">Nonzero to render; zero to measure only.</param>
        /// <param name="formatRange">The rendering destination and character-range settings.</param>
        /// <returns>The position immediately after the last processed character.</returns>
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr SendMessageFormatRange(IntPtr hwnd, uint message,
            IntPtr render, ref FORMATRANGE formatRange);

        /// <summary>
        /// Synchronously sends a general Win32 message to the specified window.
        /// </summary>
        /// <param name="hwnd">The destination window handle.</param>
        /// <param name="message">The message identifier.</param>
        /// <param name="wParam">The first message-specific parameter.</param>
        /// <param name="lParam">The second message-specific parameter.</param>
        /// <returns>The value returned by the window procedure.</returns>
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr SendMessageGeneral(IntPtr hwnd, uint message,
            IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Retrieves the RichEdit selection as a CHARRANGE structure.
        /// </summary>
        /// <param name="hwnd">The RichEdit window handle.</param>
        /// <param name="message">The message identifier to send.</param>
        /// <param name="wParam">A parameter unused by this message.</param>
        /// <param name="range">Receives the retrieved selection range.</param>
        /// <returns>The value returned by the window procedure.</returns>
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr SendMessageGetSelection(IntPtr hwnd, uint message,
            IntPtr wParam, out CHARRANGE range);

        /// <summary>
        /// Retrieves printer device information, such as DPI and printable area, from the device context.
        /// </summary>
        /// <param name="hdc">The printer device context.</param>
        /// <param name="index">The identifier of the device capability to retrieve.</param>
        /// <returns>The requested capability value; its meaning depends on the specified item.</returns>
        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int index);
        /// <summary>
        /// Starts a GDI print job.
        /// </summary>
        /// <param name="hdc">The printer device context.</param>
        /// <param name="di">The document information for the print job.</param>
        /// <returns>A positive print job ID on success; zero or a negative value on failure.</returns>
        [DllImport("gdi32.dll", EntryPoint = "StartDocW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int StartDoc(IntPtr hdc, ref DOCINFO di);
        /// <summary>
        /// Completes the current GDI print job.
        /// </summary>
        /// <param name="hdc">The printer device context.</param>
        /// <returns>A positive value on success; zero or a negative value on failure.</returns>
        [DllImport("gdi32.dll", SetLastError = true)] private static extern int EndDoc(IntPtr hdc);
        /// <summary>
        /// Aborts the current GDI print job.
        /// </summary>
        /// <param name="hdc">The printer device context.</param>
        /// <returns>A positive value on success; zero or a negative value on failure.</returns>
        [DllImport("gdi32.dll", SetLastError = true)] private static extern int AbortDoc(IntPtr hdc);
        /// <summary>
        /// Starts a new page in the print job.
        /// </summary>
        /// <param name="hdc">The printer device context.</param>
        /// <returns>A positive value on success; zero or a negative value on failure.</returns>
        [DllImport("gdi32.dll", SetLastError = true)] private static extern int StartPage(IntPtr hdc);
        /// <summary>
        /// Ends the current print page.
        /// </summary>
        /// <param name="hdc">The printer device context.</param>
        /// <returns>A positive value on success; zero or a negative value on failure.</returns>
        [DllImport("gdi32.dll", SetLastError = true)] private static extern int EndPage(IntPtr hdc);
        /// <summary>
        /// Saves the current drawing state of the device context.
        /// </summary>
        /// <param name="hdc">The device context whose state is to be saved.</param>
        /// <returns>A positive identifier for the saved state on success; zero on failure.</returns>
        [DllImport("gdi32.dll", SetLastError = true)] private static extern int SaveDC(IntPtr hdc);
        /// <summary>
        /// Restores a previously saved drawing state of the device context.
        /// </summary>
        /// <param name="hdc">The device context whose state is to be restored.</param>
        /// <param name="savedDc">The saved-state identifier returned by <see cref="SaveDC"/>.</param>
        /// <returns><see langword="true"/> if successful.</returns>
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RestoreDC(IntPtr hdc, int savedDc);
        /// <summary>
        /// Changes the viewport origin of the device context.
        /// </summary>
        /// <param name="hdc">The device context to modify.</param>
        /// <param name="x">The X coordinate of the new origin.</param>
        /// <param name="y">The Y coordinate of the new origin.</param>
        /// <param name="oldPoint">A pointer that receives the previous origin, or zero if it is not needed.</param>
        /// <returns><see langword="true"/> if successful.</returns>
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
        /// Converts a length in centimeters to twips (1/1440 of an inch).
        /// </summary>
        /// <param name="cm">The length to convert, in centimeters.</param>
        /// <returns>The twip value rounded to the nearest integer.</returns>
        private static int ToTwips(double cm) => checked((int)Math.Round(cm * 1440.0 / 2.54));
        /// <summary>
        /// Converts a pixel count to twips using the specified DPI.
        /// </summary>
        /// <param name="pixels">The number of pixels to convert.</param>
        /// <param name="dpi">The source resolution in pixels per inch.</param>
        /// <returns>The twip value rounded to the nearest integer.</returns>
        private static int PixelsToTwips(int pixels, int dpi) =>
            checked((int)Math.Round((double)pixels * 1440.0 / dpi));

        /// <summary>
        /// Prints the RTF document in a RichEditBox using the specified printer DC and page settings.
        /// </summary>
        /// <param name="editor">The RichEditBox containing the document to print.</param>
        /// <param name="hdc">The printer device context owned by the caller.</param>
        /// <param name="settings">The paper size and margin settings.</param>
        /// <param name="docName">The document name shown in the print queue.</param>
        /// <returns><see langword="true"/> if all pages are printed successfully; <see langword="false"/> for invalid input or printing errors.</returns>
        /// <remarks>
        /// This method does not release <paramref name="hdc"/>. If an error occurs after printing starts, it attempts to abort the print job and release temporary resources.
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
        /// Logs the most recent Win32 error code and the name of the failed operation to the debug output.
        /// </summary>
        /// <param name="step">A string identifying the failed operation.</param>
        private static void LogWin32(string step)
        {
            Debug.WriteLine($"[Print] {step} failed (Win32={Marshal.GetLastWin32Error()})");
        }
    }
}
