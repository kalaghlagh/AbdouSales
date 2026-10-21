; =====================================================================
;  AbdouSales — سكربت المثبّت (Inno Setup 6)
;  يبني ملف:  Installer\Output\AbdouSales_Setup.exe
;
;  قبل التصريف (Compile) يجب أولاً تنفيذ النشر:
;      publish.cmd
;  (أو شغّل build_all.cmd لتنفيذ النشر وبناء المثبّت معاً بنقرة واحدة)
;  حتى يوجد المجلد:
;      AbdouSales\bin\Release\net8.0-windows\win-x64\publish
; =====================================================================

#define MyAppName "AbdouSales"
#define MyAppNameEn "AbdouSales"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "AbdouSales"
#define MyAppExeName "AbdouSales.exe"
#define MyPublishDir "..\AbdouSales\bin\Release\net8.0-windows\win-x64\publish"
#define MyIcon "..\AbdouSales\Assets\app.ico"

[Setup]
; معرّف فريد للتطبيق — لا تغيّره بين الإصدارات، فهو ما يسمح بالترقية فوق نسخة قديمة
AppId={{7C4E9A51-2D6B-4F18-9E33-A5B1C8D07F42}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

; مجلد التثبيت الافتراضي — ويمكن للمستخدم تغييره أثناء التثبيت
DefaultDirName={autopf}\{#MyAppNameEn}
DefaultGroupName={#MyAppName}
DisableDirPage=no
DisableProgramGroupPage=yes
AllowNoIcons=yes

; يحتاج صلاحيات المدير لأن التثبيت في Program Files
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; التطبيق 64-بت فقط
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

; مخرجات المثبّت
OutputDir=Output
OutputBaseFilename=AbdouSales_Setup
SetupIconFile={#MyIcon}
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

; إن كان البرنامج مفتوحاً أثناء الترقية يطلب المثبّت إغلاقه بدل فشل نسخ الملفات
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french";  MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; ينسخ كامل مخرجات النشر (التطبيق + كل مكوّنات .NET المطلوبة)
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; نسخة من الأيقونة بجانب التطبيق (اختيارية، مفيدة للاختصارات)
Source: "{#MyIcon}"; DestDir: "{app}"; DestName: "app.ico"; Flags: ignoreversion

[Icons]
; اختصار قائمة ابدأ
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
; اختصار إلغاء التثبيت داخل قائمة ابدأ
Name: "{group}\إلغاء تثبيت {#MyAppName}"; Filename: "{uninstallexe}"
; اختصار سطح المكتب
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

; ملاحظة مهمة:
; بيانات البرنامج (الزبائن، الفواتير، المخزون، معلومات الشركة) تُحفظ في
;     %AppData%\AbdouSales
; ولذلك لا تُحذف عند إلغاء التثبيت — وهذا مقصود حتى لا تفقد بياناتك عند الترقية.
