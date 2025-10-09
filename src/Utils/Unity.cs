namespace HollowKnightNoAreaTransitions.Utils;

static class Unity
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

    public static float PointToRectDistSqr(Vector2 point, Rect rect)
    {
        float dx = Mathf.Max(rect.xMin - point.x, point.x - rect.xMax, 0f);
        float dy = Mathf.Max(rect.yMin - point.y, point.y - rect.yMax, 0f);
        return dx * dx + dy * dy;
    }
}
