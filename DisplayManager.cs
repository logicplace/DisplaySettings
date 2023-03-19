using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices; 

namespace Display {
	[StructLayout(LayoutKind.Sequential)]
	public struct DEVMODE1 {
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string dmDeviceName;
		public short dmSpecVersion;
		public short dmDriverVersion;
		public short dmSize;
		public short dmDriverExtra;
		public int dmFields;

		public short dmOrientation;
		public short dmPaperSize;
		public short dmPaperLength;
		public short dmPaperWidth;

		public short dmScale;
		public short dmCopies;
		public short dmDefaultSource;
		public short dmPrintQuality;
		public short dmColor;
		public short dmDuplex;
		public short dmYResolution;
		public short dmTTOption;
		public short dmCollate;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string dmFormName;
		public short dmLogPixels;
		public short dmBitsPerPel;
		public int dmPelsWidth;
		public int dmPelsHeight;

		public int dmDisplayFlags;
		public int dmDisplayFrequency;

		public int dmICMMethod;
		public int dmICMIntent;
		public int dmMediaType;
		public int dmDitherType;
		public int dmReserved1;
		public int dmReserved2;

		public int dmPanningWidth;
		public int dmPanningHeight;
	};

	[Flags()]
	public enum DisplayDeviceStateFlags : int {
		/// <summary>The device is part of the desktop.</summary>
		AttachedToDesktop = 0x1,
		MultiDriver = 0x2,
		/// <summary>The device is part of the desktop.</summary>
		PrimaryDevice = 0x4,
		/// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
		MirroringDriver = 0x8,
		/// <summary>The device is VGA compatible.</summary>
		VGACompatible = 0x10,
		/// <summary>The device is removable; it cannot be the primary display.</summary>
		Removable = 0x20,
		/// <summary>The device has more display modes than its output devices support.</summary>
		ModesPruned = 0x8000000,
		Remote = 0x4000000,
		Disconnect = 0x2000000
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct DISPLAY_DEVICE {
		[MarshalAs(UnmanagedType.U4)]
		public int cb;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string DeviceName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceString;
		[MarshalAs(UnmanagedType.U4)]
		public DisplayDeviceStateFlags StateFlags;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceID;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceKey;
	}

	class User_32 {
		[DllImport("user32.dll")]
		public static extern bool EnumDisplayDevices(string device, uint devNum, ref DISPLAY_DEVICE devMode, uint flags);
		[DllImport("user32.dll")]
		public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE1 devMode);
		[DllImport("user32.dll")]
		public static extern int ChangeDisplaySettingsEx(string deviceName, ref DEVMODE1 devMode, IntPtr hwnd, uint flags, IntPtr lParam);
		public const int ENUM_CURRENT_SETTINGS = -1;
		public const int CDS_UPDATEREGISTRY = 0x01;
		public const int CDS_TEST = 0x02;
		public const int DISP_CHANGE_SUCCESSFUL = 0;
		public const int DISP_CHANGE_RESTART = 1;
		public const int DISP_CHANGE_FAILED = -1;
	}


	public class Config {
		public int Width, Height, Refresh;
		public string Comment;

		public Config(int width, int height, int refresh, string comment = "") {
			Width = width;
			Height = height;
			Refresh = refresh;
			Comment = comment;
		}

		override public bool Equals(object obj) {
			return Equals(obj as Config);
		}

		public bool Equals(Config obj) {
			return (
				Width == obj.Width
				&& Height == obj.Height
				&& Refresh == obj.Refresh
			);
		}

		override public int GetHashCode() {
			return (this.Width, this.Height, this.Refresh).GetHashCode();
		}

		override public string ToString() {
			string cmt = "" != Comment ? $"  # {Comment}" : "";
			return $"{Width}x{Height}@{Refresh}Hz{cmt}";
		}

		public string ToArglist() {
			string cmt = "" != Comment ? $"  # {Comment}" : "";
			return $"-Width {Width} -Height {Height} -Refresh {Refresh}{cmt}";
		}
	}

	public class Device {
		public uint Screen;
		public string Source;
		public string Sink;

		public Device(uint screen = 1, string source = null, string sink = null) {
			Screen = screen;
			Source = source;
			Sink = sink;
		}

		public Config GetSettings() {
			DEVMODE1 dm = GetDevMode1();
			DISPLAY_DEVICE dd = GetDisplayDevice();
			User_32.EnumDisplayDevices(null, Screen - 1, ref dd, 0);
			if (!User_32.EnumDisplaySettings(dd.DeviceName, User_32.ENUM_CURRENT_SETTINGS, ref dm)) {
				Console.Error.WriteLine("Failed to get current settings");
				return null;
			}

			return new Config(
				dm.dmPelsWidth,
				dm.dmPelsHeight,
				dm.dmDisplayFrequency
			);
		}

