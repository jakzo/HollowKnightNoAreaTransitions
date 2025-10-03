namespace HollowKnightNoAreaTransitions;

public static class TilemapUtils
{
    public static int CHUNK_SIZE = 32;

    public static void UpdateTilemapMask(EdgeCollider2D[] colliders)
    {
        if (colliders.Length == 0)
            return;

        var verticalEdges = new bool[CHUNK_SIZE + 1, CHUNK_SIZE];
        foreach (var collider in colliders)
        {
            var points = collider.points;
            for (int i = 0; i < points.Length; i++)
            {
                var p0 = points[i];
                var p1 = points[(i + 1) % points.Length];
                if (!Mathf.Approximately(p0.x, p1.x))
                    continue;

                var x = Mathf.RoundToInt(p0.x);
                var p0y = Mathf.RoundToInt(p0.y);
                var p1y = Mathf.RoundToInt(p1.y);
                var (y0, y1) = p0.y < p1.y ? (p0y, p1y) : (p1y, p0y);
                for (int y = y0; y < y1; y++)
                    verticalEdges[x, y] = true;
            }
        }

        var occupied = new bool[CHUNK_SIZE, CHUNK_SIZE];
        for (int y = 0; y < CHUNK_SIZE; y++)
        {
            var isOccupied = false;
            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                if (verticalEdges[x, y])
                    isOccupied = !isOccupied;
                if (isOccupied)
                    occupied[x, y] = true;
            }
        }

        var meshFilter = colliders[0].GetComponent<MeshFilter>();
        var triangles = meshFilter.mesh.triangles;
        var vertices = meshFilter.mesh.vertices;
        var updatedTriangles = new List<int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var t0 = triangles[i];
            var t1 = triangles[i + 1];
            var t2 = triangles[i + 2];
            var v0 = vertices[t0];
            var v1 = vertices[t1];
            var v2 = vertices[t2];

            var minX = Mathf.RoundToInt(Mathf.Min(v0.x, Mathf.Min(v1.x, v2.x)));
            var minY = Mathf.RoundToInt(Mathf.Min(v0.y, Mathf.Min(v1.y, v2.y)));

            if (!occupied[minX, minY])
                continue;

