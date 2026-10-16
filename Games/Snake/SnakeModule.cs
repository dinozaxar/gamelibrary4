using System.Windows.Controls;
using gamelibrary4.Contracts;

namespace gamelibrary4.Games.Snake
{
    public class SnakeModule : IGameModule
    {
        public string Key => "snake";
        public string Title => "Змейка";
        public string Emoji => "🐍";
        public string Description => "Ешь яблоки, расти, не врезайся";

        public UserControl CreateView() => new SnakeView();
    }
}