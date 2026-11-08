using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.Foundation;
using wordpadneo.Constants;
using wordpadneo.Helpers;

namespace wordpadneo.Views
{
    /// <summary>
    /// Handles ruler rendering and indent marker interaction in the main window.
    /// </summary>
    partial class MainWindow
    {
        /// <summary>
        /// Redraws the ruler when the ruler canvas size changes.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Size change event data.</param>
        private void RulerCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawRuler();
        }

        /// <summary>
        /// Draws the ruler based on current page settings and paragraph formatting.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="RulerHelper.RulerDrawer.Draw"/> to render ticks and indent markers,
        /// and attaches marker events through <see cref="AttachMarkerEvents"/>.
        /// </remarks>
        private void DrawRuler()
        {
            RulerHelper.RulerDrawer.Draw(rulerCanvas, editor, _pageSettings, AttachMarkerEvents);
        }

        /// <summary>
        /// Registers pointer-pressed events for an indent marker.
        /// </summary>
        /// <param name="element">The indent marker element.</param>
        /// <remarks>
        /// Hooks <see cref="Marker_PointerPressed"/> to the marker's PointerPressed event.
        /// </remarks>
        private void AttachMarkerEvents(FrameworkElement element)
        {
            element.PointerPressed += Marker_PointerPressed;
        }

        /// <summary>
        /// Starts dragging when an indent marker is pressed.
        /// </summary>
        /// <param name="sender">The marker that raised the event.</param>
        /// <param name="e">Pointer event data.</param>
        /// <remarks>
        /// Stores the active marker and marker type, captures the pointer on the ruler canvas,
        /// and ensures move/release/cancel handlers are re-registered without duplicates.
        /// </remarks>
        private void Marker_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                _activeMarker = element;
                _markerType = element.Tag as string;
                _isDragging = true;

                rulerCanvas.CapturePointer(e.Pointer);

                rulerCanvas.PointerMoved -= RulerCanvas_PointerMoved;
                rulerCanvas.PointerReleased -= RulerCanvas_PointerReleased;
                rulerCanvas.PointerCanceled -= RulerCanvas_PointerReleased;

                rulerCanvas.PointerMoved += RulerCanvas_PointerMoved;
                rulerCanvas.PointerReleased += RulerCanvas_PointerReleased;
                rulerCanvas.PointerCanceled += RulerCanvas_PointerReleased;

                e.Handled = true;
            }
        }

        /// <summary>
        /// Updates paragraph indent values while dragging an indent marker.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Pointer event data containing current pointer position.</param>
        /// <remarks>
        /// Adjusts first-line, left, or right indent based on the active marker type
        /// (FirstLine, Left, or Right).
        /// Pointer positions are snapped in 0.25 cm increments and converted to points
        /// before applying to paragraph formatting.
        /// The method temporarily enables <c>_isUpdating</c> to avoid UI update conflicts,
        /// then redraws the ruler.
        /// </remarks>
        private void RulerCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging || _activeMarker == null || _markerType == null)
            {
                return;
            }

            Point pos = e.GetCurrentPoint(rulerCanvas).Position;

            ITextSelection selection = editor.Document.Selection;
            if (selection == null)
            {
                return;
            }

            ITextParagraphFormat format = selection.ParagraphFormat;

            float firstLinePt = format.FirstLineIndent;
            float leftPt = format.LeftIndent;
            float rightPt = format.RightIndent;

            _isUpdating = true;
            double snapIntervalCm = 0.25;

            double pageWidthPx = _pageSettings.PageWidthCm * PageConstants.PxPerCm;
            double rulerLeft = _pageSettings.MarginLeftCm * PageConstants.PxPerCm;
            double rulerRight = rulerLeft + (_pageSettings.PrintableWidthCm * PageConstants.PxPerCm);
            double ptToPx = PageConstants.PxPerCm / PageConstants.PtPerCm;

            double clampedMouseX = Math.Clamp(pos.X, 0, pageWidthPx);

            switch (_markerType)
            {
                case "FirstLine":
                    {
                        double currentLeftPx = _rulerLeft + (leftPt * (PageConstants.PxPerCm / PageConstants.PtPerCm));
                        double diffCm = (pos.X - currentLeftPx) / PageConstants.PxPerCm;
                        double snappedCm = Math.Round(diffCm / snapIntervalCm) * snapIntervalCm;
                        float newFirstLinePt = (float)(snappedCm * PageConstants.PtPerCm);
                        double absolutePx = rulerLeft + ((leftPt * ptToPx) + (newFirstLinePt * ptToPx));
                        if (absolutePx >= 0 && absolutePx <= pageWidthPx)
                        {
                            firstLinePt = newFirstLinePt;
                        }
                        break;
                    }

                case "Left":
                    {
                        double currentRightPx = rulerRight - (rightPt * (PageConstants.PxPerCm / PageConstants.PtPerCm));
                        double safeX = Math.Clamp(clampedMouseX, 0, Math.Max(0, currentRightPx - PageConstants.PxPerCm));
                        double diffCm = Math.Max(0, pos.X - _rulerLeft) / PageConstants.PxPerCm;
                        double snappedCm = Math.Round(diffCm / snapIntervalCm) * snapIntervalCm;
                        leftPt = (float)(snappedCm * PageConstants.PtPerCm);
                        break;
                    }

                case "Right":
                    {
                        double currentLeftPx = rulerLeft + (leftPt * (PageConstants.PxPerCm / PageConstants.PtPerCm));
                        double safeX = Math.Clamp(clampedMouseX, Math.Min(pageWidthPx, currentLeftPx + PageConstants.PxPerCm), pageWidthPx);
                        double diffCm = (rulerRight - safeX) / PageConstants.PxPerCm;
                        double snappedCm = Math.Round(diffCm / snapIntervalCm) * snapIntervalCm;
                        rightPt = (float)(snappedCm * PageConstants.PtPerCm);
                        break;
                    }
            }

            format.SetIndents(firstLinePt, leftPt, rightPt);
            selection.ParagraphFormat = format;

            _isUpdating = false;

            DrawRuler();
        }

        /// <summary>
        /// Ends marker dragging on pointer release or cancel.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Pointer event data for the released/canceled pointer.</param>
        /// <remarks>
        /// Clears drag state, resets active marker information, releases pointer capture,
        /// and detaches move/release/cancel handlers from the ruler canvas.
        /// </remarks>
        private void RulerCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _activeMarker = null;
                _markerType = null;

                rulerCanvas.ReleasePointerCapture(e.Pointer);
                rulerCanvas.PointerMoved -= RulerCanvas_PointerMoved;
                rulerCanvas.PointerReleased -= RulerCanvas_PointerReleased;
                rulerCanvas.PointerCanceled -= RulerCanvas_PointerReleased;
            }
        }
    }
}
