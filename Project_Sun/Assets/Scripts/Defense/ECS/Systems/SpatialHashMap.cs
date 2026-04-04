using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace ProjectSun.Defense.ECS
{
    /// <summary>
    /// Burst-compatible spatial hash map for O(1) range queries.
    /// Divides the world into cells of fixed size. Each cell stores entity indices.
    /// Used by TowerAttackSystem to avoid O(T*E) brute-force search.
    ///
    /// All persistent containers are allocated once in the constructor.
    /// Build() uses only Allocator.Temp for transient work.
    /// </summary>
    public struct SpatialHashMap : System.IDisposable
    {
        public float CellSize;
        public float InvCellSize;

        // cellKey -> (startIndex, count) in SortedIndices
        public NativeParallelHashMap<int2, int2> CellRanges;

        // Flat array of entity indices, grouped by cell
        public NativeList<int> SortedIndices;

        public SpatialHashMap(float cellSize, int capacity, Allocator allocator)
        {
            CellSize = cellSize;
            InvCellSize = 1f / cellSize;
            CellRanges = new NativeParallelHashMap<int2, int2>(capacity / 4, allocator);
            SortedIndices = new NativeList<int>(capacity, allocator);
        }

        /// <summary>
        /// Build the spatial hash from an array of positions.
        /// Must be called once per frame before any queries.
        /// Uses only Allocator.Temp internally — safe from Burst.
        /// </summary>
        public void Build(NativeArray<float3> positions, int count)
        {
            CellRanges.Clear();
            SortedIndices.Clear();

            if (count == 0) return;

            // Step 1: Compute cell key per entity and count per cell
            var cellKeys = new NativeArray<int2>(count, Allocator.Temp);
            var cellCounts = new NativeParallelHashMap<int2, int>(count / 4 + 16, Allocator.Temp);

            for (int i = 0; i < count; i++)
            {
                var key = GetCellKey(positions[i]);
                cellKeys[i] = key;

                if (cellCounts.TryGetValue(key, out int existing))
                {
                    cellCounts[key] = existing + 1;
                }
                else
                {
                    cellCounts[key] = 1;
                }
            }

            // Step 2: Compute start offset for each cell
            var uniqueKeys = cellCounts.GetKeyArray(Allocator.Temp);
            int runningStart = 0;
            for (int i = 0; i < uniqueKeys.Length; i++)
            {
                int cellCount = cellCounts[uniqueKeys[i]];
                CellRanges[uniqueKeys[i]] = new int2(runningStart, 0);
                runningStart += cellCount;
            }

            // Step 3: Fill SortedIndices — place each entity into its cell's range
            SortedIndices.ResizeUninitialized(count);

            var writePos = new NativeParallelHashMap<int2, int>(uniqueKeys.Length, Allocator.Temp);
            for (int i = 0; i < uniqueKeys.Length; i++)
            {
                writePos[uniqueKeys[i]] = CellRanges[uniqueKeys[i]].x;
            }

            for (int i = 0; i < count; i++)
            {
                var key = cellKeys[i];
                int pos = writePos[key];
                SortedIndices[pos] = i;
                writePos[key] = pos + 1;

                var range = CellRanges[key];
                range.y++;
                CellRanges[key] = range;
            }

            // Allocator.Temp arrays are auto-freed at end of frame, but explicit dispose is cleaner
            cellKeys.Dispose();
            cellCounts.Dispose();
            uniqueKeys.Dispose();
            writePos.Dispose();
        }

        /// <summary>
        /// Query all entity indices within a square region centered at 'center' with half-extent 'range'.
        /// Results are appended to 'results'.
        /// </summary>
        public void QueryRange(float3 center, float range, NativeList<int> results)
        {
            int2 minCell = GetCellKey(center - new float3(range, 0, range));
            int2 maxCell = GetCellKey(center + new float3(range, 0, range));

            for (int cx = minCell.x; cx <= maxCell.x; cx++)
            {
                for (int cz = minCell.y; cz <= maxCell.y; cz++)
                {
                    var cellKey = new int2(cx, cz);
                    if (CellRanges.TryGetValue(cellKey, out int2 cellRange))
                    {
                        for (int i = cellRange.x; i < cellRange.x + cellRange.y; i++)
                        {
                            results.Add(SortedIndices[i]);
                        }
                    }
                }
            }
        }

        int2 GetCellKey(float3 position)
        {
            return new int2(
                (int)math.floor(position.x * InvCellSize),
                (int)math.floor(position.z * InvCellSize)
            );
        }

        public void Dispose()
        {
            if (CellRanges.IsCreated) CellRanges.Dispose();
            if (SortedIndices.IsCreated) SortedIndices.Dispose();
        }
    }
}
