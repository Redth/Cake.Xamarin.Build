using Cake.Core.Annotations;
using Cake.Core;
using Cake.Core.Scripting;

namespace Cake.Xamarin.Build
{
    /// <summary>
    /// Xamarin Build Task aliases.
    /// </summary>
    [CakeNamespaceImport("Cake.Xamarin.Build")]
    [CakeAliasCategory("Xamarin Build Tasks")]
    public static class XamarinBuildTasksAliases
    {
        /// <summary>
        /// Creates and/or sets up Task dependency chain for Xamarin related build tasks
        /// </summary>
        /// <param name="context">The context.</param>
        [CakeMethodAlias]
        public static void SetupXamarinBuildTasks (this ICakeContext context, IScriptHost scriptHost)
        {
            XamarinBuildTasks.SetupXamarinBuildTasks (scriptHost);
        }
    }
}

