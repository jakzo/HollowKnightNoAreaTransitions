#if DEBUG
using HollowKnightNoAreaTransitions;
using Logger = HollowKnightNoAreaTransitions.Logger;

// Useful commands to use in the UnityExplorer C# console:
/*
var pos = tk2dCamera.Instance.transform.position -
              HollowKnightNoAreaTransitions.SceneLoader.WORLD_OFFSET;
pos.z = 0f;
HKNAT.MoveChunk("Bone_01", pos + new Vector3(10f, 0f));

tk2dCamera.Instance.transform.position - HollowKnightNoAreaTransitions.SceneLoader.WORLD_OFFSET;
HKNAT.MoveChunk("Tut_02", new Vector3(0f, 0f));
HKNAT.LogAndResetChangedChunks();

HollowKnightNoAreaTransitions.HollowKnightNoAreaTransitionsMod.Instance.ChunkManager.ImmediatelyUnloadAllChunks();

HollowKnightNoAreaTransitions.SceneLoader.WORLD_OFFSET = new Vector3(200f, 200f, 0f);
*/

static class HKNAT
{
    // private static void Test()
    // {
    //     HollowKnightNoAreaTransitions
    //         .HollowKnightNoAreaTransitionsMod
    //         .Instance
    //         .ChunkManager
    //         .LoadedChunkStates
    //         .Count;
    // }

    public static HashSet<Chunk> ChangedChunks = [];

    private static int LAYER_TERRAIN;
    private static int LAYER_HERO_DETECTOR;

    public static void MoveChunk(string sceneName, Vector3 pos)
    {
        var chunkManager = HollowKnightNoAreaTransitionsMod.Instance.ChunkManager;
        chunkManager.CurrentMap.ChunkBySceneName.TryGetValue(sceneName, out var chunk);
        if (chunk == null)
        {
            Logger.Debug($"Creating chunk because it did not exist: {sceneName}");
            chunk = new Chunk() { SceneName = sceneName, Position = pos };
            chunkManager.CurrentMap.Add(chunk);
        }

        ChangedChunks.Add(chunk);

        if (chunkManager.LoadedChunkStates.TryGetValue(sceneName, out var cs))
        {
            MoveChunk(cs, pos);
        }
        else
        {
            HollowKnightNoAreaTransitionsMod.Instance.SceneLoader.LoadSceneAsync(sceneName);
        }
    }

    public static void MoveChunk(ChunkState cs, Vector3 pos)
    {
        var oldChunkPos = cs.Chunk.Position;
        if (pos == oldChunkPos)
            return;
        cs.Chunk.Position = pos;
        HollowKnightNoAreaTransitionsMod.Instance.SceneLoader.MoveChunk(cs, pos - oldChunkPos);
        ChangedChunks.Add(cs.Chunk);
        SaveChangedChunksToFileNonBlocking();
    }

    public static void LogAndResetChangedChunks()
    {
        Logger.Debug("===== CHANGED CHUNKS");
        foreach (var chunk in ChangedChunks)
        {
            Logger.Debug(
                $"new() {{ SceneName = \"{chunk.SceneName}\", Position = new({chunk.Position.x}f, {chunk.Position.y}f) }},"
            );
        }
        Logger.Debug("=====");
        ChangedChunks.Clear();
    }

    public static void SaveChangedChunksToFileNonBlocking()
    {
        _ = Task.Run(() =>
        {
            try
            {
                var path = System.IO.Path.Combine(
                    MelonEnvironment.UserDataDirectory,
                    "HKNAT_ChangedChunks.txt"
                );

                // Create a snapshot to avoid threading issues
                var chunksSnapshot = ChangedChunks.ToList();

                using var writer = new System.IO.StreamWriter(path, false);
                writer.WriteLine("===== CHANGED CHUNKS");
                foreach (var chunk in chunksSnapshot)
                {
                    writer.WriteLine(
                        $"new() {{ SceneName = \"{chunk.SceneName}\", Position = new({chunk.Position.x}f, {chunk.Position.y}f) }},"
                    );
                }
                writer.WriteLine("=====");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save changed chunks to file: {ex}");
            }
        });
    }

