using System.Collections.Generic;

namespace DarshanPlayer.Models
{
    public class AppSettings
    {
        public int Volume { get; set; } = 80;
        public string Language { get; set; } = "en";
        public bool AlwaysOnTop { get; set; } = false;
        public double WindowWidth { get; set; } = 1100;
        public double WindowHeight { get; set; } = 700;
        public List<string> RecentFiles { get; set; } = new();
        public RepeatMode RepeatMode { get; set; } = RepeatMode.None;
        public bool IsShuffle { get; set; } = false;
        public float PlaybackRate { get; set; } = 1.0f;
    }
}
