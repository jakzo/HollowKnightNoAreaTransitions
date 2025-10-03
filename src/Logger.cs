namespace HollowKnightNoAreaTransitions;

class Logger
{
    public static void Info(string message)
    {
        Melon<HollowKnightNoAreaTransitionsMod>.Logger.Msg(message);
    }

    public static void Info(object message)
    {
        Melon<HollowKnightNoAreaTransitionsMod>.Logger.Msg(message);
    }

    public static void Warning(string message)
    {
        Melon<HollowKnightNoAreaTransitionsMod>.Logger.Warning(message);
    }

    public static void Warning(object message)
    {
        Melon<HollowKnightNoAreaTransitionsMod>.Logger.Warning(message);
    }

    public static void Error(string message)
    {
        Melon<HollowKnightNoAreaTransitionsMod>.Logger.Error(message);
    }

    public static void Error(object message)
    {
        Melon<HollowKnightNoAreaTransitionsMod>.Logger.Error(message);
    }

    public static void Debug(string message)
    {
        if (HollowKnightNoAreaTransitionsMod.Instance.Settings.DebugLogs)
        {
            Melon<HollowKnightNoAreaTransitionsMod>.Logger.MsgPastel("[DEBUG] " + message);
        }
    }

    public static void Debug(object message)
    {
        if (HollowKnightNoAreaTransitionsMod.Instance.Settings.DebugLogs)
        {
            Melon<HollowKnightNoAreaTransitionsMod>.Logger.MsgPastel(message);
        }
    }

    private static Dictionary<string, DateTime> _timeMarkers = [];

    public static void Time(string id, string message, bool reset = false)
    {
        if (HollowKnightNoAreaTransitionsMod.Instance.Settings.DebugLogs)
        {
            if (reset && _timeMarkers.ContainsKey(id))
                _timeMarkers.Remove(id);

            if (!_timeMarkers.TryGetValue(id, out var start))
            {
                start = DateTime.Now;
                _timeMarkers[id] = start;
            }
            var elapsed = DateTime.Now - start;
            var elapsedStr = elapsed.TotalSeconds.ToString("0.000");
            Melon<HollowKnightNoAreaTransitionsMod>.Logger.MsgPastel(
                $"[{id}] {elapsedStr} - {message}"
            );
        }
    }
}
