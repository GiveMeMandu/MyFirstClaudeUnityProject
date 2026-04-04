using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace ProjectSun.V2.Defense.ECS
{
    /// <summary>
    /// 그리드 기반 플로우 필드 데이터.
    ///
    /// 3단계 파이프라인:
    ///   CostField (장애물 정의) → IntegrationField (BFS 비용 전파) → DirectionField (이동 방향)
    ///
    /// 지상/공중 이중 필드:
    ///   - 지상: 방벽+건물 = 장애물(255). 대부분의 적이 사용.
    ///   - 공중: 건물만 장애물, 방벽은 통과 가능(비용 1). Flying 적 전용.
    ///
    /// 메모리 사용량 (100x100 그리드 기준):
    ///   CostField(byte) x2 = 20KB
    ///   IntegrationField(ushort) x2 = 40KB
    ///   DirectionField(byte) x2 = 20KB
    ///   합계: ~80KB
    /// </summary>
    public struct FlowFieldGrid : IDisposable
    {
        public int Width;
        public int Height;
        public float CellSize;
        public float InvCellSize;
        public float3 WorldOrigin;

        // --- 지상 필드 (Ground) ---
        /// <summary>셀 이동 비용. 1=기본, 255=장애물(건물+방벽)</summary>
        public NativeArray<byte> CostField;
        /// <summary>목표까지 누적 비용. ushort.MaxValue=미도달. BFS로 계산.</summary>
        public NativeArray<ushort> IntegrationField;
        /// <summary>이동 방향. 8방향 인덱스(0~7), 254=Goal, 255=None.</summary>
        public NativeArray<byte> DirectionField;

        // --- 공중 필드 (Air) ---
        /// <summary>공중 비용. 건물만 장애물, 방벽은 비용 1.</summary>
        public NativeArray<byte> AirCostField;
        public NativeArray<ushort> AirIntegrationField;
        public NativeArray<byte> AirDirectionField;

        public bool IsCreated => CostField.IsCreated;
        public int CellCount => Width * Height;

        public FlowFieldGrid(int width, int height, float cellSize, float3 worldOrigin, Allocator allocator)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            InvCellSize = 1f / cellSize;
            WorldOrigin = worldOrigin;

            int total = width * height;
            CostField = new NativeArray<byte>(total, allocator);
            IntegrationField = new NativeArray<ushort>(total, allocator);
            DirectionField = new NativeArray<byte>(total, allocator);
            AirCostField = new NativeArray<byte>(total, allocator);
            AirIntegrationField = new NativeArray<ushort>(total, allocator);
            AirDirectionField = new NativeArray<byte>(total, allocator);
        }

        /// <summary>월드 좌표 → 그리드 좌표 (x, z). 범위 클램프 없음.</summary>
        public int2 WorldToCoord(float3 worldPos)
        {
            return new int2(
                (int)math.floor((worldPos.x - WorldOrigin.x) * InvCellSize),
                (int)math.floor((worldPos.z - WorldOrigin.z) * InvCellSize)
            );
        }

        /// <summary>월드 좌표 → 1D 인덱스. 범위 밖이면 -1.</summary>
        public int WorldToIndex(float3 worldPos)
        {
            int2 c = WorldToCoord(worldPos);
            if (c.x < 0 || c.x >= Width || c.y < 0 || c.y >= Height) return -1;
            return c.y * Width + c.x;
        }

        public int CoordToIndex(int x, int z) => z * Width + x;
        public int2 IndexToCoord(int index) => new int2(index % Width, index / Width);

        /// <summary>셀 중심 월드 좌표</summary>
        public float3 IndexToWorld(int index)
        {
            int2 c = IndexToCoord(index);
            return new float3(
                WorldOrigin.x + (c.x + 0.5f) * CellSize,
                0f,
                WorldOrigin.z + (c.y + 0.5f) * CellSize
            );
        }

        public bool InBounds(int x, int z) => x >= 0 && x < Width && z >= 0 && z < Height;

        public void Dispose()
        {
            if (CostField.IsCreated) CostField.Dispose();
            if (IntegrationField.IsCreated) IntegrationField.Dispose();
            if (DirectionField.IsCreated) DirectionField.Dispose();
            if (AirCostField.IsCreated) AirCostField.Dispose();
            if (AirIntegrationField.IsCreated) AirIntegrationField.Dispose();
            if (AirDirectionField.IsCreated) AirDirectionField.Dispose();
        }
    }

    /// <summary>
    /// 8방향 인코딩 상수 + Burst 호환 방향 LUT.
    ///
    /// DirectionField 값: 0~7 = 8방향, 254 = Goal, 255 = None(장애물/미도달)
    /// </summary>
    public static class FlowDir
    {
        public const byte N  = 0; // +Z
        public const byte NE = 1;
        public const byte E  = 2; // +X
        public const byte SE = 3;
        public const byte S  = 4; // -Z
        public const byte SW = 5;
        public const byte W  = 6; // -X
        public const byte NW = 7;
        public const byte Goal = 254;
        public const byte None = 255;

        // 8방향 이웃 오프셋 (dx, dz)
        // 인덱스와 동일 순서: N, NE, E, SE, S, SW, W, NW
        public static readonly int2[] Offsets =
        {
            new int2( 0,  1), // N
            new int2( 1,  1), // NE
            new int2( 1,  0), // E
            new int2( 1, -1), // SE
            new int2( 0, -1), // S
            new int2(-1, -1), // SW
            new int2(-1,  0), // W
            new int2(-1,  1), // NW
        };

        /// <summary>
        /// 256-entry LUT: dirIndex → 정규화된 float3 방향.
        /// 0~7=방향 벡터, 254/255=zero. Persistent 할당.
        /// Burst에서 안전하게 NativeArray로 참조.
        /// </summary>
        public static NativeArray<float3> CreateLUT(Allocator allocator)
        {
            var lut = new NativeArray<float3>(256, allocator);
            float diag = math.rsqrt(2f); // 1/sqrt(2) ≈ 0.7071

            lut[N]  = new float3( 0, 0,  1);
            lut[NE] = new float3( diag, 0,  diag);
            lut[E]  = new float3( 1, 0,  0);
            lut[SE] = new float3( diag, 0, -diag);
            lut[S]  = new float3( 0, 0, -1);
            lut[SW] = new float3(-diag, 0, -diag);
            lut[W]  = new float3(-1, 0,  0);
            lut[NW] = new float3(-diag, 0,  diag);
            lut[Goal] = float3.zero;
            lut[None] = float3.zero;

            return lut;
        }
    }
}
