using Assimp;
using FromAssimp.Extensions;
using FromAssimp.Helpers;
using SoulsFormats;
using System.Numerics;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;

namespace FromAssimp.Export
{
    internal static class FlverExporter
    {
        private static List<FLVER.Node> GetNewBoneNodes(List<Node> rootBoneNodes, List<Node> boneNodes)
        {
            FLVER.Node[] newBones = new FLVER.Node[boneNodes.Count];

            int index = 0;
            void AddBone(Node node, int parentIndex, int previousSiblingIndex, bool next)
            {
                var newBone = new FLVER.Node();
                newBone.Name = node.Name;
                var transform = node.Transform.ToNumericsMatrix4x4();
                NumericsMatrix4x4.Decompose(transform, out Vector3 scale, out _, out Vector3 translation);
                newBone.Translation = translation;
                newBone.Rotation = transform.ToEulerXZY();
                newBone.Scale = scale;
                newBone.BoundingBoxMin = BoundsHelper.MaxVector3;
                newBone.BoundingBoxMax = BoundsHelper.MinVector3;
                newBones[index] = newBone;
                int childParentIndex = index;
                index++;

                int childIndex = -1;
                var childNodes = NodeHelper.GetChildBoneNodes(node, boneNodes);
                if (childNodes.Count > 0)
                {
                    childIndex = index;
                    int prev = -1;
                    for (int i = 0; i < childNodes.Count; i++)
                    {
                        int newPrev = index;
                        var child = childNodes[i];
                        AddBone(child, childParentIndex, prev, (i + 1) < childNodes.Count);
                        prev = newPrev;
                    }
                }

                newBone.ParentIndex = (short)parentIndex;
                newBone.FirstChildIndex = (short)childIndex;
                newBone.PreviousSiblingIndex = (short)previousSiblingIndex;
                newBone.NextSiblingIndex = (short)(next ? index : -1);
            }

            int prev = -1;
            for (int i = 0; i < rootBoneNodes.Count; i++)
            {
                int newPrev = index;
                var node = rootBoneNodes[i];
                AddBone(node, -1, prev, (i + 1) < rootBoneNodes.Count);
                prev = newPrev;
            }

            return newBones.ToList();
        }

        private static List<FLVER.Node> BuildSkeleton(Scene scene)
        {
            List<Node> boneNodes = NodeHelper.GetBoneNodes(scene, scene.RootNode);
            List<Node> rootBoneNodes = NodeHelper.GetRootBoneNodes(scene, boneNodes);
            return GetNewBoneNodes(rootBoneNodes, boneNodes);
        }

