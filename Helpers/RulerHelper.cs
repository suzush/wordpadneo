
using wordpadneo.Constants;
using wordpadneo.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Text;
using System;
using Windows.Foundation;

namespace wordpadneo.Helpers
{
    /// <summary>
    /// RichEditBoxの段落設定に対応したルーラーの描画を支援するクラスです。
    /// </summary>
    internal class RulerHelper
    {
        /// <summary>
        /// ルーラーの目盛りとインデントマーカーを描画するクラスです。
        /// </summary>
        public static class RulerDrawer
        {
            /// <summary>
            /// 指定されたCanvasに、ページ設定に基づくルーラーの目盛りと
            /// RichEditBoxの段落インデントを示すマーカーを描画します。
            /// </summary>
            /// <param name="canvas">
            /// ルーラーを描画する対象のCanvas。
            /// </param>
            /// <param name="editor">
            /// 段落のインデント情報を取得する対象のRichEditBox。
            /// </param>
            /// <param name="pageSettings">
            /// ルーラーの表示範囲を決定するページ設定。
            /// </param>
            /// <param name="attachMarkerEvents">
            /// 各インデントマーカーにマウスやポインター操作の
            /// イベントを登録するためのコールバック。
            /// </param>
            /// <remarks>
            /// 描画前にCanvasの既存の子要素をすべて削除します。
            /// 印刷可能領域の左端を基準として、0.25cm間隔で目盛りを描画し、
            /// 1cmごとに数値ラベルを表示します。
            /// 目盛りの描画後、先頭行、左、右のインデントマーカーを配置します。
            /// </remarks>
            public static void Draw(
                Canvas canvas,
                RichEditBox editor,
                PageSettings pageSettings,
                Action<FrameworkElement> attachMarkerEvents)
            {
                if (canvas == null || editor == null) return;

                canvas.Children.Clear();

                double rulerLeft = pageSettings.MarginLeftCm * PageConstants.PxPerCm;
                double rulerRight = rulerLeft + (pageSettings.PrintableWidthCm * PageConstants.PxPerCm);

                var textBrush = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
                var lineBrush = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

                double stepPx = PageConstants.PxPerCm / 4.0;
                int stepCount = 0;

                for (double x = rulerLeft; x <= rulerRight + 0.1; x += stepPx)
                {
                    bool isMajor = (stepCount % 4 == 0);
                    bool isHalf = (stepCount % 2 == 0);

                    double lineY = isMajor ? 12 : (isHalf ? 16 : 20);

                    Line line = new Line
                    {
                        X1 = x,
                        Y1 = lineY,
                        X2 = x,
                        Y2 = canvas.ActualHeight - 2,
                        Stroke = lineBrush,
                        StrokeThickness = 1
                    };
                    canvas.Children.Add(line);

                    if (isMajor)
                    {
                        int cmValue = stepCount / 4;
                        TextBlock txt = new TextBlock
                        {
                            Text = cmValue.ToString(),
                            FontSize = 9,
                            Foreground = textBrush
                        };
                        Canvas.SetLeft(txt, x - (cmValue >= 10 ? 6 : 3));
                        Canvas.SetTop(txt, 0);
                        canvas.Children.Add(txt);
                    }

                    stepCount++;
                }

                DrawMarkers(canvas, editor, pageSettings, lineBrush, attachMarkerEvents);
            }

            /// <summary>
            /// RichEditBoxの現在の段落書式に基づき、
            /// ルーラー上に3種類のインデントマーカーを描画します。
            /// </summary>
            /// <param name="canvas">
            /// インデントマーカーを配置する対象のCanvas。
            /// </param>
            /// <param name="editor">
            /// 現在の選択範囲から段落のインデント情報を取得するRichEditBox。
            /// </param>
            /// <param name="pageSettings">
            /// マーカーの位置を計算するためのページ設定。
            /// </param>
            /// <param name="fillBrush">
            /// インデントマーカーの塗りつぶしに使用するブラシ。
            /// </param>
            /// <param name="attachMarkerEvents">
            /// 生成した各マーカーに操作イベントを登録するコールバック。
            /// </param>
            /// <remarks>
            /// RichEditBoxの選択範囲から先頭行、左、右のインデント値を
            /// ポイント単位で取得し、ピクセル単位へ変換して配置します。
            ///
            /// 各マーカーには識別用のTagを設定します。
            /// FirstLine：先頭行インデント
            /// Left：左インデント
            /// Right：右インデント
            ///
            /// 各マーカーをCanvasに追加する前に、
            /// attachMarkerEventsを呼び出してイベントを登録します。
            /// </remarks>
            private static void DrawMarkers(
                Canvas canvas,
                RichEditBox editor,
                PageSettings pageSettings,
                Brush fillBrush,
                Action<FrameworkElement> attachMarkerEvents)
            {
                ITextSelection selection = editor.Document.Selection;
                float firstLinePt = 0, leftPt = 0, rightPt = 0;

                if (selection != null)
                {
                    ITextParagraphFormat format = selection.ParagraphFormat;
                    firstLinePt = format.FirstLineIndent;
                    leftPt = format.LeftIndent;
                    rightPt = format.RightIndent;
                }

                double rulerLeft = pageSettings.MarginLeftCm * PageConstants.PxPerCm;
                double rulerRight = rulerLeft + (pageSettings.PrintableWidthCm * PageConstants.PxPerCm);
                double ptToPx = PageConstants.PxPerCm / PageConstants.PtPerCm;

                double firstLinePx = rulerLeft + ((leftPt + firstLinePt) * ptToPx);
                double leftPx = rulerLeft + (leftPt * ptToPx);
                double rightPx = rulerRight - (rightPt * ptToPx);

                Polygon firstLineMarker = new Polygon
                {
                    Points = new PointCollection
                    {
                        new Point(0, 0), new Point(10, 0),
                        new Point(10, 4), new Point(5, 9), new Point(0, 4)
                    },
                    Fill = fillBrush,
                    Tag = "FirstLine"
                };
                Canvas.SetLeft(firstLineMarker, firstLinePx - 5);
                Canvas.SetTop(firstLineMarker, 0);
                attachMarkerEvents(firstLineMarker);
                canvas.Children.Add(firstLineMarker);

                Grid leftGroup = new Grid
                {
                    Tag = "Left",
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
                };
                Polygon leftTriangle = new Polygon
                {
                    Points = new PointCollection
                    {
                        new Point(5, 0), new Point(10, 5),
                        new Point(10, 9), new Point(0, 9), new Point(0, 5)
                    },
                    Fill = fillBrush
                };
                Rectangle leftBox = new Rectangle
                {
                    Width = 10,
                    Height = 5,
                    Fill = fillBrush,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                leftGroup.Children.Add(leftTriangle);
                leftGroup.Children.Add(leftBox);

                Canvas.SetLeft(leftGroup, leftPx - 5);
                Canvas.SetTop(leftGroup, 10);
                attachMarkerEvents(leftGroup);
                canvas.Children.Add(leftGroup);

                Polygon rightMarker = new Polygon
                {
                    Points = new PointCollection
                    {
                        new Point(5, 0), new Point(10, 5),
                        new Point(10, 9), new Point(0, 9), new Point(0, 5)
                    },
                    Fill = fillBrush,
                    Tag = "Right"
                };
                Canvas.SetLeft(rightMarker, rightPx - 5);
                Canvas.SetTop(rightMarker, 10);
                attachMarkerEvents(rightMarker);
                canvas.Children.Add(rightMarker);
            }
        }
    }
}
