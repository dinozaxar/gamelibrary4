using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using gamelibrary4.Games.TicTacToe.Network;

namespace gamelibrary4.Games.TicTacToe
{
    public partial class TicTacToeView : UserControl
    {
        private const int DefaultPort = 55555;

        // ---- Сетевое ----
        private GameServer _server;
        private GameClient _client;
        private bool _isHost;
        private bool _isConnected;

        // ---- Игровая логика (у хоста — авторитет) ----
        private readonly TicTacToeGame _game = new TicTacToeGame();
        private char _mySymbol;   // 'X' или 'O'
        private char _turn;       // чей ход

        // ---- Ссылки на кнопки поля ----
        private readonly Button[,] _cells = new Button[3, 3];

        public TicTacToeView()
        {
            InitializeComponent();
        }

        // ================================================================
        //  ЛОББИ
        // ================================================================
        private void HostButton_Click(object sender, RoutedEventArgs e)
        {
            LobbyErrorText.Visibility = Visibility.Collapsed;

            _isHost = true;
            _mySymbol = 'X';
            _turn = 'X';
            RoleText.Text = "Хост · X";
            _game.Reset();

            _server = new GameServer(DefaultPort);
            _server.Connected += OnConnectedAsHost;
            _server.MessageReceived += OnMessageReceived;
            _server.Disconnected += OnDisconnected;

            if (!_server.Start())
            {
                ShowLobbyError("Не удалось открыть порт. Возможно, занят.");
                return;
            }

            string ip = GetLocalIp();
            ShowLobbyError($"Ждём подключения...\nВаш IP: {ip}\nПорт: {DefaultPort}\nСкажите другу подключиться к этому IP.", false);
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            LobbyErrorText.Visibility = Visibility.Collapsed;

            string host = HostIpBox.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                ShowLobbyError("Введите IP-адрес хоста.");
                return;
            }

            _isHost = false;
            _mySymbol = 'O';
            _turn = 'X';
            RoleText.Text = "Гость · O";
            _game.Reset();

            _client = new GameClient();
            _client.Connected += OnConnectedAsGuest;
            _client.MessageReceived += OnMessageReceived;
            _client.Disconnected += OnDisconnected;

