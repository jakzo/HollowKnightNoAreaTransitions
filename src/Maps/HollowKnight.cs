namespace HollowKnightNoAreaTransitions.Maps;

public class ChunkMapsHollowKnight
{
    public static readonly ChunkMap Map =
        new(
            [
                // Dirtmouth
                new() { SceneName = "Town", Position = new(-132.5f, 58f) },
                // Exit to Dirtmouth
                new() { SceneName = "Crossroads_01", Position = new(0f, 0f) },
                // Black egg temple entrance
                new() { SceneName = "Crossroads_02", Position = new(100f, 9f) },
                // Corridor right of black egg temple
                new() { SceneName = "Crossroads_39", Position = new(190f, 7f) },
                // Right corner room
                new() { SceneName = "Crossroads_14", Position = new(278f, -20f) },
                // Gruz platform room
                new()
                {
                    SceneName = "Crossroads_07",
                    Position = new(-50f, -74f),
                    Colliders = new Rect[] { new(42f, 76f, 10f, 5f) },
                },
                // Grub room
                new() { SceneName = "Crossroads_38", Position = new(-121f, 5f) },
                // Cornifer room
                new()
                {
                    SceneName = "Crossroads_33",
                    Position = new(-51f, -124f),
                    Colliders = new Rect[] { new(20f, 30f, 2f, 1f) },
                },
                // Shaman entrance
                new()
                {
                    SceneName = "Crossroads_06",
                    Position = new(-6f, -93f),
                    OnLoad = scene =>
                    {
                        // TODO: Make collider not overlap gruz platform room
                    },
                },
                // False knight arena
                // TODO: Game hangs when unloading chunks if this is part of the chunk
                // map (something to do with the Crossroads_10_boss scene?)
                new() { SceneName = "Crossroads_10", Position = new(55f, -93f) },
                // Central-left corridor
                new() { SceneName = "Crossroads_05", Position = new(32f, -26f) },
                // Central-middle corridor
                new() { SceneName = "Crossroads_40", Position = new(114f, -18f) },
                // Central-right corridor
                new() { SceneName = "Crossroads_16", Position = new(202f, -24f) },
                // Right vertical passage
                new() { SceneName = "Crossroads_03", Position = new(246f, -112f) },
                // Pre-boss room
                new() { SceneName = "Crossroads_21", Position = new(138f, -95f) },
                // Glowing womb room
                new() { SceneName = "Crossroads_22", Position = new(139f, -64f) },
                // Hot springs entrance
                new() { SceneName = "Crossroads_08", Position = new(-6f, -136f) },
                // Left goam room
                new() { SceneName = "Crossroads_13", Position = new(53f, -140f) },
                // Right goam room
                new() { SceneName = "Crossroads_42", Position = new(133f, -130f) },
                // Bottom-right intersection room
                new() { SceneName = "Crossroads_19", Position = new(243f, -160f) },
                // Stag room
                new() { SceneName = "Crossroads_47", Position = new(199f, -104f) },
            ]
        );
}
