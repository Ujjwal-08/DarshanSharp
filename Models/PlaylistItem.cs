using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace DarshanPlayer.Models
{
    public class PlaylistItem : INotifyPropertyChanged
    {
        private bool _isCurrentlyPlaying;

        public string FilePath { get; set; } = string.Empty;
        public string Title => Path.GetFileNameWithoutExtension(FilePath);
        public string Duration { get; set; } = "--:--";

        public bool IsCurrentlyPlaying
        {
            get => _isCurrentlyPlaying;
            set { _isCurrentlyPlaying = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