            // Асинхронное подключение
            var task = _client.ConnectAsync(host, DefaultPort);
            task.ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (t.Result != null)
                    {
                        ShowLobbyError(t.Result);
                        _client = null;
                    }
                });
            });
        }

        private void OnConnectedAsHost()
        {
            Dispatcher.Invoke(() =>
            {
                _isConnected = true;
                LobbyPanel.Visibility = Visibility.Collapsed;
                GamePanel.Visibility = Visibility.Visible;
                BuildBoard();
                UpdateTurnStatus();
                InfoText.Text = "Соперник подключён. Ваш ход.";

                // Хост первым шлёт стартовое состояние
                SendState();
            });
        }

        private void OnConnectedAsGuest()
        {
            Dispatcher.Invoke(() =>
            {
                _isConnected = true;
                LobbyPanel.Visibility = Visibility.Collapsed;
                GamePanel.Visibility = Visibility.Visible;
                BuildBoard();
                UpdateTurnStatus();
                InfoText.Text = "Подключено. Ждём ход соперника.";
            });
        }

        private void OnDisconnected(string reason)
        {
            Dispatcher.Invoke(() =>
            {
                if (!_isConnected) return;
                _isConnected = false;

                MessageBox.Show(reason, "Соединение потеряно",
                                MessageBoxButton.OK, MessageBoxImage.Warning);

                // Возвращаемся в лобби
                GamePanel.Visibility = Visibility.Collapsed;
                LobbyPanel.Visibility = Visibility.Visible;
                InfoText.Text = "";
                StatusText.Text = "Ход: X";
            });
        }

        private void LeaveButton_Click(object sender, RoutedEventArgs e)
        {
            CleanupNetwork();
            GamePanel.Visibility = Visibility.Collapsed;
            LobbyPanel.Visibility = Visibility.Visible;
            LobbyErrorText.Visibility = Visibility.Collapsed;
        }

        private void CleanupNetwork()
        {
            _isConnected = false;
            try { _server?.Stop(); } catch { }
            try { _client?.Stop(); } catch { }
            _server = null;
            _client = null;
        }

        // ================================================================
        //  ДОСКА
        // ================================================================
        private void BuildBoard()
        {
            BoardGrid.Children.Clear();

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    var btn = new Button
                    {
                        Width = 130,
                        Height = 130,
                        Margin = new Thickness(6),
                        FontSize = 54,
                        FontWeight = FontWeights.Bold,
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Tag = (x, y),
                        Focusable = false
                    };

                    btn.Template = BuildCellTemplate();
                    btn.Click += Cell_Click;

                    _cells[x, y] = btn;
                    BoardGrid.Children.Add(btn);
                    RefreshCell(x, y);
                }
            }
        }

        private ControlTemplate BuildCellTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(16));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;

            var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty, (Brush)Application.Current.FindResource("OverlayWeakBrush"), "bd"));
            template.Triggers.Add(hover);

            return template;
        }

        private void RefreshCell(int x, int y)
        {
            var btn = _cells[x, y];
            char c = _game[x, y];

            if (c == ' ')
            {
                btn.Content = "";
                btn.Background = (Brush)FindResource("CellOpenBrush");
            }
            else if (c == 'X')
            {
                btn.Content = "✕";
                btn.Foreground = (Brush)FindResource("XBrush");
                btn.Background = (Brush)FindResource("OverlayWeakBrush");
            }
            else
            {
                btn.Content = "◯";
                btn.Foreground = (Brush)FindResource("OBrush");
                btn.Background = (Brush)FindResource("OverlayWeakBrush");
            }
        }

        private void RefreshBoard()
        {
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    RefreshCell(x, y);
        }

        // ================================================================
        //  ХОДЫ
        // ================================================================
        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            if (!_isConnected) return;
            if (_turn != _mySymbol) return;

            var btn = (Button)sender;
            var (x, y) = ((int, int))btn.Tag;

            if (!_game.IsEmpty(x, y)) return;

            if (_isHost)
            {
                // Хост сам применяет ход и рассылает
                ApplyMove(x, y, _mySymbol);
                SendState();
            }
            else
            {
                // Гость просит хост
                var msg = new NetworkMessage
                {
                    Type = "move",
                    X = x,
                    Y = y
                };
                _client?.Send(msg);
                InfoText.Text = "Ход отправлен. Ждём ответа...";
            }
        }

        private void ApplyMove(int x, int y, char symbol)
        {
            _game.Set(x, y, symbol);
            RefreshCell(x, y);

            char result = _game.CheckResult();
            if (result == 'X' || result == 'O')
            {
                _turn = ' ';  // игра окончена
                UpdateTurnStatus();
                InfoText.Text = result == _mySymbol ? "🎉 Вы победили!" : "😔 Вы проиграли.";
                return;
            }
            if (result == 'D')
            {
                _turn = ' ';
                UpdateTurnStatus();
                InfoText.Text = "🤝 Ничья.";
                return;
            }

            _turn = symbol == 'X' ? 'O' : 'X';
            UpdateTurnStatus();
            InfoText.Text = _turn == _mySymbol ? "Ваш ход" : "Ход соперника";
        }

        private void UpdateTurnStatus()
        {
            if (_turn == ' ')
                StatusText.Text = "Игра окончена";
            else
                StatusText.Text = $"Ход: {_turn}";
        }

        // ================================================================
        //  СЕТЕВЫЕ СООБЩЕНИЯ
        // ================================================================
        private void SendState()
        {
            if (!_isHost) return;

            char result = _game.CheckResult();
            string resultStr = result == ' ' ? "" : result.ToString();

            var msg = new NetworkMessage
            {
                Type = "state",
                Board = _game.Serialize(),
                Turn = _turn.ToString(),
                Result = resultStr
            };

            _server?.Send(msg);
        }

        private void OnMessageReceived(NetworkMessage msg)
        {
            Dispatcher.Invoke(() =>
            {
                switch (msg.Type)
                {
                    case "move":
                        // Хост получил ход от гостя
                        if (!_isHost) return;
                        if (_turn != 'O') return;                 // сейчас не ход гостя
                        if (msg.X < 0 || msg.X > 2 || msg.Y < 0 || msg.Y > 2) return;
                        if (!_game.IsEmpty(msg.X, msg.Y)) return;

                        ApplyMove(msg.X, msg.Y, 'O');
                        SendState();
                        break;

                    case "state":
                        // Гость получил новое состояние от хоста
                        if (_isHost) return;

                        _game.Deserialize(msg.Board);
                        RefreshBoard();

                        _turn = string.IsNullOrEmpty(msg.Turn) ? ' ' : msg.Turn[0];
                        UpdateTurnStatus();

                        if (!string.IsNullOrEmpty(msg.Result))
                        {
                            char res = msg.Result[0];
                            if (res == 'D') InfoText.Text = "🤝 Ничья.";
                            else InfoText.Text = res == _mySymbol ? "🎉 Вы победили!" : "😔 Вы проиграли.";
                        }
                        else
                        {
                            InfoText.Text = _turn == _mySymbol ? "Ваш ход" : "Ход соперника";
                        }
                        break;
                }
            });
        }

        // ================================================================
        //  ВСПОМОГАТЕЛЬНОЕ
        // ================================================================
        private void ShowLobbyError(string text, bool isError = true)
        {
            LobbyErrorText.Text = text;
            LobbyErrorText.Foreground = isError
                ? (Brush)FindResource("CoralBrush")
                : (Brush)FindResource("MutedTextBrush");
            LobbyErrorText.Visibility = Visibility.Visible;
        }

        private string GetLocalIp()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(a =>
                    a.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(a));

                return ip?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        // Аккуратно закрываем сеть при уходе с экрана
        public void Shutdown()
        {
            CleanupNetwork();
        }
    }
}