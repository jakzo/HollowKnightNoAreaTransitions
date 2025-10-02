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

HKNAT.HeroPositionInChunk("Tut_02");
HKNAT.CreateTestingCollider("Bonetown", new Rect(182f, 0f, 2f, 8f));

Inspect(HollowKnightNoAreaTransitions.HollowKnightNoAreaTransitionsMod.Instance);
*/

static class HKNAT
{
    // private static void Test()
    // {
    //     var colliders = (Paste() as GameObject).GetComponents<EdgeCollider2D>();
    //     HollowKnightNoAreaTransitions.TilemapUtils.UpdateTilemapMask(colliders);
    // }

    public static HashSet<Chunk> ChangedChunks = [];

    private static int LAYER_TERRAIN;
    private static int LAYER_HERO_DETECTOR;

    public static void MoveChunk(string sceneName, Vector3 pos, Action<ChunkState> onLoaded = null)
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
            chunkManager.LoadChunk(
                chunk,
                scene => onLoaded?.Invoke(chunkManager.LoadedChunkStates[sceneName])
            );
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
            Logger.Debug(SerializeChunkDefinition(chunk));
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
                    writer.WriteLine(SerializeChunkDefinition(chunk));
                }
                writer.WriteLine("=====");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save changed chunks to file: {ex}");
            }
        });
    }

    // HKNAT.LogAllChunks();
    public static void LogAllChunks()
    {
        var chunkManager = HollowKnightNoAreaTransitionsMod.Instance.ChunkManager;
        Logger.Info("===== ALL CHUNKS");
        foreach (var chunk in chunkManager.CurrentMap.Chunks)
            Logger.Info(SerializeChunkDefinition(chunk));
        Logger.Info("=====");
    }

    private static string SerializeChunkDefinition(Chunk chunk)
    {
        var bounds = chunk.CalculatedPlayableBounds ?? chunk.PlayableBounds;
        return $"new() {{ SceneName = \"{chunk.SceneName}\", Position = new({chunk.Position.x}f, {chunk.Position.y}f), PlayableBounds = new({bounds.x}f, {bounds.y}f, {bounds.width}f, {bounds.height}f) }},";
    }

    public static void Initialize()
    {
        LAYER_TERRAIN = LayerMask.NameToLayer("Terrain");
        LAYER_HERO_DETECTOR = LayerMask.NameToLayer("Hero Detector");

        On.CameraController.LateUpdate += OnUpdate;
        HollowKnightNoAreaTransitionsMod.Instance.SceneLoader.OnAnySceneInit += OnAnySceneInit;
        HollowKnightNoAreaTransitionsMod.Instance.ChunkManager.OnChunkLoaded += OnChunkLoaded;

        for (int i = 0; i < USceneManager.sceneCount; i++)
        {
            var scene = USceneManager.GetSceneAt(i);
            ShowColliders(scene);
        }
    }

    public static void Deinitialize()
    {
        On.CameraController.LateUpdate -= OnUpdate;
        HollowKnightNoAreaTransitionsMod.Instance.SceneLoader.OnAnySceneInit -= OnAnySceneInit;
        HideColliders();
        // PullChunkSizesFromMap();
    }

    // public static void PullChunkSizesFromMap()
    // {
    //     GameManager.instance.gameMap.;
    // }

    public static void OnAnySceneInit(Scene scene)
    {
        ShowColliders(scene);
        var chunkManager = HollowKnightNoAreaTransitionsMod.Instance.ChunkManager;
        if (
            scene.name == chunkManager.StartingChunk?.SceneName
            && chunkManager.LoadedChunkStates.TryGetValue(scene.name, out var cs)
        )
        {
            ShowPlayableAreas(cs);
        }
    }

    private static void OnChunkLoaded(ChunkState cs)
    {
        ShowPlayableAreas(cs);
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
            var mod = HollowKnightNoAreaTransitionsMod.Instance;
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
            var isShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (DraggingChunk == null && isCtrlDown && Input.GetMouseButtonDown(0))
            {
                var mousePos = GetMouseWorldPoint();
                var collider = Physics2D.OverlapPoint(mousePos, Physics2D.AllLayers);
                if (collider != null)
                {
                    var sceneName = collider.gameObject.scene.name;
                    var chunkManager = mod.ChunkManager;
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
                var speed = FlySpeed * Mathf.Pow(mod.Camera.Zoom, FlySpeedZoom) * Time.deltaTime;
                var pos = HeroController.instance.transform.position;
                HeroController.instance.transform.position = new Vector3(
                    pos.x + Input.GetAxis("Horizontal") * speed,
                    pos.y + Input.GetAxis("Vertical") * speed,
                    pos.z
                );
            }

            if (isCtrlDown && Input.GetKeyDown(KeyCode.L))
            {
                LogAndFlashClosestGameObjectToHero(isShiftDown);
            }

            if (isCtrlDown && Input.GetKeyDown(KeyCode.K))
            {
                AddNewChunkForNearestTransition();
            }

            if (
                Input.GetKeyDown(KeyCode.O)
                && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            )
            {
                if (mod.IsInitialized)
                {
                    Logger.Info("Deinitializing");
                    mod.Deinitialize();
                }
                else
                {
                    Logger.Info("Initializing");
                    mod.Initialize();
                }
            }

            if (_flashedObjects != null && Time.realtimeSinceStartup > _flashStartTime + 0.5f)
            {
                foreach (var (go, _) in _flashedObjects)
                    go.SetActive(true);
                _flashedObjects = null;
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
        var sceneX = Mathf.Round(heroPos.x - SceneLoader.WORLD_OFFSET.x);
        var sceneY = Mathf.Round(heroPos.y - SceneLoader.WORLD_OFFSET.y);

        IEnumerator LineUpTransitions(ChunkState cs)
        {
            TransitionPoint GetEntryPoint() =>
                TransitionPoint.TransitionPoints.FirstOrDefault(point =>
                    point.gameObject.scene == cs.MainScene
                    && point.name == closestTransition.entryPoint
                );

            // Wait for up to a minute for the transition points to be added
            var waitTime = 60f;
            var startTime = Time.realtimeSinceStartup;
            TransitionPoint entryPoint = null;
            while (
                (entryPoint = GetEntryPoint()) == null
                && Time.realtimeSinceStartup < startTime + waitTime
            )
                yield return null;

            Utils.Try(() =>
            {
                if (entryPoint == null)
                {
                    Logger.Error(
                        $"Could not find entry point in scene {cs.MainScene.name} with name {closestTransition.name} after timeout"
                    );
                    return;
                }

                // Line up transitions
                var oldSceneBounds = GetSceneBounds(closestTransition.gameObject.scene);
                var newSceneBounds = GetSceneBounds(cs.MainScene);

                // var entryPoint = UObject
                //     .FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None)
                //     .First(point =>
                //         point.gameObject.scene == cs.MainScene
                //         && point.name == closestTransition.name
                //     );

                float newSceneX,
                    newSceneY;

                var colliderField = typeof(TransitionPoint).GetField(
                    "collider",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                var oldSceneCollider = (BoxCollider2D)colliderField.GetValue(closestTransition);
                var entryCollider = (BoxCollider2D)colliderField.GetValue(entryPoint);
                var isDoorway = entryCollider.size.x < entryCollider.size.y;
                if (isDoorway)
                {
                    // Line up bottom of transitions
                    var diffY = entryCollider.bounds.min.y - oldSceneCollider.bounds.min.y;
                    newSceneY = Mathf.Round(sceneY - diffY);
                    // Line up chunk tiles
                    var isPointingRight =
                        closestTransition.transform.position.x > oldSceneBounds.center.x;
                    var diffX = isPointingRight
                        ? newSceneBounds.xMin - oldSceneBounds.xMax
                        : newSceneBounds.xMax - oldSceneBounds.xMin;
                    newSceneX = Mathf.Round(sceneX - diffX);
                }
                else
                {
                    // Line up left/right of transitions
                    var diffX = entryCollider.bounds.center.x - oldSceneCollider.bounds.center.x;
                    newSceneX = Mathf.Round(sceneX - diffX);
                    // Line up chunk tiles
                    var isPointingUp =
                        closestTransition.transform.position.y > oldSceneBounds.center.y;
                    var diffY = isPointingUp
                        ? newSceneBounds.yMin - oldSceneBounds.yMax
                        : newSceneBounds.yMax - oldSceneBounds.yMin;
                    newSceneY = Mathf.Round(sceneY - diffY);
                }
                MoveChunk(cs, new Vector3(newSceneX, newSceneY, 0f));
                Logger.Debug(
                    $"Moved chunk {cs.Chunk.SceneName} from {sceneX},{sceneY} to {newSceneX},{newSceneY}"
                );
            });
        }

        MoveChunk(
            closestTransition.targetScene,
            new Vector3(sceneX, sceneY, 0f),
            cs => MelonCoroutines.Start(LineUpTransitions(cs))
        );
    }

    public static Rect GetSceneBounds(Scene scene)
    {
        GameObject sceneMap = null;
        foreach (var rootObject in scene.GetRootGameObjects())
        {
            if (!rootObject.name.Contains("TileMap Render Data"))
                continue;

            sceneMap = rootObject.transform.Find("Scenemap")?.gameObject;
        }
        if (sceneMap == null)
            throw new Exception($"Could not find scenemap in scene {scene.name}");

        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;
        foreach (var collider in sceneMap.GetComponentsInChildren<EdgeCollider2D>())
        {
            var chunkX = collider.transform.position.x;
            var chunkY = collider.transform.position.y;
            foreach (var point in collider.points)
            {
                var x = chunkX + point.x;
                var y = chunkY + point.y;
                if (x < minX)
                    minX = x;
                if (y < minY)
                    minY = y;
                if (x > maxX)
                    maxX = x;
                if (y > maxY)
                    maxY = y;
            }
        }
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    public static Vector3 HeroPositionInChunk(string sceneName)
    {
        var heroPos = HeroController.instance.transform.position;
        var chunk = HollowKnightNoAreaTransitionsMod
            .Instance
            .ChunkManager
            .CurrentMap
            .ChunkBySceneName[sceneName];
        return heroPos - (chunk.Position + SceneLoader.WORLD_OFFSET);
    }

    public static int NumClosestObjectsToLog = 15;

    private static List<(GameObject, float)> _flashedObjects = null;
    private static float _flashStartTime;

    public static void LogAndFlashClosestGameObjectToHero(bool onlyColliders = false)
    {
        var heroPos = HeroController.instance.transform.position;
        var type = onlyColliders ? "colliders" : "GameObjects";
        Logger.Info($"Logging closest {NumClosestObjectsToLog} {type} to Hero at {heroPos}:");
        var closestObjects = ClosestGameObjectsToPos(
            heroPos,
            NumClosestObjectsToLog,
            onlyColliders
        );
        foreach (var (closestObject, distSqr) in closestObjects)
        {
            var transform = closestObject.transform;
            var goPath = new Stack<string>();
            while (transform != null)
            {
                goPath.Push(transform.name);
                transform = transform.parent;
            }
            goPath.Push(closestObject.scene.name);
            var pathStr = string.Join(" -> ", goPath);
            var dist = Mathf.Sqrt(distSqr);
            Logger.Info($"{dist} = {pathStr}");
        }

        _flashedObjects = closestObjects;
        _flashStartTime = Time.realtimeSinceStartup;
        foreach (var (go, _) in _flashedObjects)
            go.SetActive(false);
    }

    public static List<(GameObject, float)> ClosestGameObjectsToPos(
        Vector3 pos,
        int maxResults = 1,
        bool onlyColliders = false
    )
    {
        if (maxResults <= 0)
            return [];

        var results = new List<(GameObject go, float distSqr)>();

        void Visit(GameObject go, float ancestorClosestDistSqr)
        {
            if (!go.activeInHierarchy)
                return;

            if (!onlyColliders || go.GetComponent<Collider2D>() != null)
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
            HeroController.instance.Body.simulated = false;
            HeroController.instance.GetComponent<BoxCollider2D>().enabled = false;
            HeroController.instance.heroBox.gameObject.SetActive(false);
        }
        else
        {
            HeroController.instance.Body.simulated = true;
            HeroController.instance.GetComponent<BoxCollider2D>().enabled = true;
            HeroController.instance.heroBox.gameObject.SetActive(true);
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
        meshRenderer.material = new Material(Shader.Find("UI/Default")) { color = Color.magenta };

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
        var settings = HollowKnightNoAreaTransitionsMod.Instance.Settings;
        if (!settings.DebugColliders && !settings.DebugTransitions)
            return;

        void Visit(GameObject go, string path)
        {
            path += $"{go.name} -> ";

            Color colliderColor = Color.magenta;
            bool shouldShowColliders = false;

            if (settings.DebugColliders && go.layer == LAYER_TERRAIN)
            {
                shouldShowColliders = true;
                colliderColor = Color.green;
            }
            else if (settings.DebugTransitions && go.layer == LAYER_HERO_DETECTOR)
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
                // Logger.Debug($"Showing {layerName} colliders for {go.scene.name} -> {path}");

                var allColliders = go.GetComponents<Collider2D>();
                foreach (var collider in allColliders)
                {
                    if (collider == null || !collider.enabled)
                        continue;

                    switch (collider)
                    {
                        case BoxCollider2D boxCollider:
                            CreateBoxColliderDebug(boxCollider, colliderColor);
                            break;
                        case CircleCollider2D circleCollider:
                            CreateCircleColliderDebug(circleCollider, colliderColor);
                            break;
                        case PolygonCollider2D polygonCollider:
                            CreatePolygonColliderDebug(polygonCollider, colliderColor);
                            break;
                        case EdgeCollider2D edgeCollider:
                            CreateEdgeColliderDebug(edgeCollider, colliderColor);
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

    private static void CreateBoxColliderDebug(BoxCollider2D collider, Color color)
    {
        var go = new GameObject($"HknatDebug Box");
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

    private static void CreateCircleColliderDebug(CircleCollider2D collider, Color color)
    {
        var go = new GameObject($"HknatDebug Circle");
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

    private static void CreatePolygonColliderDebug(PolygonCollider2D collider, Color color)
    {
        var go = new GameObject($"HknatDebug Polygon");
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

    private static void CreateEdgeColliderDebug(EdgeCollider2D collider, Color color)
    {
        var go = new GameObject($"HknatDebug Edge");
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

    public static void ShowPlayableAreas(ChunkState cs)
    {
        var settings = HollowKnightNoAreaTransitionsMod.Instance.Settings;
        if (!settings.DebugPlayableAreas || cs.PlayableAreas == null)
            return;

        Logger.Debug($"Showing playable areas for {cs.Chunk.SceneName}");
        var parent = new GameObject($"HKNAT_PlayableAreas").transform;
        USceneManager.MoveGameObjectToScene(parent.gameObject, cs.MainScene);
        parent.localPosition = cs.Chunk.Position + SceneLoader.WORLD_OFFSET;
        foreach (var points in cs.PlayableAreas)
        {
            var go = new GameObject("HknatDebug PlayableArea");
            go.transform.SetParent(parent, false);
            var lineRenderer = go.AddComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                Logger.Error($"======= lineRenderer is mysteriously null in {cs.Chunk.SceneName}");
                continue;
            }
            ConfigureLineRenderer(lineRenderer, Color.magenta);

            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
        }
    }

    private static BoxCollider2D _testingCollider = null;

    public static void CreateTestingCollider(string sceneName, Rect rect)
    {
        if (_testingCollider != null)
        {
            UObject.Destroy(_testingCollider.gameObject);
            _testingCollider = null;
        }
        var chunkState = HollowKnightNoAreaTransitionsMod.Instance.ChunkManager.LoadedChunkStates[
            sceneName
        ];
        var parent = new GameObject("HKNAT_TestingCollider").transform;
        USceneManager.MoveGameObjectToScene(parent.gameObject, chunkState.MainScene);
        parent.localPosition = chunkState.Chunk.Position + SceneLoader.WORLD_OFFSET;
        _testingCollider = SceneLoader.CreateTransitionCollider(parent, rect);
        CreateBoxColliderDebug(_testingCollider, Color.yellow);
    }

    public static void AutoAddChunk()
    {
        var closestTransitionPoint = UObject
            .FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None)
            .Where(point =>
                !HollowKnightNoAreaTransitionsMod.Instance.ChunkManager.CurrentMap.ChunkBySceneName.ContainsKey(
                    point.targetScene
                )
            )
            .OrderBy(point =>
                (point.transform.position - HeroController.instance.transform.position).sqrMagnitude
            )
            .First();
        MoveChunk(
            closestTransitionPoint.targetScene,
            Vector3.zero,
            cs =>
            {
                var sceneMap = Utils.FindGameObjectByPath(
                    cs.MainScene,
                    ["TileMap Render Data", "Scenemap"]
                );
            }
        );
    }
}
#endif
