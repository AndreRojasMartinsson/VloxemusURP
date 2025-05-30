using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Terrain.Jobs
{
	public static class Voxel
	{
		[BurstCompile]
		private struct VoxelJob : IJobParallelFor
		{
			[ReadOnly] public NativeArray<int> Noise;
			[ReadOnly] public Settings Settings;
			[WriteOnly] public NativeArray<byte> Result;

			public void Execute(int index)
			{
				var x = index % Settings.ChunkSize;
				var y = (index / Settings.ChunkSize) % Settings.WorldHeight;
				var z = index / (Settings.ChunkSize * Settings.WorldHeight);

				var surfaceHeight = Noise[z * Settings.ChunkSize + x];


				if (y == surfaceHeight)
					Result[index] = (byte)(y < 10 ? VoxelType.Sand : VoxelType.Grass);
				else if (y > surfaceHeight)
					Result[index] = (byte)(y < 15 ? VoxelType.Water : VoxelType.Air);
				else if (y < surfaceHeight - 3)
					Result[index] = (byte)VoxelType.Stone;
				else
					Result[index] = (byte)VoxelType.Dirt;
			}
		}

		public static JobHandle Schedule(Settings settings, Context context,
			JobHandle voxelHandle)
		{
			var job = new VoxelJob()
			{
				Result = context.Voxels,
				Noise = context.HeightMap,
				Settings = settings,
			};

			return job.ScheduleByRef(context.Voxels.Length, 64, voxelHandle);
		}
	}
}