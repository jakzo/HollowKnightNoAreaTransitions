namespace HollowKnightNoAreaTransitions;

static class Utils
{
    public static class Layers
    {
        public class LazyLayer(string name)
        {
            public string Name = name;
            private int? _id;

            public int Id
            {
                get => _id ?? (int)(_id = LayerMask.NameToLayer(Name));
            }
        }

        public static LazyLayer Player = new("Player");
    }

    public static void Try(string id, Action action, Action onError = null)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            TryLogException(ex, id);
            onError?.Invoke();
        }
    }

    public static void Try(Action action, Action onError = null)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            TryLogException(ex);
            onError?.Invoke();
        }
    }

    public static T Try<T>(Func<T> action, Func<T> onError = null)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            TryLogException(ex);
            return onError != null ? onError() : default;
        }
    }

    private static void TryLogException(Exception ex, string id = null)
    {
        // OuterMethod() -> Try() -> TryLogException()
        id ??= new StackTrace().GetFrame(2).GetMethod().Name;
        Logger.Error($"Failed to execute {id}:");
        Logger.Error(ex);
    }

    public static GameObject FindGameObjectByPath(Scene scene, string[] pathParts)
    {
        if (pathParts == null || pathParts.Length == 0)
            return null;

        GameObject SearchRecursive(GameObject current, int pathIndex)
        {
            if (current.name != pathParts[pathIndex])
                return null;

            var nextIndex = pathIndex + 1;
            if (nextIndex >= pathParts.Length)
                return current;

            for (int i = 0; i < current.transform.childCount; i++)
            {
                var result = SearchRecursive(current.transform.GetChild(i).gameObject, nextIndex);
                if (result != null)
                    return result;
            }
            return null;
        }

        foreach (var rootObject in scene.GetRootGameObjects())
        {
            var result = SearchRecursive(rootObject, 0);
            if (result != null)
                return result;
        }

        return null;
    }

    public static void UpdateTilemapPoints(
        Scene scene,
        string[] tilemapPath,
        int colliderIndex,
        Func<Vector2[], Vector2[]> updatePoints
    )
    {
        var go = FindGameObjectByPath(scene, tilemapPath);
        var collider = go.GetComponents<EdgeCollider2D>()[colliderIndex];
        collider.points = updatePoints(collider.points);
        // TODO: How do I update the rendered mask rather than removing it entirely?
        go.GetComponent<MeshRenderer>().enabled = false;
    }
}
