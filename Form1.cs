using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ArxBuilder;

public partial class Form1 : Form
{
    private string? _cmakePath;
    private string? _msbuildPath;
    private string? _vsGenerator;
    private bool _isBuilding;

    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ArxBuilder", "config.json");

    public Form1()
    {
        InitializeComponent();
        LoadSettings();
        DetectBuildTools();
    }

    #region Settings

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null && !string.IsNullOrEmpty(config.SdkPath) && Directory.Exists(config.SdkPath))
                {
                    txtSdkPath.Text = config.SdkPath;
                    return;
                }
            }
        }
        catch { }

        txtSdkPath.Text = @"C:\Autodesk\Autodesk_ObjectARX_2019_Win_64_and_32_Bit";
    }

    private void SaveSettings()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var config = new AppConfig { SdkPath = txtSdkPath.Text.Trim() };
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }

    private class AppConfig
    {
        public string? SdkPath { get; set; }
    }

    #endregion

    #region Build Tool Detection

    private void DetectBuildTools()
    {
        _cmakePath = FindCMake();
        _msbuildPath = FindMSBuild();
        _vsGenerator = DetectVSGenerator();
        UpdateToolStatus();
    }

    private string? FindCMake()
    {
        // 1. Check PATH
        var pathCmake = SearchInPath("cmake.exe");
        if (pathCmake != null) return pathCmake;

        // 2. Check common install location
        var commonPaths = new[]
        {
            @"C:\Program Files\CMake\bin\cmake.exe",
            @"C:\Program Files (x86)\CMake\bin\cmake.exe",
        };
        foreach (var p in commonPaths)
            if (File.Exists(p)) return p;

        // 3. Check within VS installation
        var vsPath = GetVSInstallPath();
        if (vsPath != null)
        {
            var vsCmake = Path.Combine(vsPath,
                @"Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe");
            if (File.Exists(vsCmake)) return vsCmake;
        }

        return null;
    }

    private string? FindMSBuild()
    {
        // 1. Use vswhere to find MSBuild
        var vswhere = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";
        if (File.Exists(vswhere))
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = vswhere,
                    Arguments = "-latest -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    var output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0 && File.Exists(lines[0].Trim()))
                        return lines[0].Trim();
                }
            }
            catch { }
        }

        // 2. Check known paths from VS install
        var vsPath = GetVSInstallPath();
        if (vsPath != null)
        {
            var msbuild = Path.Combine(vsPath, @"MSBuild\Current\Bin\MSBuild.exe");
            if (File.Exists(msbuild)) return msbuild;
            msbuild = Path.Combine(vsPath, @"MSBuild\Current\Bin\amd64\MSBuild.exe");
            if (File.Exists(msbuild)) return msbuild;
        }

        // 3. Check Build Tools standalone paths
        var buildToolsPaths = new[]
        {
            @"C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        };
        foreach (var p in buildToolsPaths)
            if (File.Exists(p)) return p;

        return null;
    }

    private string? GetVSInstallPath()
    {
        var vswhere = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";
        if (!File.Exists(vswhere)) return null;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = vswhere,
                Arguments = "-latest -property installationPath",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit();
                if (!string.IsNullOrEmpty(output) && Directory.Exists(output))
                    return output;
            }
        }
        catch { }

        return null;
    }

    private string? DetectVSGenerator()
    {
        var vswhere = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";
        if (!File.Exists(vswhere)) return null;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = vswhere,
                Arguments = "-latest -property installationVersion",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit();
                if (!string.IsNullOrEmpty(output) && output.Contains('.'))
                {
                    var majorStr = output.Split('.')[0];
                    if (int.TryParse(majorStr, out var major))
                    {
                        return major switch
                        {
                            16 => "Visual Studio 16 2019",
                            17 => "Visual Studio 17 2022",
                            _ => null
                        };
                    }
                }
            }
        }
        catch { }

        return null;
    }

    private static string? SearchInPath(string exeName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv == null) return null;

        foreach (var dir in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var fullPath = Path.Combine(dir.Trim(), exeName);
                if (File.Exists(fullPath)) return fullPath;
            }
            catch { }
        }
        return null;
    }

    private void UpdateToolStatus()
    {
        var cmakeOk = _cmakePath != null;
        var msbuildOk = _msbuildPath != null;

        lblCMakeStatus.Text = cmakeOk ? "CMake: OK" : "CMake: NOT FOUND";
        lblCMakeStatus.ForeColor = cmakeOk ? Color.Green : Color.Red;

        lblMSBuildStatus.Text = msbuildOk ? "MSBuild: OK" : "MSBuild: NOT FOUND";
        lblMSBuildStatus.ForeColor = msbuildOk ? Color.Green : Color.Red;

        btnBuild.Enabled = cmakeOk && msbuildOk;

        if (!cmakeOk || !msbuildOk)
        {
            Log("============================================================");
            if (!cmakeOk)
                Log("[WARNING] CMake not found! Install from https://cmake.org/download/");
            if (!msbuildOk)
            {
                Log("[WARNING] MSBuild not found! Install Visual Studio Build Tools:");
                Log("           https://visualstudio.microsoft.com/visual-cpp-build-tools/");
                Log("           Select 'Desktop development with C++' workload.");
            }
            Log("============================================================");
        }
    }

    #endregion

    #region UI Event Handlers

    private void btnBrowseCpp_Click(object sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "C++ Source Files (*.cpp)|*.cpp|All Files (*.*)|*.*",
            Title = "Select .cpp file"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            txtCppPath.Text = dlg.FileName;
        }
    }

    private void btnBrowseSdk_Click(object sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select ObjectARX SDK root directory",
            ShowNewFolderButton = false
        };
        if (!string.IsNullOrEmpty(txtSdkPath.Text) && Directory.Exists(txtSdkPath.Text))
            dlg.SelectedPath = txtSdkPath.Text;

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            txtSdkPath.Text = dlg.SelectedPath;
        }
    }

    private void btnRefreshTools_Click(object sender, EventArgs e)
    {
        DetectBuildTools();
    }

    private async void btnBuild_Click(object sender, EventArgs e)
    {
        if (_isBuilding) return;
        await BuildArxAsync();
    }

    #endregion

    #region Build Logic

    private async Task BuildArxAsync()
    {
        var cppFile = txtCppPath.Text.Trim();
        var sdkPath = txtSdkPath.Text.Trim();

        if (string.IsNullOrEmpty(cppFile) || !File.Exists(cppFile))
        {
            MessageBox.Show("Please select a valid .cpp file.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (string.IsNullOrEmpty(sdkPath) || !Directory.Exists(sdkPath))
        {
            MessageBox.Show("Please set a valid ObjectARX SDK path.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!Directory.Exists(Path.Combine(sdkPath, "inc")))
        {
            MessageBox.Show("Invalid SDK path: 'inc' directory not found.\nPlease check the ObjectARX SDK root path.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (_cmakePath == null || _msbuildPath == null)
        {
            MessageBox.Show("Build tools not detected. Please install CMake and Visual Studio Build Tools.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _isBuilding = true;
        btnBuild.Enabled = false;
        btnBuild.Text = "Building...";
        rtbLog.Clear();
        SaveSettings();

        try
        {
            var cppDir = Path.GetDirectoryName(cppFile)!;
            var cppName = Path.GetFileNameWithoutExtension(cppFile);
            var buildDir = Path.Combine(cppDir, $"build_arx_{cppName}");

            // Step 1: Detect ARX lib version
            Log("[1/6] Detecting ObjectARX library version...");
            var arxVersion = DetectArxLibVersion(sdkPath);
            if (arxVersion == null)
            {
                Log("  ERROR: Cannot detect ARX library version (acdb*.lib not found in lib/x64).");
                return;
            }
            Log($"  Detected ARX lib version: {arxVersion}");

            // Step 2: Generate CMakeLists.txt
            Log("[2/6] Generating CMakeLists.txt...");
            if (Directory.Exists(buildDir))
                Directory.Delete(buildDir, true);
            Directory.CreateDirectory(buildDir);

            // Copy .cpp to build dir to avoid path issues with CMake
            var srcCopyPath = Path.Combine(buildDir, Path.GetFileName(cppFile));
            File.Copy(cppFile, srcCopyPath, true);

            var cmakeContent = GenerateCMakeLists(cppName, Path.GetFileName(cppFile), sdkPath, arxVersion);
            var cmakeFilePath = Path.Combine(buildDir, "CMakeLists.txt");
            await File.WriteAllTextAsync(cmakeFilePath, cmakeContent);
            Log($"  CMakeLists.txt: {cmakeFilePath}");

            // Step 3: Run CMake
            Log("[3/6] Running CMake...");
            var generator = _vsGenerator ?? "Visual Studio 17 2022";
            var cmakeBuildDir = Path.Combine(buildDir, "build");
            var cmakeArgs = $"-G \"{generator}\" -A x64 -S \"{buildDir}\" -B \"{cmakeBuildDir}\"";
            Log($"  cmake {cmakeArgs}");

            var cmakeExit = await RunProcessAsync(_cmakePath, cmakeArgs, buildDir);
            if (cmakeExit != 0)
            {
                Log($"  CMake failed with exit code {cmakeExit}.");
                return;
            }
            Log("  CMake completed successfully.");

            // Step 4: Run MSBuild
            Log("[4/6] Running MSBuild (Release|x64)...");
            var slnFile = Path.Combine(cmakeBuildDir, $"{cppName}.sln");
            if (!File.Exists(slnFile))
            {
                var slnFiles = Directory.GetFiles(cmakeBuildDir, "*.sln");
                if (slnFiles.Length == 0)
                {
                    Log("  ERROR: Solution file not found after CMake.");
                    return;
                }
                slnFile = slnFiles[0];
            }

            var msbuildArgs = $"\"{slnFile}\" /p:Configuration=Release /p:Platform=x64 /m /verbosity:minimal";
            Log($"  msbuild {msbuildArgs}");

            var msbuildExit = await RunProcessAsync(_msbuildPath, msbuildArgs, buildDir);
            if (msbuildExit != 0)
            {
                Log($"  MSBuild failed with exit code {msbuildExit}.");
                return;
            }
            Log("  MSBuild completed successfully.");

            // Step 5: Find and copy .arx
            Log("[5/6] Copying .arx output...");
            var arxFiles = Directory.GetFiles(cmakeBuildDir, $"{cppName}.arx", SearchOption.AllDirectories);
            if (arxFiles.Length == 0)
            {
                Log("  ERROR: .arx file not found in build output.");
                return;
            }

            var srcArx = arxFiles[0];
            var dstArx = Path.Combine(cppDir, $"{cppName}.arx");
            File.Copy(srcArx, dstArx, true);
            Log($"  Output: {dstArx}");

            // Step 6: Done
            Log("[6/6] Build completed successfully!");
            Log($"  .arx file: {dstArx}");
            Log(new string('=', 60));
            Log("  Load this .arx in AutoCAD 2019 with the APPLOAD command.");
            Log(new string('=', 60));

            MessageBox.Show($"Build successful!\n\nOutput: {dstArx}", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            Log(ex.StackTrace ?? "");
            MessageBox.Show($"Build failed:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isBuilding = false;
            btnBuild.Enabled = _cmakePath != null && _msbuildPath != null;
            btnBuild.Text = "Build .arx";
        }
    }

    private string? DetectArxLibVersion(string sdkPath)
    {
        // ObjectARX SDK 2019 uses: lib-x64, lib-win32 (not lib/x64)
        var libDirs = new[]
        {
            Path.Combine(sdkPath, "lib-x64"),
            Path.Combine(sdkPath, "lib", "x64"),
            Path.Combine(sdkPath, "lib"),
        };

        foreach (var libDir in libDirs)
        {
            if (!Directory.Exists(libDir)) continue;
            var libFiles = Directory.GetFiles(libDir, "acdb*.lib");
            foreach (var lib in libFiles)
            {
                var name = Path.GetFileNameWithoutExtension(lib);
                if (name.StartsWith("acdb") && name.Length > 4)
                    return name.Substring(4);
            }
        }

        return null;
    }

    private static string GenerateCMakeLists(string projectName, string cppFile, string sdkPath, string arxVersion)
    {
        var sdkCmake = sdkPath.Replace('\\', '/');
        var cppCmake = cppFile.Replace('\\', '/');

        // Detect actual SDK directory layout
        // ObjectARX 2019 uses: inc, inc-x64, lib-x64
        // Other versions may use: inc, inc/compat, lib/x64
        var hasLibX64 = Directory.Exists(Path.Combine(sdkPath, "lib-x64"));
        var hasIncX64 = Directory.Exists(Path.Combine(sdkPath, "inc-x64"));
        var hasCompat = Directory.Exists(Path.Combine(sdkPath, "inc", "compat"));

        var libDir = hasLibX64 ? "${ARX_SDK_ROOT}/lib-x64" : "${ARX_SDK_ROOT}/lib/x64";

        var sb = new StringBuilder();
        sb.AppendLine("cmake_minimum_required(VERSION 3.15)");
        sb.AppendLine($"project({projectName})");
        sb.AppendLine();
        sb.AppendLine("set(CMAKE_CXX_STANDARD 14)");
        sb.AppendLine("set(CMAKE_CXX_STANDARD_REQUIRED ON)");
        sb.AppendLine();
        sb.AppendLine("# ObjectARX SDK");
        sb.AppendLine($"set(ARX_SDK_ROOT \"{sdkCmake}\")");
        sb.AppendLine();
        sb.AppendLine("# Include directories");
        sb.AppendLine("include_directories(");
        sb.AppendLine("    ${ARX_SDK_ROOT}/inc");
        if (hasIncX64)
            sb.AppendLine("    ${ARX_SDK_ROOT}/inc-x64");
        if (hasCompat)
            sb.AppendLine("    ${ARX_SDK_ROOT}/inc/compat");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("# Library directories");
        sb.AppendLine("link_directories(");
        sb.AppendLine($"    {libDir}");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("# Source files");
        sb.AppendLine("add_library(${PROJECT_NAME} SHARED");
        sb.AppendLine($"    \"{cppCmake}\"");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("# Preprocessor definitions");
        sb.AppendLine("target_compile_definitions(${PROJECT_NAME} PRIVATE");
        sb.AppendLine("    _WINDOWS");
        sb.AppendLine("    _ACRXAPP");
        sb.AppendLine("    _RXSDK");
        sb.AppendLine("    _CRT_SECURE_NO_WARNINGS");
        sb.AppendLine("    UNICODE");
        sb.AppendLine("    _UNICODE");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("# Link libraries (order matters for MSVC linker)");
        sb.AppendLine("target_link_libraries(${PROJECT_NAME}");
        sb.AppendLine($"    ac1st{arxVersion}");
        sb.AppendLine($"    acdb{arxVersion}");
        sb.AppendLine($"    acge{arxVersion}");
        sb.AppendLine("    accore");
        sb.AppendLine("    acad");
        sb.AppendLine("    rxapi");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("# Output as .arx (DLL with .arx extension)");
        sb.AppendLine("set_target_properties(${PROJECT_NAME} PROPERTIES");
        sb.AppendLine("    SUFFIX \".arx\"");
        sb.AppendLine("    PREFIX \"\"");
        sb.AppendLine("    LINK_FLAGS \"/EXPORT:acrxEntryPoint /EXPORT:acrxGetApiVersion\"");
        sb.AppendLine(")");
        return sb.ToString();
    }

    #endregion

    #region Process Execution

    private async Task<int> RunProcessAsync(string fileName, string arguments, string workingDir)
    {
        var tcs = new TaskCompletionSource<int>();

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null) Log("  " + e.Data);
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null) Log("  [STDERR] " + e.Data);
        };

        process.Exited += (s, e) =>
        {
            tcs.TrySetResult(process.ExitCode);
            process.Dispose();
        };

        Log($"  Starting: {Path.GetFileName(fileName)}");

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return await tcs.Task;
    }

    #endregion

    #region Logging

    private void Log(string message)
    {
        if (rtbLog.InvokeRequired)
        {
            rtbLog.Invoke(new Action(() => Log(message)));
            return;
        }

        rtbLog.SelectionStart = rtbLog.TextLength;
        rtbLog.SelectionLength = 0;

        if (message.Contains("ERROR") || message.Contains("FAILED"))
            rtbLog.SelectionColor = Color.Red;
        else if (message.Contains("WARNING") || message.Contains("[STDERR]"))
            rtbLog.SelectionColor = Color.Orange;
        else if (message.Contains("successfully") || message.Contains("completed") || message.Contains("[6/6]"))
            rtbLog.SelectionColor = Color.Green;
        else if (message.StartsWith("["))
            rtbLog.SelectionColor = Color.FromArgb(0, 120, 215);
        else
            rtbLog.SelectionColor = rtbLog.ForeColor;

        rtbLog.AppendText(message + Environment.NewLine);
        rtbLog.SelectionColor = rtbLog.ForeColor;
        rtbLog.ScrollToCaret();
    }

    #endregion
}
