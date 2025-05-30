using System;
using System.Collections.Generic;
using System.Linq;
using Terrain;
using Unity.Mathematics;
using UnityEngine;

public class TerrainController : MonoBehaviour
{
	[Header("References")] public Transform PlayerTransform;
	public Material TerrainMaterial;
	public Material WaterMaterial;

	[Header("Chunk settings")] public int ChunkSize = 16;
	public int RenderDistance = 1;
	public int WorldHeight = 196;
	public int MaxChunksPerFrame = 2;

	[Header("Generation settings")] public float Scale = 2_000f;
	public float TreeDensityScale = 50f;
	public float TreeDensityThreshold = 0.5f;
	public int HaloThickness = 2;
	public float TreeChance = 0.3f;
	public long Seed = 458834;
	public int VoxelSize = 2;

	private Dictionary<Vector2Int, Chunk> _chunks = new();
	private HashSet<Vector2Int> _loadedChunks = new();
	private HashSet<Vector2Int> _wantedChunks = new();

	private Queue<Vector2Int> _generationQueue = new();

	private Vector2Int _lastPlayerChunk = new(int.MinValue, int.MinValue);

	private int _chunkShiftAmount;
	private Settings _settings;

	private static bool IsPowerOfTwo(ulong x)
	{
		return (x & (x - 1)) == 0;
	}

	// fast bit-twiddling
	private static int Log2(uint value)
	{
		var r = 0;
		while (value > 1)
		{
			value >>= 1;
			r++;
		}

		return r;
	}

	private void Start()
	{
		_settings.VoxelSize = VoxelSize;
		_settings.ChunkSize = ChunkSize;
		_settings.TreeDensityScale = TreeDensityScale;
		_settings.TreeDensityThreshold = TreeDensityThreshold;
		_settings.TreeChance = TreeChance;
		_settings.HaloThickness = HaloThickness;
		_settings.Scale = Scale;
		_settings.Seed = Seed;
		_settings.WorldHeight = WorldHeight;

		var chunkUnitSize = _settings.ChunkSize * _settings.VoxelSize;

		if (!IsPowerOfTwo((ulong)chunkUnitSize))
		{
			Debug.LogError("Effective chunk size must be a power of two for bitwise operations!");
			_chunkShiftAmount = -1; // Sentinel value to indicate no bitwise op
		}
		else
		{
			_chunkShiftAmount = Log2((uint)chunkUnitSize);
		}
	}

	private Vector2Int WorldToChunkPosition(Vector3 worldPosition)
	{
		if (_chunkShiftAmount != -1)
		{
			return new Vector2Int(
				(int)worldPosition.x >> _chunkShiftAmount,
				(int)worldPosition.z >> _chunkShiftAmount
			);
		}

		return new Vector2Int(
			Mathf.FloorToInt(worldPosition.x / (_settings.ChunkSize * _settings.VoxelSize)),
			Mathf.FloorToInt(worldPosition.z / (_settings.ChunkSize * _settings.VoxelSize))
		);
	}

	private void Update()
	{
		var playerChunkPosition = WorldToChunkPosition(PlayerTransform.position);

		if (playerChunkPosition != _lastPlayerChunk)
		{
			_lastPlayerChunk = playerChunkPosition;
			_wantedChunks.Clear();


			for (var deltaZ = -RenderDistance; deltaZ <= RenderDistance; deltaZ++)
			for (var deltaX = -RenderDistance; deltaX <= RenderDistance; deltaX++)
			{
				_wantedChunks.Add(new Vector2Int(playerChunkPosition.x + deltaX, playerChunkPosition.y + deltaZ));
			}

			var toLoad = _wantedChunks.Except(_loadedChunks).ToList();
			var toUnload = _loadedChunks.Except(_wantedChunks).ToList();

			toLoad.Sort((a, b) =>
			{
				float da = (a - playerChunkPosition).sqrMagnitude;
				float db = (b - playerChunkPosition).sqrMagnitude;

				return da.CompareTo(db);
			});

			_generationQueue.Clear();

			foreach (var position in toLoad)
			{
				_generationQueue.Enqueue(position);
			}

			foreach (var position in toUnload)
			{
				var chunk = _chunks[position];
				chunk.Destroy();

				_chunks.Remove(position);
				_loadedChunks.Remove(position);
			}
		}

		var chunksThisFrame = 0;
		while (_generationQueue.Count > 0 && chunksThisFrame < MaxChunksPerFrame)
		{
			var position = _generationQueue.Dequeue();
			if (_chunks.ContainsKey(position)) continue;

			var chunk = new Chunk(_settings, TerrainMaterial, WaterMaterial, position);

			_loadedChunks.Add(position);
			_chunks.Add(position, chunk);

			chunk.Process();
			chunksThisFrame++;
		}
	}
}