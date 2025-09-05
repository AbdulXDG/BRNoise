# FluentNoise (WPF, .NET 8)

A Microsoft-style (Fluent-inspired) Windows app that generates **white**, **pink**, and **brown** noise with
customizable **frequency-band sliders**, **stereo width**, **tape speed**, **animation**, **presets**, and **profile save/load**.

## Build (Visual Studio 2022+)
1. Open `FluentNoise.csproj` in Visual Studio.
2. Ensure you have **.NET 8 SDK** and NuGet package restore enabled.
3. Press **F5** to run (or **Ctrl+Shift+B** to build).

## Make a single EXE
- Right-click the project → **Publish** → **Folder** → Target: `win-x64` → enable **Trim** if desired.
- Check **Produce single file** and **Self-contained** to ship without .NET installed.

## Notes
- Audio powered by **NAudio**.
- The app implements simplified equivalents of the features visible on myNoise's White Noise Generator.
- Presets set the noise type and push gains to bands centered at 60, 125, 250, 500, 1k, 2k, 4k, 8k Hz.
