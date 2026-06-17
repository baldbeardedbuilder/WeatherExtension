# Releasing Weather for Command Palette

## Overview

This project uses an automated release pipeline via GitHub Actions. Creating a GitHub Release triggers a workflow that builds the extension, runs tests, creates MSIX packages, submits to the Microsoft Store, and publishes to WinGet.

## Prerequisites (One-Time Setup)

### 1. Microsoft Entra ID App Registration

1. Go to [Azure Portal → App registrations](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps)
2. Click **New registration**
3. Name it (e.g., "WeatherExtension CI")
4. Note the **Application (client) ID** and **Directory (tenant) ID**
5. Go to **Certificates & secrets** → **New client secret** → note the **Value**
6. In [Partner Center](https://partner.microsoft.com/dashboard) → **Account Settings** → **User Management**
7. Add the application with **Manager** role

### 2. Partner Center IDs

1. **Seller ID:** Partner Center → **Account Settings** → **Organization profile** → **Identifiers**
2. **Product ID:** Partner Center → **Apps and Games** → select "Weather for Command Palette" → note the App ID from the URL

### 3. Azure Trusted Signing

The MSIX packages are signed using [Azure Trusted Signing](https://learn.microsoft.com/azure/trusted-signing/) after building. This requires an Azure service principal with access to the signing account.

1. Ensure the Azure Trusted Signing account `bbb-signing` and certificate profile `BaldBeardedBuilder` are configured (shared with WalkThisWay)
2. Create an Azure service principal with access to the signing account
3. Store the service principal credentials as a GitHub secret named `AZURE_CREDS` in JSON format:
   ```json
   {
     "clientId": "<Application (client) ID>",
     "clientSecret": "<Client secret value>",
     "subscriptionId": "<Subscription ID>",
     "tenantId": "<Directory (tenant) ID>"
   }
   ```

### 4. WinGet Token

1. Go to [GitHub → Settings → Developer Settings → Personal access tokens → Tokens (classic)](https://github.com/settings/tokens)
2. Click **Generate new token (classic)**
3. Give it a descriptive name (e.g., "WinGet Releaser")
4. Select the `public_repo` scope
5. Generate and note the token value

> **Note:** The WinGet package identifier is `BaldBeardedBuilder.WeatherForCmdPal`. Before the first automated release, you must manually submit the initial package manifest to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) using [`wingetcreate new`](https://github.com/microsoft/winget-create). Subsequent releases will be handled automatically.

### 5. Store Publisher Identity

The Microsoft Store requires a specific publisher identity in the MSIX manifest that differs from the Trusted Signing certificate subject. The release workflow builds **two sets** of packages:

- **Sideload/WinGet** — Uses the Trusted Signing publisher, signed with Azure Trusted Signing
- **Microsoft Store** — Uses the Store publisher identity (`STORE_PUBLISHER_NAME` secret), unsigned (Microsoft re-signs during Store publication)

To find your Store publisher identity:

1. Go to [Partner Center](https://partner.microsoft.com/dashboard) → **Apps and Games** → select your app
2. **Product management** → **Product Identity**
3. Copy the **Package/Identity/Publisher** value (e.g., `CN=A8D6094E-...`)

### 5. GitHub Repository Secrets

Go to the repo **Settings** → **Secrets and variables** → **Actions** → **New repository secret**. Add:

| Secret Name | Source |
|---|---|
| `PARTNER_CENTER_TENANT_ID` | Entra ID → Directory (tenant) ID |
| `PARTNER_CENTER_CLIENT_ID` | Entra ID → Application (client) ID |
| `PARTNER_CENTER_CLIENT_SECRET` | Entra ID → Client secret value |
| `PARTNER_CENTER_SELLER_ID` | Partner Center → Seller ID |
| `STORE_PRODUCT_ID` | Partner Center → Product/App ID |
| `AZURE_CREDS` | Azure service principal JSON for Trusted Signing |
| `STORE_PUBLISHER_NAME` | Partner Center → App Identity → Package/Identity/Publisher (e.g., `CN=...`) |
| `WINGET_TOKEN` | GitHub PAT with `public_repo` scope |

## How to Release

1. Ensure all changes are merged to `main`
2. Go to GitHub → **Releases** → **Create a new release**
3. Create a new tag using semantic versioning: `v1.0.1`, `v1.2.0`, etc.
4. Write release notes (or use auto-generated notes)
5. Click **Publish release**

The workflow automatically:
- Builds x64 and ARM64 architectures
- Runs all tests
- Creates MSIX packages
- Uploads MSIX packages to the release as downloadable artifacts
- Submits to the Microsoft Store
- Submits to WinGet (via PR to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs))

## Tag Naming Convention

- Format: `vMAJOR.MINOR.PATCH` (e.g., `v1.0.1`)
- The `v` prefix is stripped and `.0` is appended to create the 4-part version for the MSIX manifest (e.g., `1.0.1.0`)

## What Happens After Publishing

- The Microsoft Store reviews the submission (may take 1–3 business days)
- If approved, the update is available to users via the Store
- The MSIX packages attached to the GitHub Release can be used for sideloading

## Troubleshooting

| Issue | Solution |
|---|---|
| Workflow fails at "Configure Store credentials" | Verify all 5 Partner Center GitHub secrets are set correctly |
| Store submission rejected | Check Partner Center dashboard for validation errors |
| WinGet submission fails | Verify `WINGET_TOKEN` secret is set and not expired. For the first release, manually submit using `wingetcreate new` |
| Build fails | The release workflow uses the same build process as CI — check for build errors in the Actions log |

## Graceful Shutdown & Uninstall

### Architecture

The extension now supports graceful shutdown during uninstall without requiring Command Palette to close. This is implemented via:

- **ShutdownCoordinator**: Listens on a named pipe (`\\.\pipe\WeatherExtension-Shutdown`) for external shutdown signals, with a fallback named event (`WeatherExtension-Shutdown`)
- **Program.cs**: Uses `WaitHandle.WaitAny()` to coordinate between normal Command Palette shutdown and external uninstall signals
- **Watchdog Timer**: A 5-second timeout ensures the process exits cleanly even if disposal hangs

### How Uninstall Works

1. Windows initiates uninstall
2. Uninstall script or tool sends shutdown signal via the named pipe
3. Extension process receives signal and gracefully disposes all resources
4. Process exits cleanly, allowing Windows to remove the package
5. No manual restart of Command Palette required

### Signaling Shutdown Externally (PowerShell Example)

```powershell
# Signal graceful shutdown via named pipe
$pipeName = "WeatherExtension-Shutdown"
$pipeClient = New-Object System.IO.Pipes.NamedPipeClientStream(".", $pipeName, "Out")
try {
    $pipeClient.Connect(1000)
    $sw = New-Object System.IO.StreamWriter($pipeClient)
    $sw.WriteLine("SHUTDOWN")
    $sw.Flush()
} catch {
    Write-Host "Could not signal shutdown: $_"
} finally {
    $pipeClient.Close()
}
```

Alternatively, use the named event for simpler but less robust signaling:

```powershell
# Signal shutdown via named event
[System.Threading.EventWaitHandle]::new($false, "Global", "WeatherExtension-Shutdown").Set()
```

### Resource Cleanup During Shutdown

The extension ensures all resources are properly disposed during shutdown:
- Timers are stopped
- HTTP client connections are closed
- Cancellation tokens are signalled
- Event handler subscriptions are unsubscribed
- No dangling handles or resources remain

See `ShutdownCoordinator.cs` and disposal methods for implementation details.
