using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Terrain.Jobs
{
	public static class WaterMeshGen
	{
		[BurstCompile]
		private struct WaterMeshGenJob : IJob
		{
			[ReadOnly] public NativeArray<Vector3> FaceVertices;
			[ReadOnly] public NativeArray<Vector3> FaceNormals;
			[ReadOnly] public NativeArray<int> FaceTriangles;
			[ReadOnly] public NativeArray<int> FaceUVs;

			[ReadOnly] public Settings Settings;
			[ReadOnly] public NativeArray<byte> Voxels;

			public NativeList<Vector3> Vertices;
			public NativeList<Vector3> Normals;
			public NativeList<Vector2> UVs;
			public NativeList<int> Triangles;

			public void Execute()
			{
				for (var z = 0; z < Settings.ChunkSize; z++)
				for (var y = 0; y < Settings.WorldHeight; y++)
				for (var x = 0; x < Settings.ChunkSize; x++)
				{
					var index = x + (y * Settings.ChunkSize) + (z * Settings.ChunkSize * Settings.WorldHeight);
					var voxel = (VoxelType)Voxels[index];


					if (voxel != VoxelType.Water) continue;

					if (y != 9) continue;


					var voxelTextures = Texture.GetVoxelFaceTextureIndices(voxel);

					for (var f = 0; f < 6; f++)
					{
						// Only render top face.
						if (f != 2) continue;

						var vStart = Vertices.Length;

						// Add vertices for this face
						for (var i = 0; i < 4; i++)
						{
							var v = (new Vector3(x, y, z) + FaceVertices[f * 4 + i]) * Settings.VoxelSize;

							Vertices.Add(v);
							Normals.Add(FaceNormals[f]);

							var uvIndex = FaceUVs[f * 4 + i];

							var uvCoord = Texture.GetTileUVCoords(0, 0);
							var uvU = uvIndex is 0 or 3 ? uvCoord.uMin : uvCoord.uMax;
							var uvV = uvIndex is 0 or 1 ? uvCoord.vMin : uvCoord.vMax;

							UVs.Add(new Vector2(uvU, uvV));
						}

						// Add triangles (indices)
						for (var t = 0; t < 6; t++)
							Triangles.Add(vStart + FaceTriangles[f * 6 + t]);
					}
				}
			}
		}

		public static JobHandle Schedule(Settings settings, Context context, JobHandle terrainMeshHandle)
		{
			var job = new WaterMeshGenJob()
			{
				Settings = settings,
				Voxels = context.Voxels,
				FaceNormals = context.FaceNormals,
				FaceTriangles = context.FaceTriangles,
				FaceVertices = context.FaceVertices,
				Normals = context.WaterNormals,
				Triangles = context.WaterTriangles,
				UVs = context.WaterUVs,
				Vertices = context.WaterVertices,
				FaceUVs = context.FaceUVs,
			};

			return job.ScheduleByRef(terrainMeshHandle);
		}
	}
}