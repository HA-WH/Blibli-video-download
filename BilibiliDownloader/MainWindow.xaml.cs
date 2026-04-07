using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace BilibiliDownloader
{
    public partial class MainWindow : Window
    {
        private string _currentUrl = "";
        private string _currentTitle = "bilibili";
        private SearchResponse? _streamData;
        private AppSettings _settings;

        public MainWindow()
        {
            InitializeComponent();

            _settings = AppConfig.Load();

            // 加载保存的路径
            if (!string.IsNullOrEmpty(_settings.DefaultSavePath))
                PathBox.Text = _settings.DefaultSavePath;

            // 加载保存的主题
            if (_settings.IsDarkMode)
            {
                App.SetTheme(true);
                ThemeToggleButton.Content = "\u2600\uFE0F";
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                ToggleMaximize();
            else
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeButton.Content = "\u25A1";
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeButton.Content = "\u2750";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _settings.IsDarkMode = !_settings.IsDarkMode;
            App.SetTheme(_settings.IsDarkMode);
            ThemeToggleButton.Content = _settings.IsDarkMode ? "\u2600\uFE0F" : "\uD83C\uDF19";
            AppConfig.Save(_settings);
        }

        private void Download_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_streamData != null)
            {
                UpdateQualityUI();
            }
        }

        private void ClearUrl_Click(object sender, RoutedEventArgs e)
        {
            UrlBox.Clear();
        }

        private void ClearCookie_Click(object sender, RoutedEventArgs e)
        {
            CookieBox.Clear();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择保存位置"
            };

            if (dialog.ShowDialog() == true)
            {
                PathBox.Text = dialog.FolderName;
                _settings.DefaultSavePath = dialog.FolderName;
                AppConfig.Save(_settings);
            }
        }

        private void PathBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string path = PathBox.Text.Trim();
            if (path != _settings.DefaultSavePath)
            {
                _settings.DefaultSavePath = path;
                AppConfig.Save(_settings);
            }
        }

        private async void Query_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlBox.Text.Trim();

            if (string.IsNullOrEmpty(url))
            {
                ShowStatus("\u274C 请输入视频链接", "#E04D6D");
                return;
            }

            string cookie = CookieBox.Text.Trim();
            QueryButton.IsEnabled = false;
            ShowStatus("🔍  正在查询...", "#FB7299");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "Search.exe",
                    Arguments = $"\"{url}\" \"{cookie}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                string output = await Task.Run(() =>
                {
                    using var p = Process.Start(psi);
                    if (p == null) return "";
                    string result = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    return result;
                });

                if (string.IsNullOrEmpty(output))
                {
                    ShowStatus("\u274C 启动查询失败", "#E04D6D");
                    return;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _streamData = JsonSerializer.Deserialize<SearchResponse>(output, options);

                if (_streamData?.Error != null)
                {
                    ShowStatus($"\u274C {_streamData.Error}", "#E04D6D");
                    return;
                }

                _currentUrl = url;
                _currentTitle = _streamData?.Title ?? "bilibili";

                FillQualityCombos();

                ShowStatus("\u2705  查询成功", "#4CAF50");
            }
            catch (Exception ex)
            {
                ShowStatus($"\u274C 错误: {ex.Message}", "#E04D6D");
            }
            finally
            {
                QueryButton.IsEnabled = true;
            }
        }

        private void FillQualityCombos()
        {
            if (_streamData == null) return;

            // 先取消订阅，防止重复注册
            QualityCombo.SelectionChanged -= QualityCombo_SelectionChanged;
            VideoQualityCombo.SelectionChanged -= QualityCombo_SelectionChanged;
            AudioQualityCombo.SelectionChanged -= QualityCombo_SelectionChanged;

            string? content = (DownloadTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (content != null && content.Contains("音视频"))
            {
                QualityCombo.Visibility = Visibility.Collapsed;
                DualQualityGrid.Visibility = Visibility.Visible;
                DownloadButton.IsEnabled = false;

                VideoQualityCombo.ItemsSource = _streamData.Videos;
                VideoQualityCombo.DisplayMemberPath = "DisplayText";
                VideoQualityCombo.SelectedIndex = 0;
                VideoQualityCombo.SelectionChanged += QualityCombo_SelectionChanged;

                AudioQualityCombo.ItemsSource = _streamData.Audios;
                AudioQualityCombo.DisplayMemberPath = "DisplayText";
                AudioQualityCombo.SelectedIndex = 0;
                AudioQualityCombo.SelectionChanged += QualityCombo_SelectionChanged;
            }
            else if (content != null && content.Contains("仅视频"))
            {
                QualityCombo.Visibility = Visibility.Visible;
                DualQualityGrid.Visibility = Visibility.Collapsed;
                DownloadButton.IsEnabled = false;

                QualityCombo.ItemsSource = _streamData.Videos;
                QualityCombo.DisplayMemberPath = "DisplayText";
                QualityCombo.SelectedIndex = 0;
                QualityCombo.SelectionChanged += QualityCombo_SelectionChanged;
            }
            else
            {
                QualityCombo.Visibility = Visibility.Visible;
                DualQualityGrid.Visibility = Visibility.Collapsed;
                DownloadButton.IsEnabled = false;

                QualityCombo.ItemsSource = _streamData.Audios;
                QualityCombo.DisplayMemberPath = "DisplayText";
                QualityCombo.SelectedIndex = 0;
                QualityCombo.SelectionChanged += QualityCombo_SelectionChanged;
            }
        }

        private void QualityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DownloadButton.IsEnabled = true;
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            string path = PathBox.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                path = "~/Desktop";
            }

            string? content = (DownloadTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string mode = "merge";

            if (content != null && content.Contains("仅音频"))
            {
                mode = "only_audio";
                var audio = (StreamInfo)QualityCombo.SelectedItem;
                LaunchDownload(_currentUrl, _currentTitle, "", audio.Url, path, mode);
            }
            else if (content != null && content.Contains("仅视频"))
            {
                mode = "only_video";
                var video = (VideoStreamInfo)QualityCombo.SelectedItem;
                LaunchDownload(_currentUrl, _currentTitle, video.Url, "", path, mode);
            }
            else
            {
                var video = (VideoStreamInfo)VideoQualityCombo.SelectedItem;
                var audio = (StreamInfo)AudioQualityCombo.SelectedItem;
                LaunchDownload(_currentUrl, _currentTitle, video.Url, audio.Url, path, mode);
            }
        }

        private void LaunchDownload(string url, string title, string videoUrl, string audioUrl, string path, string mode)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.Arguments = $"/c download.exe \"{url}\" \"{title}\" \"{videoUrl}\" \"{audioUrl}\" \"{path}\" {mode}";
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.CreateNoWindow = false;
                p.Start();

                ShowStatus("\u2705 已启动下载", "#4CAF50");
            }
            catch (Exception ex)
            {
                ShowStatus("\u274C 异常：" + ex.Message, "#E04D6D");
            }
        }

        private void UpdateQualityUI()
        {
            if (_streamData != null)
            {
                FillQualityCombos();
            }
            else
            {
                QualityCombo.Visibility = Visibility.Collapsed;
                DualQualityGrid.Visibility = Visibility.Collapsed;
                DownloadButton.IsEnabled = false;
            }
        }

        private void ShowStatus(string text, string color)
        {
            StatusText.Text = text;
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));

            var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            StatusText.BeginAnimation(OpacityProperty, animation);
        }
    }
}
