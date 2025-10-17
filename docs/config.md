# PuttyStarter Configuration File (`PuttyStarter.conf`)

This document describes all configuration options available for **PuttyStarter**,  
a lightweight Windows launcher for quickly opening PuTTY SSH sessions via a global hotkey.

---

## 📁 Location and Format

- The configuration file **must be located in the same directory** as `PuttyStarter.exe`.
- If the file is missing, it will be automatically created on first run with default values.
- The file format is **TOML-like**, easy to read and edit manually.

**Syntax rules:**
- `key = "value"`
- Comments start with `#`
- Strings should be enclosed in quotes
- Session definitions are placed under the `[sessions]` section

---

## 🔹 Sections Overview

| Section | Purpose |
|----------|----------|
| *Application settings* | Global app behavior and theme configuration |
| *PuTTY settings* | Launch and display parameters for PuTTY |
| *Picker window* | Behavior and appearance of the session picker |
| *Autostart* | Whether PuttyStarter runs on Windows startup |
| *Logging* | Local log settings |
| `[sessions]` | List of SSH sessions (`username@host[:port]`) |

---

## ⚙️ Application Settings

| Key | Type | Default | Description |
|-----|------|----------|-------------|
| `hotkey` | string | `"Ctrl+Alt+P"` | Global keyboard shortcut that opens the picker window. Use modifiers `Ctrl`, `Alt`, `Shift`, `Win` (e.g. `"Win+Shift+S"`). |
| `theme` | string | `"auto"` | UI theme: `"auto"`, `"light"`, `"dark"`. `"auto"` follows the Windows system setting. |
| `single_instance` | bool | `true` | Allows only one instance of the application to run at a time. |
| `show_notifications` | bool | `true` | Displays tray notifications when something goes wrong (e.g., hotkey conflict, missing PuTTY). |

---

## 🖥️ PuTTY Settings

| Key | Type | Default | Description |
|-----|------|----------|-------------|
| `putty_path` | string | `""` | Full path to `putty.exe`. If left empty, the system PATH is searched. |
| `fullscreen_mode` | string | `"maximize"` | Defines how PuTTY opens. Currently only `"maximize"` is supported. |
| `putty_start_timeout_ms` | int | `4000` | Timeout in milliseconds to wait for PuTTY’s window before applying maximize/position operations. |

---

## 🪟 Picker Window

| Key | Type | Default | Description |
|-----|------|----------|-------------|
| `picker_topmost` | bool | `true` | Keeps the picker window always on top of other windows. |
| `picker_monitor` | string | `"cursor"` | Determines where the picker window appears: `"cursor"` = on monitor with the mouse, `"primary"` = on the main monitor. |
| `dpi_awareness` | string | `"per_monitor_v2"` | DPI scaling mode (reserved for future use). |
| `remember_last_selection` | bool | `true` | Highlights the last used session when reopening the picker. |

---

## 🚀 Autostart

| Key | Type | Default | Description |
|-----|------|----------|-------------|
| `run_at_startup` | bool | `false` | If enabled, adds PuttyStarter to Windows **HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run**, starting it automatically with Windows. |

---

## 🧾 Logging

| Key | Type | Default | Description |
|-----|------|----------|-------------|
| `log_enabled` | bool | `true` | Enables writing to a local log file (`PuttyStarter.log`) in the same directory as the executable. |
| `log_level` | string | `"info"` | Log verbosity: `"error"`, `"warn"`, `"info"`, `"debug"`. |
| `log_max_files` | int | `3` | Maximum number of rotated log files to keep. |
| `log_max_size_kb` | int | `256` | Maximum size of a single log file (in kilobytes) before rotation. |

---

## 🧩 Session Definitions (`[sessions]`)

This section defines user sessions to be launched through PuTTY.  
Each entry uses the format:

```toml
[sessions]
id = "username@host[:port]"
```

| Field | Description |
|-----|------|
| id | Unique name displayed in the picker (any text without spaces). | 
| username@host | Standard SSH target passed directly to PuTTY. | 
| :port | Optional SSH port. If provided, -P <port> is added to PuTTY’s  command. | 

**Example:**
```toml
[sessions]
work_vm   = "admin@10.0.0.15"
prod01    = "deploy@prod1.example.com"
bastion   = "ops@bastion.local:2222"
```

> ⚠️ PuttyStarter does not handle authentication.
It assumes keys or Pageant are already configured and available.

---

## 🧰 Full Example Configuration
```toml
# PuttyStarter.conf — example configuration

# — Application —
hotkey = "Ctrl+Alt+P"
theme = "auto"
single_instance = true
show_notifications = true

# — PuTTY —
putty_path = "C:\\Tools\\PuTTY\\putty.exe"
fullscreen_mode = "maximize"
putty_start_timeout_ms = 4000

# — Picker Window —
picker_topmost = true
picker_monitor = "cursor"
dpi_awareness = "per_monitor_v2"
remember_last_selection = true

# — Autostart —
run_at_startup = false

# — Logging —
log_enabled = true
log_level = "info"
log_max_files = 3
log_max_size_kb = 256

# — Sessions —
[sessions]
vm_work   = "admin@10.0.0.15"
prod01    = "deploy@prod1.example.com"
bastion   = "ops@bastion.local:2222"
```

---

## 🗒️ Notes

* Changes to the configuration file take effect after restarting the app.
* It’s safe to edit the file manually in any text editor (UTF-8 recommended).
* If the file becomes invalid, PuttyStarter will revert to defaults and log a warning.
* Default theme and hotkey can always be restored by deleting PuttyStarter.conf — a new one will be generated on the next launch.