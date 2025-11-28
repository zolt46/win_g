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

        private static IEnumerable<string> SafeEnumerateFiles(string root, string pattern)
        {
            try
            {
                return Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories);
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
        }
    }
}
