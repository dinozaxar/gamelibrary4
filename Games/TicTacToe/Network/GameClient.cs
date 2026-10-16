using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace gamelibrary4.Games.TicTacToe.Network
{
    /// <summary>
    /// Гость: подключается к хосту по IP:порт.
    /// </summary>
    public class GameClient
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public bool IsConnected => _client != null && _client.Connected;

        public event Action Connected;
        public event Action<NetworkMessage> MessageReceived;
        public event Action<string> Disconnected;

        /// <summary>Асинхронное подключение. Возвращает null при успехе, иначе текст ошибки.</summary>
        public async Task<string> ConnectAsync(string host, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();
                Connected?.Invoke();
                _ = Task.Run(ReadLoop);
                return null;
            }
            catch (Exception ex)
            {
                return "Не удалось подключиться: " + ex.Message;
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
                            catch { }
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

            Disconnected?.Invoke("Хост отключился.");
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
            _stream = null;
            _client = null;
        }
    }
}