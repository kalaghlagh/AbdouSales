# Deployment guide — AbdouSales

## 0. Quick start (one click)

1. Install the **.NET 8 SDK** (https://dotnet.microsoft.com/download/dotnet/8.0) and **Inno Setup 6**
   (https://jrsoftware.org/isdl.php) on your Windows PC.
2. Double-click **`build_all.cmd`** in the project root. It runs `publish.cmd` (Release / win-x64 /
   self-contained) and then `build_installer.cmd` (Inno Setup).
3. Your installer is created at **`Installer\Output\AbdouSales_Setup.exe`** — copy only that file to
   the other PC. Nothing else (no .NET, no Visual Studio) is needed there.

Files involved: `AbdouSales.sln`, `AbdouSales\AbdouSales.csproj`, `AbdouSales\Properties\PublishProfiles\FolderProfile.pubxml`,
`publish.cmd`, `build_installer.cmd`, `build_all.cmd`, `Installer\AbdouSales.iss`.
The installer offers English and French wizard languages; the app itself keeps Arabic/French switching inside Settings.
If the app is running during an upgrade, the installer asks to close it.

`tools\generate_lang.py` regenerates `Resources\Lang.ar.xaml` / `Lang.fr.xaml`. It now contains all 174 keys
(S001–S174), so running it does not lose any text. Edit texts there, not in the XAML files, if you use it.

## 1. What this project actually is (inspection results)

| Item | Finding |
|---|---|
| Project type | WPF desktop app, single project, `Microsoft.NET.Sdk` |
| Target framework | `net8.0-windows` (.NET 8) |
| `UseWPF` | true |
| Architecture | AnyCPU today → publishing as **win-x64** |
| NuGet packages | **None.** `project.assets.json` lists no `PackageReference` |
| Native DLLs | **None** |
| **SQLite** | **Not used anywhere in the project** — see section 2 |
| Data storage | JSON files via `Services/DataStore.cs` |
| Data location | `%AppData%\AbdouSales\` (customers/suppliers/products/invoices/company `.json`) |
| Entry point | `App.xaml` → `StartupUri="Views/LoginWindow.xaml"` |
| App icon (before) | none configured |
| External runtime files | none — no images, no fonts, no config files |
| Fonts used | `Segoe UI` and `Arial` — both ship with Windows |

## 2. About SQLite

There is **no SQLite in this project**. No `Microsoft.Data.Sqlite`, no `System.Data.SQLite`,
no `SQLitePCLRaw`, no `e_sqlite3.dll`, no `.db` file, no connection string, no NuGet packages at all.

Persistence is `System.Text.Json` writing plain JSON files in:

```
%AppData%\AbdouSales\
    customers.json
    suppliers.json
    products.json
    invoices.json
    company.json
```

This is good news for deployment:

* `%AppData%` is a per-user **writable** folder, so the "never write inside `C:\Program Files`" rule
  is already satisfied. **No database path change was needed.**
* `DataStore.Load()` calls `Directory.CreateDirectory(RootFolder)`, so the storage folder is created
  on first run automatically.
* There are no native libraries to bundle, and nothing that single-file publishing could break.

## 3. What was changed

Only four things were added. No business logic, no XAML, no views, no models were touched.

1. **`AbdouSales/Assets/app.ico`** (new) — a real multi-resolution Windows ICO built from your
   logo: 16, 24, 32, 48, 64, 128 and 256 px. The ≤64 px frames are stored as 32-bit BMP with an
   alpha channel and the 128/256 frames as PNG, which is the layout Windows Explorer handles most
   reliably. It is not a renamed PNG.
2. **`AbdouSales/AbdouSales.csproj`** — added `<ApplicationIcon>`, version/product metadata,
   and `DebugType=none` for Release only. The original properties were left exactly as they were.
3. **`AbdouSales/Properties/PublishProfiles/FolderProfile.pubxml`** (new) — Release / win-x64 /
   self-contained publish profile for the Visual Studio *Publish* button.
4. **`Installer/AbdouSales.iss`** (new) — Inno Setup script, plus `publish.cmd` and
   `build_installer.cmd` helper scripts at the project root.

Window icons were deliberately **not** added to each `.xaml`: a WPF `Window` with no `Icon` set falls
back to the icon embedded in the EXE, so `<ApplicationIcon>` already covers the title bar and taskbar.
That avoids editing nine XAML files for no benefit.

## 4. Build steps (on your Windows machine)

You need the **.NET 8 SDK** and an internet connection the first time (a self-contained publish
downloads the win-x64 runtime pack). Inno Setup 6 is needed for step 2.

### Step 1 — Publish

Double-click `publish.cmd`, or from a terminal in the project root:

```
dotnet publish AbdouSales\AbdouSales.csproj -c Release -r win-x64 --self-contained true -o AbdouSales\bin\Release\net8.0-windows\win-x64\publish
```

Output folder (~150 MB, ~200 files — that is normal for self-contained WPF):

```
AbdouSales\bin\Release\net8.0-windows\win-x64\publish\AbdouSales.exe
```

You can already run that EXE to test.

### Step 2 — Build the installer

Install **Inno Setup 6** from <https://jrsoftware.org/isdl.php>, then double-click
`build_installer.cmd` (or open `Installer\AbdouSales.iss` in the Inno Setup IDE and press
*Build → Compile*). Result:

```
Installer\Output\AbdouSales_Setup.exe
```

Copy that single file to a USB drive. That is all the other PC needs.

## 5. Why these publish settings

* **win-x64** — correct for essentially every Windows 10/11 PC you will meet in practice. Use x86
  only if you must support a genuinely 32-bit Windows install (very rare now); use win-arm64 only
  for Snapdragon/ARM laptops, and note that ARM64 devices run x64 apps through emulation anyway, so
  win-x64 still works there. Stick with win-x64.
* **Self-contained** — the .NET 8 Desktop Runtime is bundled, so the target PC needs nothing
  installed. This is what makes the "no Visual Studio, no SDK" requirement work.
* **PublishSingleFile = false** — you asked whether single-file is safe with SQLite. There is no
  SQLite here, so single-file would technically be safe. I still left it off because: the installer
  already delivers one file to the user (`AbdouSales_Setup.exe`), single-file WPF apps extract native
  bits to a temp folder on every start and are slower to launch, and it makes crash diagnostics
  harder. There is no upside for an installer-based deployment.
* **PublishTrimmed = false** — trimming breaks WPF (heavy reflection in XAML/BAML loading). Never
  enable it here.
* **PublishReadyToRun = false** — optional; enabling it would speed startup slightly at the cost of
  a bigger output. Safe to turn on later if you want.
* **InvariantGlobalization was deliberately NOT enabled** — it would break Arabic culture handling
  and date/number formatting.

## 6. Deployment review — what I checked

Checks I performed by reading the project:

* No `PackageReference` entries → no missing NuGet runtime assets, no native DLLs to copy.
* No hardcoded absolute paths (`C:\...`) anywhere in the C# or XAML.
* No `pack://` resource URIs, no image/font/config files loaded at runtime → nothing to forget in the
  installer payload.
* No reliance on the current working directory: all file access goes through
  `Path.Combine(Environment.GetFolderPath(ApplicationData), "AbdouSales")`, which is absolute and
  independent of where the shortcut launches from. This matters because shortcuts often start with a
  different working directory.
* Data folder is per-user and writable → no UAC/permission failure after install to `Program Files`.
* `DataStore.Load()` creates the folder; each save calls `Directory.CreateDirectory` again → first
  run on a clean PC works.
* Corrupt-file handling exists (`try/catch` returning empty collections) → the app will not crash on
  a bad JSON file.
* Printing uses WPF's `PrintDialog` (`Services/PrintHelper.cs`) → no external dependency.
* Fonts referenced are Windows built-ins.
* ICO validated: all 7 frames decode with correct colours and a transparent background.

**What I could NOT do:** this environment is Linux with no .NET SDK and no network access, so I did
not compile the project, did not run `dotnet publish`, did not run `ISCC.exe`, and did not launch the
EXE. I am not claiming any of that was tested. Steps 1 and 2 above must be run by you on Windows.

## 7. Test checklist for the other Windows PC

On a PC with no Visual Studio, no .NET SDK, no dev tools:

1. Copy `AbdouSales_Setup.exe` from the USB drive and run it. Accept the UAC prompt.
2. Change the install folder on the directory page — confirm it is not forced.
3. Finish the install with "create desktop icon" ticked.
4. Check the **desktop** shortcut: correct Arabic name, your logo as the icon.
5. Check **Start Menu → AbdouSales**: shortcut plus uninstall entry.
6. Open the install folder in Explorer and confirm `AbdouSales.exe` shows your logo at Large
   Icons *and* Details view.
7. Launch it. The login window should appear. Log in with `admin` / `tam12123`.
8. ⚙ إعدادات الشركة → enter the company details → save.
9. Add a customer including رقم السجل التجاري and الرقم الجبائي → save → reopen the window and
   confirm it persisted.
10. Add a product to the stock list.
11. تحرير فاتورة → pick the customer, add lines, check the 19% VAT total → 💾 save.
12. سجل الفواتير → confirm the invoice is listed.
13. 🖨 معاينة وطباعة → confirm the header shows the company info and the customer's registre de
    commerce / identifiant fiscal, and that the amount in Arabic words is right. Print to
    *Microsoft Print to PDF* as a safe test.
14. Close the app, reopen it, confirm all data is still there.
15. Check `%AppData%\AbdouSales\` contains the five JSON files.
16. Settings → Apps → confirm "AbdouSales" version 1.0.0 is listed, then test Uninstall.
17. After uninstalling, confirm `%AppData%\AbdouSales\` still has your data (intentional — it
    survives upgrades).

## 8. Notes on naming

`AssemblyName` is `AbdouSales`, so the built executable is `AbdouSales.exe` and the installer is
`AbdouSales_Setup.exe`. If you ever need a different executable name, change `AssemblyName` in
`AbdouSales.csproj` and `MyAppExeName` in `Installer\AbdouSales.iss`, then re-run `publish.cmd`
and `build_installer.cmd`.

## 9. Publishing a future version

Bump `<Version>`, `<AssemblyVersion>` and `<FileVersion>` in the `.csproj` and `MyAppVersion` in the
`.iss`, then re-run both scripts. Keep the `AppId` GUID in the `.iss` unchanged — that is what lets
the new installer upgrade the old install in place instead of creating a duplicate entry. User data
in `%AppData%` is untouched by upgrades.
