using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using gamelibrary4.Services;

namespace gamelibrary4.Views
{
    public partial class MusicBarView : UserControl
    {
        private bool _isUpdatingUi = false;

        public MusicBarView()
        {
            InitializeComponent();
        }

        private void MusicBarView_Loaded(object sender, RoutedEventArgs e)
        {
            // Подписки
            MusicService.TrackChanged += OnTrackChanged;
            MusicService.PlayStateChanged += OnPlayStateChanged;
            MusicService.PositionChanged += OnPositionChanged;

            // Стартовое состояние UI
            VolumeSlider.Value = MusicService.GetVolume();
            UpdateMuteIcon();
            UpdateShuffleIcon();
            UpdateTrackInfo();
            UpdatePlayIcon();
        }

        private void MusicBarView_Unloaded(object sender, RoutedEventArgs e)
        {
            MusicService.TrackChanged -= OnTrackChanged;
            MusicService.PlayStateChanged -= OnPlayStateChanged;
            MusicService.PositionChanged -= OnPositionChanged;
        }

        // ================================================================
        //  РЕАКЦИИ НА СОБЫТИЯ ПЛЕЕРА
        // ================================================================
        private void OnTrackChanged()
        {
            Dispatcher.Invoke(() =>
            {
                UpdateTrackInfo();
                UpdatePlayIcon();
            });
        }

        private void OnPlayStateChanged()
        {
            Dispatcher.Invoke(() =>
            {
                UpdatePlayIcon();
                UpdateMuteIcon();
            });
        }

        private void OnPositionChanged()
        {
            // Сейчас просто обновляем «играет/пауза»
            Dispatcher.Invoke(UpdatePlayIcon);
        }

        // ================================================================
        //  КНОПКИ
        // ================================================================
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (MusicService.Playlist.Count == 0)
            {
                MessageBox.Show("Папка музыки пуста. Нажмите 📁, чтобы её открыть, и положите туда mp3-файлы.",
                                "Нет треков", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            MusicService.TogglePlayPause();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
            => MusicService.Next();

        private void PrevButton_Click(object sender, RoutedEventArgs e)
            => MusicService.Previous();

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            MusicService.SetShuffle(!MusicService.IsShuffle());
            UpdateShuffleIcon();
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            MusicService.ToggleMute();
            UpdateMuteIcon();
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dir = SettingsService.MusicDir;
            Process.Start(new ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }

        private void RescanButton_Click(object sender, RoutedEventArgs e)
        {
            MusicService.RescanPlaylist();
            UpdateTrackInfo();
            MessageBox.Show($"Плейлист обновлён. Найдено треков: {MusicService.Playlist.Count}",
                            "Обновлено", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ================================================================
        //  ГРОМКОСТЬ
        // ================================================================
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi) return;
            MusicService.SetVolume(e.NewValue);
        }

        // ================================================================
        //  ОБНОВЛЕНИЕ UI
        // ================================================================
        private void UpdateTrackInfo()
        {
            int count = MusicService.Playlist.Count;

            if (count == 0)
            {
                TrackNameText.Text = "— папка пуста —";
                TrackInfoText.Text = "Положите mp3 в папку музыки";
                return;
            }

            var name = MusicService.CurrentTrackName;
            TrackNameText.Text = string.IsNullOrEmpty(name) ? "— выберите трек —" : name;
            TrackInfoText.Text = $"{count} треков в плейлисте";
        }

        private void UpdatePlayIcon()
        {
            PlayPauseButton.Content = MusicService.IsPaused ? "▶" : "⏸";
        }

        private void UpdateMuteIcon()
        {
            MuteButton.Content = MusicService.IsMuted ? "🔇" : "🔊";
        }

        private void UpdateShuffleIcon()
        {
            bool on = MusicService.IsShuffle();
            ShuffleButton.Foreground = on
                ? (Brush)Application.Current.FindResource("CoralBrush")
                : (Brush)Application.Current.FindResource("TextBrush");
        }
    }
}