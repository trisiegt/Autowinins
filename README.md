# Autowinins
**The best Windows installer EVER!!**

Autowinins is a deployment tool designed to bypass the extremely shitty default Windows installer. It handles disk partitioning, image application, and boot configuration in one pass.

## Features
* **Full Automation:** Partitioning (GPT/UEFI), Image applying, and BCD configuration.
* **Custom User Creation:** Prompts for username and password during setup; accounts are created before first login, bypassing the OOBE!
* **Clean Exit:** The post-install automation script self-destructs after completion to keep the system clean.
* **Architecture Support:** Available as a Batch script or a standalone x64 VB.NET executable.

## How it Works
1. **Disk Initialization:** Absolutely ANNIHILATES Disk 0 and sets up a 500MB EFI partition and a primary NTFS partition.
2. **DISM Application:** Applies the selected WIM/ESD index directly to C:.
3. **Registry Injection:** Mounts the offline SYSTEM and SOFTWARE hives to inject the `CmdLine` setup trigger and enable verbose status messages.
4. **Post-Install:** On first boot, `windeploy.exe` runs, the user is created, and the system bypasses OOBE to reach the desktop.

## Usage
1. Boot into a WinPE environment (Standard Windows ISO -> Shift + F10).
2. Run the Batch file or the executable.
3. The tool will auto-scan for `install.wim` or `install.esd` in the `\sources` directory of any attached drive.
4. Follow the prompts for Index selection and User creation.
5. Reboot when prompted.

## Safety Warning
This tool is destructive! It is hardcoded to target **Disk 0** and will run the `clean` command. Do not use this on a drive containing data you want to keep.
Use

## Technical Details
* **Language:** VB.NET or Batch, take your pick!
* **Target Runtime:** win-x64 or win-x86 (Self-contained)
* **Framework:** .NET 8.0
* **License:** GPL-3.0

Made by justTrisie.
