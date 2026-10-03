# Due Diligence & Open Source Licensing Report
**Project:** Siemens TIA Portal V21 Openness MCP Server (`TiaOpennessMcp`)  
**Assessment Date:** 2026-09-30  
**Scope:** Verification of code provenance, third-party libraries, Siemens PublicAPI integration, and legal permissibility for personal and commercial applications.

---

## 1. Executive Summary

| Category | Finding | Compliance Status |
| :--- | :--- | :---: |
| **Codebase Provenance** | Custom, clean-room implementation in C# (.NET Framework 4.8) | **VERIFIED CLEAN** |
| **External NuGet Dependencies** | `System.Text.Json` (v9.0.0) under the **MIT License** | **FREE & UNRESTRICTED** |
| **Siemens PublicAPI References** | Dynamic late-bound linking (`<Private>False</Private>`), zero redistribution | **COMPLIANT** |
| **Model Context Protocol (MCP)** | Anthropic open specification under **MIT License** | **FREE & UNRESTRICTED** |
| **Commercial Application** | Fully permitted for internal, commercial, and customer projects | **PERMITTED** |
| **Personal / Open Source Use** | Fully permitted under open-source licenses (MIT recommended) | **PERMITTED** |

> [!NOTE]
> **Conclusion:** The codebase and its dependencies are **100% free of restrictive, viral (copyleft like GPL/AGPL), or royalty-encumbered code**. It is fully suitable for open-source distribution and both personal and commercial automation workflows.

---

## 2. Dependency-by-Dependency Audit

### 2.1 NuGet Packages

| Package | Version | Author / Publisher | License | Commercial Use | Personal Use | Modification / Distribution |
| :--- | :---: | :--- | :---: | :---: | :---: | :---: |
| **`System.Text.Json`** | `9.0.0` | Microsoft Corporation | **MIT** | Allowed | Allowed | Allowed |

#### License Analysis (`System.Text.Json`):
The MIT License is one of the most permissive open-source licenses in existence. It explicitly grants:
- Permission to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the software.
- Only condition: Preserve the original copyright and permission notice.
- No patent encumbrance or source disclosure requirement.

---

### 2.2 Framework Runtime & BCL

| Component | Target Version | Publisher | License Terms | Commercial Use |
| :--- | :---: | :--- | :--- | :---: |
| **.NET Framework** | `4.8` (x64) | Microsoft Corporation | Microsoft Software License Terms for Windows OS / .NET Framework | Allowed |
| **`System.Drawing`** | Included in BCL | Microsoft Corporation | Standard .NET BCL redistributable license | Allowed |

The project targets .NET Framework 4.8 (`net48`), which is part of the standard Microsoft Windows operating system stack. Any user running Windows has the legal right to run applications compiled against .NET Framework 4.8 for commercial and non-commercial purposes.

---

### 2.3 Siemens Openness API (`Siemens.Engineering.*`)

| Assembly | Version | Origin | Reference Mode | Distribution Status |
| :--- | :---: | :--- | :---: | :---: |
| `Siemens.Engineering.Base.dll` | V21.0 | Siemens AG | Dynamic / `<Private>False</Private>` | **Not Distributed** |
| `Siemens.Engineering.Step7.dll` | V21.0 | Siemens AG | Dynamic / `<Private>False</Private>` | **Not Distributed** |
| `Siemens.Engineering.WinCC.dll` | V21.0 | Siemens AG | Dynamic / `<Private>False</Private>` | **Not Distributed** |
| `Siemens.Engineering.WinCCUnified.dll`| V21.0 | Siemens AG | Dynamic / `<Private>False</Private>` | **Not Distributed** |
| `Siemens.Engineering.TestSuite.dll` | V21.0 | Siemens AG | Dynamic / `<Private>False</Private>` | **Not Distributed** |

