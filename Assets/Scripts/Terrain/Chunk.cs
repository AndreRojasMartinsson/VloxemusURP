using Terrain.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;

namespace Terrain
{
	public class Chunk
	{
		private readonly Settings _settings;

		private readonly Vector2Int _chunkPosition;

		private readonly GameObject _object;
		private readonly MeshFilter _mf;
		private readonly MeshRenderer _mr;
		private readonly Mesh _mesh;

		private readonly GameObject _waterObject;
		private readonly MeshFilter _waterMf;
		private readonly MeshRenderer _waterMr;
		private readonly Mesh _waterMesh;

		private readonly Material _terrainMaterial;
		private readonly Material _waterMaterial;

		public Chunk(Settings settings, Material terrainMaterial, Material waterMaterial, Vector2Int chunkPosition)
		{
			_terrainMaterial = terrainMaterial;
			_waterMaterial = waterMaterial;
			_settings = settings;
			_chunkPosition = chunkPosition;

			_object = new GameObject("Chunk");
			_object.transform.position = new Vector3(chunkPosition.x * settings.ChunkSize * settings.VoxelSize, 0,
				chunkPosition.y * settings.ChunkSize * settings.VoxelSize);

			// Create required components for mesh to render
			_mf = _object.AddComponent<MeshFilter>();
			_mr = _object.AddComponent<MeshRenderer>();
			_mesh = new Mesh { indexFormat = IndexFormat.UInt32 };

			_waterObject = new GameObject("Water");
			_waterObject.transform.position = new Vector3(chunkPosition.x * settings.ChunkSize * settings.VoxelSize, -0.35f,
				chunkPosition.y * settings.ChunkSize * settings.VoxelSize);

			// Create required components for mesh to render
			_waterMf = _waterObject.AddComponent<MeshFilter>();
			_waterMr = _waterObject.AddComponent<MeshRenderer>();
			_waterMesh = new Mesh { indexFormat = IndexFormat.UInt32 };
		}

		public void Process()
		{
			var context = new Context(_settings);
			var noiseHandle = Noise.Schedule(_settings, context, _chunkPosition);
			var voxelHandle = Voxel.Schedule(_settings, context, noiseHandle);
			var terrainMeshHandle = MeshGen.Schedule(_settings, context, voxelHandle);
			var waterMeshHandle = WaterMeshGen.Schedule(_settings, context, terrainMeshHandle);

			waterMeshHandle.Complete();

			_mesh.Clear();

			_mesh.SetVertices(context.Vertices.AsArray());
			_mesh.SetNormals(context.Normals.AsArray());
			_mesh.SetTriangles(context.Triangles.AsArray().ToArray(), 0);
			_mesh.SetUVs(0, context.UVs.AsArray());

			_mesh.RecalculateBounds();
			_mesh.RecalculateTangents();

			_mf.sharedMesh = _mesh;

			if (_mr.sharedMaterial != null &&
			    _mr.sharedMaterial.shader.name != "Hidden/InternalErrorShader") return;

			_mr.sharedMaterial = _terrainMaterial;

			_waterMesh.Clear();
			_waterMesh.SetVertices(context.WaterVertices.AsArray());
			_waterMesh.SetNormals(context.WaterNormals.AsArray());
			_waterMesh.SetTriangles(context.WaterTriangles.AsArray().ToArray(), 0);
			_waterMesh.SetUVs(0, context.WaterUVs.AsArray());

			_waterMesh.RecalculateBounds();

			_waterMf.sharedMesh = _waterMesh;


			if (_waterMr.sharedMaterial != null &&
			    _waterMr.sharedMaterial.shader.name != "Hidden/InternalErrorShader") return;

			_waterMr.sharedMaterial = _waterMaterial;
			_waterMr.shadowCastingMode = ShadowCastingMode.Off;


			context.Dispose();
		}


		public void Destroy()
		{
			_mesh.Clear();
			_waterMesh.Clear();
			Object.Destroy(_object);
			Object.Destroy(_waterObject);
		}
	}
}