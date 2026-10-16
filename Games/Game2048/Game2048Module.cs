using System.Windows.Controls;
using gamelibrary4.Contracts;

namespace gamelibrary4.Games.Game2048
{
    public class Game2048Module : IGameModule
    {
        public string Key => "2048";
        public string Title => "2048";
        public string Emoji => "🔢";
        public string Description => "Складывай плитки и собери 2048";

        public UserControl CreateView() => new Game2048View();
    }
}