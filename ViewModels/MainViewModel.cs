using DarshanPlayer.Models;
using DarshanPlayer.Services;
using LibVLCSharp.Shared;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DarshanPlayer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IMediaService _media;
        private readonly PlaylistService _playlist;
        private readonly SettingsService _settings;
        private readonly LanguageManager _lang;
        private readonly DispatcherTimer _uiTimer;

        // ─── Playback State ────────────────────────────────
        private bool _isPlaying;
        private long _position; // ms
        private long _duration; // ms
        private int _volume = 80;
        private bool _isMuted;
        private float _rate = 1.0f;
        private string _currentTitle = "No media loaded";
        private string _currentTimeStr = "0:00";
        private string _durationStr = "0:00";

        // ─── UI State ──────────────────────────────────────
        private bool _isPlaylistVisible;
        private bool _isFullscreen;
        private bool _isPipMode;
        private bool _alwaysOnTop;
        private bool _isControlsVisible = true;
        private bool _isSleepTimerActive;
        private string _sleepTimerLabel = "";
        private LanguageOption _selectedLanguage;
        private ObservableCollection<MediaTrackInfo> _audioTracks = new();
        private ObservableCollection<MediaTrackInfo> _subtitleTracks = new();
        private MediaTrackInfo? _selectedAudioTrack;
        private MediaTrackInfo? _selectedSubtitleTrack;
        private string _selectedAspectRatio = "Default";
        private DispatcherTimer? _sleepTimer;
        private DateTime _sleepTimerEnd;

        public static readonly IReadOnlyList<string> AspectRatios =
            new[] { "Default", "16:9", "4:3", "1:1", "21:9", "Fill" };

        public static readonly IReadOnlyList<float> PlaybackRates =
            new[] { 0.25f, 0.5f, 0.75f, 1.0f, 1.25f, 1.5f, 1.75f, 2.0f };

        public IReadOnlyList<LanguageOption> Languages => LanguageManager.SupportedLanguages;

        // Playlist ViewModel (exposed for XAML binding)
        public PlaylistViewModel? PlaylistVM { get; set; }

        // ─── Properties ────────────────────────────────────
        public bool IsPlaying { get => _isPlaying; private set { _isPlaying = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayPauseIcon)); } }
        public long Position { get => _position; set { _position = value; OnPropertyChanged(); _media.SeekTo(value); } }
        public long Duration { get => _duration; private set { _duration = value; OnPropertyChanged(); } }
        public int Volume
        {
            get => _volume;
            set
            {
                _volume = value; OnPropertyChanged();
                _media.Volume = value;
                _settings.Current.Volume = value;
            }
        }
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value; OnPropertyChanged(); OnPropertyChanged(nameof(MuteIcon));
                _media.IsMuted = value;
            }
        }
        public float Rate
        {
            get => _rate;
            set
            {
                _rate = value; OnPropertyChanged();
                _media.Rate = value;
                _settings.Current.PlaybackRate = value;
            }
        }
        public string CurrentTitle { get => _currentTitle; private set { _currentTitle = value; OnPropertyChanged(); } }
        public string CurrentTimeStr { get => _currentTimeStr; private set { _currentTimeStr = value; OnPropertyChanged(); } }
        public string DurationStr { get => _durationStr; private set { _durationStr = value; OnPropertyChanged(); } }
        public bool IsPlaylistVisible { get => _isPlaylistVisible; set { _isPlaylistVisible = value; OnPropertyChanged(); } }
        public bool IsFullscreen { get => _isFullscreen; set { _isFullscreen = value; OnPropertyChanged(); } }
        public bool IsPipMode { get => _isPipMode; set { _isPipMode = value; OnPropertyChanged(); } }
        public bool AlwaysOnTop { get => _alwaysOnTop; set { _alwaysOnTop = value; OnPropertyChanged(); _settings.Current.AlwaysOnTop = value; } }
        public bool IsControlsVisible { get => _isControlsVisible; set { _isControlsVisible = value; OnPropertyChanged(); } }
        public string PlayPauseIcon => IsPlaying ? "⏸" : "▶";
        public string MuteIcon => IsMuted ? "🔇" : "🔊";
        public bool IsSleepTimerActive { get => _isSleepTimerActive; private set { _isSleepTimerActive = value; OnPropertyChanged(); } }
        public string SleepTimerLabel { get => _sleepTimerLabel; private set { _sleepTimerLabel = value; OnPropertyChanged(); } }

        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value; OnPropertyChanged();
                _lang.Apply(value.Code);
                _settings.Current.Language = value.Code;
                _settings.Save();
            }
        }
        public ObservableCollection<MediaTrackInfo> AudioTracks { get => _audioTracks; private set { _audioTracks = value; OnPropertyChanged(); } }
        public ObservableCollection<MediaTrackInfo> SubtitleTracks { get => _subtitleTracks; private set { _subtitleTracks = value; OnPropertyChanged(); } }
        public MediaTrackInfo? SelectedAudioTrack
        {
            get => _selectedAudioTrack;
            set { _selectedAudioTrack = value; OnPropertyChanged(); if (value != null) _media.CurrentAudioTrack = value.Id; }
        }
        public MediaTrackInfo? SelectedSubtitleTrack
        {
            get => _selectedSubtitleTrack;
            set { _selectedSubtitleTrack = value; OnPropertyChanged(); if (value != null) _media.CurrentSubtitleTrack = value.Id; }
        }
        public string SelectedAspectRatio
        {
            get => _selectedAspectRatio;
            set
            {
                _selectedAspectRatio = value; OnPropertyChanged();
                _media.SetAspectRatio(value == "Default" || value == "Fill" ? null : value);
            }
        }

        public PlaylistService Playlist => _playlist;
        public ObservableCollection<string> RecentFiles { get; } = new();

        // ─── Commands ──────────────────────────────────────
        public ICommand PlayPauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand OpenSubtitleCommand { get; }
        public ICommand SkipForwardCommand { get; }
        public ICommand SkipBackCommand { get; }
        public ICommand NextTrackCommand { get; }
        public ICommand PrevTrackCommand { get; }
        public ICommand ToggleMuteCommand { get; }
        public ICommand TogglePlaylistCommand { get; }
        public ICommand ToggleFullscreenCommand { get; }
        public ICommand ToggleAlwaysOnTopCommand { get; }
        public ICommand TakeScreenshotCommand { get; }
        public ICommand SetSleepTimerCommand { get; }
        public ICommand CancelSleepTimerCommand { get; }
        public ICommand PlayRecentCommand { get; }

        public MainViewModel(IMediaService media, PlaylistService playlist, SettingsService settings, LanguageManager lang)
        {
            _media = media;
            _playlist = playlist;
            _settings = settings;
            _lang = lang;

            // Load persisted settings
            _volume = settings.Current.Volume;
            _rate = settings.Current.PlaybackRate;
            _alwaysOnTop = settings.Current.AlwaysOnTop;
            _media.Volume = _volume;
            _media.Rate = _rate;

            _selectedLanguage = Languages.FirstOrDefault(l => l.Code == settings.Current.Language)
                ?? Languages[0];

            foreach (var f in settings.Current.RecentFiles)
                RecentFiles.Add(f);

            _selectedAudioTrack = default!;
            _selectedSubtitleTrack = default!;

            // Wire media events
            _media.Playing += (_, _) => App.Current.Dispatcher.Invoke(() => { IsPlaying = true; RefreshTracks(); });
            _media.Paused += (_, _) => App.Current.Dispatcher.Invoke(() => IsPlaying = false);
            _media.Stopped += (_, _) => App.Current.Dispatcher.Invoke(() => { IsPlaying = false; _position = 0; OnPropertyChanged(nameof(Position)); CurrentTimeStr = "0:00"; });
            _media.EndReached += (_, _) => App.Current.Dispatcher.Invoke(() => { if (_playlist.HasNext()) _playlist.PlayNext(); });
            _media.LengthChanged += (_, e) => App.Current.Dispatcher.Invoke(() =>
            {
                Duration = e.Length;
                DurationStr = FormatTime(e.Length);
            });
            _media.TimeChanged += (_, e) => App.Current.Dispatcher.Invoke(() =>
            {
                _position = e.Time;
                OnPropertyChanged(nameof(Position));
                CurrentTimeStr = FormatTime(e.Time);
            });

            _playlist.PlayRequested += (_, item) => { _media.PlayFile(item.FilePath); CurrentTitle = item.Title; };

            // UI timer for sleep timer countdown
            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();

            // Commands
            PlayPauseCommand = new RelayCommand(() => 
            { 
                if (_media.IsPlaying) _media.Pause(); 
                else if (_playlist.Current != null || _media.MediaPlayer.Media != null) _media.Play(); 
            });
            StopCommand = new RelayCommand(_media.Stop);
            SkipForwardCommand = new RelayCommand(() => _media.SkipBy(10_000));
            SkipBackCommand = new RelayCommand(() => _media.SkipBy(-10_000));
            NextTrackCommand = new RelayCommand(_playlist.PlayNext);
            PrevTrackCommand = new RelayCommand(_playlist.PlayPrevious);
            ToggleMuteCommand = new RelayCommand(() => IsMuted = !IsMuted);
            TogglePlaylistCommand = new RelayCommand(() => IsPlaylistVisible = !IsPlaylistVisible);
            ToggleFullscreenCommand = new RelayCommand(() => IsFullscreen = !IsFullscreen);
            ToggleAlwaysOnTopCommand = new RelayCommand(() => AlwaysOnTop = !AlwaysOnTop);

            OpenFileCommand = new RelayCommand(OpenFile);
            OpenFolderCommand = new RelayCommand(OpenFolder);
            OpenSubtitleCommand = new RelayCommand(OpenSubtitle);
            TakeScreenshotCommand = new RelayCommand(TakeScreenshot);
            SetSleepTimerCommand = new RelayCommand(p => SetSleepTimer((int)p!));
            CancelSleepTimerCommand = new RelayCommand(CancelSleepTimer);
            PlayRecentCommand = new RelayCommand(p => { if (p is string path) PlayPath(path); });
        }

        private void OpenFile()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Media Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm;*.mp3;*.aac;*.flac;*.wav;*.ogg;*.m4a;*.m4v;*.ts;*.m2ts;*.3gp|All Files|*.*",
                Multiselect = true,
                Title = "Open Media File(s)"
            };
            if (dlg.ShowDialog() == true)
            {
                foreach (var f in dlg.FileNames) { _playlist.Add(f); _settings.AddRecentFile(f); }
                if (!string.IsNullOrEmpty(dlg.FileName)) _playlist.PlayAt(_playlist.Items.Count - dlg.FileNames.Length);
                RefreshRecentFiles();
            }
        }

        private void OpenFolder()
        {
            var dlg = new OpenFileDialog { Title = "Select any file in the folder", ValidateNames = false, CheckFileExists = false, FileName = "Select Folder" };
            if (dlg.ShowDialog() == true)
            {
                var dir = Path.GetDirectoryName(dlg.FileName)!;
                var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mp3", ".aac", ".flac", ".wav", ".ogg", ".m4a", ".m4v", ".ts", ".3gp" };
                var files = Directory.GetFiles(dir).Where(f => exts.Contains(Path.GetExtension(f))).OrderBy(f => f).ToList();
                foreach (var f in files) _playlist.Add(f);
                if (files.Any()) _playlist.PlayAt(_playlist.Items.IndexOf(_playlist.Items.First(i => i.FilePath == files[0])));
            }
        }

        private void OpenSubtitle()
        {
            var dlg = new OpenFileDialog { Filter = "Subtitle Files|*.srt;*.ass;*.ssa;*.vtt;*.sub|All Files|*.*", Title = "Load External Subtitle" };
            if (dlg.ShowDialog() == true) _media.LoadExternalSubtitle(dlg.FileName);
        }

        private void PlayPath(string path)
        {
            _playlist.Add(path);
            _playlist.PlayAt(_playlist.Items.IndexOf(_playlist.Items.First(i => i.FilePath == path)));
        }

        private void TakeScreenshot()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DarshanPlayer");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            _media.TakeSnapshot(path);
            MessageBox.Show($"Screenshot saved:\n{path}", "Screenshot", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetSleepTimer(int minutes)
        {
            _sleepTimer?.Stop();
            _sleepTimerEnd = DateTime.Now.AddMinutes(minutes);
            _sleepTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _sleepTimer.Tick += SleepTimer_Tick;
            _sleepTimer.Start();
            IsSleepTimerActive = true;
        }

        private void CancelSleepTimer()
        {
            _sleepTimer?.Stop();
            _sleepTimer = null;
            IsSleepTimerActive = false;
            SleepTimerLabel = "";
        }

        private void SleepTimer_Tick(object? s, EventArgs e)
        {
            var remaining = _sleepTimerEnd - DateTime.Now;
            if (remaining <= TimeSpan.Zero)
            {
                _media.Pause();
                CancelSleepTimer();
            }
            else
            {
                SleepTimerLabel = $"Sleep: {remaining:mm\\:ss}";
            }
        }

        private void UiTimer_Tick(object? s, EventArgs e) { }

        private void RefreshTracks()
        {
            AudioTracks = new ObservableCollection<MediaTrackInfo>(_media.AudioTracks);
            SubtitleTracks = new ObservableCollection<MediaTrackInfo>(_media.SubtitleTracks);
        }

        private void RefreshRecentFiles()
        {
            RecentFiles.Clear();
            foreach (var f in _settings.Current.RecentFiles) RecentFiles.Add(f);
        }

        private static string FormatTime(long ms)
        {
            if (ms < 0) ms = 0;
            var t = TimeSpan.FromMilliseconds(ms);
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}" : $"{t.Minutes}:{t.Seconds:D2}";
        }

        public void AddDroppedFiles(IEnumerable<string> paths)
        {
            var added = false;
            foreach (var p in paths) { _playlist.Add(p); if (!added) { added = true; } }
            if (added) _playlist.PlayAt(_playlist.Items.Count - 1);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
