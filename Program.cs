using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

class GodotLauncher
{
    private static readonly string ConfigFileName = "Gdrun.json";
    private static bool skipDefault = false;

    private class LauncherConfig
    {
        public string? GodotRoot { get; set; }
        public Dictionary<string, string> DefaultExecutables { get; set; } = new();
    }

    private static string GetConfigFilePath()
    {
        return Path.Combine(AppContext.BaseDirectory, ConfigFileName);
    }

    private static LauncherConfig ReadConfig(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
                return new LauncherConfig();

            string json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<LauncherConfig>(json);
            return config ?? new LauncherConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"配置文件读取失败，使用默认配置: {ex.Message}");
            return new LauncherConfig();
        }
    }

    private static void WriteConfig(string configPath, LauncherConfig config)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        try
        {
            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(configPath, json);
            Console.WriteLine($"配置已保存到: {configPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"无法保存配置文件: {ex.Message}");
        }
    }

    private static bool IsExecutableFile(string filePath)
    {
        if (OperatingSystem.IsWindows())
            return Path.GetExtension(filePath).Equals(".exe", StringComparison.OrdinalIgnoreCase);
        else
            try
            {
                return (new FileInfo(filePath).UnixFileMode & UnixFileMode.UserExecute) != 0;
            }
            catch
            {
                return false;
            }
    }

    private static bool IsConsoleVersion(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
        return fileName.Contains("console");
    }

    private static string GetExecutableLabel(string filePath)
    {
        return IsConsoleVersion(filePath) ? "控制台版本 (Console)" : "图形界面版本 (Editor)";
    }

    static void Main(string[] args)
    {
        if (args.Contains("-n"))
        {
            skipDefault = true;
            args = args.Where(a => a != "--n").ToArray();
        }

        string configPath = GetConfigFilePath();
        Console.WriteLine($"配置文件路径: {configPath}");

        var config = ReadConfig(configPath);
        string? godotRoot = config.GodotRoot;

        if (string.IsNullOrEmpty(godotRoot))
        {
            godotRoot = PromptForGodotRoot();
            if (godotRoot == null) return;

            config.GodotRoot = godotRoot;
            WriteConfig(configPath, config);
        }

        if (!Directory.Exists(godotRoot))
        {
            Console.WriteLine($"Godot 根目录不存在: {godotRoot}");
            return;
        }

        var candidates = ScanGodotVersions(godotRoot);
        if (candidates.Count == 0)
        {
            Console.WriteLine("在指定目录中未找到任何 godot*.exe 文件。");
            return;
        }

        candidates = candidates.OrderBy(c => c.VersionName).ToList();

        Console.WriteLine($"\n 发现 {candidates.Count} 个 Godot 版本：");
        for (int i = 0; i < candidates.Count; i++)
        {
            Console.WriteLine($"  [{i + 1}] {candidates[i].VersionName}");
        }

        int choice = PromptForVersionChoice(candidates.Count);
        if (choice == -1) return;

        var selectedDir = candidates[choice - 1];
        string versionKey = selectedDir.VersionName;

        // 尝试使用默认可执行文件（除非跳过）
        string? executableToRun = null;
        if (!skipDefault && config.DefaultExecutables.TryGetValue(versionKey, out string? savedPath))
        {
            if (File.Exists(savedPath))
            {
                executableToRun = savedPath;
                Console.WriteLine($"使用上次默认启动项: {GetExecutableLabel(executableToRun)} → {Path.GetFileName(executableToRun)}");
            }
            else
            {
                Console.WriteLine($"默认启动项已丢失，重新选择...");
            }
        }
        else if (skipDefault)
        {
            Console.WriteLine(" --n 参数已启用，跳过默认启动项。");
        }

        // 如果没有有效默认项，则显示菜单选择
        if (executableToRun == null)
        {
            var exeFiles = GetExecutableFiles(selectedDir.DirPath);
            if (exeFiles.Count == 0)
            {
                Console.WriteLine(" 未找到 godot*.exe 文件。");
                return;
            }

            executableToRun = SelectExecutableFromList(exeFiles, selectedDir.VersionName);
            if (executableToRun == null) return;
        }

        // 保存为默认
        config.DefaultExecutables[versionKey] = executableToRun;
        WriteConfig(configPath, config);

        LaunchGodot(executableToRun, args);
    }

    private static string? PromptForGodotRoot()
    {
        Console.WriteLine(" 首次运行：请设置 Godot 根目录路径");
        Console.WriteLine(" 示例:");
        if (OperatingSystem.IsWindows())
            Console.WriteLine("   C:\\GODOT");
        else
            Console.WriteLine("   /home/user/GODOT");

        Console.Write("\n请输入 Godot 根目录路径: ");
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine(" 路径不能为空。");
            return null;
        }

        input = input.Trim().Trim('"', '\'');
        if (!Directory.Exists(input))
        {
            Console.WriteLine($" 路径不存在: {input}");
            return null;
        }

        return input;
    }

    private static List<(string VersionName, string DirPath)> ScanGodotVersions(string godotRoot)
    {
        var versionDirs = Directory.GetDirectories(godotRoot);
        var candidates = new List<(string VersionName, string DirPath)>();

        foreach (var dir in versionDirs)
        {
            string dirName = Path.GetFileName(dir);

            // 👇 跳过名为 "Gdrun" 的文件夹（不区分大小写更安全）
            if (string.Equals(dirName, "Gdrun", StringComparison.OrdinalIgnoreCase))
                continue;

            var exes = Directory.GetFiles(dir)
                .Where(f => IsExecutableFile(f) &&
                           Path.GetFileName(f).StartsWith("godot", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (exes.Length > 0)
            {
                candidates.Add((dirName, dir));
            }
        }

        return candidates;
    }

    private static int PromptForVersionChoice(int max)
    {
        Console.Write($"请选择要启动的版本（1-{max}），或回车取消: ");
        string? choiceInput = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(choiceInput))
        {
            Console.WriteLine(" 已取消。");
            return -1;
        }

        if (!int.TryParse(choiceInput, out int choice) || choice < 1 || choice > max)
        {
            Console.WriteLine(" 无效选择。");
            return -1;
        }

        return choice;
    }

    private static List<(string Path, string Label)> GetExecutableFiles(string dirPath)
    {
        return Directory.GetFiles(dirPath)
            .Where(f => IsExecutableFile(f) &&
                       Path.GetFileName(f).StartsWith("godot", StringComparison.OrdinalIgnoreCase))
            .Select(f => (Path: f, Label: GetExecutableLabel(f)))
            .OrderBy(x => x.Label) // 控制台版排后面（因为 "🔧" < "🎨" 在 ASCII 中？实际上我们希望图形版优先）
                                   // 更可靠：显式排序，图形版优先
            .OrderByDescending(x => x.Label.Contains("图形") ? 1 : 0)
            .ToList();
    }

    private static string? SelectExecutableFromList(List<(string Path, string Label)> exeFiles, string versionName)
    {
        if (exeFiles.Count == 1)
        {
            string path = exeFiles[0].Path;
            Console.WriteLine($" 自动选择: {exeFiles[0].Label} → {Path.GetFileName(path)}");
            return path;
        }

        Console.WriteLine($"\n 在 '{versionName}' 中发现多个可执行文件：");
        for (int i = 0; i < exeFiles.Count; i++)
        {
            Console.WriteLine($"  [{i + 1}] {exeFiles[i].Label} → {Path.GetFileName(exeFiles[i].Path)}");
        }

        Console.Write($"请选择（1-{exeFiles.Count}）: ");
        string? subChoice = Console.ReadLine();

        if (int.TryParse(subChoice, out int idx) && idx >= 1 && idx <= exeFiles.Count)
        {
            return exeFiles[idx - 1].Path;
        }
        else
        {
            Console.WriteLine("❌ 无效选择，已取消。");
            return null;
        }
    }

    private static void LaunchGodot(string executableToRun, string[] args)
    {
        Console.WriteLine($"\n 正在启动: {Path.GetFileName(executableToRun)} ...");

        try
        {
            ProcessStartInfo startInfo;

            if (OperatingSystem.IsWindows())
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = executableToRun,
                    Arguments = string.Join(" ", args.Select(a => $"\"{a}\"")), // 安全转义参数
                    UseShellExecute = true
                };
            }
            else
            {
                // 使用 sh -c 启动，并后台运行，避免阻塞终端
                string escapedPath = executableToRun.Replace("\"", "\\\"");
                string argString = string.Join(" ", args.Select(a => $"\"{a.Replace("\"", "\\\"")}\""));
                startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"setsid \\\"{escapedPath}\\\" {argString} < /dev/null > /dev/null 2>&1 &\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            Process.Start(startInfo);

            Console.WriteLine(" Godot 已启动！");
            Console.WriteLine(" 启动器即将退出...");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($" 启动失败: {ex.Message}");
        }
    }
}