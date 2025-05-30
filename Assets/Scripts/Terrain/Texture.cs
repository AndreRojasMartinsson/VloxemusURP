using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;

namespace Terrain
{
	[BurstCompile]
	public static class Texture
	{
		public struct UVCoord
		{
			public float uMin;
			public float vMin;

			public float uMax;
			public float vMax;
		}

		[ReadOnly] private const float TileSizePx = 32f;
		[ReadOnly] private const float AtlasSizePx = 512f;
		[ReadOnly] private const float TileUVSize = TileSizePx / AtlasSizePx;


		public static UVCoord GetTileUVCoords(int tileX, int tileY)
		{
			//const float padding = 0.1f / 512.0f;
			const float halfTexel = 1f / AtlasSizePx;

			var uMin = (tileX * TileUVSize) + halfTexel;
			var uMax = ((tileX + 1) * TileUVSize) - halfTexel;

			var vMin = (1f - (tileY + 1) * TileUVSize) + halfTexel;
			var vMax = (1f - tileY * TileUVSize) - halfTexel;

			return new UVCoord { uMax = uMax, uMin = uMin, vMax = vMax, vMin = vMin };
		}


		public static (int2 top, int2 bottom, int2 side) GetVoxelFaceTextureIndices(VoxelType voxel)
		{
			return voxel switch
			{
				VoxelType.Dirt => (int2(2, 0), int2(2, 0), int2(2, 0)),
				VoxelType.Stone => (int2(1, 0), int2(1, 0), int2(1, 0)),
				VoxelType.Grass => (int2(0, 0), int2(2, 0), int2(3, 0)),
				VoxelType.Sand => (int2(0, 1), int2(0, 1), int2(0, 1)),
				VoxelType.Log => (int2(2, 1), int2(1, 1), int2(1, 1)),
				VoxelType.Leaves => (int2(3, 1), int2(3, 1), int2(3, 1)),
				_ => (0, 0, 0)
			};
		}
	}
}