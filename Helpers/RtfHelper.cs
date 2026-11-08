using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using System.Text.RegularExpressions;
using wordpadneo.Constants;
using wordpadneo.Models;

namespace wordpadneo.Helpers
{
    /// <summary>
    /// Utility helpers for loading and saving RTF content with page settings.
    /// </summary>
    public static class RtfHelper
    {
        /// <summary>
        /// Reads paper size and margin settings from an RTF string and loads it into the RichEditBox.
        /// </summary>
        /// <param name="editor">The RichEditBox that receives the RTF content.</param>
        /// <param name="rtfContent">The source RTF string.</param>
        /// <param name="pageSettings">The page settings object to update from the RTF metadata.</param>
        /// <remarks>
        /// Paper width/height and margins are parsed from twips and converted to centimeters.
        /// If a value is not present in the RTF content, the current page setting is preserved.
        /// </remarks>
        public static void LoadRtfDocument(
            RichEditBox editor,
            string rtfContent,
            PageSettings pageSettings)
        {
            if (TryGetRtfValue(rtfContent, @"\\paperw(\d+)", out double paperWidth))
            {
                pageSettings.PageWidthCm = paperWidth / PageConstants.TwipsPerCm;
            }

            if (TryGetRtfValue(rtfContent, @"\\paperh(\d+)", out double paperHeight))
            {
                pageSettings.PageHeightCm = paperHeight / PageConstants.TwipsPerCm;
            }

            if (TryGetRtfValue(rtfContent, @"\\margl(\d+)", out double marginLeft))
            {
                pageSettings.MarginLeftCm = marginLeft / PageConstants.TwipsPerCm;
            }

            if (TryGetRtfValue(rtfContent, @"\\margr(\d+)", out double marginRight))
            {
                pageSettings.MarginRightCm = marginRight / PageConstants.TwipsPerCm;
            }

            if (TryGetRtfValue(rtfContent, @"\\margt(\d+)", out double marginTop))
            {
                pageSettings.MarginTopCm = marginTop / PageConstants.TwipsPerCm;
            }

            if (TryGetRtfValue(rtfContent, @"\\margb(\d+)", out double marginBottom))
            {
                pageSettings.MarginBottomCm = marginBottom / PageConstants.TwipsPerCm;
            }

            editor.Document.SetText(TextSetOptions.FormatRtf, rtfContent);
        }

        /// <summary>
        /// Reads a numeric RTF control value from text using the specified regex pattern.
        /// </summary>
        /// <param name="input">The RTF text to search.</param>
        /// <param name="pattern">The regex pattern whose first capture group contains the numeric value.</param>
        /// <param name="result">The parsed numeric value, or 0 when parsing fails.</param>
        /// <returns>True when the value is found and parsed; otherwise false.</returns>
        /// <remarks>
        /// The value is returned as-is without unit conversion.
        /// </remarks>
        private static bool TryGetRtfValue(
            string input,
            string pattern,
            out double result)
        {
            var match = Regex.Match(input, pattern);

            if (match.Success && double.TryParse(match.Groups[1].Value, out double twips))
            {
                result = twips;
                return true;
            }

            result = 0;
            return false;
        }

        /// <summary>
        /// Gets the current RichEditBox document as RTF and applies paper/margin settings to the header.
        /// </summary>
        /// <param name="editor">The RichEditBox that provides the RTF document.</param>
        /// <param name="pageSettings">The paper size and margin settings to write into the RTF metadata.</param>
        /// <returns>
        /// An RTF string with updated page settings when the expected header exists;
        /// otherwise, the original RTF string.
        /// </returns>
        /// <remarks>
        /// Page setting values are converted from centimeters to twips before writing to the header.
        /// </remarks>
        public static string GetRtfDocument(
            RichEditBox editor,
            PageSettings pageSettings)
        {
            editor.Document.GetText(TextGetOptions.FormatRtf, out string rtfContent);

            int paperWidthTwips = (int)(pageSettings.PageWidthCm * PageConstants.TwipsPerCm);
            int paperHeightTwips = (int)(pageSettings.PageHeightCm * PageConstants.TwipsPerCm);
            int marginLeftTwips = (int)(pageSettings.MarginLeftCm * PageConstants.TwipsPerCm);
            int marginRightTwips = (int)(pageSettings.MarginRightCm * PageConstants.TwipsPerCm);
            int marginTopTwips = (int)(pageSettings.MarginTopCm * PageConstants.TwipsPerCm);
            int marginBottomTwips = (int)(pageSettings.MarginBottomCm * PageConstants.TwipsPerCm);

            string pageSetupRtf = $@"{{\rtf1\paperw{paperWidthTwips}\paperh{paperHeightTwips}\margl{marginLeftTwips}\margr{marginRightTwips}\margt{marginTopTwips}\margb{marginBottomTwips}";

            if (rtfContent.StartsWith(@"{\rtf1"))
            {
                return pageSetupRtf + rtfContent.Substring(6);
            }

            return rtfContent;
        }
    }
}
