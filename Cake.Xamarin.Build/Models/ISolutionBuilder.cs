using Cake.Core;

namespace Cake.Xamarin.Build
{
    public interface ISolutionBuilder
    {
        void BuildSolution ();
        void CopyOutput ();

        ICakeContext CakeContext { get; }
    }

}
