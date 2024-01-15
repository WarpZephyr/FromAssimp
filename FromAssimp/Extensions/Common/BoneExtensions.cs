using Assimp;
using SoulsFormats;

namespace FromAssimp.Extensions.Common
{
    internal static class BoneExtensions
    {
        internal static bool HasBones(this FLVER0.Mesh mesh)
        {
            for (int i = 0; i < 28; i++)
            {
                if (mesh.BoneIndices[i] != -1)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool HasBones(this FLVER2.Mesh mesh)
        {
            return mesh.BoneIndices.Count > 0;
        }

        internal static bool HasBones(this IFlverMesh mesh)
        {
            if (mesh is FLVER0.Mesh flver0)
            {
                return HasBones(flver0);
            }
            else if (mesh is FLVER2.Mesh flver2)
            {
                return HasBones(flver2);
            }
            else
            {
                throw new NotSupportedException("The underlying type of the interface was not supported.");
            }
        }

        internal static bool HasBones(this MDL4.Mesh mesh)
        {
            for (int i = 0; i < 28; i++)
            {
                if (mesh.BoneIndices[i] != -1)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool HasBones(this SMD4.Mesh mesh)
        {
            for (int i = 0; i < 28; i++)
            {
                if (mesh.BoneIndices[i] != -1)
                {
                    return true;
                }
            }
            return false;
        }

        internal static Bone[] GetLocalBones(this List<short> boneIndices, int usedCount, Bone[] bones)
        {
            Bone[] localBones = new Bone[usedCount];
            for (int i = 0; i < usedCount; i++)
                localBones[i] = bones[boneIndices[i]];
            return localBones;
        }

        internal static Bone[] GetLocalBones(this List<int> boneIndices, Bone[] bones)
        {
            int count = boneIndices.Count;
            Bone[] localBones = new Bone[count];
            for (int i = 0; i < count; i++)
                localBones[i] = bones[boneIndices[i]];
            return localBones;
        }
    }
}
