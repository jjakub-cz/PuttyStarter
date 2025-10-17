# PuttyStarter

<img align="right" width="100px" src="./docs/logo.png">

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![C%23](https://img.shields.io/badge/C%23-★-239120)
![WinForms](https://img.shields.io/badge/WinForms-UI-0078D4)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

[![Release](https://img.shields.io/github/v/release/jjakub-cz/puttystarter)](https://github.com/jjakub-cz/puttystarter/releases)
[![Downloads](https://img.shields.io/github/downloads/jjakub-cz/puttystarter/total)](https://github.com/jjakub-cz/puttystarter/releases)

A tiny Windows tray and _hot-key_ utility that enables you to quickly start a *PuTTy* session of your choosing.

- **Repository:** https://github.com/jjakub-cz/puttystarter  
- **License:** MIT  
- **OS:** Windows 10/11  

## Features
- Lightweight tray icon
- Option to run at startup
- Hotkey to select desired PuTTY connection

## Tech stack
- C# / .NET 8
- WinForms (system tray app)
- Single-file, self-contained publish

## Download
Grab the latest build from **[Releases](https://github.com/jjakub-cz/puttystarter/releases)**.

> Windows SmartScreen may warn about an unknown publisher if the binary is not code-signed.

## Usage
1. Run `PuttyStarter.exe`.  
2. Press `"Ctrl+Alt+P"`
3. Navigate and select with `"Enter"`

Consider [`docs/config.md`](./docs/config.md) for additional information.

## Privacy
PuttyStarter does **not** collect personal data and has **no telemetry**.  


## Build (Developer)
- .NET 8 (`net8.0-windows`)
- WinForms
- Recommended: **do not enable trimming** for WinForms apps.

### Quick build
```bash
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained false
```

### Build with PowerShell script
There is handy PowerShell script located at `./build/script.ps1` that should handle all that is needed for building this project.

## Disclaimer

This software is provided "AS IS", without warranties of any kind. Use at your own risk.
