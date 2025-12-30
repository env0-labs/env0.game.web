
# env0.terminal JSON Authoring Guide

## 1. Filesystem JSON Structure

- **Top-level property must be** `"root"` (case-insensitive).
- **Every directory is a JSON object.**
- **Every file is an object with:**
    - `"type": "file"`
    - `"content": "..."` (string, required)
- **Directories may *not* have `type` or `content`.**
- **No additional properties allowed on file nodes.**
- **No additional properties allowed on directory nodes except other files/dirs.**
- **Case matters for keys (directory/file names)!**

### Example

```json
{
  "root": {
    "tutorial.txt": {
      "type": "file",
      "content": "Welcome to env0.terminal! Try commands like ls, cd, cat, and help."
    },
    "docs": {
      "readme.txt": {
        "type": "file",
        "content": "This is your README file."
      },
      "deep": {
        "hidden.log": {
          "type": "file",
          "content": "You found a hidden log file!"
        }
      }
    }
  }
}
```

### Rules Recap

- **Files:**
    - Must have only `"type"` and `"content"`.
    - `"type"` must be exactly `"file"`.
    - `"content"` must be a string (can be empty, multi-line, etc).
- **Directories:**
    - Must NOT have `"type"` or `"content"`.
    - Contain only more file/directory entries.
- **No arrays, no numbers, no booleans at any node.**
- **Max depth: 5 layers.**
- **File and folder names are always keys, never values.**

### Loader Will Reject If

- Directory node has `"type"` or `"content"`.
- File node has extra properties beyond `"type"` and `"content"`.
- File node is missing either property.
- Empty `"root"` (must contain at least one entry).
- `"content"` is not a string.

---

## 2. Devices.json

- Must be a JSON array (`[ ... ]`).
- Each device entry must include:
    - `ip` (string)
    - `subnet` (string)
    - `hostname` (string)
    - `mac` (string, unique per device)
    - `username` (string)
    - `password` (string)
    - `filesystem` (filename, e.g., `"Filesystem_1.json"`)
    - `motd` (string)
    - `description` (string)
    - `ports` (array of strings, e.g. `[ "22", "80" ]`)
    - `interfaces` (array of objects, each with `name`, `ip`, `subnet`, `mac`)
- **All fields are required.**
- **Filesystem value must match a real filesystem JSON file.**
- **Example:**

```json
[
  {
    "ip": "10.10.10.1",
    "subnet": "255.255.255.0",
    "hostname": "workstation.node.zero",
    "mac": "00:11:22:33:44:01",
    "username": "admin",
    "password": "pass",
    "filesystem": "Filesystem_1.json",
    "motd": "Welcome to workstation.node.zero",
    "description": "Primary user workstation",
    "ports": ["22", "80"],
    "interfaces": [
      {
        "name": "eth0",
        "ip": "10.10.10.1",
        "subnet": "255.255.255.0",
        "mac": "00:11:22:33:44:01"
      }
    ]
  }
]
```

---

## 3. BootConfig.json

- **Must contain a property:** `BootText`
- **Type:** Array of strings, in the order shown at boot.

**Example:**
```json
{
  "BootText": [
    "Loading system...",
    "Initializing hardware...",
    "Mounting virtual drives...",
    "Launching terminal...",
    "Boot complete."
  ]
}
```

---

## 4. UserConfig.json

- **Properties:**
    - `Username` (string)
    - `Password` (string)
- **All fields required, ASCII only.**
- **Example:**
```json
{
  "Username": "admin",
  "Password": "pass"
}
```

---

## 5. General Tips

- **All files must be valid UTF-8, with no BOM.**
- **No comments allowed in JSON files (JSON does not support comments).**
- **All string values should be ASCII only (especially usernames and passwords).**
- **Never add extra properties—strict schema enforced.**
- **If a file or device is missing required properties, loader will log errors and may refuse to load.**
- **Use double quotes only (`"..."`). Single quotes are not valid in JSON.**
- **Always validate with a linter (e.g. https://jsonlint.com) before loading.**

---

## 6. Troubleshooting Loader Errors

- If your file does not appear or is `(empty file)`, check:
    - Property casing (`root` vs `Root`)
    - Presence of `type` and `content` on all files
    - No forbidden properties on directories
    - No missing/extra fields in device entries

---

*Stick to this structure and your env0.terminal content will load perfectly, every time!*
