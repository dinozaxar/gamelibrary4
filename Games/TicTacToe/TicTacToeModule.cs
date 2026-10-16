using System.Windows.Controls;
using gamelibrary4.Contracts;

namespace gamelibrary4.Games.TicTacToe
{
    public class TicTacToeModule : IGameModule
    {
        public string Key => "tictactoe";
        public string Title => "Крестики-нолики";
        public string Emoji => "❌";
        public string Description => "Классика 3×3 с игрой по локальной сети";

        public UserControl CreateView() => new TicTacToeView();
    }
}