            updatedTriangles.Add(t0);
            updatedTriangles.Add(t1);
            updatedTriangles.Add(t2);
        }

        if (updatedTriangles.Count == triangles.Length)
            return;

        meshFilter.mesh.triangles = updatedTriangles.ToArray();
        meshFilter.mesh.RecalculateNormals();
    }

    public static void UpdateTilemapPoints(
        Scene scene,
        string[] tilemapPath,
        int colliderIndex,
        Func<Vector2[], Vector2[]> updatePoints
    )
    {
        var go = Utils.FindGameObjectByPath(scene, tilemapPath);
        var colliders = go.GetComponents<EdgeCollider2D>();
        var collider = colliders[colliderIndex];
        collider.points = updatePoints(collider.points);
        UpdateTilemapMask(colliders);
    }

    public static void ClampColliderPoints(
        Scene scene,
        string[] path,
        Func<Vector2, Vector2> clampPoint
    )
    {
        var collider = Utils.FindGameObjectByPath(scene, path).GetComponent<PolygonCollider2D>();
        collider.points = collider.points.Select(clampPoint).ToArray();
    }

    public static void ClampEdgeColliderPoints(
        Scene scene,
        string[] path,
        int index,
        Func<Vector2, Vector2> clampPoint
    )
    {
        var collider = Utils.FindGameObjectByPath(scene, path).GetComponents<EdgeCollider2D>()[
            index
        ];
        collider.points = collider.points.Select(clampPoint).ToArray();
    }

    private const int TILE_EMPTY = -1;
    private static readonly (int dx, int dy)[] DIRECTIONS = [(0, -1), (1, 0), (0, 1), (-1, 0)];

    public static (
        bool[,] lookupTable,
        List<Vector3[]> perimeters,
        Rect bounds
    ) CalculatePlayableArea(tk2dTileMap tilemap)
    {
        Assert.IsTrue(tilemap.Layers.Length == 1);
        var layer = tilemap.Layers[0];
        var playableLookupTable = new bool[layer.width, layer.height];
        var playablePerimeters = new List<Vector3[]>();
        var transitionTiles = new HashSet<(int x, int y)>();
        var minX = layer.width;
        var minY = layer.height;
        var maxX = 0;
        var maxY = 0;

        bool IsOutOfBounds(int x, int y) => x < 0 || x >= layer.width || y < 0 || y >= layer.height;
        bool IsChunkTile(int x, int y) => layer.GetTile(x, y) != TILE_EMPTY;
        bool IsTileOrTransition(int x, int y) =>
            transitionTiles.Contains((x, y)) || IsChunkTile(x, y);
        bool IsTiledOrOutOfBounds(int x, int y) => IsOutOfBounds(x, y) || IsChunkTile(x, y);
        bool IsNonEmpty(int x, int y) => IsOutOfBounds(x, y) || IsTileOrTransition(x, y);
        bool IsNonEmptyOrVisited(int x, int y) =>
            IsOutOfBounds(x, y) || playableLookupTable[x, y] || IsTileOrTransition(x, y);

        void FloodFillAsPlayable(int startX, int startY, Func<int, int, bool> isOccupied = null)
        {
            isOccupied ??= ((x, y) => false);

            var stack = new Stack<(int, int)>();
            stack.Push((startX, startY));
            while (stack.Count > 0)
            {
                var (cx, cy) = stack.Pop();
                if (isOccupied(cx, cy) || playableLookupTable[cx, cy])
                    continue;
                playableLookupTable[cx, cy] = true;
                foreach (var (dx, dy) in DIRECTIONS)
                    stack.Push((cx + dx, cy + dy));
            }
        }

        // Mark transition point tiles as non-playable
        foreach (var rootObj in tilemap.gameObject.scene.GetRootGameObjects())
        {
            var transitionPoints = rootObj
                .GetComponents<TransitionPoint>()
                .Concat(rootObj.GetComponentsInChildren<TransitionPoint>(true));
            foreach (var tp in transitionPoints)
            {
                if (tp.PromptMarker != null)
                    continue;

                var collider = Utils.GetTransitionPointBoxCollider(tp);
                if (collider == null)
                    continue;

                var min = collider.bounds.min - tilemap.transform.position;
                var max = collider.bounds.max - tilemap.transform.position;

                // Some transitions have gaps between them and the tilemap colliders so extend them
                const int MAX_TRANSITION_GAP = 1;
                var startX = Mathf.FloorToInt(min.x);
                var endX = Mathf.CeilToInt(max.x);
                var startY = Mathf.FloorToInt(min.y);
                var endY = Mathf.CeilToInt(max.y);
                var isDoorway = collider.size.x < collider.size.y;
                if (isDoorway)
                {
                    int midX = Mathf.FloorToInt((min.x + max.x) / 2);
                    for (int i = 0; i < MAX_TRANSITION_GAP; i++)
                        if (IsTiledOrOutOfBounds(startY, midX))
                            break;
                        else
                            startY--;
                    for (int i = 0; i < MAX_TRANSITION_GAP; i++)
                        if (IsTiledOrOutOfBounds(endY, midX))
                            break;
                        else
                            endY++;
                }
                else
                {
                    int midY = Mathf.FloorToInt((min.y + max.y) / 2);
                    for (int i = 0; i < MAX_TRANSITION_GAP; i++)
                        if (IsTiledOrOutOfBounds(startX, midY))
                            break;
                        else
                            startX--;
                    for (int i = 0; i < MAX_TRANSITION_GAP; i++)
                        if (IsTiledOrOutOfBounds(endX, midY))
                            break;
                        else
                            endX++;
                }

                for (int x = Mathf.FloorToInt(min.x); x <= Mathf.CeilToInt(max.x); x++)
                for (int y = Mathf.FloorToInt(min.y); y <= Mathf.CeilToInt(max.y); y++)
                    transitionTiles.Add((x, y));

                var dir = Utils.GetTransitionPointDirection(tp);
                if (dir == Utils.Direction.None)
                {
                    Logger.Warning(
                        $"Could not determine side of transition {tp.name} in scene {tp.gameObject.scene.name}"
                    );
                    continue;
                }

                if (dir == Utils.Direction.Left || dir == Utils.Direction.Right)
                {
                    var x =
                        dir == Utils.Direction.Right
                            ? Mathf.CeilToInt(max.x) + 1
                            : Mathf.FloorToInt(min.x) - 1;
                    for (int y = startY; y <= endY; y++)
                        FloodFillAsPlayable(x, y, IsNonEmpty);
                }
                else
                {
                    var y =
                        dir == Utils.Direction.Up
                            ? Mathf.CeilToInt(max.y) + 1
                            : Mathf.FloorToInt(min.y) - 1;
                    for (int x = startX; x <= endX; x++)
                        FloodFillAsPlayable(x, y, IsNonEmpty);
                }
            }
        }

        for (int startY = 0; startY < layer.height; startY++)
        {
            for (int startX = 0; startX < layer.width; startX++)
            {
                if (IsNonEmptyOrVisited(startX, startY))
                    continue;

                // Trace perimeter by following non-empty tile boundary anti-clockwise
                var perimeter = new List<(int x, int y)>();
                var x = startX;
                var y = startY;
                var dirIndex = 0;

                do
                {
                    perimeter.Add((x, y));
                    playableLookupTable[x, y] = true;
                    if (x < minX)
                        minX = x;
                    if (x > maxX)
                        maxX = x;
                    if (y < minY)
                        minY = y;
                    if (y > maxY)
                        maxY = y;

                    for (int i = 0; i < DIRECTIONS.Length; i++)
                    {
                        var (dx, dy) = DIRECTIONS[(dirIndex + i) % DIRECTIONS.Length];
                        var newX = x + dx;
                        var newY = y + dy;
                        if (IsNonEmpty(newX, newY))
                            continue;

                        x = newX;
                        y = newY;
                        dirIndex = (dirIndex + i + DIRECTIONS.Length - 1) % DIRECTIONS.Length;
                        break;
                    }
                } while (!(x == startX && y == startY));

                // Find a point inside the perimeter by checking for an empty adjacent point
                var innerX = startX;
                var innerY = startY;
                foreach (var (px, py) in perimeter)
                {
                    foreach (var (dx, dy) in DIRECTIONS)
                    {
                        var checkX = px + dx;
                        var checkY = py + dy;
                        if (IsNonEmptyOrVisited(checkX, checkY))
                            continue;
                        innerX = checkX;
                        innerY = checkY;
                        break;
                    }
                }

                FloodFillAsPlayable(innerX, innerY);

                var withDiagonals = AddDiagonals(perimeter);
                if (withDiagonals.Count < 3)
                    continue; // ignore tiny areas

                var simplifiedPerimeter = SimplifyPerimeter(withDiagonals);
                var perimeterVectors = simplifiedPerimeter
                    .Concat(simplifiedPerimeter.Take(1)) // close the loop
                    .Select(p => new Vector3(p.x + 0.5f, p.y + 0.5f, 0f))
                    .ToArray();
                playablePerimeters.Add(perimeterVectors);
            }
        }

        // TODO: Remove playable areas on the wrong side of transitions

        var bounds = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);

        return (playableLookupTable, playablePerimeters, bounds);
    }

    private static List<(int x, int y)> AddDiagonals(List<(int x, int y)> perimeter)
    {
        var withDiagonals = new List<(int x, int y)>();
        for (int i = 0; i < perimeter.Count; i++)
        {
            var p0 = perimeter[i];
            var p1 = perimeter[(i + 1) % perimeter.Count];
            var p2 = perimeter[(i + 2) % perimeter.Count];
            if (Math.Abs(p0.x - p2.x) != 1 || Math.Abs(p0.y - p2.y) != 1)
                withDiagonals.Add(p1);
        }
        return withDiagonals;
    }

    private static List<(int x, int y)> SimplifyPerimeter(List<(int x, int y)> perimeter)
    {
        var simplified = new List<(int x, int y)>();
        var curr = perimeter[0];
        var next = perimeter[1];
        var direction = (next.x - curr.x, next.y - curr.y);
        simplified.Add(curr); // first point is always at a corner so will always be kept

        for (int i = 1; i <= perimeter.Count; i++)
        {
            next = perimeter[i % perimeter.Count];
            var newDirection = (next.x - curr.x, next.y - curr.y);
            if (newDirection != direction)
            {
                simplified.Add(curr);
                direction = newDirection;
            }
            curr = next;
        }

        return simplified;
    }
}
