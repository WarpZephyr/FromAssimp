using Assimp;
using FromAssimp.Extensions.Numerics;
using SoulsFormats;
using System.Numerics;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using NumericsQuaternion = System.Numerics.Quaternion;

namespace FromAssimp.Extensions.Assimp
{
    public static class NodeExtensions
    {
        public static FLVER.Bone ToFlverBone(this Node node)
        {
            FLVER.Bone bone = new FLVER.Bone();
            var transform = node.Transform.ToNumericsMatrix4x4();
            NumericsMatrix4x4.Decompose(transform, out Vector3 scale, out NumericsQuaternion qRotation, out Vector3 translation);
            bone.Translation = translation;
            bone.Rotation = transform.ToEulerXZY();
            bone.Scale = scale;
            bone.Name = node.Name;
            bone.BoundingBoxMin = new Vector3(float.MinValue);
            bone.BoundingBoxMax = new Vector3(float.MaxValue);
            return bone;
        }
    }
}
