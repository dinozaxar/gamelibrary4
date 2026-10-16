using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using gamelibrary4.Models;

namespace gamelibrary4.Services
{
    public static class ScoreService
    {
        private static string DataDir
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "gamelibrary4");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        private static string ScoresFile => Path.Combine(DataDir, "scores.json");

        public static List<ScoreEntry> LoadAll()
        {
            if (!File.Exists(ScoresFile)) return new List<ScoreEntry>();
            try
            {
                var json = File.ReadAllText(ScoresFile);
                return JsonSerializer.Deserialize<List<ScoreEntry>>(json) ?? new List<ScoreEntry>();
            }
            catch
            {
                return new List<ScoreEntry>();
            }
        }

        public static void SaveAll(List<ScoreEntry> entries)
        {
            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ScoresFile, json);
        }

        /// <summary>
        /// Добавляет запись рекорда для текущего пользователя.
        /// </summary>
        public static void AddScore(string gameKey, int score, int timeSeconds, int value = 0)
        {
            if (UserService.CurrentUser == null) return;

            var entry = new ScoreEntry
            {
                UserLogin = UserService.CurrentUser.Login,
                GameKey = gameKey,
                Score = score,
                TimeSeconds = timeSeconds,
                Value = value,
                AchievedAt = DateTime.Now
            };

            var all = LoadAll();
            all.Add(entry);
            SaveAll(all);
        }

        /// <summary>
        /// Возвращает записи по игре, отсортированные по критерию.
        /// </summary>
        public static List<ScoreEntry> GetForGame(string gameKey, ScoreSort sort, bool onlyCurrentUser = false)
        {
            var q = LoadAll().Where(s => s.GameKey == gameKey);

            if (onlyCurrentUser && UserService.CurrentUser != null)
                q = q.Where(s => s.UserLogin == UserService.CurrentUser.Login);

            return sort switch
            {
                ScoreSort.ScoreDesc => q.OrderByDescending(s => s.Score).ThenBy(s => s.TimeSeconds).ToList(),
                ScoreSort.ScoreAsc => q.OrderBy(s => s.Score).ThenBy(s => s.TimeSeconds).ToList(),
                ScoreSort.TimeAsc => q.Where(s => s.TimeSeconds > 0).OrderBy(s => s.TimeSeconds).ToList(),
                ScoreSort.TimeDesc => q.Where(s => s.TimeSeconds > 0).OrderByDescending(s => s.TimeSeconds).ToList(),
                ScoreSort.DateDesc => q.OrderByDescending(s => s.AchievedAt).ToList(),
                ScoreSort.DateAsc => q.OrderBy(s => s.AchievedAt).ToList(),
                _ => q.ToList()
            };
        }
        /// <summary>
        /// Количество записей для указанного пользователя.
        /// </summary>
        public static int CountForUser(string login)
        {
            return LoadAll().Count(s => s.UserLogin == login);
        }

        /// <summary>
        /// Любимая игра пользователя (по количеству записей).
        /// </summary>
        public static string FavoriteGameForUser(string login)
        {
            var entries = LoadAll().Where(s => s.UserLogin == login).ToList();
            if (entries.Count == 0) return "—";
            return entries
                .GroupBy(s => s.GameKey)
                .OrderByDescending(g => g.Count())
                .First().Key;
        }
    }

    public enum ScoreSort
    {
        ScoreDesc,
        ScoreAsc,
        TimeAsc,
        TimeDesc,
        DateDesc,
        DateAsc
    }
}