    public static void Initialize()
    {
        LAYER_TERRAIN = LayerMask.NameToLayer("Terrain");
        LAYER_HERO_DETECTOR = LayerMask.NameToLayer("Hero Detector");

        On.CameraController.LateUpdate += OnUpdate;
        HollowKnightNoAreaTransitionsMod.Instance.SceneLoader.OnAnySceneInit += OnAnySceneInit;

        if (HollowKnightNoAreaTransitionsMod.Instance.Settings.DebugColliders)
        {
            for (int i = 0; i < USceneManager.sceneCount; i++)
            {
                var scene = USceneManager.GetSceneAt(i);
                ShowColliders(scene);
            }
        }
    }

    public static void Deinitialize()
    {
        On.CameraController.LateUpdate -= OnUpdate;
        HollowKnightNoAreaTransitionsMod.Instance.SceneLoader.OnAnySceneInit -= OnAnySceneInit;
        HideColliders();
    }

    public static void OnAnySceneInit(Scene scene)
    {
        if (HollowKnightNoAreaTransitionsMod.Instance.Settings.DebugColliders)
            ShowColliders(scene);
    }

    public static void HideColliders()
    {
        for (int i = 0; i < USceneManager.sceneCount; i++)
        {
            var scene = USceneManager.GetSceneAt(i);
            var allGameObjects = scene.GetRootGameObjects();
            foreach (var rootGo in allGameObjects)
                CleanupDebugCollidersInHierarchy(rootGo);
        }
    }

    private static void CleanupDebugCollidersInHierarchy(GameObject go)
    {
        for (int i = go.transform.childCount - 1; i >= 0; i--)
        {
            var child = go.transform.GetChild(i);
            var childGo = child.gameObject;
            if (childGo.name.StartsWith("HknatDebug "))
            {
                UObject.DestroyImmediate(childGo);
            }
            else
            {
                CleanupDebugCollidersInHierarchy(childGo);
            }
        }
    }

    public static ChunkState DraggingChunk;
    private static Vector2 DragStartMouse;
    private static Vector3 DragStartChunk;

