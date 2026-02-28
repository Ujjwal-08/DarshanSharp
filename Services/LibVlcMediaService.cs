using DarshanPlayer.Models;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DarshanPlayer.Services
{
    public class LibVlcMediaService : IMediaService
    {
        public LibVLC LibVlc { get; }
        public MediaPlayer MediaPlayer { get; }

        public bool IsPlaying => MediaPlayer.IsPlaying;
        public long Duration => MediaPlayer.Length;
        public long CurrentTime => MediaPlayer.Time;
        public float Position
        {
            get => MediaPlayer.Position;
            set => MediaPlayer.Position = value;
        }
        public int Volume
        {
            get => MediaPlayer.Volume;
            set => MediaPlayer.Volume = Math.Clamp(value, 0, 200);
        }
        public bool IsMuted
        {
            get => MediaPlayer.Mute;
            set => MediaPlayer.Mute = value;
        }
        public float Rate
        {
            get => MediaPlayer.Rate;
            set => MediaPlayer.SetRate(value);
        }

        public IReadOnlyList<MediaTrackInfo> AudioTracks
        {
            get
            {
                var desc = MediaPlayer.AudioTrackDescription;
                if (desc == null) return new List<MediaTrackInfo>();
                return desc.Select(t => new MediaTrackInfo(t.Id, t.Name)).ToList();
            }
        }

        public IReadOnlyList<MediaTrackInfo> SubtitleTracks
        {
            get
            {
                var desc = MediaPlayer.SpuDescription;
                if (desc == null) return new List<MediaTrackInfo>();
                return desc.Select(t => new MediaTrackInfo(t.Id, t.Name)).ToList();
            }
        }

        public int CurrentAudioTrack
        {
            get => MediaPlayer.AudioTrack;
            set => MediaPlayer.SetAudioTrack(value);
        }
        public int CurrentSubtitleTrack
        {
            get => MediaPlayer.Spu;
            set => MediaPlayer.SetSpu(value);
        }

        // Events
        public event EventHandler<TimeChangedEventArgs>? TimeChanged;
        public event EventHandler<MediaPlayerPositionChangedEventArgs>? PositionChanged;
        public event EventHandler? Playing;
        public event EventHandler? Paused;
        public event EventHandler? Stopped;
        public event EventHandler? EndReached;
        public event EventHandler<MediaPlayerLengthChangedEventArgs>? LengthChanged;

        public LibVlcMediaService()
        {
            Core.Initialize();
            LibVlc = new LibVLC(enableDebugLogs: false);
            MediaPlayer = new MediaPlayer(LibVlc);

            MediaPlayer.TimeChanged += (_, e) => TimeChanged?.Invoke(this, new TimeChangedEventArgs(e.Time));
            MediaPlayer.PositionChanged += (_, e) => PositionChanged?.Invoke(this, e);
            MediaPlayer.Playing += (_, _) => Playing?.Invoke(this, EventArgs.Empty);
            MediaPlayer.Paused += (_, _) => Paused?.Invoke(this, EventArgs.Empty);
            MediaPlayer.Stopped += (_, _) => Stopped?.Invoke(this, EventArgs.Empty);
            MediaPlayer.EndReached += (_, _) => EndReached?.Invoke(this, EventArgs.Empty);
            MediaPlayer.LengthChanged += (_, e) => LengthChanged?.Invoke(this, e);
        }

        public void Play() => MediaPlayer.Play();
        public void Pause() => MediaPlayer.Pause();
        public void Stop() => MediaPlayer.Stop();

        public void PlayFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            var media = new Media(LibVlc, path, FromType.FromPath);
            MediaPlayer.Play(media);
            media.Dispose();
        }

        public void SeekTo(long timeMs)
        {
            if (MediaPlayer.IsSeekable)
                MediaPlayer.Time = timeMs;
        }

        public void SkipBy(long offsetMs)
        {
            var length = MediaPlayer.Length;
            if (length <= 0) return; // Cannot skip if no duration
            var newTime = Math.Clamp(MediaPlayer.Time + offsetMs, 0, length);
            SeekTo(newTime);
        }

        public void LoadExternalSubtitle(string path)
        {
            MediaPlayer.AddSlave(MediaSlaveType.Subtitle, new Uri(path).AbsoluteUri, true);
        }

        public void TakeSnapshot(string outputPath)
        {
            MediaPlayer.TakeSnapshot(0, outputPath, 0, 0);
        }

        public void SetAspectRatio(string? ratio)
        {
            MediaPlayer.AspectRatio = ratio;
        }

        public void Dispose()
        {
            MediaPlayer.Stop();
            MediaPlayer.Dispose();
            LibVlc.Dispose();
        }
    }
}
