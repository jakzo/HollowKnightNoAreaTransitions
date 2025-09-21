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
}
