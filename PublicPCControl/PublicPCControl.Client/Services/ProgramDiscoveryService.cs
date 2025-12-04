// File: PublicPCControl.Client/Services/ProgramDiscoveryService.cs
using System;
using System.Collections.Generic;
using System.IO;
using PublicPCControl.Client.Models;

namespace PublicPCControl.Client.Services
{
    public static class ProgramDiscoveryService
    {
        public static IEnumerable<ProgramSuggestion> FindSuggestions()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var shortcut in EnumerateShortcutFiles())
            {
                var target = ShortcutResolver.ResolveTargetPath(shortcut);
                if (string.IsNullOrWhiteSpace(target) || !File.Exists(target))
                {
                    continue;
                }

                if (!target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!seen.Add(target))
                {
                    continue;
                }

                var displayName = Path.GetFileNameWithoutExtension(shortcut);
                yield return new ProgramSuggestion
                {
                    DisplayName = displayName,
                    ExecutablePath = target,
                    Source = "시작 메뉴 / 바탕화면",
                    Icon = IconHelper.LoadIcon(target)
                };
            }

            foreach (var installed in EnumerateInstalledExecutables())
            {
                if (!seen.Add(installed))
                {
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(installed);
                yield return new ProgramSuggestion
                {
                    DisplayName = name,
                    ExecutablePath = installed,
                    Source = "설치된 프로그램",
                    Icon = IconHelper.LoadIcon(installed)
                };
            }
        }

        private static IEnumerable<string> EnumerateShortcutFiles()
        {
            var locations = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
            };

            foreach (var location in locations)
            {
                if (!Directory.Exists(location))
                {
                    continue;
                }

                foreach (var file in SafeEnumerateFiles(location, "*.lnk"))
                {
                    yield return file;
                }
            }
        }

        private static IEnumerable<string> EnumerateInstalledExecutables()
        {
            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            foreach (var root in roots)
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                {
                    continue;
                }

                foreach (var exe in SafeEnumerateFiles(root, "*.exe", maxDepth: 3))
                {
                    yield return exe;
                }
            }
        }

        private static IEnumerable<string> SafeEnumerateFiles(string root, string pattern, int maxDepth = int.MaxValue)
        {
            var stack = new Stack<(string path, int depth)>();
            stack.Push((root, 0));

            while (stack.Count > 0)
            {
                var (current, depth) = stack.Pop();
                IEnumerable<string> files;

                try
                {
                    files = Directory.EnumerateFiles(current, pattern, SearchOption.TopDirectoryOnly);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (IOException)
                {
                    continue;
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                if (depth >= maxDepth)
                {
                    continue;
                }

                IEnumerable<string> children;
                try
                {
                    children = Directory.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (IOException)
                {
                    continue;
                }

                foreach (var child in children)
                {
                    stack.Push((child, depth + 1));
                }
            }
        }
    }
}
