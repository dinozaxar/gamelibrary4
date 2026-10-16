using System.Windows.Controls;
using gamelibrary4.Contracts;

namespace gamelibrary4.Games.Hangman
{
    public class HangmanModule : IGameModule
    {
        public string Key => "hangman";
        public string Title => "Виселица";
        public string Emoji => "🎯";
        public string Description => "Угадай слово по буквам, пока не поздно";

        public UserControl CreateView() => new HangmanView();
    }
}