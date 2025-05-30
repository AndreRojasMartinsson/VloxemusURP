using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using static Unity.Mathematics.math;
using UnityEngine;

namespace Terrain.Jobs
{
	public static class Noise
	{
		[BurstCompile]
		private struct NoiseJob : IJobParallelFor
		{
			[ReadOnly] public LinearSpline Spline;
			[WriteOnly] public NativeArray<int> Result;

			[ReadOnly] public Vector2Int ChunkPosition;
			[ReadOnly] public Settings Settings;

			private const int Octaves = 6;
			private const float Persistence = 0.5f;
			private const float Lacunarity = 2.0f;

			public void Execute(int index)
			{
				var x = index % Settings.ChunkSize;
				var y = index / Settings.ChunkSize;


				var worldX = ((ChunkPosition.x * Settings.ChunkSize + x) * Settings.VoxelSize) + Settings.Seed;
				var worldY = ((ChunkPosition.y * Settings.ChunkSize + y) * Settings.VoxelSize) + (Settings.Seed / 2);

				var amplitude = 1f;
				var frequency = 1f;
				var value = 0f;

				for (var i = 0; i < Octaves; i++)
				{
					var sampleX = (worldX / Settings.Scale) * frequency;
					var sampleY = (worldY / Settings.Scale) * frequency;

					var result = noise.snoise(new float2(sampleX, sampleY));

					value += result * amplitude;

					amplitude *= Persistence;
					frequency *= Lacunarity;
				}

				var surfaceHeight = Spline.Evaluate(value);
				Result[index] = Mathf.CeilToInt(surfaceHeight);
			}
		}

		public static JobHandle Schedule(Settings settings, Context context, Vector2Int chunkPosition)
		{
			var spline = new LinearSpline(context.SplinePoints);
			var job = new NoiseJob()
			{
				Spline = spline,
				Result = context.HeightMap,
				Settings = settings,
				ChunkPosition = chunkPosition,
			};


			return job.ScheduleByRef(context.HeightMap.Length, 128);
		}
	}
}