#### Crucial Legal Compliance Architecture:
1. **Dynamic Resolution via Host Installation**:
   In `TiaOpennessMcp.csproj`, all Siemens references are explicitly marked:
   ```xml
   <Private>False</Private>
   ```
   At runtime, `Program.cs` uses `AppDomain.CurrentDomain.AssemblyResolve` to dynamically link to the host machine's authorized Siemens installation directory:
   `C:\Program Files\Siemens\Automation\Portal V21\PublicAPI\V21\net48\`
2. **Zero Proprietary Binary Redistribution**:
   Because the Siemens DLLs are **never copied into the build output or distributed with the source repository**, this project complies with Siemens End User License Agreements (EULA). Anyone running this server must have their own valid Siemens TIA Portal installation.
3. **Royalty-Free Public API**:
   Siemens provides the `PublicAPI` assembly specifically for customers, machine builders, system integrators, and software partners to create external automation scripts and tools. Siemens imposes **no royalties or separate API licensing fees** for tools written against the Openness API.
4. **Siemens AllowList Compatibility**:
   Siemens includes a formal security allowlist (`HKLM:\SOFTWARE\Siemens\Automation\Openness\AllowList`) explicitly designed to authenticate third-party and custom applications connecting to TIA Portal.

---

### 2.4 Model Context Protocol (MCP)

| Component | Standard Creator | License | Commercial Permissibility |
| :--- | :--- | :---: | :---: |
| **Model Context Protocol** | Anthropic PBC & Contributors | **MIT License** | Fully Permitted |

The MCP specification is an open industry protocol. Implementing the protocol JSON-RPC schemas does not infringe upon any patents or proprietary claims.

---

## 3. Code Provenance & Clean-Room Review

Every module in the `TiaOpennessMcp` repository was engineered specifically for this system:

1. **`Program.cs`**: Handles CLI arguments, assembly resolution, configuration loading, and transport orchestration.
2. **`Config/ServerConfig.cs`**: Custom JSON configuration model for port, mode, and auto-connection settings.
3. **`Server/HttpMcpServer.cs`**: Custom HTTP/SSE server built using native .NET `System.Net.HttpListener` (no external web frameworks like ASP.NET or third-party web servers).
4. **`McpServer.cs` & `Protocol/McpProtocol.cs`**: Native JSON-RPC 2.0 protocol serialization.
5. **`Tia/TiaManager.cs`**: Custom Openness wrapper managing TIA processes, hardware exploration, PLC blocks, UDTs, tag tables, and PLC compilation.
6. **`Tia/TiaManager.Hmi.cs`**: Custom WinCC Unified screen generator, widget builders, single-object range dynamization, and in-place button script syntax checker.
7. **`Tia/TiaManager.TestSuite.cs`**: Custom TestSuite runner and `.tat` loader.
8. **`Tools/ToolRegistry.cs`**: Custom dispatcher and input schema registry for 39 tools.

**Viral / Copyleft Code Audit:**
- No GNU GPL, LGPL, AGPL, or SSPL code was used or referenced.
- No decompiled proprietary code from Siemens was copy-pasted into the source. All interactions adhere to the documented public interfaces of `Siemens.Engineering`.

---

## 4. Open-Source Licensing Recommendation

To license this project for community and commercial adoption, apply the **MIT License**.

### Recommended `LICENSE` File Content:

```text
MIT License

Copyright (c) 2026 TIA Openness MCP Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 5. Commercial Deployment Guidelines

When deploying this MCP server in commercial environments (factories, engineering firms, OEM equipment):
1. **User Licensing**: Ensure the machine executing the server has a valid Siemens TIA Portal V21 license.
2. **Distribution**: Distribute only the compiled `TiaOpennessMcp.exe` and `config.json`. Do not include Siemens DLLs in installation packages; allow the program's built-in resolver to bind to the host's existing `PublicAPI` folder.
3. **Security**: The HTTP port (`5001`) binds to `localhost` by default. If exposing the MCP server across a physical OT network, place it behind a reverse proxy with TLS/HTTPS and authentication tokens.