        public static FLVER0 ExportFlver0(Scene scene)
        {
            // Set here for now
            bool useTriangleStrips = true;
            int version = 0x12;

            var newModel = new FLVER0();
            newModel.Header.Version = version;
            newModel.Header.BigEndian = true;
            newModel.Header.Unicode = false;
            newModel.Header.VertexIndexSize = 16;
            newModel.Header.Unk4A = 1;
            newModel.Nodes = BuildSkeleton(scene);

            bool forceTriangleStrips = newModel.Header.Version < 0x15;

            // Get the inverse full bone transforms
            // These will be the inverse of a full world transform for every bone
            // These will be used to transform vertices into an inverse bind pose if we aren't using bone indices and/or bone weights
            NumericsMatrix4x4[] inverseTransforms = new NumericsMatrix4x4[newModel.Nodes.Count];
            void GetTransforms(int index, NumericsMatrix4x4 parentTransform)
            {
                if (index > -1)
                {
                    var bone = newModel.Nodes[index];
                    var localTransform = bone.ComputeLocalTransform();
                    var transform = localTransform * parentTransform; // Get world
                    NumericsMatrix4x4.Invert(transform, out NumericsMatrix4x4 inverseTransform);
                    inverseTransforms[index] = inverseTransform; // Store inverse world

                    // Move onto this bone's next sibling
                    GetTransforms(bone.NextSiblingIndex, parentTransform);

                    // Move onto this bone's children
                    GetTransforms(bone.FirstChildIndex, transform);
                }
            }

            // Go from the top of the hiearchy down, enabling getting world transforms for every bone only once
            for (int boneIndex = 0; boneIndex < newModel.Nodes.Count; boneIndex++)
            {
                var bone = newModel.Nodes[boneIndex];
                if (bone.ParentIndex != -1) // Skip bones that have parents
                    continue;

                var transform = bone.ComputeLocalTransform();
                NumericsMatrix4x4.Invert(transform, out NumericsMatrix4x4 inverseTransform);
                inverseTransforms[boneIndex] = inverseTransform; // Store inverse world

                // Move onto this bone's children
                GetTransforms(bone.FirstChildIndex, transform);
            }

            foreach (var material in scene.Materials)
            {
                var newMaterial = new FLVER0.Material();
                newMaterial.Name = material.Name;

                var properties = material.GetAllProperties();
                var textureProperties = properties.Where(p => p.TextureType != TextureType.None).OrderBy(tp => tp.TextureIndex);
                foreach (var property in textureProperties)
                {
                    var newTexture = new FLVER0.Texture();
                    newTexture.Path = property.Name;
                    newTexture.Type = $"{property.TextureType}";
                    newMaterial.Textures.Add(newTexture);
                }

                newModel.Materials.Add(newMaterial);
            }

            bool setPrimitiveRestartConstant = false;
            Span<int> boneIndicesAlloc = stackalloc int[4];
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;
            foreach (var mesh in scene.Meshes)
            {
                var assimpMesh = new AssimpMesh(mesh, forceTriangleStrips || useTriangleStrips);
                var newMesh = new FLVER0.Mesh();

                var material = scene.Materials[assimpMesh.MaterialIndex];
                newMesh.MaterialIndex = (byte)assimpMesh.MaterialIndex;
                newMesh.TriangleStrip = assimpMesh.TriangleStrip;
                newMesh.CullBackfaces = !material.IsTwoSided;
                newMesh.Indices = new List<int>(assimpMesh.Indices);
                setPrimitiveRestartConstant |= newMesh.TriangleStrip;

                // Versions older than 0x15 do not appear to set this, even though its true.
                newMesh.TriangleStrip = !forceTriangleStrips && newMesh.TriangleStrip;

                int boneCount = Math.Min(assimpMesh.Bones.Count, 28);
                for (int meshBoneIndex = 0; meshBoneIndex < boneCount; meshBoneIndex++)
                {
                    var boneName = assimpMesh.Bones[meshBoneIndex];
                    for (int boneIndex = 0; boneIndex < newModel.Nodes.Count; boneIndex++)
                    {
                        var newBone = newModel.Nodes[boneIndex];
                        if (newBone.Name == boneName)
                        {
                            newMesh.BoneIndices[meshBoneIndex] = (short)boneIndex;
                            break;
                        }
                    }
                }

                // Build layout
                var newLayout = new FLVER0.BufferLayout();
                var positionMember = new FLVER.LayoutMember(FLVER.LayoutType.Float3, FLVER.LayoutSemantic.Position, 0, 0);
                newLayout.Add(positionMember);

                if (assimpMesh.HasNormals)
                {
                    var member = new FLVER.LayoutMember(FLVER.LayoutType.Byte4A, FLVER.LayoutSemantic.Normal, 0, 0);
                    newLayout.Add(member);
                }

                if (assimpMesh.HasTangents)
                {
                    var member = new FLVER.LayoutMember(FLVER.LayoutType.Byte4A, FLVER.LayoutSemantic.Tangent, 0, 0);
                    newLayout.Add(member);
                }

                if (assimpMesh.HasBiTangents)
                {
                    var member = new FLVER.LayoutMember(FLVER.LayoutType.Byte4A, FLVER.LayoutSemantic.Bitangent, 0, 0);
                    newLayout.Add(member);
                }

                int memberIndex = 0;
                if (assimpMesh.HasColors)
                {
                    for (int i = 0; i < assimpMesh.ColorCount; i++)
                    {
                        var member = new FLVER.LayoutMember(FLVER.LayoutType.Byte4A, FLVER.LayoutSemantic.VertexColor, memberIndex++, 0);
                        newLayout.Add(member);
                    }
                }

                memberIndex = 0;
                if (assimpMesh.HasUVs)
                {
                    for (int i = 0; i < assimpMesh.UVCount; i++)
                    {
                        if (assimpMesh.UVComponentCount < 3)
                        {
                            var member = new FLVER.LayoutMember(FLVER.LayoutType.Float2, FLVER.LayoutSemantic.UV, memberIndex++, 0);
                            newLayout.Add(member);
                        }
                        else
                        {
                            var member = new FLVER.LayoutMember(FLVER.LayoutType.Float3, FLVER.LayoutSemantic.UV, memberIndex++, 0);
                            newLayout.Add(member);
                        }
                    }
                }

                bool useNormalW = false;
                bool useBoneIndices = false;
                if (assimpMesh.HasBoneIndices)
                {
                    if (assimpMesh.VertexBoneCount > 1)
                    {
                        var member = new FLVER.LayoutMember(FLVER.LayoutType.Byte4B, FLVER.LayoutSemantic.BoneIndices, 0, 0);
                        newLayout.Add(member);
                        useBoneIndices = true;
                    }
                    else
                    {
                        useNormalW = true;
                    }
                }

                bool useBoneWeights = false;
                if (assimpMesh.HasBoneWeights)
                {
                    if (assimpMesh.VertexBoneCount > 1)
                    {
                        var member = new FLVER.LayoutMember(FLVER.LayoutType.Byte4A, FLVER.LayoutSemantic.BoneWeights, 0, 0);
                        newLayout.Add(member);
                        useBoneWeights = true;
                    }
                }

                var newMaterial = newModel.Materials[newMesh.MaterialIndex];
                int layoutIndex = newMaterial.Layouts.IndexOf(newLayout);
                if (layoutIndex == -1)
                {
                    layoutIndex = newMaterial.Layouts.Count;
                    newMaterial.Layouts.Add(newLayout);
                }

                newMesh.LayoutIndex = layoutIndex;

                bool invertTransforms = !(useBoneIndices || useBoneWeights);
                newMesh.Dynamic = (byte)(!invertTransforms ? 1 : 0);

                // Build vertices
                foreach (var vertex in assimpMesh.Vertices)
                {
                    var newVertex = new FLVER.Vertex();
                    newVertex.Position = vertex.Position.ToNumericsVector3();

                    if (assimpMesh.HasNormals)
                        newVertex.Normal = vertex.Normal.ToNumericsVector3();

                    if (assimpMesh.HasTangents)
                        newVertex.Tangents.Add(vertex.Tangent.ToNumericsVector4());

                    if (assimpMesh.HasBiTangents)
                        newVertex.Bitangent = vertex.BiTangent.ToNumericsVector4();

                    if (assimpMesh.HasColors)
                        foreach (var color in vertex.Colors)
                            newVertex.Colors.Add(color.ToFlverVertexColor());

                    if (assimpMesh.HasUVs)
                    {
                        foreach (var uv in vertex.UVs)
                        {
                            var u = uv.X;
                            var v = 1 - uv.Y; // Flip UV
                            var w = uv.Z;
                            newVertex.UVs.Add(new Vector3(u, v, w));
                        }
                    }

                    if (useNormalW)
                    {
                        newVertex.NormalW = vertex.BoneIndices[0];
                        boneIndicesAlloc[0] = newMesh.BoneIndices[newVertex.NormalW];
                        boneIndicesAlloc[1] = -1;
                        boneIndicesAlloc[2] = -1;
                        boneIndicesAlloc[3] = -1;
                    }
                    else if (useBoneIndices)
                    {
                        boneIndicesAlloc[0] = -1;
                        boneIndicesAlloc[1] = -1;
                        boneIndicesAlloc[2] = -1;
                        boneIndicesAlloc[3] = -1;

                        int count = int.Min(vertex.BoneIndices.Count, 4);
                        for (int i = 0; i < count; i++)
                        {
                            newVertex.BoneIndices[i] = vertex.BoneIndices[i];
                            boneIndicesAlloc[i] = newMesh.BoneIndices[newVertex.BoneIndices[i]];
                        }
                    }

                    if (useBoneWeights)
                    {
                        int count = int.Min(vertex.BoneWeights.Count, 4);
                        for (int i = 0; i < count; i++)
                        {
                            newVertex.BoneWeights[i] = vertex.BoneWeights[i];
                        }
                    }

                    // Header bounds are for fully transformed vertices
                    // So get them before inverting
                    BoundsHelper.GetMin(ref minX, ref minY, ref minZ, newVertex.Position);
                    BoundsHelper.GetMax(ref maxX, ref maxY, ref maxZ, newVertex.Position);

                    if (invertTransforms)
                    {
                        int boneIndex = boneIndicesAlloc[0];
                        var inverseTransform = inverseTransforms[boneIndex];
                        newVertex.Position = Vector3.Transform(newVertex.Position, inverseTransform);

                        if (assimpMesh.HasNormals)
                            newVertex.Normal = Vector3.Normalize(Vector3.TransformNormal(newVertex.Normal, inverseTransform));

                        if (assimpMesh.HasTangents)
                            for (int i = 0; i < newVertex.Tangents.Count; i++)
                                newVertex.Tangents[i] = new Vector4(Vector3.Normalize(Vector3.TransformNormal(newVertex.Tangents[i].ToNumericsVector3(), inverseTransform)), newVertex.Tangents[i].W);

                        if (assimpMesh.HasBiTangents)
                            newVertex.Bitangent = new Vector4(Vector3.Normalize(Vector3.TransformNormal(newVertex.Bitangent.ToNumericsVector3(), inverseTransform)), newVertex.Bitangent.W);
                    }

                    foreach (var boneIndex in boneIndicesAlloc)
                    {
                        if (boneIndex == -1)
                            continue;

                        var bone = newModel.Nodes[boneIndex];
                        bone.BoundingBoxMin = BoundsHelper.GetMin(bone.BoundingBoxMin, newVertex.Position);
                        bone.BoundingBoxMax = BoundsHelper.GetMax(bone.BoundingBoxMax, newVertex.Position);
                        newModel.Nodes[boneIndex] = bone;
                    }

                    newMesh.Vertices.Add(newVertex);
                }

                newModel.Meshes.Add(newMesh);
            }

            // Set primitive restart constant
            // Versions older than 0x15 appear to always use triangle strips
            if (forceTriangleStrips || setPrimitiveRestartConstant)
                newModel.Header.Unk4C = IndexHelper.DefaultPrimitiveRestart;

            // Setup and correct bounds
            foreach (var bone in newModel.Nodes)
            {
                BoundsHelper.CorrectBounds(bone.BoundingBoxMin, true);
                BoundsHelper.CorrectBounds(bone.BoundingBoxMax, false);
            }

            newModel.Header.BoundingBoxMin = new Vector3(minX, minY, minZ);
            newModel.Header.BoundingBoxMax = new Vector3(maxX, maxY, maxZ);

            BoundsHelper.CorrectBounds(newModel.Header.BoundingBoxMin, true);
            BoundsHelper.CorrectBounds(newModel.Header.BoundingBoxMax, false);

            return newModel;
        }
    }
}
