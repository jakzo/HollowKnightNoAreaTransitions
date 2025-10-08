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

    private static FieldInfo _transitionPointColliderField;

    public static Collider2D GetTransitionPointCollider(TransitionPoint tp)
    {
        if (_transitionPointColliderField == null)
        {
            _transitionPointColliderField = typeof(TransitionPoint).GetField(
                "collider",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
        }
        return (Collider2D)_transitionPointColliderField.GetValue(tp);
    }

    public static BoxCollider2D GetTransitionPointBoxCollider(TransitionPoint tp)
    {
        var collider = GetTransitionPointCollider(tp);
        return collider is BoxCollider2D boxCollider ? boxCollider : null;
    }

    public static Direction GetTransitionPointDirection(TransitionPoint tp)
    {
        if (tp.PromptMarker != null)
            return Direction.None;

        var collider = GetTransitionPointCollider(tp);
        var name = tp.name.ToLower();
        if (collider.bounds.size.x < collider.bounds.size.y)
        {
            if (name.Contains("left"))
                return Direction.Left;
            if (name.Contains("right"))
                return Direction.Right;
        }
        else
        {
            if (name.Contains("top"))
                return Direction.Up;
            if (name.Contains("bot"))
                return Direction.Down;
        }
        return Direction.None;
    }
}
