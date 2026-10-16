using System;

namespace gamelibrary4.Models
{
    /// <summary>
    /// Одна запись рекорда.
    /// Score — очки. Time — время в секундах (0, если не применимо).
    /// Value — универсальное число (например, длина змейки, кол-во мин и т.п.).
    /// </summary>
    public class ScoreEntry
    {
        public string UserLogin { get; set; } = "";
        public string GameKey { get; set; } = "";      // "snake", "minesweeper", ...
        public int Score { get; set; }
        public int TimeSeconds { get; set; }
        public int Value { get; set; }
        public DateTime AchievedAt { get; set; } = DateTime.Now;
    }
}