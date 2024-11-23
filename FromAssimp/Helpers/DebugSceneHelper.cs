using Assimp;
using FromAssimp.Extensions;
using System.Text;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;

namespace FromAssimp.Helpers
{
    internal static class DebugSceneHelper
    {
        internal static void DebugPrintSceneInfo(Scene scene)
        {
            // Counts
            Console.WriteLine($"MATERIAL_COUNT: {scene.MaterialCount}");
            Console.WriteLine($"MESH_COUNT: {scene.MeshCount}");

            // Materials
            if (scene.HasMaterials)
            {
                Console.WriteLine("MATERIALS:");
                for (int materialIndex = 0; materialIndex < scene.MaterialCount; materialIndex++)
                {
                    var material = scene.Materials[materialIndex];
                    Console.WriteLine($"MATERIAL_{materialIndex}:");
                    Console.WriteLine($"NAME: {material.Name}");
                }
            }

            // Meshes and bones
            if (scene.HasMeshes)
            {
                Console.WriteLine("MESHES:");
                for (int meshIndex = 0; meshIndex < scene.MeshCount; meshIndex++)
                {
                    var mesh = scene.Meshes[meshIndex];
                    Console.WriteLine($"MESH_{meshIndex}:");
                    Console.WriteLine($"NAME: {mesh.Name}");
                    Console.WriteLine($"MATERIAL_INDEX: {mesh.MaterialIndex}");
                    Console.WriteLine($"PRIMITIVE_TYPE: {mesh.PrimitiveType}");
                    Console.WriteLine($"BOUNDING_BOX: {mesh.BoundingBox.Min.ToNumericsVector3()},{mesh.BoundingBox.Max.ToNumericsVector3()}");
                    Console.WriteLine($"FACE_COUNT: {mesh.FaceCount}");
                    Console.WriteLine($"VERTEX_COUNT: {mesh.VertexCount}");
                    Console.WriteLine($"NORMAL_COUNT: {mesh.Normals.Count}");
                    Console.WriteLine($"TANGENT_COUNT: {mesh.Tangents.Count}");
                    Console.WriteLine($"BITANGENT_COUNT: {mesh.BiTangents.Count}");
                    Console.WriteLine($"TEXTURE_COORD_CHANNEL_COUNT: {mesh.TextureCoordinateChannelCount}");
                    for (int channelIndex = 0; channelIndex < mesh.TextureCoordinateChannelCount; channelIndex++)
                    {
                        var channel = mesh.TextureCoordinateChannels[channelIndex];
                        Console.WriteLine($"TEXTURE_COORD_COUNT_{channelIndex}: {channel.Count}");
                    }

                    for (int channelIndex = 0; channelIndex < mesh.TextureCoordinateChannelCount; channelIndex++)
                    {
                        Console.WriteLine($"UV_COMPONENT_COUNT_{channelIndex}: {mesh.UVComponentCount[channelIndex]}");
                    }

                    Console.WriteLine($"COLOR_CHANNEL_COUNT: {mesh.VertexColorChannelCount}");
                    for (int channelIndex = 0; channelIndex < mesh.VertexColorChannelCount; channelIndex++)
                    {
                        var channel = mesh.VertexColorChannels[channelIndex];
                        Console.WriteLine($"COLOR_COUNT_{channelIndex}: {channel.Count}");
                    }

                    // Bones
                    Console.WriteLine($"BONE_COUNT: {mesh.BoneCount}");
                    if (mesh.HasBones)
                    {
                        Console.WriteLine($"BONES:");
                        for (int boneMeshIndex = 0; boneMeshIndex < mesh.BoneCount; boneMeshIndex++)
                        {
                            var bone = mesh.Bones[boneMeshIndex];
                            Console.WriteLine($"MESH_BONE_{boneMeshIndex}:");
                            Console.WriteLine($"NAME: {bone.Name}");

                            var inverseWorldTransform = bone.OffsetMatrix.ToNumericsMatrix4x4();
                            Console.WriteLine($"INVERSE_WORLD_MATRIX: {inverseWorldTransform}");
                            NumericsMatrix4x4.Invert(inverseWorldTransform, out NumericsMatrix4x4 worldTransform);
                            Console.WriteLine($"WORLD_MATRIX: {worldTransform}");

                            Console.WriteLine($"VERTEX_WEIGHT_COUNT: {bone.VertexWeightCount}");
                        }
                    }
                }
            }

            // Nodes
            if (scene.RootNode != null)
            {
                Console.WriteLine("NODES:");
                DebugPrintNodeInfo(scene.RootNode, string.Empty);
            }

            // Warnings
            if (scene.RootNode != null)
            {
                for (int meshIndex = 0; meshIndex < scene.MeshCount; meshIndex++)
                {
                    var mesh = scene.Meshes[meshIndex];
                    int meshNameInstanceCount = CountNodeInstances(scene.RootNode, mesh.Name);
                    if (meshNameInstanceCount < 1)
                    {
                        Console.WriteLine($"INFO: Mesh {meshIndex} named {mesh.Name} has no nodes referencing it by name.");
                    }
                    else if (meshNameInstanceCount > 1)
                    {
                        Console.WriteLine($"WARNING: Mesh {meshIndex} named {mesh.Name} has {meshNameInstanceCount} nodes referencing it by name.");
                    }

                    int meshIndexInstanceCount = CountNodeMeshInstances(scene.RootNode, meshIndex);
                    if (meshIndexInstanceCount < 1)
                    {
                        Console.WriteLine($"WARNING: Mesh {meshIndex} named {mesh.Name} has no nodes referencing it by index.");
                    }
                    else if (meshIndexInstanceCount > 1)
                    {
                        Console.WriteLine($"WARNING: Mesh {meshIndex} named {mesh.Name} has {meshIndexInstanceCount} nodes referencing it by index.");
                    }

                    if (mesh.MaterialIndex < 0)
                    {
                        Console.WriteLine($"WARNING: Mesh {meshIndex} named {mesh.Name} has an invalid material index.");
                    }

                    if (mesh.MaterialIndex > scene.MaterialCount)
                    {
                        Console.WriteLine($"WARNING: Mesh {meshIndex} named {mesh.Name} has a material index that is out of range.");
                    }

                    for (int boneMeshIndex = 0; boneMeshIndex < mesh.BoneCount; boneMeshIndex++)
                    {
                        var bone = mesh.Bones[boneMeshIndex];
                        int boneInstanceCount = CountNodeInstances(scene.RootNode, bone.Name);
                        if (boneInstanceCount < 1)
                        {
                            Console.WriteLine($"WARNING: Bone {boneMeshIndex} named {bone.Name} in mesh {meshIndex} named {mesh.Name} has no nodes referencing it.");
                        }
                        else if (boneInstanceCount > 1)
                        {
                            Console.WriteLine($"WARNING: Bone {boneMeshIndex} named {bone.Name} in mesh {meshIndex} named {mesh.Name} has multiple nodes referencing it.");
                        }
                    }
                }
            }
        }

