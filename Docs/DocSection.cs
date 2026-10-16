using System.Collections.Generic;

namespace gamelibrary4.Docs
{
    /// <summary>
    /// Один раздел документации. Содержимое — список абзацев.
    /// Поддерживает простую разметку через префиксы:
    ///   "# "  — заголовок 1
    ///   "## " — заголовок 2
    ///   "- "  — элемент списка
    ///   "! "  — важное (выделяется)
    ///   "> "  — код (моноширинный шрифт)
    ///   ""    — обычный абзац
    /// </summary>
    public class DocSection
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Emoji { get; set; }
        public List<string> Content { get; set; } = new List<string>();
    }
}