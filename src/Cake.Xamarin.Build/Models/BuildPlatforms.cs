using Cake.Common;
using Cake.Core;
using System;

namespace Cake.Xamarin.Build
{
    [Flags]
    public enum BuildPlatforms
    {
        Mac = 1,
        Windows = 2,
        Linux = 4
    }

    public static class BuildPlatformUtil
    {
        public static bool BuildsOnCurrentPlatform (ICakeContext cake, BuildPlatforms eligiblePlatforms)
        {
            if (cake.IsRunningOnUnix() &&
                ((eligiblePlatforms & BuildPlatforms.Linux) != 0 || (eligiblePlatforms & BuildPlatforms.Mac) != 0))
                return true;

            if (cake.IsRunningOnWindows() &&
            (eligiblePlatforms & BuildPlatforms.Windows) != 0)
                return true;

            return false;
        }
    }
}
