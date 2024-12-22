using SoulsFormats;
using System.Numerics;

namespace FromAssimp.Extensions
{
    internal static class BoneExtensions
    {
        internal static Matrix4x4 ComputeLocalTransformScaleless(this FLVER.Node node)
        {
            return Matrix4x4.CreateRotationX(node.Rotation.X)
                * Matrix4x4.CreateRotationZ(node.Rotation.Z)
                * Matrix4x4.CreateRotationY(node.Rotation.Y)
                * Matrix4x4.CreateTranslation(node.Translation);
        }

        internal static Matrix4x4 ComputeLocalTransformScaleless(this MDL4.Node node)
        {
            return Matrix4x4.CreateRotationX(node.Rotation.X)
                * Matrix4x4.CreateRotationZ(node.Rotation.Z)
                * Matrix4x4.CreateRotationY(node.Rotation.Y)
                * Matrix4x4.CreateTranslation(node.Translation);
        }

        internal static Matrix4x4 ComputeLocalTransformScaleless(this SMD4.Node node)
        {
            return Matrix4x4.CreateRotationX(node.Rotation.X)
                * Matrix4x4.CreateRotationZ(node.Rotation.Z)
                * Matrix4x4.CreateRotationY(node.Rotation.Y)
                * Matrix4x4.CreateTranslation(node.Translation);
        }
    }
}
