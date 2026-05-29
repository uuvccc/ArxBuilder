# ObjectARX One-Click Builder

A Windows desktop tool (C# WinForms) that compiles a single `.cpp` file into an ObjectARX `.arx` plugin with one click. No Visual Studio project setup required.

## Features

- Select a `.cpp` file and click **Build .arx**
- Auto-generates `CMakeLists.txt` adapted to the detected ObjectARX SDK layout (inc, inc-x64, lib-x64, lib/x64, etc.)
- Auto-detects CMake, MSBuild, and Visual Studio generator (any VS version)
- No hardcoded SDK path — auto-scans `C:\Autodesk` / `D:\Autodesk`, supports `ARX_SDK_ROOT` env var, or manual Browse
- Real-time build log output with color-coded messages
- Outputs `.arx` file to the same directory as the source `.cpp`
- Saves SDK path between sessions

## Prerequisites

1. **ObjectARX SDK** (any version)
   - Download from [Autodesk Developer Center](https://aps.autodesk.com/developer/overview/objectarx)
   - Set path via one of:
     - **Browse** button in the UI
     - Environment variable `ARX_SDK_ROOT`
     - Auto-detection from `C:\Autodesk\*ObjectARX*` / `D:\Autodesk\*ObjectARX*`

2. **CMake** (any version ≥ 3.1)
   - Install from [cmake.org](https://cmake.org/download/) (add to PATH)
   - Or install via Visual Studio "Desktop development with C++" workload

3. **Visual Studio** (any version with C++ workload)
   - [Visual Studio Build Tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/)
   - Or full Visual Studio with "Desktop development with C++" workload
   - VS generator is auto-detected via `vswhere` + `cmake --help`

## How to Build the Tool

```bash
# Requires .NET SDK 6.0+ or Visual Studio with .NET Framework 4.6.2+ support
dotnet build -c Release
```

The output executable will be in `bin/Release/net462/`.

## Usage

1. Launch `ArxBuilder.exe`
2. Click **Browse...** to select your `.cpp` file
3. Set the ObjectARX SDK path via one of:
   - **Browse** button — pick the SDK root directory manually
   - **Environment variable** — set `ARX_SDK_ROOT` to the SDK root before launch
   - **Auto-detection** — scans `C:\Autodesk\*ObjectARX*` and `D:\Autodesk\*ObjectARX*` (prefers newer versions)
   - The path is saved across sessions (no need to re-enter)
4. Check the status bar for **CMake** and **MSBuild** detection results
   - If either shows NOT FOUND, click **Refresh** after installing the missing tool
   - Build button is disabled until both tools are detected
5. Click **Build .arx**
6. The compiled `.arx` file appears next to your `.cpp` file
7. Load it in AutoCAD with the `APPLOAD` command

## Example .cpp File

A minimal ObjectARX plugin:

```cpp
#include <aced.h>
#include <rxregsvc.h>

void myCommand() {
    acutPrintf(_T("\nHello from ObjectARX!"));
}

void initApp() {
    acedRegCmds->addCommand(
        _T("MYGROUP"), _T("MYCMD"), _T("MYCMD"), ACRX_CMD_MODAL, myCommand);
}

void unloadApp() {
    acedRegCmds->removeGroup(_T("MYGROUP"));
}

extern "C" AcRx::AppRetCode
acrxEntryPoint(AcRx::AppMsgCode msg, void* pkt) {
    switch (msg) {
    case AcRx::kInitAppMsg:
        acrxUnlockApplication(pkt);
        acrxRegisterAppMDIAware(pkt);
        initApp();
        break;
    case AcRx::kUnloadAppMsg:
        unloadApp();
        break;
    }
    return AcRx::kRetOK;
}
```

## How It Works

1. Reads the selected `.cpp` file
2. Scans the ObjectARX SDK library directories (`lib-x64/`, `lib/x64/`, `lib/`) to detect library version (e.g., `acdb23.lib` → version `23`)
3. Generates a `CMakeLists.txt` with correct include paths, library paths, and link flags — adapted to the SDK's directory layout
4. Runs `cmake` to generate a Visual Studio solution (generator auto-matched from installed CMake)
5. Runs `msbuild` to compile in Release|x64 configuration
6. Copies the resulting `.arx` to the source directory

## Project Structure

```
ArxBuilder/
├── ArxBuilder.csproj       # .NET Framework 4.6.2 WinForms project
├── Program.cs               # Entry point
├── Form1.cs                 # Main form logic (build, detection, logging)
├── Form1.Designer.cs        # UI layout
├── .gitignore
└── README.md
```

## License

MIT
