# DisplaySettings

PowerShell cmdlets for querying and altering display settings.

Based on [this script][1] by Microsoft but heavily modified.

[1]: https://web.archive.org/web/20190511070456/https://gallery.technet.microsoft.com/ScriptCenter/2a631d72-206d-4036-a3f2-2e150f297515/

## Setup

```ps1
# Find where to install it
echo $Env:PSModulePath

# Pick a path in your user space, preferably
# Mine is:
cd ([environment]::getfolderpath("mydocuments") + "\PowerShell\Modules")

# Clone this repo to that location as DisplaySettings (the default)
git clone https://github.com/logicplace/DisplaySettings.git

# Now import it whenever you need it
Import-Module -Name DisplaySettings
```

### From a script

When running from a script, my module path is different and the module needs to be signed. To sign the script yourself, follow [this][2], but I'll maybe figure this out and release it on PowerShell Gallery sometime... Still, you'll have to sign any scripts you make, too, if you want to double-click run them.

[2]: https://adamtheautomator.com/how-to-sign-powershell-script/

```ps1
# Make a script to find where to install it
Write-Host $Env:PSModulePath

# Configure .ps1 files to run with pwsh.exe via:
# Right-click -> Properties -> General -> Opens with
# For me that's "C:\Program Files\PowerShell\7\pwsh.exe"

# Pick a path in your user space, preferably
# Mine is:
cd ([environment]::getfolderpath("mydocuments") + "\WindowsPowerShell\Modules")

# Copy/sign/whatever

# Example script:
Import-Module -Name DisplaySettings
Set-ScreenSettings 1:3840x1080@120
```

## Get-ScreenDevices

Calling by itself will retrieve a list of available display devices as well as their current settings. For example:

```
PS C:\> Get-ScreenDevices
1. NVIDIA GeForce RTX 3070 -> ROG STRIX XG49VQ : 3840x1080 @ 120Hz
```

Use this index to look up which configurations are available for that device via:

```
PS C:\> Get-ScreenDevices -Screen 1

Width Height Refresh Comment
----- ------ ------- -------
 3840   2160      60
 3840   2160      59
            ...
 1600   1200      60
 1600   1200      59
 1600   1200      50
 3840   1080     120 current
 3840   1080     100
 3840   1080      60
 3840   1080      59
           ...
  640    480      60
  640    480      59
```

You may use any of these to set the state with Set-ScreenSettings.

To view virtual devices in addition to monitors, you may use the -All flag without -Screen.

## Set-ScreenSettings

Use this to set the screen resolution for one or more displays.

When configuring a single display, you may use flag arguments:

* -Width = Screen resolution width, required
* -Height = Screen resolution height, required
* -Refresh = Screen refresh rate in Hz. Defaults to 60
* -Screen = Screen to configure. Defaults to 1 (primary)

However in order to configure multiple screens you may specify a series of configurations in one of the following formats:

* `1024x768` - width and height specified, uses `-Screen` and `-Refresh` flags for those values.
* `1:1024x768` - configures screen 1 to the specified resolution and uses the `-Refresh` flag for that value.
* `1024x768@60` - uses 60 Hz refresh rate, same res, `-Screen` screen
* `1:1024x768@60` - I hope you get it
