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
}
