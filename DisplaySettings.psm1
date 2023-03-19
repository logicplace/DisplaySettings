# Functions for managing the string.
# Throw the folder in one of the folders on $Env:PSModulePath

Function Get-ScreenDevices {
	<#
	.Synopsis
		Gets information about available monitors
	.Description
		Uses Pinvoke and EnumDisplayDevices Win32API to get the info
	#>
	param (
		[int] $Screen = 0,
		[switch] $All = $false
	)

	if ( $Screen -gt 0 ) {
		[Display.Device]::new($Screen).ListPossibleConfigs()
	}
	else {
		[Display.Device]::ListScreens($All) | ForEach-Object {
			if ($null -eq $_.Sink) {
				"$($_.Screen). (disconnected) $($_.Source)"
			}
			else {
				$cur = ([string]$_.GetSettings()).replace("@", " @ ");
				"$($_.Screen). $($_.Source) -> $($_.Sink) : $cur"
			}
		}
	}
}

Function Set-ScreenSettings {
	<#
	.Synopsis
		Sets the screen resolution and refresh rate of a monitor
	.Description
		Uses Pinvoke and ChangeDisplaySettings Win32API to make the change
	.Example
		Set-ScreenSettings -Width 1024 -Height 768 -Refresh 60
		Set-ScreenSettings -Screen 1 -Width 1024 -Height 768 -Refresh 60
	#>
	param (
		[int] $Width,
		[int] $Height,
		[int] $Refresh = 60,
		[int] $Screen = 1,
		[Parameter(Position=0, ValueFromRemainingArguments = $true)]
        [string[]] $Settings
	)

	if ($Settings.Count -gt 0) {
		$Settings | ForEach-Object {
			$res = $_ | Select-String -Pattern '^(?:(\d+):)?(\d+)x(\d+)(?:@(\d+)(?:Hz)?)?$'
			if ($null -ne $res) {
				$groups = $res.Matches[0].Groups
				$sc = $groups[1].Success ? [int]$groups[1].Value : $Screen
				[Display.Device]::new($sc).ChangeSettings(
					[int]$groups[2].Value,
					[int]$groups[3].Value,
					$groups[4].Success ? [int]$groups[4].Value : $Refresh
				) ? "Screen $sc's settings changed successfully." : "Failed to set screen $sc."
			}
		}
	}
	elseif ([Display.Device]::new($Screen).ChangeSettings($Width, $Height, $Refresh)) {
		"Screen $Screen's settings changed successfully."
	}
	else {
		"Failed to set screen $sc."
	}
}

$pinvokeCode = Get-Content -Path "$PSScriptRoot\DisplayManager.cs" -raw
Add-Type $pinvokeCode

Export-ModuleMember -Function Get-ScreenDevices,Set-ScreenSettings
