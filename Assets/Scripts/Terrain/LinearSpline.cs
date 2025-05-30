using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Terrain
{
	[BurstCompile]
	public struct LinearSpline
	{
		[ReadOnly] private NativeArray<float2> _points;

		public LinearSpline(NativeArray<float2> points)
		{
			_points = points;
		}

		[BurstCompile]
		public float Evaluate(float x)
		{
			var count = _points.Length;

			if (count == 0) return 0f;

			if (x <= _points[0].x) return _points[0].y;
			if (x >= _points[count - 1].x) return _points[count - 1].y;

			// Binary search for a correct interval
			var lo = 0;
			var hi = count - 1;

			while (lo <= hi)
			{
				var mid = (lo + hi) >> 1;
				var midX = _points[mid].x;

				if (x < midX)
					hi = mid - 1;
				else if (x > midX)
					lo = mid + 1;
				else
					return _points[mid].y;
			}

			var i0 = math.max(0, lo - 1);
			var i1 = lo;

			var p0 = _points[i0];
			var p1 = _points[i1];

			var t = math.saturate((x - p0.x) / (p1.x - p0.x));
			return math.lerp(p0.y, p1.y, t);
		}
	}
}