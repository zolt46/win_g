// File: PublicPCControl.Client/Services/ShortcutResolver.cs
using System;
using System.IO;
using IWshRuntimeLibrary;

namespace PublicPCControl.Client.Services
{
    public static class ShortcutResolver
    {
        public static string? ResolveTargetPath(string shortcutPath)
        {
            if (string.IsNullOrWhiteSpace(shortcutPath) || !File.Exists(shortcutPath))
            {
                return null;
            }

            try
            {
                var shell = new WshShell();
                var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                return shortcut.TargetPath;
            }
            catch
            {
                return null;
            }
        }
    }
}
