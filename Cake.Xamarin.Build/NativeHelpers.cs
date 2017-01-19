using System;
using System.Runtime.InteropServices;

namespace Cake.Xamarin.Build
{
	public static class NativeHelpers
	{
		[DllImport("libc")]
		static extern int uname(IntPtr buf);

		public static bool IsRunningOnMac()
		{
			IntPtr buf = IntPtr.Zero;
			try
			{
				buf = Marshal.AllocHGlobal(8192);
				if (uname(buf) == 0)
				{
					string os = Marshal.PtrToStringAnsi(buf);
					if (os == "Darwin")
					{
						return true;
					}
				}
			}
			finally
			{
				if (buf != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(buf);
				}
			}

			return false;
		}
	}
}