		public bool ChangeSettings(int width, int height, int refresh) {
			DEVMODE1 dm = GetDevMode1();
			DISPLAY_DEVICE dd = GetDisplayDevice();
			User_32.EnumDisplayDevices(null, Screen - 1, ref dd, 0);

			if (User_32.EnumDisplaySettings(dd.DeviceName, User_32.ENUM_CURRENT_SETTINGS, ref dm)) {
				dm.dmPelsWidth = width;
				dm.dmPelsHeight = height;
				dm.dmDisplayFrequency = refresh;

				int iRet = User_32.ChangeDisplaySettingsEx(dd.DeviceName, ref dm, 0, User_32.CDS_TEST, 0);

				if (iRet == User_32.DISP_CHANGE_FAILED) {
					Console.Error.WriteLine("Unable to process your request.");
				}
				else {
					iRet = User_32.ChangeDisplaySettingsEx(dd.DeviceName, ref dm, 0, User_32.CDS_UPDATEREGISTRY, 0);
					switch (iRet) {
						case User_32.DISP_CHANGE_SUCCESSFUL:
							return true;
						case User_32.DISP_CHANGE_RESTART:
							Console.Error.WriteLine(
								"You need to reboot for the change to happen.\n"
								+" If you have any problem after rebooting your machine,\n"
								+"then try to change resolution in safe mode."
							);
							break;
						default:
							Console.Error.WriteLine("Failed to change the resolution");
							break;
					}

				}
			}
			else {
				Console.Error.WriteLine("Failed to change the resolution.");
			}

			return false;
		}

		public bool ChangeSettings(Config config) {
			return ChangeSettings(config.Width, config.Height, config.Refresh);
		}

		public List<Config> ListPossibleConfigs() {
			// TODO: filters
			DEVMODE1 dm = GetDevMode1();
			DISPLAY_DEVICE dd = GetDisplayDevice();
			User_32.EnumDisplayDevices(null, Screen - 1, ref dd, 0);
			if (!User_32.EnumDisplaySettings(dd.DeviceName, User_32.ENUM_CURRENT_SETTINGS, ref dm)) {
				Console.Error.WriteLine("Failed to get current settings");
				return null;
			}

			// Collect all options
			DEVMODE1 opt = GetDevMode1();
			var hsOpts = new HashSet<Config>();
			for (int id = 0; User_32.EnumDisplaySettings(dd.DeviceName, id, ref opt); id++) {
				int width = opt.dmPelsWidth,
					height = opt.dmPelsHeight,
					refresh = opt.dmDisplayFrequency;
				var comments = new List<string>();
				
				// TODO: HD/SD/etc notes
				if (
					width == dm.dmPelsWidth
					&& height == dm.dmPelsHeight
					&& refresh == dm.dmDisplayFrequency
				) {
					comments.Add("current");
				}

				var comment = String.Join(", ", comments.ToArray());
				hsOpts.Add(new Config(width, height, refresh, comment));
				opt.dmSize = (short)Marshal.SizeOf(opt);
			}

			// Sort the options
			List<Config> opts = hsOpts.ToList();
			opts.Sort((x, y) => {
				int result = y.Height.CompareTo(x.Height);
				if (result == 0) {
					result = y.Width.CompareTo(x.Width);
					if (result == 0) result = y.Refresh.CompareTo(x.Refresh);
				}
				return result;
			});

			return opts;
		}

		static public List<Device> ListScreens(bool allScreens = false) {
			var output = new List<Device>();
			DISPLAY_DEVICE dd = GetDisplayDevice();
			for (uint id = 0; User_32.EnumDisplayDevices(null, id, ref dd, 0); id++) {
				if (allScreens || dd.StateFlags.HasFlag(DisplayDeviceStateFlags.AttachedToDesktop)) {
					DEVMODE1 dm = GetDevMode1();
					DISPLAY_DEVICE ddm = GetDisplayDevice();

					if (allScreens && !dd.StateFlags.HasFlag(DisplayDeviceStateFlags.AttachedToDesktop)) {
						output.Add(new Device(id+1, dd.DeviceString));
					}
					else if (User_32.EnumDisplayDevices(dd.DeviceName, 0, ref ddm, 0)) {
						output.Add(new Device(id+1, dd.DeviceString, ddm.DeviceString));
					}
					else {
						Console.Error.WriteLine($"Failed to get device info for screen {id+1}");
					}
				}
				dd.cb = Marshal.SizeOf(dd);
			}
			return output;
		}

		private static DEVMODE1 GetDevMode1()
		{
			DEVMODE1 dm = new DEVMODE1();
			dm.dmDeviceName = new String(new char[32]);
			dm.dmFormName = new String(new char[32]);
			dm.dmSize = (short)Marshal.SizeOf(dm);
			return dm;
		}

		private static DISPLAY_DEVICE GetDisplayDevice()
		{
			DISPLAY_DEVICE dd = new DISPLAY_DEVICE();
			dd.cb = Marshal.SizeOf(dd);
			return dd;
		}
	}
}
