using Assimp;
using SoulsFormats;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using FromAssimp.Extensions.Numerics;
using FromAssimp.Extensions.Common;
using FromAssimp.Extensions.Assimp;
using System.Numerics;

namespace FromAssimp
{
    public static class FlverImport
    {
        private static void CollectBones(Node rootNode, IReadOnlyList<FLVER.Bone> bones, out Bone[] newBones)
        {
            int count = bones.Count;
            Node[] newBoneNodes = new Node[count];
            newBones = new Bone[count];

            for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++)
            {
                var bone = bones[boneIndex];
                Node parentNode;
                if (bone.ParentIndex == -1)
                {
                    parentNode = rootNode;
                }
                else
                {
                    parentNode = newBoneNodes[bone.ParentIndex];
                }

                Node newNode = new Node(bone.Name, parentNode);
                Bone newBone = new Bone();
                NumericsMatrix4x4 worldTransform = bone.ComputeWorldTransform(bones);
                NumericsMatrix4x4.Invert(worldTransform, out NumericsMatrix4x4 inverseWorldTransform);

                newNode.Transform = bone.ComputeLocalTransform().ToAssimpMatrix4x4();
                newBone.Name = bone.Name;
                newBone.OffsetMatrix = inverseWorldTransform.ToAssimpMatrix4x4();

                parentNode.Children.Add(newNode);
                newBoneNodes[boneIndex] = newNode;
                newBones[boneIndex] = newBone;
            }
        }

