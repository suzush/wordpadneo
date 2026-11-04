
using wordpadneo.Constants;
using wordpadneo.Helpers;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.Foundation;

namespace wordpadneo.Views
{
    /// <summary>
    /// メインウィンドウにおけるルーラーの描画と
    /// インデントマーカーの操作を管理します。
    /// </summary>
    partial class MainWindow
    {
        /// <summary>
        /// ルーラーのCanvasサイズが変更された際に描画を更新します。
        /// </summary>
        /// <param name="sender">
        /// サイズ変更イベントを発生させたオブジェクト。
        /// </param>
        /// <param name="e">
        /// サイズ変更前後の情報を含むイベントデータ。
        /// </param>
        private void RulerCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawRuler();
        }

        /// <summary>
        /// 現在のページ設定と段落書式に基づいてルーラーを描画します。
        /// </summary>
        /// <remarks>
        /// RulerHelper.RulerDrawer.Drawメソッドを使用して、
        /// 目盛りとインデントマーカーを描画します。
        /// マーカーのイベント登録にはAttachMarkerEventsを使用します。
        /// </remarks>
        private void DrawRuler()
        {
            RulerHelper.RulerDrawer.Draw(rulerCanvas, editor, _pageSettings, AttachMarkerEvents);
        }

        /// <summary>
        /// インデントマーカーにポインター押下イベントを登録します。
        /// </summary>
        /// <param name="element">
        /// イベントを登録する対象のインデントマーカー。
        /// </param>
        /// <remarks>
        /// マーカーが押された際にMarker_PointerPressedが呼び出されるよう、
        /// PointerPressedイベントへハンドラーを登録します。
        /// </remarks>
        private void AttachMarkerEvents(FrameworkElement element)
        {
            element.PointerPressed += Marker_PointerPressed;
        }

        /// <summary>
        /// インデントマーカーが押された際にドラッグ操作を開始します。
        /// </summary>
        /// <param name="sender">
        /// ポインター押下イベントを発生させたマーカー。
        /// </param>
        /// <param name="e">
        /// ポインターの情報を含むイベントデータ。
        /// </param>
        /// <remarks>
        /// 操作対象のマーカーとその種類を記録し、
        /// ドラッグ中の状態へ切り替えます。
        /// ルーラーCanvasでポインターをキャプチャし、
        /// 移動、解放、キャンセルのイベントを登録します。
        /// イベントハンドラーの重複登録を防ぐため、
        /// 登録前に既存のハンドラーを解除します。
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
        /// インデントマーカーのドラッグ位置に応じて、
        /// 選択中の段落のインデント値を更新します。
        /// </summary>
        /// <param name="sender">
        /// ポインター移動イベントを発生させたオブジェクト。
        /// </param>
        /// <param name="e">
        /// 現在のポインター位置を含むイベントデータ。
        /// </param>
        /// <remarks>
        /// ドラッグ中のマーカーの種類に応じて、
        /// 先頭行インデント、左インデント、右インデントを変更します。
        ///
        /// マーカーの種類は以下のとおりです。
        /// FirstLine：先頭行インデント
        /// Left：左インデント
        /// Right：右インデント
        ///
        /// ポインター座標をセンチメートル単位に変換し、
        /// 0.25cm刻みでスナップ処理を行います。
        /// 算出した値をポイント単位に変換して段落書式へ反映します。
        ///
        /// 更新中は_isUpdatingをtrueに設定して、
        /// 選択範囲の変更に伴う画面更新との競合を抑制します。
        /// インデントの更新後はルーラーを再描画します。
        /// </remarks>
        private void RulerCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging || _activeMarker == null || _markerType == null) return;

            Point pos = e.GetCurrentPoint(rulerCanvas).Position;

            ITextSelection selection = editor.Document.Selection;
            if (selection == null) return;

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
        /// インデントマーカーのドラッグ操作を終了します。
        /// </summary>
        /// <param name="sender">
        /// ポインター解放またはキャンセルイベントを発生させたオブジェクト。
        /// </param>
        /// <param name="e">
        /// 解放またはキャンセルされたポインターのイベントデータ。
        /// </param>
        /// <remarks>
        /// ドラッグ状態を解除し、操作対象のマーカー情報を初期化します。
        /// ポインターのキャプチャを解除した後、
        /// 移動、解放、キャンセルのイベントハンドラーを解除します。
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
