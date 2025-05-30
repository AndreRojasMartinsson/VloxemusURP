using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Terrain
{
	public class Context
	{
		// Mesh Job Related Native Collections
		public NativeArray<byte> Voxels;

		public NativeList<Vector3> Vertices;
		public NativeList<Vector3> Normals;
		public NativeList<Vector2> UVs;
		public NativeList<int> Triangles;

		public NativeList<Vector3> WaterVertices;
		public NativeList<Vector3> WaterNormals;
		public NativeList<Vector2> WaterUVs;
		public NativeList<int> WaterTriangles;

		// Noise Job Related Native Collections
		public NativeArray<int> HeightMap;
		public NativeArray<float2> SplinePoints;

		[ReadOnly] public NativeArray<Vector3> FaceVertices;
		[ReadOnly] public NativeArray<Vector3> FaceNormals;
		[ReadOnly] public NativeArray<int> FaceTriangles;
		[ReadOnly] public NativeArray<int> FaceUVs;

		public Context(Settings settings)
		{
			Voxels = new NativeArray<byte>(settings.ChunkSize * settings.ChunkSize * settings.WorldHeight, Allocator.TempJob);

			SplinePoints = new NativeArray<float2>(new[]
			{
				new float2(-2, 0),
				new float2(-1, 6),
				new float2(-0.9f, 10),
				new float2(-0.75f, 20),
				new float2(-0.35f, 45),
				new float2(0.7f, 70),
				new float2(1.0f, 128),
				new float2(2.5f, 168),
			}, Allocator.TempJob);

			HeightMap = new NativeArray<int>(settings.ChunkSize * settings.ChunkSize, Allocator.TempJob);

			Vertices = new NativeList<Vector3>(Allocator.TempJob);
			Normals = new NativeList<Vector3>(Allocator.TempJob);
			UVs = new NativeList<Vector2>(Allocator.TempJob);
			Triangles = new NativeList<int>(Allocator.TempJob);

			WaterVertices = new NativeList<Vector3>(Allocator.TempJob);
			WaterNormals = new NativeList<Vector3>(Allocator.TempJob);
			WaterUVs = new NativeList<Vector2>(Allocator.TempJob);
			WaterTriangles = new NativeList<int>(Allocator.TempJob);


			FaceVertices = new NativeArray<Vector3>(FaceVerts, Allocator.TempJob);
			FaceNormals = new NativeArray<Vector3>(FaceNorms, Allocator.TempJob);

			var flatFaceTriangles = new int[6 * 6];
			for (var f = 0; f < 6; f++)
			for (var i = 0; i < 6; i++)
				flatFaceTriangles[f * 6 + i] = FaceTris[f, i];

			FaceTriangles = new NativeArray<int>(flatFaceTriangles, Allocator.TempJob);

			var flatFaceUVs = new int[6 * 4];
			for (var face = 0; face < 6; face++)
			for (var corner = 0; corner < 4; corner++)
				flatFaceUVs[face * 4 + corner] = FaceUV[face, corner];

			FaceUVs = new NativeArray<int>(flatFaceUVs, Allocator.TempJob);
		}

		public void Dispose()
		{
			HeightMap.Dispose();
			SplinePoints.Dispose();
			Voxels.Dispose();
			Vertices.Dispose();
			Normals.Dispose();
			UVs.Dispose();
			Triangles.Dispose();

			WaterNormals.Dispose();
			WaterTriangles.Dispose();
			WaterUVs.Dispose();
			WaterVertices.Dispose();

			FaceVertices.Dispose();
			FaceNormals.Dispose();
			FaceTriangles.Dispose();
			FaceUVs.Dispose();
		}


		private static readonly Vector3[] FaceVerts =
		{
			// +X
			new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1),
			// -X
			new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(0, 0, 0),
			// +Y
			new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(0, 1, 0),
			// -Y
			new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1),
			// +Z
			new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1),
			// -Z
			new Vector3(1, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0)
		};

		private static readonly int[,] FaceTris =
		{
			{ 0, 1, 2, 2, 3, 0 }, // +X
			{ 0, 1, 2, 2, 3, 0 }, // -X
			{ 0, 1, 2, 2, 3, 0 }, // +Y
			{ 0, 1, 2, 2, 3, 0 }, // -Y
			{ 0, 1, 2, 2, 3, 0 }, // +Z
			{ 0, 1, 2, 2, 3, 0 }, // -Z
		};

		private static readonly Vector3[] FaceNorms =
		{
			Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
		};

		// Order for each face: [0]=lower-left, [1]=lower-right, [2]=upper-right, [3]=upper-left
		// For each face, list which corner (of the quad) gets which UV

		// Each row is a face; the four numbers are the order to use the UVs for that face
		// Example: {0, 1, 2, 3} = normal; {1, 2, 3, 0} = rotate; {3, 2, 1, 0} = flip
		private static readonly int[,] FaceUV =
		{
			{ 1, 2, 3, 0 }, // +X
			{ 1, 2, 3, 0 }, // -X
			{ 3, 0, 1, 2 }, // +Y (rotate -90)
			{ 0, 1, 2, 3 }, // -Y
			{ 0, 1, 2, 3 }, // +Z
			{ 0, 1, 2, 3 } // -Z (rotate -90)
		};
	}
}