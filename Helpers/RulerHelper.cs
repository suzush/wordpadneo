using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.Foundation;
using wordpadneo.Constants;
using wordpadneo.Models;

namespace wordpadneo.Helpers
{
    /// <summary>
    /// Helper types for drawing the ruler based on RichEditBox paragraph settings.
    /// </summary>
    internal class RulerHelper
    {
        /// <summary>
        /// Draws ruler ticks and indent markers.
        /// </summary>
        public static class RulerDrawer
        {
            /// <summary>
            /// Draws ruler ticks and paragraph indent markers on the specified canvas.
            /// </summary>
            /// <param name="canvas">The target canvas for ruler rendering.</param>
            /// <param name="editor">The RichEditBox used to read current paragraph indent values.</param>
            /// <param name="pageSettings">The page settings that define ruler bounds.</param>
            /// <param name="attachMarkerEvents">Callback used to attach pointer events to each marker.</param>
            /// <remarks>
            /// Existing canvas children are cleared first.
            /// Tick marks are rendered every 0.25 cm and numeric labels are shown every 1 cm.
            /// After drawing ticks, first-line, left, and right indent markers are placed.
            /// </remarks>
            public static void Draw(
                Canvas canvas,
                RichEditBox editor,
                PageSettings pageSettings,
                Action<FrameworkElement> attachMarkerEvents)
            {
                if (canvas == null || editor == null)
                {
                    return;
                }

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

                    double tickStartY;
                    if (isMajor)
                    {
                        tickStartY = 12;
                    }
                    else if (isHalf)
                    {
                        tickStartY = 16;
                    }
                    else
                    {
                        tickStartY = 20;
                    }

                    Line line = new Line
                    {
                        X1 = x,
                        Y1 = tickStartY,
                        X2 = x,
                        Y2 = canvas.ActualHeight - 2,
                        Stroke = lineBrush,
                        StrokeThickness = 1
                    };
                    canvas.Children.Add(line);

                    if (isMajor)
                    {
                        int cmValue = stepCount / 4;
                        TextBlock tickLabel = new TextBlock
                        {
                            Text = cmValue.ToString(),
                            FontSize = 9,
                            Foreground = textBrush
                        };
                        double labelOffsetPx;
                        if (cmValue >= 10)
                        {
                            labelOffsetPx = 6;
                        }
                        else
                        {
                            labelOffsetPx = 3;
                        }

                        Canvas.SetLeft(tickLabel, x - labelOffsetPx);
                        Canvas.SetTop(tickLabel, 0);
                        canvas.Children.Add(tickLabel);
                    }

                    stepCount++;
                }

                DrawMarkers(canvas, editor, pageSettings, lineBrush, attachMarkerEvents);
            }

            /// <summary>
            /// Draws three indent markers on the ruler using the current paragraph format.
            /// </summary>
            /// <param name="canvas">The canvas where markers are placed.</param>
            /// <param name="editor">The RichEditBox used to read paragraph indent values from selection.</param>
            /// <param name="pageSettings">Page settings used to calculate marker positions.</param>
            /// <param name="fillBrush">Brush used to fill marker shapes.</param>
            /// <param name="attachMarkerEvents">Callback used to attach interaction events to created markers.</param>
            /// <remarks>
            /// First-line, left, and right indents are read in points and converted to pixels.
            /// Marker tags are set as: FirstLine, Left, and Right.
            /// Events are attached through <paramref name="attachMarkerEvents"/> before adding markers to the canvas.
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
