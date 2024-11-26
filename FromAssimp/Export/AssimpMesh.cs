using Assimp;
using FromAssimp.Helpers;

namespace FromAssimp.Export
{
    internal class AssimpMesh
    {
        public int MaterialIndex { get; set; }
        public List<string> Bones { get; set; }
        public Vertex[] Vertices { get; set; }
        public List<int> Indices { get; set; }
        public BoundingBox BoundingBox { get; set; }
        public bool TriangleStrip { get; set; }
        public bool HasNormals { get; set; }
        public bool HasTangents { get; set; }
        public bool HasBiTangents { get; set; }
        public bool HasColors { get; set; }
        public bool HasUVs { get; set; }
        public bool HasBoneIndices { get; set; }
        public bool HasBoneWeights { get; set; }
        public int ColorCount { get; set; }
        public int UVCount { get; set; }
        public int UVComponentCount { get; set; }
        public int VertexBoneCount { get; set; }

        public AssimpMesh(Mesh mesh, bool useTriangleStrips)
        {
            MaterialIndex = mesh.MaterialIndex;
            BoundingBox = mesh.BoundingBox;

            HasNormals = mesh.Normals.Count == mesh.Vertices.Count;
            HasTangents = mesh.Tangents.Count == mesh.Vertices.Count;
            HasBiTangents = mesh.BiTangents.Count == mesh.Vertices.Count;

            foreach (var channel in mesh.VertexColorChannels)
            {
                if (channel != null)
                {
                    if (channel.Count == mesh.Vertices.Count)
                    {
                        ColorCount++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            HasColors = ColorCount > 0;

            UVComponentCount = 2;
            for (int channelIndex = 0; channelIndex < mesh.TextureCoordinateChannels.Length; channelIndex++)
            {
                var channel = mesh.TextureCoordinateChannels[channelIndex];
                var compCount = mesh.UVComponentCount[channelIndex];
                if (channel != null)
                {
                    if (channel.Count == mesh.Vertices.Count)
                    {
                        UVCount++;
                        if (compCount == 3)
                        {
                            // Prevent setting 3, then 2 later possibly
                            UVComponentCount = 3;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            HasUVs = UVCount > 0;
            if (!HasUVs)
                UVComponentCount = 0;

            Vertices = new Vertex[mesh.Vertices.Count];
            for (int vertexIndex = 0; vertexIndex < mesh.Vertices.Count; vertexIndex++)
            {
                var vertex = new Vertex();
                vertex.Position = mesh.Vertices[vertexIndex];
                
                if (HasNormals)
                    vertex.Normal = mesh.Normals[vertexIndex];

                if (HasTangents)
                    vertex.Tangent = mesh.Tangents[vertexIndex];

                if (HasBiTangents)
                    vertex.BiTangent = mesh.BiTangents[vertexIndex];

                for (int channelIndex = 0; channelIndex < ColorCount; channelIndex++)
                {
                    var channel = mesh.VertexColorChannels[channelIndex];
                    vertex.Colors.Add(channel[vertexIndex]);
                }

                for (int channelIndex = 0; channelIndex < UVCount; channelIndex++)
                {
                    var channel = mesh.TextureCoordinateChannels[channelIndex];
                    vertex.UVs.Add(channel[vertexIndex]);
                }

                Vertices[vertexIndex] = vertex;
            }

            Bones = new List<string>(mesh.Bones.Count);
            for (int boneIndex = 0; boneIndex < mesh.Bones.Count; boneIndex++)
            {
                var bone = mesh.Bones[boneIndex];

                Bones.Add(bone.Name);
                foreach (var weight in bone.VertexWeights)
                {
                    var vertex = Vertices[weight.VertexID];
                    vertex.BoneIndices.Add(boneIndex);
                    vertex.BoneWeights.Add(weight.Weight);
                    Vertices[weight.VertexID] = vertex;
                }
            }

            HasBoneIndices = Bones.Count > 0;

            foreach (var vertex in Vertices)
            {
                for (int weightIndex = 1; weightIndex < vertex.BoneWeights.Count; weightIndex++)
                {
                    var weight = vertex.BoneWeights[weightIndex];
                    if (weight != 0f)
                    {
                        int weightCount = weightIndex + 1;
                        if (weightCount > VertexBoneCount)
                            VertexBoneCount = weightCount;
                    }
                }
            }

            HasBoneWeights = VertexBoneCount > 0;

            var triangles = new List<int>(mesh.Faces.Count * 3);
            foreach (var face in mesh.Faces)
            {
                var indices = new List<int>(face.Indices);
                indices.Reverse();
                triangles.AddRange(indices);
            }

            TriangleStrip = useTriangleStrips;
            if (TriangleStrip && IndexHelper.TryStripify(triangles, out List<int>? triangleStrip))
            {
                Indices = triangleStrip;
            }
            else
            {
                TriangleStrip = false;
                Indices = triangles;
            }
        }

        internal class Vertex
        {
            public Vector3D Position { get; set; }
            public Vector3D Normal { get; set; }
            public Vector3D Tangent { get; set; }
            public Vector3D BiTangent { get; set; }
            public List<Color4D> Colors { get; set; }
            public List<Vector3D> UVs { get; set; }
            public List<int> BoneIndices { get; set; }
            public List<float> BoneWeights { get; set; }

            public Vertex()
            {
                Colors = new List<Color4D>();
                UVs = new List<Vector3D>();
                BoneIndices = new List<int>();
                BoneWeights = new List<float>();
            }
        }
    }
}
