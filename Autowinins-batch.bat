@echo off
echo Autowinins - Batch Windows installer!
echo.
echo      made by justTrisie!
echo program executed at %time%.
echo.

:search
echo Searching for install image...
rem Searching for install.wim or install.esd automatically
for %%i in (C D E F G H I J K L M N O P Q R S T U V W Y Z) do (
    if exist "%%i:\sources\install.wim" (
        set "installfile=%%i:\sources\install.wim"
        goto found
    )
    if exist "%%i:\sources\install.esd" (
        set "installfile=%%i:\sources\install.esd"
        goto found
    )
)

echo [!] Could not find an install image automatically.
:fetchindex
echo Enter the FULL path to install.esd or install.wim! It's normally on the D or E drive.
set /p installfile=full path: 

:found
echo Found image at: %installfile%
dism /get-wiminfo /wimfile:"%installfile%"

echo.
set /p WinIndex=which index do you want to install?: 

echo.
echo Simple user creation prompt
set /p customuser=enter the username you want: 
set /p custompass=enter the password for %customuser% (leave blank for none): 

rem Diskpart
echo cleaning disk 0 and partitioning...
(
echo sel dis 0
echo clean
echo convert gpt
echo cre par efi size=500
echo form fs=fat32 quick
echo ass letter w
echo cre par pri
echo form fs=ntfs quick
echo ass letter c
echo exit
) > X:\dp.txt

diskpart /s X:\dp.txt >nul
del X:\dp.txt

echo applying windows image...
dism /apply-image /imagefile:"%installfile%" /index:%WinIndex% /applydir:C:\

if %errorlevel% neq 0 (
    echo.
    echo [!] DISM has failed. Please check your path.
    pause
    goto fetchindex
)

rem --- boot files ---
echo configuring boot files...
bcdboot C:\Windows /s W: /f UEFI

rem Creating the postinstall script
echo creating automation script for %customuser%...
echo @echo off > C:\Windows\System32\automate_setup.bat
echo echo running windeploy... >> C:\Windows\System32\automate_setup.bat
echo cd /d %%windir%%\system32 >> C:\Windows\System32\automate_setup.bat
echo call oobe\windeploy >> C:\Windows\System32\automate_setup.bat
echo echo creating user account: %customuser%... >> C:\Windows\System32\automate_setup.bat

if "%custompass%"=="" (
    echo net user "%customuser%" /add >> C:\Windows\System32\automate_setup.bat
) else (
    echo net user "%customuser%" "%custompass%" /add >> C:\Windows\System32\automate_setup.bat
)

echo net localgroup Users "%customuser%" /add >> C:\Windows\System32\automate_setup.bat
echo net localgroup Administrators "%customuser%" /add >> C:\Windows\System32\automate_setup.bat
echo echo final registry tweaks... >> C:\Windows\System32\automate_setup.bat
echo reg add HKLM\System\Setup /v OOBEInProgress /t REG_DWORD /d 0 /f >> C:\Windows\System32\automate_setup.bat
echo reg add HKLM\System\Setup /v SetupType /t REG_DWORD /d 0 /f >> C:\Windows\System32\automate_setup.bat
echo reg add HKLM\System\Setup /v SystemSetupInProgress /t REG_DWORD /d 0 /f >> C:\Windows\System32\automate_setup.bat
echo echo logic finished. self-destructing... >> C:\Windows\System32\automate_setup.bat
echo timeout /t 5 >> C:\Windows\System32\automate_setup.bat
echo start /b "" cmd /c del "%%~f0" ^& exit >> C:\Windows\System32\automate_setup.bat

rem --- offline registry tweaks ---
echo loading registry for pre-boot tweaks...
reg load HKLM\OFFLINE_SOFT C:\Windows\System32\config\SOFTWARE
reg load HKLM\OFFLINE_SYS C:\Windows\System32\config\SYSTEM

reg add "HKLM\OFFLINE_SOFT\Microsoft\Windows\CurrentVersion\Policies\System" /v VerboseStatus /t REG_DWORD /d 1 /f
reg add "HKLM\OFFLINE_SOFT\Microsoft\Windows\CurrentVersion\Policies\System" /v EnableCursorSuppression /t REG_DWORD /d 0 /f

rem setup cmdline to run the automation script on boot
reg add "HKLM\OFFLINE_SYS\Setup" /v CmdLine /t REG_SZ /d "cmd.exe /c C:\Windows\System32\automate_setup.bat" /f

echo Unloading hives...
reg unload HKLM\OFFLINE_SOFT
reg unload HKLM\OFFLINE_SYS

echo.
echo Installation complete. Hit any key to reboot, and Autowinins will take it from here!
pause>nul
wpeutil reboot