        private static void DebugPrintNodeInfo(Node node, string tabStr)
        {
            Console.WriteLine($"{tabStr} NODE:");
            Console.WriteLine($"{tabStr} NAME: {node.Name}");
            Console.WriteLine($"{tabStr} MESH_INDICES: {string.Join(',', node.MeshIndices)}");
            Console.WriteLine($"{tabStr} TRANSFORM: {node.Transform}");

            if (node.HasChildren)
            {
                Console.WriteLine($"{tabStr} CHILD_NODES:");
                string childTabStr = tabStr + ">>>>";
                foreach (var child in node.Children)
                {
                    DebugPrintNodeInfo(child, childTabStr);
                }
            }
        }

        private static int CountNodeInstances(Node node, string name)
        {
            int count = 0;
            if (node.Name == name)
            {
                count += 1;
            }

            foreach (var child in node.Children)
            {
                count += CountNodeInstances(child, name);
            }
            return count;
        }

        private static int CountNodeMeshInstances(Node node, int meshIndex)
        {
            int count = 0;
            if (node.HasMeshes && node.MeshIndices.Contains(meshIndex))
            {
                count += 1;
            }

            foreach (var child in node.Children)
            {
                count += CountNodeMeshInstances(child, meshIndex);
            }
            return count;
        }

        internal static void DebugPrintNormals(Scene scene)
        {
            StringBuilder buffer = new StringBuilder();
            for (int meshIndex = 0; meshIndex < scene.MeshCount; meshIndex++)
            {
                var mesh = scene.Meshes[meshIndex];
                buffer.Append($"MESH{meshIndex}_NORMALS:\n");
                for (int normalIndex = 0; normalIndex < mesh.Vertices.Count; normalIndex++)
                {
                    var normal = mesh.Normals[normalIndex];
                    buffer.Append($"{normal.X},{normal.Y},{normal.Z}\n");
                }
            }
            Console.WriteLine(buffer);
        }
    }
}
