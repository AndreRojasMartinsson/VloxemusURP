namespace Terrain
{
	public enum VoxelType
	{
		Air = 0,
		Grass = 1,
		Dirt = 2,
		Stone = 3,
		Water = 4,
		Sand = 5,
		PlaceTree = 6,
		Log = 7,
		Leaves = 8,
	}

	public enum VoxelMeshType
	{
		Opaque,
		Water
	}

	public struct VoxelFaceTextures
	{
		public int Top, Bottom, Side;
	}
}