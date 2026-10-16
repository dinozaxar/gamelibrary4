using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace gamelibrary4.Games.TicTacToe.Network
{
    /// <summary>
    /// Хост-сторона: слушает один TCP-порт, ждёт одного клиента.
    /// После подключения — принимает сообщения и умеет отправлять.
    /// </summary>
    public class GameServer
    {
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;

        public bool IsConnected => _client != null && _client.Connected;

        public event Action Connected;                  // клиент подключился
        public event Action<NetworkMessage> MessageReceived;
        public event Action<string> Disconnected;       // отключился/ошибка

        public int Port { get; }

        public GameServer(int port = 55555)
        {
            Port = port;
        }

        /// <summary>Запустить прослушивание. Возвращает false, если порт занят.</summary>
        public bool Start()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start();
                _ = Task.Run(AcceptLoop);
                return true;
            }
            catch (Exception ex)
            {
                Disconnected?.Invoke("Не удалось открыть порт: " + ex.Message);
                return false;
            }
        }

        private async Task AcceptLoop()
        {
            try
            {
                _client = await _listener.AcceptTcpClientAsync();
                _stream = _client.GetStream();
                Connected?.Invoke();
                await ReadLoop();
            }
            catch (Exception ex)
            {
                Disconnected?.Invoke("Ошибка приёма подключения: " + ex.Message);
            }
        }

        private async Task ReadLoop()
        {
            try
            {
                var buffer = new byte[8192];
                var sb = new StringBuilder();

                while (IsConnected)
                {
                    int read = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read <= 0) break;

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, read));

                    // Разбираем по строкам (каждое сообщение — одна строка JSON)
                    string all = sb.ToString();
                    int idx;
                    while ((idx = all.IndexOf('\n')) >= 0)
                    {
                        string line = all.Substring(0, idx).Trim();
                        all = all.Substring(idx + 1);
                        if (line.Length > 0)
                        {
                            try
                            {
                                var msg = NetworkMessage.FromJson(line);
                                MessageReceived?.Invoke(msg);
                            }
                            catch { /* пропускаем битые */ }
                        }
                    }
                    sb.Clear();
                    sb.Append(all);
                }
            }
            catch (Exception ex)
            {
                Disconnected?.Invoke("Разрыв соединения: " + ex.Message);
                return;
            }

            Disconnected?.Invoke("Соперник отключился.");
        }

        public void Send(NetworkMessage msg)
        {
            if (!IsConnected) return;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(msg.ToJson() + "\n");
                _stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Disconnected?.Invoke("Ошибка отправки: " + ex.Message);
            }
        }

        public void Stop()
        {
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            try { _listener?.Stop(); } catch { }
            _stream = null;
            _client = null;
            _listener = null;
        }
    }
}