namespace HollowKnightNoAreaTransitions.Utils;

public static class Game
{
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

    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right,
    }

    public static Direction OppositeDirection(Direction dir) =>
        dir switch
        {
            Direction.None => Direction.None,
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null),
        };
}
