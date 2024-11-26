using Assimp;

namespace FromAssimp.Helpers
{
    internal static class NodeHelper
    {
        public static Node? FindBoneNode(Node node, Bone bone)
        {
            if (node.Name == bone.Name)
                return node;

            foreach (var child in node.Children)
            {
                var childSearch = FindBoneNode(child, bone);
                if (childSearch != null)
                    return childSearch;
            }

            return null;
        }

        public static bool IsBoneNode(Scene scene, Node node)
        {
            foreach (var mesh in scene.Meshes)
            {
                foreach (var bone in mesh.Bones)
                {
                    if (bone.Name == node.Name)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static List<Node> GetBoneNodes(Scene scene, Node rootNode)
        {
            List<Node> boneNodes = new List<Node>();
            HashSet<string> addedBones = new HashSet<string>();
            foreach (var mesh in scene.Meshes)
            {
                foreach (var bone in mesh.Bones)
                {
                    string name = bone.Name;
                    if (addedBones.Contains(name))
                        continue;

                    Node? boneNode = FindBoneNode(rootNode, bone);
                    if (boneNode != null)
                    {
                        boneNodes.Add(boneNode);
                        addedBones.Add(name);
                    }
                }
            }

            return boneNodes;
        }

        public static List<Node> GetRootBoneNodes(Scene scene, List<Node> boneNodes)
        {
            List<Node> rootBoneNodes = new List<Node>();
            foreach (var boneNode in boneNodes)
            {
                if (boneNode.Parent == null || !IsBoneNode(scene, boneNode.Parent))
                {
                    rootBoneNodes.Add(boneNode);
                }
            }
            return rootBoneNodes;
        }

        public static List<Node> GetChildBoneNodes(Node node, List<Node> boneNodes)
        {
            List<Node> childNodes = new List<Node>();
            if (node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    if (boneNodes.Contains(child))
                    {
                        childNodes.Add(child);
                    }
                }
            }

            return childNodes;
        }
    }
}
