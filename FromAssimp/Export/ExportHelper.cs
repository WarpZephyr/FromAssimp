using Assimp;

namespace FromAssimp.Export
{
    internal static class ExportHelper
    {
        internal static List<Node> CollectSceneBones(Scene scene)
        {
            List<Node> boneNodes = new List<Node>();
            foreach (Mesh mesh in scene.Meshes)
            {
                foreach (Bone bone in mesh.Bones)
                {
                    Node boneNode = scene.RootNode.FindNode(bone.Name);
                    if (!boneNodes.Contains(boneNode))
                    {
                        boneNodes.Add(boneNode);
                    }
                }
            }
            return boneNodes;
        }

        internal static List<Node> CollectRootBoneNodes(List<Node> boneNodes)
        {
            List<Node> rootBoneNodes = new List<Node>();
            foreach (Node boneNode in boneNodes)
            {
                if (boneNode.Parent == null)
                {
                    rootBoneNodes.Add(boneNode);
                }
            }
            return rootBoneNodes;
        }

        internal static List<Node> CollectChildBoneNodes(Node boneNode, List<Node> boneNodes)
        {
            List<Node> childBoneNodes = new List<Node>();
            foreach (Node childNode in boneNode.Children)
            {
                if (!boneNodes.Contains(childNode))
                {
                    continue;
                }

                childBoneNodes.Add(childNode);
            }
            return childBoneNodes;
        }
    }
}
