# ObjectARX 2019 One-Click Builder

A Windows desktop tool (C# WinForms) that compiles a single `.cpp` file into an ObjectARX 2019 `.arx` plugin with one click. No Visual Studio project setup required.

## Features

- Select a `.cpp` file and click **Build .arx**
- Auto-generates `CMakeLists.txt` with correct ObjectARX 2019 includes and libraries
- Auto-detects CMake and MSBuild installations
- Real-time build log output with color-coded messages
- Outputs `.arx` file to the same directory as the source `.cpp`
- Saves SDK path between sessions

## Prerequisites

1. **ObjectARX 2019 SDK**
   - Download from [Autodesk Developer Center](https://aps.autodesk.com/developer/overview/objectarx)
   - Extract to `C:\Autodesk\Autodesk_ObjectARX_2019_Win_64_and_32_Bit` (default)

2. **CMake** (one of the following)
   - Install from [cmake.org](https://cmake.org/download/) (add to PATH)
   - Or install via Visual Studio "Desktop development with C++" workload

3. **Visual Studio Build Tools** (one of the following)
   - [Visual Studio 2022 Build Tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/)
   - [Visual Studio 2019 Build Tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/)
   - Or full Visual Studio 2019/2022 with "Desktop development with C++" workload

## How to Build the Tool

```bash
# Requires .NET 8 SDK
dotnet build -c Release
```

The output executable will be in `bin/Release/net8.0-windows/`.

## Usage

1. Launch `ArxBuilder.exe`
2. Click **Browse...** to select your `.cpp` file
3. Verify the ObjectARX SDK path (auto-filled with default)
4. Click **Build .arx**
5. The compiled `.arx` file appears next to your `.cpp` file
6. Load it in AutoCAD 2019 with the `APPLOAD` command

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
2. Scans the ObjectARX SDK `lib/x64/` directory to detect library version (e.g., `acdb23.lib` -> version `23`)
3. Generates a `CMakeLists.txt` with correct include paths, library paths, and link flags
4. Runs `cmake` to generate a Visual Studio solution
5. Runs `msbuild` to compile in Release|x64 configuration
6. Copies the resulting `.arx` to the source directory

## Project Structure

```
ArxBuilder/
├── ArxBuilder.csproj       # .NET 8 WinForms project
├── Program.cs               # Entry point
├── Form1.cs                 # Main form logic (build, logging)
├── Form1.Designer.cs        # UI layout
├── Properties/
│   └── Settings.settings    # User settings (SDK path)
├── .gitignore
└── README.md
```

## License

MIT
