using System.Windows.Controls;
using gamelibrary4.Contracts;

namespace gamelibrary4.Games.Minesweeper
{
    public class MinesweeperModule : IGameModule
    {
        public string Key => "minesweeper";
        public string Title => "Сапёр";
        public string Emoji => "💣";
        public string Description => "Открой все клетки, не попав на мину";

        public UserControl CreateView() => new MinesweeperView();
    }
}