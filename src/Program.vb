Imports System.IO
Imports System.Diagnostics

Module Module1
    Sub Main()
        Console.WriteLine("Autowinins - VB.NET Windows installer!")
        Console.WriteLine()
        Console.WriteLine("      made by justTrisie!")
        Console.WriteLine("program executed at " & DateTime.Now.ToShortTimeString() & ".")
        Console.WriteLine()

        Dim installfile As String = ""

        ' Searching for install.wim or install.esd automatically
        Console.WriteLine("Searching for install image...")
        Dim drives As String() = Directory.GetLogicalDrives()
        For Each drive In drives
            If File.Exists(drive & "sources\install.wim") Then
                installfile = drive & "sources\install.wim"
                Exit For
            ElseIf File.Exists(drive & "sources\install.esd") Then
                installfile = drive & "sources\install.esd"
                Exit For
            End If
        Next

        If installfile = "" Then
            Console.WriteLine("Enter the FULL path to install.esd or install.wim! It's normally on the D or E drive.")
            Console.Write("full path: ")
            installfile = Console.ReadLine()
        End If

        Console.WriteLine("Found image at: " & installfile)
        RunCmd("dism.exe", "/get-wiminfo /wimfile:""" & installfile & """")

        Console.WriteLine()
        Console.Write("which index do you want to install?: ")
        Dim WinIndex As String = Console.ReadLine()

        Console.WriteLine()
        Console.WriteLine("Simple user creation prompt")
        Console.Write("enter the username you want: ")
        Dim customuser As String = Console.ReadLine()
        Console.Write("enter the password for " & customuser & " (leave blank for none): ")
        Dim custompass As String = Console.ReadLine()

        ' Diskpart
        Console.WriteLine("cleaning disk 0 and partitioning...")
        Dim dpScript As String = "sel dis 0" & vbCrLf &
                                 "clean" & vbCrLf &
                                 "convert gpt" & vbCrLf &
                                 "cre par efi size=500" & vbCrLf &
                                 "form fs=fat32 quick" & vbCrLf &
                                 "ass letter w" & vbCrLf &
                                 "cre par pri" & vbCrLf &
                                 "form fs=ntfs quick" & vbCrLf &
                                 "ass letter c" & vbCrLf &
                                 "exit"
        File.WriteAllText("X:\dp.txt", dpScript)
        RunCmd("diskpart.exe", "/s X:\dp.txt")
        File.Delete("X:\dp.txt")

        Console.WriteLine("applying windows image...")
        Dim dismResult = RunCmd("dism.exe", "/apply-image /imagefile:""" & installfile & """ /index:" & WinIndex & " /applydir:C:\")

        If dismResult <> 0 Then
            Console.WriteLine()
            Console.WriteLine("[!] DISM has failed. Please check your path.")
            Console.ReadLine()
            Return
        End If

        ' --- boot files ---
        Console.WriteLine("configuring boot files...")
        RunCmd("bcdboot.exe", "C:\Windows /s W: /f UEFI")

        ' Creating the postinstall script
        Console.WriteLine("creating automation script for " & customuser & "...")

        ' Handle the blank password logic for the net user command
        Dim userCommand As String
        If String.IsNullOrWhiteSpace(custompass) Then
            userCommand = "net user """ & customuser & """ /add"
        Else
            userCommand = "net user """ & customuser & """ """ & custompass & """ /add"
        End If

        Dim postInstall As String = "@echo off" & vbCrLf &
    "echo running windeploy..." & vbCrLf &
    "cd /d %windir%\system32" & vbCrLf &
    "call oobe\windeploy" & vbCrLf &
    "echo creating user account: " & customuser & "..." & vbCrLf &
    userCommand & vbCrLf &
    "net localgroup Users """ & customuser & """ /add" & vbCrLf &
    "net localgroup Administrators """ & customuser & """ /add" & vbCrLf &
    "echo final registry tweaks..." & vbCrLf &
    "reg add HKLM\System\Setup /v OOBEInProgress /t REG_DWORD /d 0 /f" & vbCrLf &
    "reg add HKLM\System\Setup /v SetupType /t REG_DWORD /d 0 /f" & vbCrLf &
    "reg add HKLM\System\Setup /v SystemSetupInProgress /t REG_DWORD /d 0 /f" & vbCrLf &
    "echo logic finished. self-destructing..." & vbCrLf &
    "timeout /t 5" & vbCrLf &
    "start /b """" cmd /c del ""%~f0"" ^& exit"

        File.WriteAllText("C:\Windows\System32\automate_setup.bat", postInstall)

        ' --- offline registry tweaks ---
        Console.WriteLine("loading registry for pre-boot tweaks...")
        RunCmd("reg.exe", "load HKLM\OFFLINE_SOFT C:\Windows\System32\config\SOFTWARE")
        RunCmd("reg.exe", "load HKLM\OFFLINE_SYS C:\Windows\System32\config\SYSTEM")

        RunCmd("reg.exe", "add ""HKLM\OFFLINE_SOFT\Microsoft\Windows\CurrentVersion\Policies\System"" /v VerboseStatus /t REG_DWORD /d 1 /f")
        RunCmd("reg.exe", "add ""HKLM\OFFLINE_SOFT\Microsoft\Windows\CurrentVersion\Policies\System"" /v EnableCursorSuppression /t REG_DWORD /d 0 /f")

        ' setup cmdline to run the automation script on boot
        RunCmd("reg.exe", "add ""HKLM\OFFLINE_SYS\Setup"" /v CmdLine /t REG_SZ /d ""cmd.exe /c C:\Windows\System32\automate_setup.bat"" /f")

        Console.WriteLine("Unloading hives...")
        RunCmd("reg.exe", "unload HKLM\OFFLINE_SOFT")
        RunCmd("reg.exe", "unload HKLM\OFFLINE_SYS")

        Console.WriteLine()
        Console.WriteLine("Installation complete. Hit any key to reboot, and Autowinins will take it from here!")
        Console.ReadKey(True)

        ' wpeutil reboot
        Dim rebootProc As New ProcessStartInfo("wpeutil", "reboot")
        Process.Start(rebootProc)
    End Sub

    Function RunCmd(exe As String, args As String) As Integer
        Dim psi As New ProcessStartInfo(exe, args)
        psi.UseShellExecute = False
        psi.CreateNoWindow = False
        Dim p = Process.Start(psi)
        p.WaitForExit()
        Return p.ExitCode
    End Function
End Module