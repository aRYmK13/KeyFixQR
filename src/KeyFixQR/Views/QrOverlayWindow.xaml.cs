using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using KeyFixQR.Services;

namespace KeyFixQR.Views
{
    public partial class QrOverlayWindow : Window
    {
        public string Payload { get; private set; } = "";
        public Action<string>? CopyRequested { get; set; }

        private Point _anchor = new(200, 200);

        public QrOverlayWindow()
        {
            InitializeComponent();
            FlowDirection = LocalizationService.FlowDirection;
            copyBtn.Content = LocalizationService.T("copyBtn");
            Loaded += (_, _) => Reposition();
        }

        public void SetImage(byte[] png, string payload)
        {
            Payload = payload;
            var image = new BitmapImage();
            using var ms = new MemoryStream(png);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            qrImage.Source = image;

            string preview = payload.Replace("\r", "").Replace("\n", " ⏎ ");
            payloadPreview.Text = preview.Length > 400 ? preview.Substring(0, 400) + "…" : preview;
        }

        public void PlaceNear(Point screenPx)
        {
            _anchor = screenPx;
            Reposition();
        }

        private void Reposition()
        {
            double scale = MonitorHelper.DpiScaleAt(_anchor.X, _anchor.Y);
            var wa = MonitorHelper.WorkAreaAt(_anchor.X, _anchor.Y);

            double w = (ActualWidth > 20 ? ActualWidth : Width) + 28;
            double h = (ActualHeight > 40 ? ActualHeight : 360) + 28;

            double anchorL = _anchor.X / scale;
            double anchorT = _anchor.Y / scale;
            double waL = wa.Left / scale, waT = wa.Top / scale, waR = wa.Right / scale, waB = wa.Bottom / scale;

            double left = anchorL - w + 24;
            if (left < waL + 8) left = anchorL + 24;
            double top = anchorT - h + 34;
            if (top < waT + 8) top = anchorT + 28;

            left = Math.Clamp(left, waL + 8, Math.Max(waL + 8, waR - w - 8));
            top = Math.Clamp(top, waT + 8, Math.Max(waT + 8, waB - h - 8));

            Left = left;
            Top = top;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsInsideInteractive(e.OriginalSource as DependencyObject)) return;
            try { DragMove(); }
            catch { }
        }

        private static bool IsInsideInteractive(DependencyObject? source)
        {
            while (source != null)
            {
                if (source is Button or TextBox or Image) return true;
                source = System.Windows.Media.VisualTreeHelper.GetParent(source);
            }
            return false;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private async void CopyBtn_Click(object sender, RoutedEventArgs e)
        {
            CopyRequested?.Invoke(Payload);
            string original = LocalizationService.T("copyBtn");
            string copied = LocalizationService.T("copiedMsg");
            if ((string)copyBtn.Content != copied)
            {
                copyBtn.Content = copied;
                await System.Threading.Tasks.Task.Delay(1200);
                copyBtn.Content = original;
            }
        }
    }
}
