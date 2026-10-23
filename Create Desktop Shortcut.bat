@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$target = Join-Path $PSScriptRoot 'StarGymApp.Desktop.exe'; if (-not (Test-Path -LiteralPath $target)) { Write-Error 'StarGymApp.Desktop.exe was not found. Keep this script beside the app executable.'; exit 1 }; $desktop = [Environment]::GetFolderPath('DesktopDirectory'); $shortcutPath = Join-Path $desktop 'StarGym.lnk'; $shell = New-Object -ComObject WScript.Shell; $shortcut = $shell.CreateShortcut($shortcutPath); $shortcut.TargetPath = $target; $shortcut.WorkingDirectory = $PSScriptRoot; $shortcut.IconLocation = $target + ',0'; $shortcut.Save(); Write-Host ('Created desktop shortcut: ' + $shortcutPath)"
if errorlevel 1 pause
