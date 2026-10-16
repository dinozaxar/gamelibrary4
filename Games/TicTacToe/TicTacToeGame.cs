namespace gamelibrary4.Games.TicTacToe
{
    /// <summary>
    /// Логика крестиков-ноликов 3×3. Никакого UI, никакой сети.
    /// Доска: массив 9 символов. ' ' = пусто, 'X' / 'O' = занято.
    /// </summary>
    public class TicTacToeGame
    {
        private readonly char[] _cells = new char[9];

        public TicTacToeGame()
        {
            for (int i = 0; i < 9; i++) _cells[i] = ' ';
        }

        public char this[int x, int y] => _cells[y * 3 + x];

        public void Set(int x, int y, char value) => _cells[y * 3 + x] = value;

        public bool IsEmpty(int x, int y) => _cells[y * 3 + x] == ' ';

        public void Reset()
        {
            for (int i = 0; i < 9; i++) _cells[i] = ' ';
        }

        /// <summary>Проверка победы. Возвращает 'X', 'O', 'D' (ничья) или ' ' (игра идёт).</summary>
        public char CheckResult()
        {
            int[,] lines = new int[,]
            {
                {0,1,2}, {3,4,5}, {6,7,8},   // строки
                {0,3,6}, {1,4,7}, {2,5,8},   // столбцы
                {0,4,8}, {2,4,6}             // диагонали
            };

            for (int i = 0; i < lines.GetLength(0); i++)
            {
                char a = _cells[lines[i, 0]];
                char b = _cells[lines[i, 1]];
                char c = _cells[lines[i, 2]];
                if (a != ' ' && a == b && b == c) return a;
            }

            foreach (var c in _cells)
                if (c == ' ') return ' ';

            return 'D'; // ничья
        }

        public string Serialize()
        {
            var arr = new char[9];
            for (int i = 0; i < 9; i++) arr[i] = _cells[i] == ' ' ? '.' : _cells[i];
            return new string(arr);
        }

        public void Deserialize(string s)
        {
            for (int i = 0; i < 9 && i < s.Length; i++)
                _cells[i] = s[i] == '.' ? ' ' : s[i];
        }
    }
}