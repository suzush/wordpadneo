
using wordpadneo.Constants;
using wordpadneo.Models;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using System.Text.RegularExpressions;

namespace wordpadneo.Helpers
{
    /// <summary>
    /// RTF形式の文書とページ設定の読み込み・保存を支援する
    /// ユーティリティクラスです。
    /// </summary>
    public static class RtfHelper
    {
        /// <summary>
        /// RTF文字列から用紙サイズと余白設定を取得し、
        /// RichEditBoxにRTF文書を読み込みます。
        /// </summary>
        /// <param name="editor">
        /// RTF文書を読み込む対象のRichEditBox。
        /// </param>
        /// <param name="rtfContent">
        /// 読み込むRTF形式の文字列。
        /// </param>
        /// <param name="pageSettings">
        /// RTF文書から取得した用紙サイズと余白を反映するページ設定。
        /// </param>
        /// <remarks>
        /// RTF内の用紙幅、高さ、および上下左右の余白を
        /// Twips単位からセンチメートル単位に変換します。
        /// 該当する設定がRTF内に存在しない場合は、
        /// pageSettingsの既存の値を維持します。
        /// </remarks>
        public static void SetFormattedRtf(
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
        /// 指定した正規表現を使用して文字列からRTF制御値を取得します。
        /// </summary>
        /// <param name="input">
        /// 検索対象となるRTF形式の文字列。
        /// </param>
        /// <param name="pattern">
        /// 取得する数値をキャプチャする正規表現パターン。
        /// </param>
        /// <param name="result">
        /// 取得した数値。取得に失敗した場合は0。
        /// </param>
        /// <returns>
        /// 指定したパターンに一致し、数値の取得に成功した場合はtrue。
        /// それ以外の場合はfalse。
        /// </returns>
        /// <remarks>
        /// 正規表現の最初のキャプチャグループから数値を取得します。
        /// 取得した数値は単位変換せず、そのまま返します。
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
        /// RichEditBoxの文書をRTF形式で取得し、
        /// 指定された用紙サイズと余白設定を反映します。
        /// </summary>
        /// <param name="editor">
        /// RTF文書を取得する対象のRichEditBox。
        /// </param>
        /// <param name="pageSettings">
        /// RTF文書に反映する用紙サイズと余白のページ設定。
        /// </param>
        /// <returns>
        /// 用紙サイズと余白設定を反映したRTF形式の文字列。
        /// RTFヘッダーが想定した形式でない場合は、
        /// RichEditBoxから取得した元のRTF文字列。
        /// </returns>
        /// <remarks>
        /// ページ設定の各値をセンチメートル単位から
        /// Twips単位に変換し、RTFヘッダーへ追加します。
        /// RTF文字列が所定のヘッダーで始まる場合のみ、
        /// ページ設定を含むヘッダーに置き換えます。
        /// </remarks>
        public static string GetFormattedRtf(
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
