﻿using Assimp;
using FromAssimp.Extensions.Numerics;
using SoulsFormats;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using FromAssimp.Extensions.Common;
using System.Numerics;

namespace FromAssimp
{
    internal static class FlverImporter
    {
        public static Scene ImportFlver0(FLVER0 model, bool doCheckFlip)
        {
            var scene = new Scene();
            var rootNode = new Node("Root");
            scene.RootNode = rootNode;

            // Add materials
            foreach (var material in model.Materials)
            {
                var newMaterial = new Material();
                newMaterial.Name = material.Name;
                scene.Materials.Add(newMaterial);
            }

            // Add bone nodes
            // In Assimp every bone needs a node with the same name
            // To have bones transform properly you need to set the transform of the bone relative to its parent in the node's Transform.
            // This is also known as the local transform.
            var boneRootNode = rootNode;
            Node[] newBoneNodes = new Node[model.Bones.Count];
            NumericsMatrix4x4[] localTransforms = new NumericsMatrix4x4[model.Bones.Count];
            for (int boneIndex = 0; boneIndex < model.Bones.Count; boneIndex++)
            {
                var bone = model.Bones[boneIndex];
                var localTransform = bone.ComputeLocalTransform();
                Node parentNode = bone.ParentIndex > -1 ? newBoneNodes[bone.ParentIndex] : boneRootNode;

                localTransforms[boneIndex] = localTransform;
                var newBoneNode = new Node(bone.Name, parentNode);
                newBoneNode.Transform = localTransform.ToAssimpMatrix4x4();
                newBoneNodes[boneIndex] = newBoneNode;
                parentNode.Children.Add(newBoneNode);
            }

            // Get full bone transforms
            // These will be a full world transform for every bone
            // These will be used to transform vertices that aren't already in bind pose,
            // As well as be used in Assimp Bone OffsetMatrix values.
            NumericsMatrix4x4[] transforms = new NumericsMatrix4x4[model.Bones.Count];
            void GetTransforms(int index, NumericsMatrix4x4 parentTransform)
            {
                if (index > -1)
                {
                    var bone = model.Bones[index];
                    var localTransform = localTransforms[index];
                    var transform = localTransform * parentTransform; // Get world
                    transforms[index] = transform; // Store world

                    // Move onto this bone's next sibling
                    GetTransforms(bone.NextSiblingIndex, parentTransform);

                    // Move onto this bone's children
                    GetTransforms(bone.ChildIndex, transform);
                }
            }

            // Go from the top of the hiearchy down, enabling getting world transforms for every bone only once
            for (int boneIndex = 0; boneIndex < model.Bones.Count; boneIndex++)
            {
                var bone = model.Bones[boneIndex];
                if (bone.ParentIndex != -1) // Skip bones that have parents
                    continue;

                var transform = localTransforms[boneIndex];
                transforms[boneIndex] = transform; // Store world

                // Move onto this bone's children
                GetTransforms(bone.ChildIndex, transform);
            }

            // Get inverse transforms
            // Used for the offset matrix in bones stored in meshes for assimp
            // Assimp stores bone references per mesh, along with the weights of vertices this bone affects in that mesh
            // This means you will need to duplicate bone references per mesh, because they cannot share the same vertex IDs
            AssimpMatrix4x4[] offsetTransforms = new AssimpMatrix4x4[model.Bones.Count];
            for (int boneIndex = 0; boneIndex < model.Bones.Count; boneIndex++)
            {
                var transform = transforms[boneIndex];
                NumericsMatrix4x4.Invert(transform, out NumericsMatrix4x4 inverseTransform);
                offsetTransforms[boneIndex] = inverseTransform.ToAssimpMatrix4x4();
            }

            // Allocate for indices and weights to be used
            Span<int> boneIndicesAlloc = stackalloc int[4];
            Span<float> boneWeightsAlloc = stackalloc float[4];

            // Store what bones are referenced for later
            // This is so we know which ones aren't referenced and can make a decision on whether or not to export them
            HashSet<int> referencedBoneIndices = [];

            // Add meshes
            var meshRootNode = rootNode;
            for (int meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
            {
                var mesh = model.Meshes[meshIndex];
                var parentNode = meshRootNode;
                var newMeshNode = new Node($"MeshNode_{meshIndex}", parentNode);
                var newMesh = new Mesh($"Mesh_{meshIndex}", PrimitiveType.Triangle);
                newMesh.MaterialIndex = mesh.MaterialIndex;

                // Add faces
                List<int> faces = mesh.Triangulate(model.Header.Version, doCheckFlip, true);
                for (int faceIndex = 0; faceIndex < faces.Count - 2; faceIndex += 3)
                {
                    newMesh.Faces.Add(new Face([faces[faceIndex + 2], faces[faceIndex + 1], faces[faceIndex]]));
                }

                // Get info
                // This is so we know what to export for each vertex ahead of time
                var layout = model.Materials[mesh.MaterialIndex].Layouts[mesh.LayoutIndex];
                bool isStatic = mesh.Dynamic == 0;
                bool hasPosition = false;
                bool hasNormal = false;
                bool usesNormalW = false;
                bool hasTangents = false;
                bool hasBitangent = false;
                bool hasUVs = false;
                bool hasUVW = false;
                bool hasColors = false;
                bool hasBoneIndices = false;
                bool hasBoneWeights = false;
                bool hasDefaultBoneIndex = mesh.DefaultBoneIndex > -1 && mesh.DefaultBoneIndex < model.Bones.Count;
                int tangentCount = 0; // Not used at the moment
                int uvCount = 0; // Not used at the moment
                int colorCount = 0; // Not used at the moment
                var meshBoneIndices = mesh.BoneIndices;
                var defaultBoneIndex = mesh.DefaultBoneIndex;
                foreach (var member in layout)
                {
                    switch (member.Semantic)
                    {
                        case FLVER.LayoutSemantic.Position:
                            hasPosition = true;
                            break;
                        case FLVER.LayoutSemantic.BoneWeights:
                            hasBoneWeights = true;
                            break;
                        case FLVER.LayoutSemantic.BoneIndices:
                            hasBoneIndices = true;
                            break;
                        case FLVER.LayoutSemantic.Normal:
                            hasNormal = true;
                            if (member.Type == FLVER.LayoutType.Byte4A ||
                                member.Type == FLVER.LayoutType.Byte4B ||
                                member.Type == FLVER.LayoutType.Byte4E ||
                                member.Type == FLVER.LayoutType.Short2toFloat2)
                            {
                                usesNormalW = true; // TODO: Investigate this more
                            }
                            break;
                        case FLVER.LayoutSemantic.UV:
                            hasUVs = true;
                            if (member.Type == FLVER.LayoutType.Short4toFloat4B)
                            {
                                hasUVW = true;
                            }

                            uvCount++;
                            break;
                        case FLVER.LayoutSemantic.Tangent:
                            hasTangents = true;
                            tangentCount++;
                            break;
                        case FLVER.LayoutSemantic.Bitangent:
                            hasBitangent = true;
                            break;
                        case FLVER.LayoutSemantic.VertexColor:
                            hasColors = true;
                            colorCount++;
                            break;
                    }
                }

                // Reserve bone map for vertex weights
                Dictionary<int, Bone> meshBoneMap = new Dictionary<int, Bone>(meshBoneIndices.Length);

                // Add vertices
                bool doTransform = isStatic;
                bool doNormalWTransform = usesNormalW;
                bool doIndexNoWeightTransform = !doNormalWTransform && hasBoneWeights && !hasBoneIndices;
                bool doDefaultTransform = !doNormalWTransform & hasDefaultBoneIndex;
                for (int vertexIndex = 0; vertexIndex < mesh.Vertices.Count; vertexIndex++)
                {
                    // Gather elements
                    var vertex = mesh.Vertices[vertexIndex];
                    var pos = vertex.Position;
                    var normal = vertex.Normal;
                    var normalW = vertex.NormalW;
                    var tangents = vertex.Tangents;
                    var bitangent = vertex.Bitangent;
                    var uvs = vertex.UVs;
                    var colors = vertex.Colors;
                    var boneIndices = vertex.BoneIndices;
                    var boneWeights = vertex.BoneWeights;
                    bool validNormalW = usesNormalW &&
                        normalW > -1 && normalW < meshBoneIndices.Length && // Make sure mesh bone index is valid
                        meshBoneIndices[normalW] > -1 && meshBoneIndices[normalW] < model.Bones.Count; // Make sure bone index is valid

                    // Transform elements
                    if (doTransform)
                    {
                        int boneTransformIndex = -1;
                        if (doNormalWTransform && validNormalW)
                        {
                            boneTransformIndex = meshBoneIndices[normalW];
                        }
                        else if (doIndexNoWeightTransform &&
                            boneIndices[0] > -1 && boneIndices[0] < meshBoneIndices.Length && // Make sure mesh bone index is valid
                            meshBoneIndices[boneIndices[0]] > -1 && meshBoneIndices[boneIndices[0]] < model.Bones.Count) // Make sure bone index is valid
                        {
                            boneTransformIndex = meshBoneIndices[boneIndices[0]];
                        }
                        else if (doDefaultTransform)
                        {
                            boneTransformIndex = defaultBoneIndex;
                        }

                        // Bone index found and valid
                        if (boneTransformIndex > -1)
                        {
                            var transform = transforms[boneTransformIndex];
                            if (hasPosition)
                                pos = Vector3.Transform(pos, transform);

                            if (hasNormal)
                                normal = Vector3.Normalize(Vector3.TransformNormal(normal, transform));

                            if (hasTangents)
                                for (int tangentIndex = 0; tangentIndex < tangents.Count; tangentIndex++)
                                    tangents[tangentIndex] = new Vector4(Vector3.Normalize(Vector3.TransformNormal(tangents[tangentIndex].ToNumericsVector3(), transform)), tangents[tangentIndex].W);

                            if (hasBitangent)
                                bitangent = new Vector4(Vector3.Normalize(Vector3.TransformNormal(bitangent.ToNumericsVector3(), transform)), bitangent.W);
                        }
                    }

                    // Add elements
                    if (hasPosition)
                        newMesh.Vertices.Add(pos.ToAssimpVector3D());

                    if (hasNormal)
                        newMesh.Normals.Add(normal.ToAssimpVector3D());

                    if (hasTangents && tangents.Count > 0)
                        newMesh.Tangents.Add(tangents[0].ToAssimpVector3D());

                    if (hasBitangent)
                        newMesh.BiTangents.Add(bitangent.ToAssimpVector3D());

                    if (hasUVs)
                    {
                        int loopCount = Math.Min(vertex.UVs.Count, 8);
                        for (int i = 0; i < loopCount; i++)
                        {
                            var uv = vertex.UVs[i];

                            if (hasUVW)
                            {
                                newMesh.TextureCoordinateChannels[i].Add(new Vector3D(uv.X, 1 - uv.Y, uv.Z)); // TODO: Investigate if this is correct
                                newMesh.UVComponentCount[i] = 3;
                            }
                            else
                            {
                                newMesh.TextureCoordinateChannels[i].Add(new Vector3D(uv.X, 1 - uv.Y, 0f));
                                newMesh.UVComponentCount[i] = 2;
                            }
                        }
                    }

                    if (hasColors)
                    {
                        int loopCount = vertex.Colors.Count;
                        for (int i = 0; i < 2; i++)
                        {
                            if (i < loopCount)
                            {
                                var color = vertex.Colors[i];
                                newMesh.VertexColorChannels[i].Add(new Color4D(color.R, color.G, color.B, color.A));
                            }
                        }
                    }

                    // Setup bone indices and bone weights
                    boneIndicesAlloc[0] = -1;
                    boneIndicesAlloc[1] = -1;
                    boneIndicesAlloc[2] = -1;
                    boneIndicesAlloc[3] = -1;
                    boneWeightsAlloc[0] = 0f;
                    boneWeightsAlloc[1] = 0f;
                    boneWeightsAlloc[2] = 0f;
                    boneWeightsAlloc[3] = 0f;
                    if (hasBoneIndices)
                    {
                        var lastMeshBoneIndex = -1;
                        int boneCount = 0;

                        // Keep adding bone indices until we hit bone indices that go back
                        // TODO: This might be invalid, I suppose bone weights being 0 will invalidate unused extra indices anyways
                        for (int i = 0; i < 4; i++)
                        {
                            var meshBoneIndex = boneIndices[i];
                            if (meshBoneIndex > -1 && meshBoneIndex < meshBoneIndices.Length &&
                                meshBoneIndex > lastMeshBoneIndex &&
                                meshBoneIndices[meshBoneIndex] > -1 && meshBoneIndices[meshBoneIndex] < model.Bones.Count)
                            {
                                boneCount++;
                                lastMeshBoneIndex = meshBoneIndex;
                                boneIndicesAlloc[i] = meshBoneIndices[meshBoneIndex];
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (hasBoneWeights)
                        {
                            for (int i = 0; i < boneCount; i++)
                            {
                                boneWeightsAlloc[i] = boneWeights[i];
                            }
                        }
                        else
                        {
                            boneWeightsAlloc[0] = 1f;
                            boneWeightsAlloc[1] = 0f;
                            boneWeightsAlloc[2] = 0f;
                            boneWeightsAlloc[3] = 0f;
                        }
                    }
                    else if (validNormalW)
                    {
                        boneIndicesAlloc[0] = meshBoneIndices[normalW];
                        boneIndicesAlloc[1] = -1;
                        boneIndicesAlloc[2] = -1;
                        boneIndicesAlloc[3] = -1;
                        boneWeightsAlloc[0] = 1f;
                        boneWeightsAlloc[1] = 0f;
                        boneWeightsAlloc[2] = 0f;
                        boneWeightsAlloc[3] = 0f;
                    }
                    else if (hasDefaultBoneIndex)
                    {
                        boneIndicesAlloc[0] = defaultBoneIndex;
                        boneIndicesAlloc[1] = -1;
                        boneIndicesAlloc[2] = -1;
                        boneIndicesAlloc[3] = -1;
                        boneWeightsAlloc[0] = 1f;
                        boneWeightsAlloc[1] = 0f;
                        boneWeightsAlloc[2] = 0f;
                        boneWeightsAlloc[3] = 0f;
                    }

                    // Is a local function to allow for recursion and easier to read code
                    // Needed to add parent bones that no vertices ever reference themselves
                    Bone AddMeshBone(int boneIndex)
                    {
                        // Add the bone if we couldn't find it
                        if (!meshBoneMap.TryGetValue(boneIndex, out Bone? meshBone))
                        {
                            var bone = model.Bones[boneIndex];
                            var offsetTransform = offsetTransforms[boneIndex];

                            // The name of an assimp mesh bone must be the same as an existing Node added in the scene node tree
                            meshBone = new Bone
                            {
                                Name = bone.Name,
                                OffsetMatrix = offsetTransform,
                            };

                            newMesh.Bones.Add(meshBone);
                            meshBoneMap.Add(boneIndex, meshBone);
                            referencedBoneIndices.Add(boneIndex);

                            // Add this bone's parents
                            if (bone.ParentIndex > -1)
                                AddMeshBone(bone.ParentIndex);
                        }

                        return meshBone;
                    }

                    // Add bone weights
                    for (int i = 0; i < 4; i++)
                    {
                        int boneIndex = boneIndicesAlloc[i];
                        float boneWeight = boneWeightsAlloc[i];
                        if (boneIndex > -1 && boneIndex < model.Bones.Count && boneWeight > 0f)
                        {
                            // Add a mesh bone and all it's parents, or find an existing one we already added
                            var meshBone = AddMeshBone(boneIndex);
                            meshBone.VertexWeights.Add(new VertexWeight(vertexIndex, boneWeight));
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                scene.Meshes.Add(newMesh);
                newMeshNode.MeshIndices.Add(meshIndex);
                parentNode.Children.Add(newMeshNode);
            }

            // Find unused bones
            var unusedBoneIndices = new List<int>();
            for (int i = 0; i < model.Bones.Count; i++)
            {
                if (!referencedBoneIndices.Contains(i))
                    unusedBoneIndices.Add(i);
            }

            // Add unused bones into unused bones mesh
            if (unusedBoneIndices.Count > 0)
            {
                Mesh unusedBonesMesh = new Mesh("UnusedBonesMesh");
                Node unusedBonesMeshNode = new Node("UnusedBonesMeshNode", meshRootNode);
                Material unusedBonesMeshMaterial = new Material();
                unusedBonesMeshMaterial.Name = "UnusedBonesMeshMaterial";

                foreach (var boneIndex in unusedBoneIndices)
                {
                    var newBone = new Bone(model.Bones[boneIndex].Name, offsetTransforms[boneIndex], []);
                    unusedBonesMesh.Bones.Add(newBone);
                }

                // Count now will be last index once added
                unusedBonesMesh.MaterialIndex = scene.MaterialCount;
                unusedBonesMeshNode.MeshIndices.Add(scene.MeshCount);
                meshRootNode.Children.Add(unusedBonesMeshNode);
                scene.Materials.Add(unusedBonesMeshMaterial);
                scene.Meshes.Add(unusedBonesMesh);
            }

            return scene;
        }
    }
}
