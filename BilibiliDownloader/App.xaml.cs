using System.Windows;

namespace BilibiliDownloader
{
    public partial class App : Application
    {
        public static void SetTheme(bool isDark)
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri(isDark
                    ? "Themes/DarkTheme.xaml"
                    : "Themes/LightTheme.xaml", UriKind.Relative)
            };

            var mergedDicts = Current.Resources.MergedDictionaries;
            if (mergedDicts.Count > 0)
                mergedDicts[0] = dict;
            else
                mergedDicts.Insert(0, dict);
        }
    }
}