        private static void CollectVertices(List<FLVER.Vertex> vertices, Mesh newMesh, IList<int> boneIndices, Bone[] newBones, byte dynamic, int defaultBoneIndex, out Dictionary<int, Bone> boneMap)
        {
            // Prepare a bone map
            boneMap = new Dictionary<int, Bone>(boneIndices.Count);
            bool hasBones = boneIndices.Count > 0;

            for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                // Gather vertex information
                var vertex = vertices[vertexIndex];
                var position = vertex.Position;
                var normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                var normalW = vertex.NormalW;
                var bitangent = new Vector3(vertex.Bitangent.X, vertex.Bitangent.Y, vertex.Bitangent.Z);
                var tangents = new List<Vector3>(vertex.Tangents.Count);
                foreach (var tangent in vertex.Tangents)
                    tangents.Add(new Vector3(tangent.X, tangent.Y, tangent.Z));

                // Gather transformation information
                NumericsMatrix4x4 worldTransform = NumericsMatrix4x4.Identity;
                int boneTransformationIndex = -1;
                bool transformPosition = false;
                if (hasBones && dynamic == 0)
                {
                    transformPosition = true;
                    boneTransformationIndex = boneIndices[vertex.NormalW];

                    if (boneTransformationIndex > -1)
                    {
                        // If the bone map does not already have the bone add it
                        if (!boneMap.ContainsKey(boneTransformationIndex))
                        {
                            var newBone = new Bone();
                            newBone.Name = newBones[boneTransformationIndex].Name;
                            newBone.OffsetMatrix = newBones[boneTransformationIndex].OffsetMatrix;
                            boneMap.Add(boneTransformationIndex, newBone);
                        }

                        // Add bone weight
                        boneMap[boneTransformationIndex].VertexWeights.Add(new VertexWeight(vertexIndex, 1f));
                    }
                }
                else if (hasBones && dynamic == 1)
                {
                    boneTransformationIndex = defaultBoneIndex;

                    // Add bone weights
                    for (int i = 0; i < 4; i++)
                    {
                        int boneIndex = vertex.BoneIndices[i];
                        float boneWeight = vertex.BoneWeights[i];
                        if (boneWeight > 0f)
                        {
                            // If the bone map does not already have the bone add it
                            if (!boneMap.ContainsKey(boneIndex))
                            {
                                var newBone = new Bone();
                                newBone.Name = newBones[boneIndex].Name;
                                newBone.OffsetMatrix = newBones[boneIndex].OffsetMatrix;
                                boneMap.Add(boneIndex, newBone);
                            }

                            if (!boneMap[boneIndex].VertexWeights.Any(x => x.VertexID == vertexIndex))
                            {
                                boneMap[boneIndex].VertexWeights.Add(new VertexWeight(vertexIndex, boneWeight));
                            }
                        }
                    }
                }
                else
                {
                    transformPosition = true;
                    boneTransformationIndex = defaultBoneIndex;

                    // If the bone map does not already have the bone add it
                    if (!boneMap.ContainsKey(boneTransformationIndex))
                    {
                        var newBone = new Bone();
                        newBone.Name = newBones[boneTransformationIndex].Name;
                        newBone.OffsetMatrix = newBones[boneTransformationIndex].OffsetMatrix;
                        boneMap.Add(boneTransformationIndex, newBone);
                    }

                    // Add bone weight
                    boneMap[boneTransformationIndex].VertexWeights.Add(new VertexWeight(vertexIndex, 1f));
                }

                // Transform Position, Normal, Tangents, and BiTangent
                if (boneTransformationIndex > -1)
                {
                    NumericsMatrix4x4.Invert(newBones[boneTransformationIndex].OffsetMatrix.ToNumericsMatrix4x4(), out worldTransform);
                    if (transformPosition)
                        position = Vector3.Transform(position, worldTransform);

                    normal = Vector3.TransformNormal(normal, worldTransform) * -1;
                    bitangent = Vector3.TransformNormal(bitangent, worldTransform) * -1;
                    for (int i = 0; i < tangents.Count; i++)
                        tangents[i] = Vector3.TransformNormal(tangents[i], worldTransform) * -1;
                }

                // Add Position, Normal, Tangents, and BiTangent
                newMesh.Vertices.Add(position.ToAssimpVector3D());
                newMesh.Normals.Add(normal.ToAssimpVector3D());
                newMesh.BiTangents.Add(bitangent.ToAssimpVector3D());
                for (int i = 0; i < tangents.Count; i++)
                    newMesh.Tangents.Add(tangents[i].ToAssimpVector3D());

                // Add UVs
                int uvCount = vertex.UVs.Count;
                for (int i = 0; i < 4; i++)
                {
                    Vector3D textureCoordinate;
                    if (uvCount > i)
                    {
                        var uv = vertex.UVs[i];
                        textureCoordinate = new Vector3D(uv.X, 1 - uv.Y, 0f);
                    }
                    else
                    {
                        textureCoordinate = new Vector3D(1f, 1f, 1f);
                    }
                    newMesh.TextureCoordinateChannels[i].Add(textureCoordinate);
                }

                // Each UV is only X and Y so set the component count to 2
                for (int i = 0; i < newMesh.TextureCoordinateChannelCount; i++)
                {
                    if (i < 4)
                    {
                        newMesh.UVComponentCount[i] = 2;
                    }
                    else
                    {
                        // There are only 4 channels for these models
                        newMesh.UVComponentCount[i] = 0;
                    }
                }

                // Add Colors
                int colorCount = vertex.Colors.Count;
                for (int i = 0; i < 2; i++)
                {
                    if (i < colorCount)
                    {
                        var color = vertex.Colors[i];
                        newMesh.VertexColorChannels[i].Add(new Color4D(color.R, color.G, color.B, color.A));
                    }
                }
            }
        }

        private static void CollectMeshes(Scene scene, FLVER0 model, Bone[] newBones, out int[] consumedBoneIndices)
        {
            List<int> consumedBoneIndicesList = new List<int>(model.Bones.Count);
            for (int meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
            {
                var mesh = model.Meshes[meshIndex];
                var meshNode = new Node($"Mesh_{meshIndex}", scene.RootNode);
                var newMesh = new Mesh($"Mesh_M{meshIndex}", PrimitiveType.Triangle);

                // Collect Faces
                var faceIndices = mesh.GetFaceIndices(model.Header.Version);
                foreach (int[] indices in faceIndices)
                {
                    newMesh.Faces.Add(new Face(indices));
                }

                CollectVertices(mesh.Vertices, newMesh, mesh.BoneIndices.ToIntArray(), newBones, mesh.Dynamic, mesh.DefaultBoneIndex, out Dictionary<int, Bone> boneMap);
                newMesh.Bones.AddRange(boneMap.Values);
                consumedBoneIndicesList.AddRange(boneMap.Keys);

                // Collect Bones referenced in this mesh, but not in it's vertices.
                for (int i = 0; i < mesh.BoneIndices.Length; i++)
                {
                    int boneIndex = mesh.BoneIndices[i];
                    if (boneIndex >= 0 && !boneMap.ContainsKey(boneIndex))
                    {
                        var bone = model.Bones[boneIndex];
                        NumericsMatrix4x4.Invert(bone.ComputeWorldTransform(model.Bones), out NumericsMatrix4x4 inverseWorldTransform);
                        newMesh.Bones.Add(new Bone(bone.Name, inverseWorldTransform.ToAssimpMatrix4x4(), Array.Empty<VertexWeight>()));
                        consumedBoneIndicesList.Add(boneIndex);
                    }
                }

                meshNode.Transform = AssimpMatrix4x4.Identity;
                meshNode.MeshIndices.Add(meshIndex);
                newMesh.MaterialIndex = mesh.MaterialIndex;

                scene.RootNode.Children.Add(meshNode);
                scene.Meshes.Add(newMesh);
            }
            consumedBoneIndices = consumedBoneIndicesList.ToArray();
        }

        private static void CollectMeshes(Scene scene, FLVER2 model, Bone[] newBones, out int[] consumedBoneIndices)
        {
            List<int> consumedBoneIndicesList = new List<int>(model.Bones.Count);
            for (int meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
            {
                var mesh = model.Meshes[meshIndex];
                var meshNode = new Node($"Mesh_{meshIndex}", scene.RootNode);
                var newMesh = new Mesh($"Mesh_M{meshIndex}", PrimitiveType.Triangle);

                // Collect Faces
                foreach (var faceset in mesh.FaceSets)
                {
                    var indices = faceset.Triangulate(mesh.Vertices.Count < ushort.MaxValue);
                    for (int i = 0; i < indices.Count - 2; i += 3)
                    {
                        newMesh.Faces.Add(new Face(new int[] { indices[i], indices[i + 1], indices[i + 2] }));
                    }
                }

                // Each UV is only X and Y so set the component count to 2
                for (int i = 0; i < newMesh.TextureCoordinateChannelCount; i++)
                {
                    newMesh.UVComponentCount[i] = 2;
                }

                CollectVertices(mesh.Vertices, newMesh, mesh.BoneIndices, newBones, mesh.Dynamic, mesh.DefaultBoneIndex, out Dictionary<int, Bone> boneMap);

                // Add Bone references holding bone weights to the mesh
                newMesh.Bones.AddRange(boneMap.Values);
                consumedBoneIndicesList.AddRange(boneMap.Keys);

                // Collect Bones referenced in this mesh, but not in it's vertices.
                for (int i = 0; i < mesh.BoneIndices.Count; i++)
                {
                    int boneIndex = mesh.BoneIndices[i];
                    if (boneIndex >= 0 && !boneMap.ContainsKey(boneIndex))
                    {
                        var bone = model.Bones[boneIndex];
                        NumericsMatrix4x4.Invert(bone.ComputeWorldTransform(model.Bones), out NumericsMatrix4x4 inverseWorldTransform);
                        newMesh.Bones.Add(new Bone(bone.Name, inverseWorldTransform.ToAssimpMatrix4x4(), Array.Empty<VertexWeight>()));
                        consumedBoneIndicesList.Add(boneIndex);
                    }
                }

                meshNode.Transform = AssimpMatrix4x4.Identity;
                meshNode.MeshIndices.Add(meshIndex);
                newMesh.MaterialIndex = mesh.MaterialIndex;

                scene.RootNode.Children.Add(meshNode);
                scene.Meshes.Add(newMesh);
            }
            consumedBoneIndices = consumedBoneIndicesList.ToArray();
        }

        public static Scene ToAssimpScene(this IFlver model)
        {
            var scene = new Scene();
            scene.RootNode = new Node();

            // Collect Materials
            foreach (var material in model.Materials)
            {
                Material newMaterial = new Material();
                newMaterial.Name = material.Name;
                scene.Materials.Add(newMaterial);
            }

            CollectBones(scene.RootNode, model.Bones, out Bone[] newBones);

            int[] consumedBoneIndices = Array.Empty<int>();
            if (model is FLVER0 flver0)
            {
                CollectMeshes(scene, flver0, newBones, out consumedBoneIndices);
            }
            else if (model is FLVER2 flver2)
            {
                CollectMeshes(scene, flver2, newBones, out consumedBoneIndices);
            }

            // Collect Bones not referenced in Meshes or Vertices.
            for (int i = 0; i < model.Bones.Count; i++)
            {
                if (!consumedBoneIndices.Contains(i))
                {
                    var bone = model.Bones[i];
                    NumericsMatrix4x4.Invert(bone.ComputeWorldTransform(model.Bones), out NumericsMatrix4x4 inverseWorldTransform);

                    if (scene.MeshCount == 0)
                    {
                        string newMeshName = "ASSIMP_MESH_PLACEHOLDER";
                        Node newMeshNode = new Node(newMeshName, scene.RootNode);
                        newMeshNode.Transform = AssimpMatrix4x4.Identity;
                        newMeshNode.MeshIndices.Add(0);
                        scene.RootNode.Children.Add(newMeshNode);

                        if (scene.MaterialCount == 0)
                        {
                            Material newMaterial = new()
                            {
                                Name = "default"
                            };
                            scene.Materials.Add(newMaterial);
                        }

                        Mesh newMesh = new Mesh(newMeshName, PrimitiveType.Triangle);
                        newMesh.MaterialIndex = 0;
                        scene.Meshes.Add(newMesh);
                    }

                    scene.Meshes[0].Bones.Add(new Bone(bone.Name, inverseWorldTransform.ToAssimpMatrix4x4(), Array.Empty<VertexWeight>()));
                }
            }

            return scene;
        }
    }
}
