using Assimp;
using FromAssimp.Extensions.Assimp;
using SoulsFormats;
using static FromAssimp.Export.ExportHelper;

namespace FromAssimp.Export
{
    /// <summary>
    /// Currently very incomplete.
    /// </summary>
    public static class FlverExport
    {
        internal static short CollectChildBones(Node boneNode, short previousSiblingIndex, List<Node> boneNodes, List<FLVER.Bone> finalBones)
        {
            short parentIndex = (short)(finalBones.Count - 1);
            short currentIndex = (short)finalBones.Count;
            short nextSiblingIndex = -1;
            short childIndex = -1;
            
            List<Node> childBoneNodes = CollectChildBoneNodes(boneNode, boneNodes);
            if (childBoneNodes.Count > 0)
            {
                childIndex = (short)(currentIndex + 1);
                short startChildIndex = childIndex;

                for (int i = 0; i < childBoneNodes.Count; i++)
                {
                    Node childBoneNode = childBoneNodes[i];

                    short childPreviousSiblingIndex = -1;
                    short currentChildIndex = (short)(startChildIndex + i);
                    if (currentChildIndex != startChildIndex)
                        childPreviousSiblingIndex = (short)(currentChildIndex - 1);

                    nextSiblingIndex = CollectChildBones(childBoneNode, childPreviousSiblingIndex, boneNodes, finalBones);
                }
            }

            FLVER.Bone newBone = boneNode.ToFlverBone();
            newBone.ParentIndex = parentIndex;
            newBone.ChildIndex = childIndex;
            newBone.NextSiblingIndex = nextSiblingIndex;
            newBone.PreviousSiblingIndex = previousSiblingIndex;

            finalBones.Add(newBone);
            return (short)(currentIndex + 1);
        }

        internal static List<FLVER.Bone> BuildSkeleton(Scene scene)
        {
            List<FLVER.Bone> newBones = new List<FLVER.Bone>();
            List<Node> boneNodes = CollectSceneBones(scene);
            List<Node> rootBoneNodes = CollectRootBoneNodes(boneNodes);

            // Attach root bone nodes to a single hierarchy
            short startIndex = 0;
            for (int i = 0; i < rootBoneNodes.Count; i++)
            {
                Node rootBoneNode = rootBoneNodes[i];

                short previousSiblingIndex = -1;
                short currentIndex = (short)(startIndex + i);
                if (currentIndex != startIndex)
                    previousSiblingIndex = (short)(currentIndex - 1);

                short nextSiblingIndex = CollectChildBones(rootBoneNode, previousSiblingIndex, boneNodes, newBones);
                newBones[currentIndex].NextSiblingIndex = nextSiblingIndex;
            }

            return newBones;
        }

        public static FLVER0 ToFlver0(Scene scene)
        {            
            FLVER0 model = new FLVER0();
            model.Bones = BuildSkeleton(scene);

            return model;
        }
    }
}
