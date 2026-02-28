using DarshanPlayer.Models;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;

namespace DarshanPlayer.Services
{
    public interface IMediaService : IDisposable
    {
        LibVLC LibVlc { get; }
        MediaPlayer MediaPlayer { get; }

        // State
        bool IsPlaying { get; }
        long Duration { get; }
        long CurrentTime { get; }
        float Position { get; set; }
        int Volume { get; set; }
        bool IsMuted { get; set; }
        float Rate { get; set; }

        // Tracks
        IReadOnlyList<MediaTrackInfo> AudioTracks { get; }
        IReadOnlyList<MediaTrackInfo> SubtitleTracks { get; }
        int CurrentAudioTrack { get; set; }
        int CurrentSubtitleTrack { get; set; }

        // Playback
        void Play();
        void Pause();
        void Stop();
        void PlayFile(string path);
        void SeekTo(long timeMs);
        void SkipBy(long offsetMs);
        void LoadExternalSubtitle(string path);
        void TakeSnapshot(string outputPath);
        void SetAspectRatio(string? ratio);

        // Events
        event EventHandler<TimeChangedEventArgs> TimeChanged;
        event EventHandler<MediaPlayerPositionChangedEventArgs> PositionChanged;
        event EventHandler Playing;
        event EventHandler Paused;
        event EventHandler Stopped;
        event EventHandler EndReached;
        event EventHandler<MediaPlayerLengthChangedEventArgs> LengthChanged;
    }

    public class TimeChangedEventArgs : EventArgs
    {
        public long Time { get; }
        public TimeChangedEventArgs(long time) => Time = time;
    }
}
