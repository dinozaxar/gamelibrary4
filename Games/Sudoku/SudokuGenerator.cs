using System;
using System.Collections.Generic;

namespace gamelibrary4.Games.Sudoku
{
    /// <summary>
    /// Генератор судоку: полное решение + вырезание клеток с проверкой
    /// единственности решения (backtracking-счётчик до 2).
    /// </summary>
    public class SudokuGenerator
    {
        public int[,] Solution { get; private set; }   // полное решение 9×9
        public int[,] Puzzle { get; private set; }     // головоломка (0 = пусто)

        private readonly Random _rng;

        public SudokuGenerator(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // ================================================================
        //  ГЕНЕРАЦИЯ
        // ================================================================
        public void Generate(Difficulty difficulty)
        {
            Solution = new int[9, 9];

            // 1. Заполняем полное решение
            FillSolution(Solution);

            // 2. Копируем в головоломку и вырезаем клетки
            Puzzle = (int[,])Solution.Clone();

            int targetEmpty = difficulty switch
            {
                Difficulty.Easy => 38,
                Difficulty.Medium => 48,
                Difficulty.Hard => 54,
                _ => 45
            };

            // Готовим список всех позиций и тасуем
            var positions = new List<(int, int)>();
            for (int y = 0; y < 9; y++)
                for (int x = 0; x < 9; x++)
                    positions.Add((x, y));

            Shuffle(positions);

            int removed = 0;
            foreach (var (x, y) in positions)
            {
                if (removed >= targetEmpty) break;

                int backup = Puzzle[x, y];
                if (backup == 0) continue;

                Puzzle[x, y] = 0;

                if (CountSolutions(Puzzle, 2) != 1)
                {
                    // Решений стало 0 или больше 1 — возвращаем
                    Puzzle[x, y] = backup;
                }
                else
                {
                    removed++;
                }
            }
        }

        private void FillSolution(int[,] board)
        {
            // Простая диагональная инициализация + backtracking
            FillBox(0, 0);
            FillBox(3, 3);
            FillBox(6, 6);
            Solve(board);
        }

        private void FillBox(int startX, int startY)
        {
            var nums = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Shuffle(nums);

            int idx = 0;
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    Solution[startX + x, startY + y] = nums[idx++];
        }

        private bool Solve(int[,] board)
        {
            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (board[x, y] != 0) continue;

                    var candidates = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                    Shuffle(candidates);

                    foreach (var n in candidates)
                    {
                        if (IsValid(board, x, y, n))
                        {
                            board[x, y] = n;
                            if (Solve(board)) return true;
                            board[x, y] = 0;
                        }
                    }
                    return false;
                }
            }
            return true;
        }

        // ================================================================
        //  ПРОВЕРКИ
        // ================================================================
        public static bool IsValid(int[,] board, int x, int y, int value)
        {
            // Строка и столбец
            for (int i = 0; i < 9; i++)
            {
                if (i != x && board[i, y] == value) return false;
                if (i != y && board[x, i] == value) return false;
            }

            // Блок 3×3
            int bx = (x / 3) * 3;
            int by = (y / 3) * 3;
            for (int dy = 0; dy < 3; dy++)
                for (int dx = 0; dx < 3; dx++)
                {
                    int cx = bx + dx;
                    int cy = by + dy;
                    if ((cx != x || cy != y) && board[cx, cy] == value)
                        return false;
                }

            return true;
        }

        /// <summary>
        /// Считает решения. Останавливается, как только найдёт limit.
        /// </summary>
        public static int CountSolutions(int[,] board, int limit)
        {
            int count = 0;
            CountRecursive(board, ref count, limit);
            return count;
        }

        private static void CountRecursive(int[,] board, ref int count, int limit)
        {
            if (count >= limit) return;

            int bestX = -1, bestY = -1;
            int bestCandidates = 10;

            // Ищем клетку с минимумом кандидатов (MRV-эвристика)
            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (board[x, y] != 0) continue;

                    int cnt = 0;
                    for (int n = 1; n <= 9; n++)
                        if (IsValid(board, x, y, n)) cnt++;

                    if (cnt < bestCandidates)
                    {
                        bestCandidates = cnt;
                        bestX = x;
                        bestY = y;
                        if (cnt == 0) return;
                    }
                }
            }

            // Нет пустых клеток → решение найдено
            if (bestX == -1)
            {
                count++;
                return;
            }

            for (int n = 1; n <= 9; n++)
            {
                if (IsValid(board, bestX, bestY, n))
                {
                    board[bestX, bestY] = n;
                    CountRecursive(board, ref count, limit);
                    board[bestX, bestY] = 0;
                    if (count >= limit) return;
                }
            }
        }

        // ================================================================
        //  ВСПОМОГАТЕЛЬНОЕ
        // ================================================================
        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }
}