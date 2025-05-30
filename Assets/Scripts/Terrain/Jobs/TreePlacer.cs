using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Terrain.Jobs
{
	public static class TreePlacer
	{
		[BurstCompile]
		private struct TreePlacerJob : IJob
		{
			public NativeArray<byte> Voxels;
			[ReadOnly] public Settings Settings;

			[ReadOnly] public Vector2Int ChunkPosition;

			private int GetIndex(int x, int y, int z)
				=> x + (y * Settings.ChunkSize) + (z * Settings.ChunkSize * Settings.WorldHeight);

			public void Execute()
			{
				for (var z = 0; z < Settings.ChunkSize; z++)
				for (var y = 0; y < Settings.WorldHeight; y++)
				for (var x = 0; x < Settings.ChunkSize; x++)
				{
					var globalX = x + ChunkPosition.x;
					var globalZ = z + ChunkPosition.y;

					var index = GetIndex(x, y, z);
					var voxel = (VoxelType)Voxels[index];

					if (voxel != VoxelType.PlaceTree) continue;

					var rand = new Unity.Mathematics.Random((uint)(Settings.Seed + globalX +
					                                               globalZ));

					var trunkHeight = rand.NextInt(4, 7);


					for (var i = 0; i < trunkHeight; i++)
					{
						var idx = x + ((y + i) * Settings.ChunkSize) + (z * Settings.ChunkSize * Settings.WorldHeight);
						if ((y + i) < Settings.WorldHeight)
							Voxels[idx] = (byte)VoxelType.Log;
					}

					var leafRadius = rand.NextInt(2, 3);
					//var leafRadius = 2 + ((hash / 10) % 2); // 2-3 radius
					var cy = y + trunkHeight;

					for (var dx = -leafRadius; dx <= leafRadius; dx++)
					for (var dz = -leafRadius; dz <= leafRadius; dz++)
					for (var dy = -leafRadius; dy <= leafRadius; dy++)
					{
						int lx = x + dx, ly = cy + dy, lz = z + dz;
						if (lx < 0 || lx >= Settings.ChunkSize || lz < 0 || lz >= Settings.ChunkSize || ly < 0 ||
						    ly >= Settings.WorldHeight) continue;

						var dist = math.sqrt(dx * dx + dy * dy + dz * dz);
						var leafHash = Settings.Seed + globalX +
						               globalZ;
						var skip = (leafHash % 5 == 0) && dist > 1.3f;

						if (dist <= leafRadius + 0.25f && !skip)
						{
							var lidx = GetIndex(lx, ly, lz);
							if (Voxels[lidx] == (byte)VoxelType.Air)
								Voxels[lidx] = (byte)VoxelType.Leaves;
						}
					}
				}
			}
		}

		public static JobHandle Schedule(Settings settings, Context context, Vector2Int chunkPosition,
			JobHandle voxelHandle)
		{
			var job = new TreePlacerJob()
			{
				ChunkPosition = chunkPosition,
				Voxels = context.Voxels,
				Settings = settings,
			};

			return job.ScheduleByRef(voxelHandle);
		}
	}
}