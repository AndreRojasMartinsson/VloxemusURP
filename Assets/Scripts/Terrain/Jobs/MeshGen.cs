using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Terrain.Jobs
{
	public static class MeshGen
	{
		[BurstCompile]
		private struct MeshGenJob : IJob
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


					if (voxel is VoxelType.Air or VoxelType.Water) continue;


					var voxelTextures = Texture.GetVoxelFaceTextureIndices(voxel);

					for (var f = 0; f < 6; f++)
					{
						int nx = x, ny = y, nz = z;
						switch (f)
						{
							case 0: nx = x + 1; break; // +X
							case 1: nx = x - 1; break; // -X
							case 2: ny = y + 1; break; // +Y
							case 3: ny = y - 1; break; // -Y
							case 4: nz = z + 1; break; // +Z
							case 5: nz = z - 1; break; // -Z
						}

						// Out of bounds = exposed face (edge of chunk)
						var exposed =
							nx < 0 || nx >= Settings.ChunkSize ||
							ny < 0 || ny >= Settings.WorldHeight ||
							nz < 0 || nz >= Settings.ChunkSize;

						// If not out of bounds, check neighbor
						if (!exposed)
						{
							var nIndex = nx + (ny * Settings.ChunkSize) + (nz * Settings.ChunkSize * Settings.WorldHeight);
							exposed = (Voxels[nIndex] == (byte)VoxelType.Air) || (Voxels[nIndex] == (byte)VoxelType.Water);
						}

						if (!exposed) continue;
						var vStart = Vertices.Length;


						// Add vertices for this face
						for (var i = 0; i < 4; i++)
						{
							var v = (new Vector3(x, y, z) + FaceVertices[f * 4 + i]) * Settings.VoxelSize;

							Vertices.Add(v);
							Normals.Add(FaceNormals[f]);
						}

						// Add triangles (indices)
						for (var t = 0; t < 6; t++)
							Triangles.Add(vStart + FaceTriangles[f * 6 + t]);


						var tileIndex = f switch
						{
							2 => voxelTextures.top,
							3 => voxelTextures.bottom,
							_ => voxelTextures.side
						};

						var uvCoord = Texture.GetTileUVCoords(tileIndex.x, tileIndex.y);

						for (var i = 0; i < 4; i++)
						{
							var uvIndex = FaceUVs[f * 4 + i];

							var u = uvIndex is 0 or 3 ? uvCoord.uMin : uvCoord.uMax;
							var v = uvIndex is 0 or 1 ? uvCoord.vMin : uvCoord.vMax;

							UVs.Add(new Vector2(u, v));
						}
					}
				}
			}
		}

		public static JobHandle Schedule(Settings settings, Context context, JobHandle voxelHandle)
		{
			var job = new MeshGenJob()
			{
				Settings = settings,
				Voxels = context.Voxels,
				FaceNormals = context.FaceNormals,
				FaceTriangles = context.FaceTriangles,
				FaceVertices = context.FaceVertices,
				Normals = context.Normals,
				Triangles = context.Triangles,
				UVs = context.UVs,
				Vertices = context.Vertices,
				FaceUVs = context.FaceUVs,
			};

			return job.ScheduleByRef(voxelHandle);
		}
	}
}