    public static void OnUpdate(On.CameraController.orig_LateUpdate orig, CameraController self)
    {
        orig(self);

        Utils.Try(() =>
        {
            if (DraggingChunk != null)
            {
                if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0))
                {
                    DraggingChunk = null;
                }
                else
                {
                    var mousePos = GetMouseWorldPoint();
                    var mouseDelta = mousePos - DragStartMouse;
                    var newChunkPos = new Vector3(
                        Mathf.Round(DragStartChunk.x + mouseDelta.x),
                        Mathf.Round(DragStartChunk.y + mouseDelta.y),
                        DragStartChunk.z
                    );
                    MoveChunk(DraggingChunk, newChunkPos);
                }
            }

            var isCtrlDown =
                Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (DraggingChunk == null && isCtrlDown && Input.GetMouseButtonDown(0))
            {
                var mousePos = GetMouseWorldPoint();
                var collider = Physics2D.OverlapPoint(mousePos, Physics2D.AllLayers);
                if (collider != null)
                {
                    var sceneName = collider.gameObject.scene.name;
                    var chunkManager = HollowKnightNoAreaTransitionsMod.Instance.ChunkManager;
                    if (
                        chunkManager.CurrentMap.ChunkBySceneName.TryGetValue(
                            sceneName,
                            out var chunk
                        ) && chunkManager.LoadedChunkStates.TryGetValue(chunk.SceneName, out var cs)
                    )
                    {
                        Logger.Debug($"Dragging chunk = {cs.Chunk.SceneName}");
                        DraggingChunk = cs;
                        DragStartMouse = mousePos;
                        DragStartChunk = cs.Chunk.Position;
                    }
                }
            }

            if (isCtrlDown && Input.GetKeyDown(KeyCode.P))
            {
                ToggleFlyMode();
            }
            if (FlyMode)
            {
                var speed =
                    FlySpeed
                    * Mathf.Pow(HollowKnightNoAreaTransitionsMod.Instance.Camera.Zoom, FlySpeedZoom)
                    * Time.deltaTime;
                var pos = HeroController.instance.transform.position;
                HeroController.instance.transform.position = new Vector3(
                    pos.x + Input.GetAxis("Horizontal") * speed,
                    pos.y + Input.GetAxis("Vertical") * speed,
                    pos.z
                );
            }

            if (isCtrlDown && Input.GetKeyDown(KeyCode.L))
            {
                LogClosestGameObjectToHero();
            }

            if (isCtrlDown && Input.GetKeyDown(KeyCode.K))
            {
                AddNewChunkForNearestTransition();
            }

            if (ThingToMove != null)
            {
                ThingToMove.transform.position = GetMouseWorldPoint();
                var cam = GameCameras.instance.tk2dCam.GetComponent<UCamera>();
                if (cam.transform.position != LastCamPos)
                {
                    // Logger.Debug($"LastCamPos = {LastCamPos}");
                    LastCamPos = cam.transform.position;
                }
            }
        });
    }

    public static void AddNewChunkForNearestTransition()
    {
        var heroPos = HeroController.instance.transform.position;
        var transitions = UObject.FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None);
        TransitionPoint closestTransition = null;
        var closestDistSqr = float.MaxValue;
        foreach (var transition in transitions)
        {
            var distSqr = (transition.transform.position - heroPos).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closestTransition = transition;
            }
        }
        if (closestTransition == null)
        {
            Logger.Warning("No transitions found");
            return;
        }
        MoveChunk(
            closestTransition.targetScene,
            new Vector3(
                heroPos.x - SceneLoader.WORLD_OFFSET.x,
                heroPos.y - SceneLoader.WORLD_OFFSET.y,
                0f
            )
        );
    }

    public static int NumClosestObjectsToLog = 15;

    public static void LogClosestGameObjectToHero()
    {
        var heroPos = HeroController.instance.transform.position;
        Logger.Info($"Logging closest {NumClosestObjectsToLog} GameObjects to Hero at {heroPos}:");
        var closestObjects = ClosestGameObjectsToPos(heroPos, NumClosestObjectsToLog);
        foreach (var (closestObject, distSqr) in closestObjects)
        {
            var transform = closestObject.transform;
            var goPath = new Stack<string>();
            while (transform != null)
            {
                goPath.Push(transform.name);
                transform = transform.parent;
            }
            var pathStr = goPath.Count > 0 ? string.Join(" -> ", goPath.Reverse()) : "null";
            var dist = Mathf.Sqrt(distSqr);
            Logger.Info($"{dist} = {pathStr}");
        }
    }

    public static List<(GameObject, float)> ClosestGameObjectsToPos(Vector3 pos, int maxResults = 1)
    {
        if (maxResults <= 0)
            return [];

        var results = new List<(GameObject go, float distSqr)>();

        void Visit(GameObject go, float ancestorClosestDistSqr)
        {
            var distSqr = (go.transform.position - pos).sqrMagnitude;
            if (distSqr < ancestorClosestDistSqr)
            {
                var index = results.Count;
                while (index > 0 && distSqr < results[index - 1].distSqr)
                {
                    if (index == results.Count && index < maxResults)
                    {
                        results.Add(results[index - 1]);
                    }
                    else if (index < results.Count)
                    {
                        results[index] = results[index - 1];
                    }
                    index--;
                }
                if (index < maxResults)
                {
                    if (index == results.Count)
                    {
                        results.Add((go, distSqr));
                    }
                    else
                    {
                        results[index] = (go, distSqr);
                    }
                }
                ancestorClosestDistSqr = distSqr;
            }

            for (int i = 0; i < go.transform.childCount; i++)
                Visit(go.transform.GetChild(i).gameObject, ancestorClosestDistSqr);
        }

        for (int i = 0; i < USceneManager.sceneCount; i++)
        {
            var scene = USceneManager.GetSceneAt(i);
            var allGameObjects = scene.GetRootGameObjects();
            foreach (var go in allGameObjects)
                Visit(go, float.PositiveInfinity);
        }

        return results;
    }

    public static bool FlyMode = false;
    public static float FlySpeed = 50f;
    public static float FlySpeedZoom = 0.5f;

    public static void ToggleFlyMode()
    {
        FlyMode = !FlyMode;
        Logger.Debug($"FlyMode = {FlyMode}");
        if (FlyMode)
        {
            HeroController.instance.GetComponent<Rigidbody2D>().simulated = false;
            HeroController.instance.GetComponent<BoxCollider2D>().enabled = false;
        }
        else
        {
            HeroController.instance.GetComponent<Rigidbody2D>().simulated = true;
            HeroController.instance.GetComponent<BoxCollider2D>().enabled = true;
        }
    }

    public static GameObject ThingToMove;
    public static Vector3 LastCamPos;

    public static void DebugMouse(Vector2 size = default)
    {
        if (size == Vector2.zero)
            size = new(2f, 2f);

        var go = new GameObject("HKNAT_DebugMouse");

        var meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.mesh = new()
        {
            vertices = [new(0f, 0f), new(size.x, 0f), new(0f, size.y), new(size.x, size.y)],
            triangles = [0, 2, 1, 2, 3, 1],
        };
        meshFilter.mesh.RecalculateNormals();

        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Sprites/Lit")) { color = Color.magenta };

        ThingToMove = go;
    }

    private static Vector2 GetMouseWorldPoint()
    {
        var mousePos = Input.mousePosition;
        var cam = GameCameras.instance.tk2dCam.GetComponent<UCamera>();
        mousePos.z = cam.WorldToScreenPoint(Vector3.zero).z;
        return cam.ScreenToWorldPoint(mousePos);
    }

    public static void ShowColliders(Scene scene)
    {
        static void Visit(GameObject go, string path)
        {
            path += $"{go.name} -> ";

            Color colliderColor = Color.magenta;
            bool shouldShowColliders = false;

            if (go.layer == LAYER_TERRAIN)
            {
                shouldShowColliders = true;
                colliderColor = Color.green;
            }
            else if (go.layer == LAYER_HERO_DETECTOR)
            {
                var transitionPoint = go.GetComponent<TransitionPoint>();
                if (transitionPoint != null)
                {
                    var targetSceneHasChunk =
                        HollowKnightNoAreaTransitionsMod.Instance.ChunkManager.CurrentMap?.ChunkBySceneName.ContainsKey(
                            transitionPoint.targetScene
                        ) ?? false;
                    shouldShowColliders = true;
                    colliderColor = targetSceneHasChunk ? Color.cyan : Color.blue;
                }
            }

            if (shouldShowColliders)
            {
                var layerName = LayerMask.LayerToName(go.layer);
                // Logger.Debug($"Showing {layerName} colliders for {path}");

                var allColliders = go.GetComponents<Collider2D>();
                var colliderTypeCounts = new Dictionary<Type, int>();

                foreach (var collider in allColliders)
                {
                    if (collider == null || !collider.enabled)
                        continue;

                    var colliderType = collider.GetType();
                    if (!colliderTypeCounts.ContainsKey(colliderType))
                        colliderTypeCounts[colliderType] = 0;

                    var index = colliderTypeCounts[colliderType];
                    colliderTypeCounts[colliderType]++;

                    switch (collider)
                    {
                        case BoxCollider2D boxCollider:
                            CreateBoxColliderDebug(boxCollider, index, colliderColor);
                            break;
                        case CircleCollider2D circleCollider:
                            CreateCircleColliderDebug(circleCollider, index, colliderColor);
                            break;
                        case PolygonCollider2D polygonCollider:
                            CreatePolygonColliderDebug(polygonCollider, index, colliderColor);
                            break;
                        case EdgeCollider2D edgeCollider:
                            CreateEdgeColliderDebug(edgeCollider, index, colliderColor);
                            break;
                        default:
                            Logger.Debug(
                                $"Unknown collider type: {collider.GetType().Name} on {go.name}"
                            );
                            break;
                    }
                }
            }

            for (int i = 0; i < go.transform.childCount; i++)
                Visit(go.transform.GetChild(i).gameObject, path);
        }

        foreach (var go in scene.GetRootGameObjects())
            Visit(go, "");
    }

    private static void CreateBoxColliderDebug(BoxCollider2D collider, int index, Color color)
    {
        var go = new GameObject($"HknatDebug Box {index}");
        go.transform.SetParent(collider.transform, false);

        var lineRenderer = go.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lineRenderer, color);

        // Use local collider bounds
        var size = collider.size;
        var offset = collider.offset;
        var halfWidth = size.x * 0.5f;
        var halfHeight = size.y * 0.5f;

        var corners = new Vector3[]
        {
            new(offset.x - halfWidth, offset.y - halfHeight, 0),
            new(offset.x + halfWidth, offset.y - halfHeight, 0),
            new(offset.x + halfWidth, offset.y + halfHeight, 0),
            new(offset.x - halfWidth, offset.y + halfHeight, 0),
            new(offset.x - halfWidth, offset.y - halfHeight, 0),
        };

        lineRenderer.positionCount = corners.Length;
        lineRenderer.SetPositions(corners);
    }

    private static void CreateCircleColliderDebug(CircleCollider2D collider, int index, Color color)
    {
        var go = new GameObject($"HknatDebug Circle {index}");
        go.transform.SetParent(collider.transform, false);

        var lineRenderer = go.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lineRenderer, color);

        const int segments = 32;
        var points = new Vector3[segments + 1];
        var center = (Vector3)collider.offset; // Use local offset instead of world position
        var radius = collider.radius; // Use base radius (scaling will be handled by parent transform)

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2;
            points[i] =
                center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }

        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
    }

    private static void CreatePolygonColliderDebug(
        PolygonCollider2D collider,
        int index,
        Color color
    )
    {
        var go = new GameObject($"HknatDebug Polygon {index}");
        go.transform.SetParent(collider.transform, false);

        for (int pathIndex = 0; pathIndex < collider.pathCount; pathIndex++)
        {
            var pathGo = pathIndex == 0 ? go : new GameObject($"DebugPolygonPath_{pathIndex}");
            if (pathIndex > 0)
                pathGo.transform.SetParent(go.transform, false);

            var lineRenderer = pathGo.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lineRenderer, color);

            var path = collider.GetPath(pathIndex);
            var localPoints = new Vector3[path.Length + 1];

            for (int i = 0; i < path.Length; i++)
            {
                localPoints[i] = new Vector3(path[i].x, path[i].y, 0); // Use local path points directly
            }
            localPoints[path.Length] = localPoints[0];

            lineRenderer.positionCount = localPoints.Length;
            lineRenderer.SetPositions(localPoints);
        }
    }

    private static void CreateEdgeColliderDebug(EdgeCollider2D collider, int index, Color color)
    {
        var go = new GameObject($"HknatDebug Edge {index}");
        go.transform.SetParent(collider.transform, false);

        var lineRenderer = go.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lineRenderer, color);

        var points = collider.points;
        var localPoints = new Vector3[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            localPoints[i] = new Vector3(points[i].x, points[i].y, 0); // Use local points directly
        }

        lineRenderer.positionCount = localPoints.Length;
        lineRenderer.SetPositions(localPoints);
    }

    private static void ConfigureLineRenderer(LineRenderer lineRenderer, Color color)
    {
        lineRenderer.material = new Material(Shader.Find("UI/Default")) { color = color };
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.useWorldSpace = false;
        lineRenderer.sortingOrder = 1000; // Render on top
    }
}
#endif
