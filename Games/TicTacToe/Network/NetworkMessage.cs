using System.Text.Json;

namespace gamelibrary4.Games.TicTacToe.Network
{
    /// <summary>
    /// Сообщение между хостом и гостем. Одно поле Type + data.
    /// </summary>
    public class NetworkMessage
    {
        public string Type { get; set; } = "";

        // Общие данные
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;

        // Состояние поля (передаётся как строка вида "XO..X....")
        public string Board { get; set; } = "";

        // Чей ход: "X" или "O"
        public string Turn { get; set; } = "";

        // Результат: "", "X", "O", "draw"
        public string Result { get; set; } = "";

        // Текст (для чата/системных сообщений)
        public string Text { get; set; } = "";

        public string ToJson() => JsonSerializer.Serialize(this);

        public static NetworkMessage FromJson(string json)
            => JsonSerializer.Deserialize<NetworkMessage>(json) ?? new NetworkMessage();
    }
}