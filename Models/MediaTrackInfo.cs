namespace DarshanPlayer.Models
{
    /// <summary>Lightweight track info — decouples UI from LibVLC types.</summary>
    public record MediaTrackInfo(int Id, string Name)
    {
        public override string ToString() => Name;
    }
}
