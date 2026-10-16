using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;
using gamelibrary4.Models;

namespace gamelibrary4.Services
{
    /// <summary>
    /// Плеер на базе встроенного WPF MediaPlayer.
    /// Поддерживает mp3, wav, wma, m4a (зависит от кодеков Windows).
    /// </summary>
    public static class MusicService
    {
        private static readonly MediaPlayer _player = new MediaPlayer();
        private static readonly DispatcherTimer _positionTimer;

        private static List<string> _playlist = new List<string>();
        private static int _currentIndex = -1;
        private static readonly Random _rng = new Random();

        public static bool IsPlaying => _player.Position > TimeSpan.Zero && !IsPaused;
        public static bool IsPaused { get; private set; }
        public static bool IsMuted => _player.IsMuted;

        public static IReadOnlyList<string> Playlist => _playlist;
        public static string CurrentTrackPath =>
            (_currentIndex >= 0 && _currentIndex < _playlist.Count) ? _playlist[_currentIndex] : "";
        public static string CurrentTrackName =>
            string.IsNullOrEmpty(CurrentTrackPath) ? "" : Path.GetFileNameWithoutExtension(CurrentTrackPath);

        // ---- События ----
        public static event Action TrackChanged;      // переключился трек
        public static event Action PlayStateChanged;  // play/pause/mute
        public static event Action PositionChanged;   // обновление позиции (раз в 500 мс)

        static MusicService()
        {
            // Стартовая громкость
            _player.Volume = SettingsService.Current.Volume;
            _player.IsMuted = SettingsService.Current.MusicMuted;

            // Автопереход к следующему треку
            _player.MediaEnded += (s, e) => Next(auto: true);

            // Таймер обновления позиции
            _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _positionTimer.Tick += (s, e) => PositionChanged?.Invoke();
            _positionTimer.Start();

            // Загружаем плейлист из папки
            RescanPlaylist();
        }

        // ================================================================
        //  ПЛЕЙЛИСТ
        // ================================================================
        public static void RescanPlaylist()
        {
            var dir = SettingsService.MusicDir;
            var exts = new[] { ".mp3", ".wav", ".wma", ".m4a", ".aac", ".flac" };
            _playlist = Directory.Exists(dir)
                ? Directory.GetFiles(dir)
                    .Where(f => exts.Contains(Path.GetExtension(f).ToLower()))
                    .OrderBy(f => f)
                    .ToList()
                : new List<string>();
        }

        // ================================================================
        //  ВОСПРОИЗВЕДЕНИЕ
        // ================================================================
        public static void PlayTrack(int index, bool restorePosition = false)
        {
            if (_playlist.Count == 0) return;
            if (index < 0 || index >= _playlist.Count) return;

            _currentIndex = index;
            _player.Open(new Uri(_playlist[index]));

            if (restorePosition && SettingsService.Current.LastTrackPath == _playlist[index])
            {
                // восстановим позицию после открытия
                _player.MediaOpened += RestorePositionOnce;
            }

            _player.Play();
            IsPaused = false;
            TrackChanged?.Invoke();
            PlayStateChanged?.Invoke();
        }

        private static void RestorePositionOnce(object sender, EventArgs e)
        {
            _player.MediaOpened -= RestorePositionOnce;
            var pos = TimeSpan.FromSeconds(SettingsService.Current.LastPositionSeconds);
            if (pos < _player.NaturalDuration.TimeSpan)
                _player.Position = pos;
        }

        public static void TogglePlayPause()
        {
            if (_playlist.Count == 0) return;

            if (_currentIndex < 0)
            {
                // Ещё ничего не играло — начнём с первого
                PlayTrack(0);
                return;
            }

            if (IsPaused)
            {
                _player.Play();
                IsPaused = false;
            }
            else
            {
                _player.Pause();
                IsPaused = true;
            }
            PlayStateChanged?.Invoke();
        }

        public static void Next(bool auto = false)
        {
            if (_playlist.Count == 0) return;

            int nextIndex;
            if (SettingsService.Current.Shuffle && _playlist.Count > 1)
            {
                do { nextIndex = _rng.Next(_playlist.Count); }
                while (nextIndex == _currentIndex);
            }
            else
            {
                nextIndex = (_currentIndex + 1) % _playlist.Count;
            }

            PlayTrack(nextIndex);
        }

        public static void Previous()
        {
            if (_playlist.Count == 0) return;

            int prevIndex = _currentIndex - 1;
            if (prevIndex < 0) prevIndex = _playlist.Count - 1;

            PlayTrack(prevIndex);
        }

        // ================================================================
        //  ГРОМКОСТЬ / MUTE
        // ================================================================
        public static void SetVolume(double volume)
        {
            volume = Math.Max(0, Math.Min(1, volume));
            _player.Volume = volume;
            SettingsService.Current.Volume = volume;
            SettingsService.Save();
        }

        public static double GetVolume() => _player.Volume;

        public static void ToggleMute()
        {
            _player.IsMuted = !_player.IsMuted;
            SettingsService.Current.MusicMuted = _player.IsMuted;
            SettingsService.Save();
            PlayStateChanged?.Invoke();
        }

        public static void SetShuffle(bool on)
        {
            SettingsService.Current.Shuffle = on;
            SettingsService.Save();
        }

        public static bool IsShuffle() => SettingsService.Current.Shuffle;

        // ================================================================
        //  СОХРАНЕНИЕ СОСТОЯНИЯ ПЕРЕД ВЫХОДОМ
        // ================================================================
        public static void SaveState()
        {
            if (_currentIndex >= 0 && _currentIndex < _playlist.Count)
            {
                SettingsService.Current.LastTrackPath = _playlist[_currentIndex];
                SettingsService.Current.LastPositionSeconds = _player.Position.TotalSeconds;
                SettingsService.Save();
            }
        }

        /// <summary>
        /// Восстановить последний трек при старте приложения (без авто-игры).
        /// </summary>
        public static void RestoreLastTrack()
        {
            if (string.IsNullOrEmpty(SettingsService.Current.LastTrackPath)) return;

            int idx = _playlist.FindIndex(p => p == SettingsService.Current.LastTrackPath);
            if (idx < 0) return;

            _currentIndex = idx;
            _player.Open(new Uri(_playlist[idx]));

            _player.MediaOpened += (s, e) =>
            {
                var pos = TimeSpan.FromSeconds(SettingsService.Current.LastPositionSeconds);
                if (_player.NaturalDuration.HasTimeSpan && pos < _player.NaturalDuration.TimeSpan)
                    _player.Position = pos;
                // Не запускаем воспроизведение — юзер сам нажмёт Play
                IsPaused = true;
                TrackChanged?.Invoke();
                PlayStateChanged?.Invoke();
            };
        }
    }
}