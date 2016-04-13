using Cake.Core;

namespace Cake.Xamarin.Build
{
    public interface ISolutionBuilder
    {
        void BuildSolution ();
        void CopyOutput ();

        ICakeContext CakeContext { get; }
        BuildSpec BuildSpec { get; }

        void Init(ICakeContext context, BuildSpec buildSpec);
    }
}
