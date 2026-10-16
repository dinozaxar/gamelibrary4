using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using gamelibrary4.Contracts;

namespace gamelibrary4.Contracts
{
    /// <summary>
    /// Реестр всех игр: встроенных (в самой сборке) + внешних (плагины из DLL).
    /// </summary>
    public static class GameRegistry
    {
        private static List<IGameModule> _modules;

        public static IReadOnlyList<IGameModule> Modules
        {
            get
            {
                if (_modules == null) Load();
                return _modules;
            }
        }

        public static string PluginsDir
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "gamelibrary4", "plugins");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static void Reload()
        {
            _modules = null;
        }

        private static void Load()
        {
            _modules = new List<IGameModule>();

            // 1) Встроенные — из текущей сборки
            LoadFromAssembly(Assembly.GetExecutingAssembly(), _modules);

            // 2) Внешние — из папки plugins
            foreach (var dll in Directory.GetFiles(PluginsDir, "*.dll"))
            {
                try
                {
                    var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
                    LoadFromAssembly(asm, _modules);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Plugin] Ошибка загрузки {dll}: {ex.Message}");
                }
            }

            _modules = _modules.OrderBy(m => m.Title).ToList();
        }

        private static void LoadFromAssembly(Assembly asm, List<IGameModule> target)
        {
            IEnumerable<Type> types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null); }

            foreach (var t in types)
            {
                if (t == null) continue;
                if (!typeof(IGameModule).IsAssignableFrom(t)) continue;
                if (t.IsInterface || t.IsAbstract) continue;
                if (t.GetConstructor(Type.EmptyTypes) == null) continue;

                try
                {
                    var module = (IGameModule)Activator.CreateInstance(t);
                    if (target.All(m => m.Key != module.Key))
                        target.Add(module);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Plugin] Не удалось создать {t.FullName}: {ex.Message}");
                }
            }
        }

        public static IGameModule Find(string key)
            => Modules.FirstOrDefault(m => m.Key == key);
    }
}