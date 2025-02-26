using Ces.Utilities;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Ces.Collections
{
    public readonly struct RawHeuristicGeo : IRawHeuristicable<int2>
    {
        readonly double3 _targetUnitSphere;
        readonly int2 _textureSize;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawHeuristicGeo(int2 targetPixelCoord, int2 textureSize)
        {
            var targetUv = GeoUtilitiesDouble.PixelCoordToPlaneUv(targetPixelCoord, textureSize);

            _targetUnitSphere = GeoUtilitiesDouble.PlaneUvToUnitSphere(targetUv);
            _textureSize = textureSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CalculateHeuristic(int2 item)
        {
            var uv = GeoUtilitiesDouble.PixelCoordToPlaneUv(item, _textureSize);
            var unitSphere = GeoUtilitiesDouble.PlaneUvToUnitSphere(uv);

            return GeoUtilitiesDouble.DistanceAtanFast(unitSphere, _targetUnitSphere);
        }
    }
}