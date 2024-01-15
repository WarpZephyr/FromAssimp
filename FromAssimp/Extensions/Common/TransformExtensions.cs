using Assimp;
using SoulsFormats;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;

namespace FromAssimp.Extensions.Common
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Compute a world transform starting from a given bone to the root bone.
        /// </summary>
        /// <param name="bone">The bone to start from.</param>
        /// <param name="bones">A list of all bones.</param>
        /// <returns>A transform.</returns>
        /// <exception cref="InvalidDataException">The parent index of a bone was outside of the provided bone array.</exception>
        public static NumericsMatrix4x4 ComputeWorldTransform(this FLVER.Bone bone, IReadOnlyList<FLVER.Bone> bones)
        {
            var transform = bone.ComputeLocalTransform();
            while (bone.ParentIndex != -1)
            {
                if (!(bone.ParentIndex < -1) && !(bone.ParentIndex > bones.Count))
                {
                    bone = bones[bone.ParentIndex];
                    transform *= bone.ComputeLocalTransform();
                }
                else
                {
                    throw new InvalidDataException("Bone has a parent index outside of the provided bone array.");
                }
            }

            return transform;
        }

        /// <summary>
        /// Compute a world transform starting from a given bone to the root bone.
        /// </summary>
        /// <param name="bone">The bone to start from.</param>
        /// <param name="bones">A list of all bones.</param>
        /// <returns>A transform.</returns>
        /// <exception cref="InvalidDataException">The parent index of a bone was outside of the provided bone array.</exception>
        public static NumericsMatrix4x4 ComputeWorldTransform(this MDL4.Bone bone, IReadOnlyList<MDL4.Bone> bones)
        {
            var transform = bone.ComputeLocalTransform();
            while (bone.ParentIndex != -1)
            {
                if (!(bone.ParentIndex < -1) && !(bone.ParentIndex > bones.Count))
                {
                    bone = bones[bone.ParentIndex];
                    transform *= bone.ComputeLocalTransform();
                }
                else
                {
                    throw new InvalidDataException("Bone has a parent index outside of the provided bone array.");
                }
            }

            return transform;
        }

        /// <summary>
        /// Compute a world transform starting from a given bone to the root bone.
        /// </summary>
        /// <param name="bone">The bone to start from.</param>
        /// <param name="bones">A list of all bones.</param>
        /// <returns>A transform.</returns>
        /// <exception cref="InvalidDataException">The parent index of a bone was outside of the provided bone array.</exception>
        public static NumericsMatrix4x4 ComputeWorldTransform(this SMD4.Bone bone, IReadOnlyList<SMD4.Bone> bones)
        {
            var transform = bone.ComputeLocalTransform();
            while (bone.ParentIndex != -1)
            {
                if (!(bone.ParentIndex < -1) && !(bone.ParentIndex > bones.Count))
                {
                    bone = bones[bone.ParentIndex];
                    transform *= bone.ComputeLocalTransform();
                }
                else
                {
                    throw new InvalidDataException("Bone has a parent index outside of the provided bone array.");
                }
            }

            return transform;
        }

        /// <summary>
        /// Compute a world transform starting from a given node to the root node.
        /// </summary>
        /// <param name="node">The node to start from.</param>
        /// <returns>A transform.</returns>
        public static AssimpMatrix4x4 ComputeWorldTransform(this Node node)
        {
            var transform = node.Transform;
            while (node.Parent != null)
            {
                transform *= node.Transform;
                node = node.Parent;
            }

            return transform;
        }
    }
}
