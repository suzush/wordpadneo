using wordpadneo.Models;
using Microsoft.UI.Xaml;
using System;
using Windows.Storage;


namespace wordpadneo.Views
{
    public sealed partial class MainWindow : Window
    {
        private FrameworkElement _activeMarker = null;
        private string _markerType = null;
        private bool _isDragging = false;
        private double _rulerLeft = 0;
        private double _rulerRight = 0;
        private bool _isUpdating = false;

        private PageSettings _pageSettings = new PageSettings();

        private StorageFile _currentFile = null;
        private bool _isFileSaved = true;

        private string _lastSearchKeyword = string.Empty;
        private string _lastReplaceKeyword = string.Empty;

        public MainWindow()
        {
            System.Diagnostics.Debug.WriteLine("A: 初期化開始");

            InitializeComponent();

            System.Diagnostics.Debug.WriteLine("B: XAML初期化成功");

            // PopulateFontList();
            PopulateFontSizeList();

            System.Diagnostics.Debug.WriteLine("C: 初期化終了");
            _isFileSaved = true;
            AppWindow.Closing += AppWindow_Closing;
        }
    }
}