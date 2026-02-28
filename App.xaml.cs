using DarshanPlayer.Services;
using System.Windows;

namespace DarshanPlayer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Settings first (needed by other services)
            var settings = new SettingsService();
            settings.Load();
            ServiceLocator.SettingsService = settings;

            // Language manager
            var lang = new LanguageManager();
            ServiceLocator.LanguageManager = lang;

            // Media service
            ServiceLocator.MediaService = new LibVlcMediaService();

            // Playlist service
            var playlist = new PlaylistService();
            playlist.RepeatMode = settings.Current.RepeatMode;
            playlist.IsShuffle = settings.Current.IsShuffle;
            ServiceLocator.PlaylistService = playlist;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ServiceLocator.MediaService?.Dispose();
            base.OnExit(e);
        }
    }
}
