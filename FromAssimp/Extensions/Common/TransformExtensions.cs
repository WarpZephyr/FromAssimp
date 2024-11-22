using Assimp;
using SoulsFormats;
using System.Numerics;
using System.Transactions;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;

namespace FromAssimp.Extensions.Common
{
    public static class TransformExtensions
    {
        static Vector3 Up = new Vector3(0f, 1f, 0f);

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
        /// Compute a world transform starting from a given dummy to the root bone of its parent bone.
        /// </summary>
        /// <param name="dummy">The dummy to start from.</param>
        /// <param name="bones">A list of all bones.</param>
        /// <returns>A transform.</returns>
        public static NumericsMatrix4x4 ComputeWorldTransform(this FLVER.Dummy dummy, IReadOnlyList<FLVER.Bone> bones)
        {
            var transform = dummy.ComputeLocalTransform();
            if (dummy.ParentBoneIndex > -1 && dummy.ParentBoneIndex < bones.Count)
            {
                transform *= bones[dummy.ParentBoneIndex].ComputeWorldTransform(bones);
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
        /// Compute a world transform starting from a given dummy to the root bone of its parent bone.
        /// </summary>
        /// <param name="dummy">The dummy to start from.</param>
        /// <param name="bones">A list of all bones.</param>
        /// <returns>A transform.</returns>
        public static NumericsMatrix4x4 ComputeWorldTransform(this MDL4.Dummy dummy, IReadOnlyList<MDL4.Bone> bones)
        {
            var transform = dummy.ComputeLocalTransform();
            if (dummy.ParentBoneIndex > -1 && dummy.ParentBoneIndex < bones.Count)
            {
                transform *= bones[dummy.ParentBoneIndex].ComputeWorldTransform(bones);
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

        /// <summary>
        /// Creates a transformation matrix from the position, forward, and optionally the upward vector of the dummy as if it were a bone.
        /// </summary>
        public static NumericsMatrix4x4 ComputeLocalTransform(this FLVER.Dummy dummy)
        {
            return NumericsMatrix4x4.CreateScale(Vector3.One) // Scale
                    * NumericsMatrix4x4.CreateLookAt(dummy.Position, dummy.Forward, dummy.UseUpwardVector ? dummy.Upward : Up) // Not sure if this Up Vector is correct
                    * NumericsMatrix4x4.CreateTranslation(dummy.Position);
        }

        /// <summary>
        /// Creates a transformation matrix from the position, and forward vector of the dummy as if it were a bone.
        /// </summary>
        public static NumericsMatrix4x4 ComputeLocalTransform(this MDL4.Dummy dummy)
        {
            return NumericsMatrix4x4.CreateScale(Vector3.One) // Scale
                    * NumericsMatrix4x4.CreateLookAt(dummy.Position, dummy.Forward, Up) // Not sure if this Up Vector is correct
                    * NumericsMatrix4x4.CreateTranslation(dummy.Position);
        }
    }
}
