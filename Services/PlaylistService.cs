using DarshanPlayer.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DarshanPlayer.Services
{
    public class PlaylistService
    {
        private readonly Random _rng = new();

        public ObservableCollection<PlaylistItem> Items { get; } = new();
        public int CurrentIndex { get; private set; } = -1;
        public RepeatMode RepeatMode { get; set; } = RepeatMode.None;
        public bool IsShuffle { get; set; } = false;

        public PlaylistItem? Current => CurrentIndex >= 0 && CurrentIndex < Items.Count
            ? Items[CurrentIndex] : null;

        public event EventHandler<PlaylistItem>? PlayRequested;

        public void Add(string filePath)
        {
            if (!Items.Any(i => i.FilePath == filePath))
                Items.Add(new PlaylistItem { FilePath = filePath });
        }

        public void Remove(PlaylistItem item)
        {
            var idx = Items.IndexOf(item);
            Items.Remove(item);
            if (idx <= CurrentIndex && CurrentIndex > 0) CurrentIndex--;
        }

        public void Clear()
        {
            Items.Clear();
            CurrentIndex = -1;
        }

        public void PlayAt(int index)
        {
            if (index < 0 || index >= Items.Count) return;
            if (CurrentIndex >= 0 && CurrentIndex < Items.Count)
                Items[CurrentIndex].IsCurrentlyPlaying = false;
            CurrentIndex = index;
            Items[CurrentIndex].IsCurrentlyPlaying = true;
            PlayRequested?.Invoke(this, Items[CurrentIndex]);
        }

        public void PlayItem(PlaylistItem item)
        {
            var idx = Items.IndexOf(item);
            if (idx >= 0) PlayAt(idx);
        }

        public bool HasNext()
        {
            if (Items.Count == 0) return false;
            if (RepeatMode == RepeatMode.All || IsShuffle) return true;
            return CurrentIndex < Items.Count - 1;
        }

        public void PlayNext()
        {
            if (Items.Count == 0) return;
            if (RepeatMode == RepeatMode.One) { PlayAt(CurrentIndex); return; }
            if (IsShuffle) { PlayAt(_rng.Next(Items.Count)); return; }
            if (CurrentIndex < Items.Count - 1) PlayAt(CurrentIndex + 1);
            else if (RepeatMode == RepeatMode.All) PlayAt(0);
        }

        public void PlayPrevious()
        {
            if (Items.Count == 0) return;
            if (CurrentIndex > 0) PlayAt(CurrentIndex - 1);
            else if (RepeatMode == RepeatMode.All) PlayAt(Items.Count - 1);
        }

        public void Shuffle()
        {
            // Fisher-Yates in-place
            for (int i = Items.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = Items[i];
                Items[i] = Items[j];
                Items[j] = tmp;
            }
            CurrentIndex = Current != null ? Items.IndexOf(Current) : 0;
        }
    }
}
