# Building PublicPCControl

## Prerequisites
- .NET SDK 8.0 or later.
- Windows Desktop targeting pack (automatically available on Windows when the WindowsDesktop workload is installed).

## Build steps
1. Restore dependencies and build the solution:
   ```sh
   dotnet build PublicPCControl.sln
   ```

## Notes about building on Linux/macOS
This project targets **`net8.0-windows`** and uses WPF. The required `Microsoft.NET.Sdk.WindowsDesktop` SDK/workload is only available on Windows, so building on non-Windows platforms will fail with an error indicating that `Microsoft.NET.Sdk.WindowsDesktop.targets` cannot be found. To produce binaries, run the build on a Windows machine (or Windows container/VM) with the Windows Desktop workload installed:

```sh
# On Windows
winget install Microsoft.DotNet.SDK.8
# or download the installer from https://dotnet.microsoft.com
# then install the Windows Desktop SDK/workload if prompted
```

If you simply need to restore packages or inspect source code on Linux/macOS, those steps will work, but a full compile requires Windows.
