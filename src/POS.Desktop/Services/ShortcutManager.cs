using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace POS.Desktop.Services;

public static class ShortcutManager
{
    public static string AppName => "NexPOS";
    public static string AppDescription => "NexPOS - Enterprise Point of Sale System";

    public static string GetExePath()
    {
        var proc = Process.GetCurrentProcess();
        var main = proc.MainModule;
        var path = main?.FileName;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            return path;

        var loc = AppContext.BaseDirectory;
        var exe = Path.Combine(loc, "NexPOS.exe");
        if (File.Exists(exe)) return exe;
        exe = Path.Combine(loc, "POS.Desktop.exe");
        if (File.Exists(exe)) return exe;
        return loc;
    }

    public static string GetDesktopPath()
        => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

    public static string GetStartMenuPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", AppName);

    public static string DesktopShortcutPath
        => Path.Combine(GetDesktopPath(), $"{AppName}.lnk");

    public static bool DesktopShortcutExists()
        => File.Exists(DesktopShortcutPath);

    public static async Task<(bool Success, string Message)> CreateDesktopShortcutAsync(string? workingDir = null, string? iconPath = null)
    {
        var exePath = GetExePath();
        if (!File.Exists(exePath))
            return (false, "Application executable not found.");

        var shortcutPath = DesktopShortcutPath;
        workingDir ??= Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;

        try
        {
            bool ok = await CreateShortcutViaPowerShellAsync(
                shortcutPath: shortcutPath,
                targetPath: exePath,
                workingDir: workingDir,
                description: AppDescription,
                iconPath: string.IsNullOrWhiteSpace(iconPath) ? exePath : iconPath,
                arguments: string.Empty);

            return ok
                ? (true, $"✅ Shortcut created on Desktop!\n\nPath: {shortcutPath}")
                : (false, "Could not create shortcut. Try running the app as Administrator.");
        }
        catch (Exception ex)
        {
            return (false, $"Error creating shortcut: {ex.Message}");
        }
    }

    public static async Task<(bool Success, string Message)> CreateStartMenuShortcutAsync(string? workingDir = null)
    {
        var exePath = GetExePath();
        if (!File.Exists(exePath))
            return (false, "Application executable not found.");

        try
        {
            var startMenuFolder = GetStartMenuPath();
            Directory.CreateDirectory(startMenuFolder);
            var shortcutPath = Path.Combine(startMenuFolder, $"{AppName}.lnk");
            workingDir ??= Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;

            bool ok = await CreateShortcutViaPowerShellAsync(
                shortcutPath: shortcutPath,
                targetPath: exePath,
                workingDir: workingDir,
                description: AppDescription,
                iconPath: exePath,
                arguments: string.Empty);

            return ok
                ? (true, $"✅ Start Menu shortcut created!\n\nPath: {shortcutPath}")
                : (false, "Could not create Start Menu shortcut. Try running as Administrator.");
        }
        catch (Exception ex)
        {
            return (false, $"Error creating Start Menu shortcut: {ex.Message}");
        }
    }

    private static async Task<bool> CreateShortcutViaPowerShellAsync(
        string shortcutPath,
        string targetPath,
        string workingDir,
        string description,
        string iconPath,
        string arguments)
    {
        try
        {
            var psScript = new StringBuilder();
            psScript.AppendLine("$ErrorActionPreference = 'Stop';");
            psScript.AppendLine("$s = (New-Object -ComObject WScript.Shell).CreateShortcut('" + EscapeForPs(shortcutPath) + "');");
            psScript.AppendLine("$s.TargetPath = '" + EscapeForPs(targetPath) + "';");
            psScript.AppendLine("$s.WorkingDirectory = '" + EscapeForPs(workingDir) + "';");
            psScript.AppendLine("$s.Description = '" + EscapeForPs(description) + "';");
            psScript.AppendLine("$s.Arguments = '" + EscapeForPs(arguments) + "';");
            psScript.AppendLine("$s.IconLocation = '" + EscapeForPs(iconPath) + ",0';");
            psScript.AppendLine("$s.Save();");
            psScript.AppendLine("exit 0;");

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{psScript.ToString().Replace("\"", "\"\"")}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var proc = Process.Start(startInfo);
            if (proc == null) return false;

            await proc.WaitForExitAsync();
            return proc.ExitCode == 0 && File.Exists(shortcutPath);
        }
        catch
        {
            try
            {
                if (File.Exists(shortcutPath)) return true;
            }
            catch { /* ignore */ }
            return false;
        }
    }

    private static string EscapeForPs(string value)
        => (value ?? string.Empty).Replace("'", "''");
}
