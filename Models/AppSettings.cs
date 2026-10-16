namespace gamelibrary4.Models
{
    public class AppSettings
    {
        public double Volume { get; set; } = 0.5;     // 0.0 – 1.0
        public bool Shuffle { get; set; } = false;
        public string LastTrackPath { get; set; } = "";
        public double LastPositionSeconds { get; set; } = 0;
        public bool MusicMuted { get; set; } = false;
        public string Theme { get; set; } = "Dark";

    }
}
