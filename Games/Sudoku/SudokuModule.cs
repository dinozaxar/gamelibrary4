using System.Windows.Controls;
using gamelibrary4.Contracts;

namespace gamelibrary4.Games.Sudoku
{
    public class SudokuModule : IGameModule
    {
        public string Key => "sudoku";
        public string Title => "Судоку";
        public string Emoji => "🧩";
        public string Description => "Заполни поле цифрами 1–9 без повторов";

        public UserControl CreateView() => new SudokuView();
    }
}