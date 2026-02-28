using DarshanPlayer.Models;
using DarshanPlayer.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DarshanPlayer.ViewModels
{
    public class PlaylistViewModel : INotifyPropertyChanged
    {
        public PlaylistService Service { get; }

        public ICommand PlaySelectedCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ShuffleCommand { get; }
        public ICommand ToggleRepeatCommand { get; }
        public ICommand ToggleShuffleCommand { get; }

        private PlaylistItem? _selectedItem;
        public PlaylistItem? SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); }
        }

        public PlaylistViewModel(PlaylistService service)
        {
            Service = service;
            PlaySelectedCommand = new RelayCommand(() => { if (SelectedItem != null) Service.PlayItem(SelectedItem); });
            RemoveCommand = new RelayCommand(() => { if (SelectedItem != null) Service.Remove(SelectedItem); });
            ClearCommand = new RelayCommand(Service.Clear);
            ShuffleCommand = new RelayCommand(Service.Shuffle);
            ToggleRepeatCommand = new RelayCommand(() =>
            {
                Service.RepeatMode = Service.RepeatMode switch
                {
                    RepeatMode.None => RepeatMode.One,
                    RepeatMode.One => RepeatMode.All,
                    RepeatMode.All => RepeatMode.None,
                    _ => RepeatMode.None
                };
                OnPropertyChanged(nameof(RepeatIcon));
            });
            ToggleShuffleCommand = new RelayCommand(() =>
            {
                Service.IsShuffle = !Service.IsShuffle;
                OnPropertyChanged(nameof(ShuffleIcon));
            });
        }

        public string RepeatIcon => Service.RepeatMode switch
        {
            RepeatMode.None => "🔁",
            RepeatMode.One => "🔂",
            RepeatMode.All => "🔁",
            _ => "🔁"
        };
        public string ShuffleIcon => Service.IsShuffle ? "🔀" : "🔀";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
