namespace HollowKnightNoAreaTransitions.Utils;

public static class Hooks
{
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
}
