namespace HollowKnightNoAreaTransitions.Maps;

public class ChunkMapsSilksong
{
    // TODO: How do I update the rendered masks on tiles I update the points of?
    public static readonly ChunkMap Map =
        new(
            [
                // Bonetown
                new()
                {
                    SceneName = "Bonetown",
                    Position = new(-138f, 125f),
                    OnLoad = scene =>
                    {
                        // TODO: Fix lift position (it is controlled by FSM)
                        // Hide collider blocking entrance to town
                        var toHide = new string[][]
                        {
                            ["Steep Slope"],
                            ["terrain collider (6)"],
                            ["TileMap Render Data", "Scenemap", "Chunk 2 9"],
                        };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        Utils.UpdateTilemapPoints(
                            scene,
                            ["TileMap Render Data", "Scenemap", "Chunk 2 8"],
                            0,
                            points =>
                            {
                                points[21] = new Vector2(32f, 10f);
                                points[22] = new Vector2(0f, 10f);
                                return points;
                            }
                        );
                    },
                },
                new()
                {
                    SceneName = "Bone_01",
                    Position = new(177f, 125f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][]
                        {
                            ["TileMap Render Data", "Scenemap", "Chunk 2 0"],
                        };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        Utils.UpdateTilemapPoints(
                            scene,
                            ["TileMap Render Data", "Scenemap", "Chunk 1 0"],
                            0,
                            points =>
                            {
                                points[31] = new Vector2(32f, 18f);
                                points[32] = new Vector2(0f, 18f);
                                return points;
                            }
                        );
                    },
                },
                new()
                {
                    SceneName = "Bone_01b",
                    Position = new(177f, 125f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][]
                        {
                            ["Masks"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 0"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 1"],
                            ["TileMap Render Data", "Scenemap", "Chunk 1 1"],
                        };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        Utils.UpdateTilemapPoints(
                            scene,
                            ["TileMap Render Data", "Scenemap", "Chunk 1 0"],
                            0,
                            points =>
                            {
                                points[0] = points[9] = new Vector2(0f, 19f);
                                points[1] = points[2] = new Vector2(32f, 19f);
                                return points;
                            }
                        );
                    },
                },
                new() { SceneName = "Bone_11b", Position = new(124f, 201f) },
                // Moss Grotto
                new()
                {
                    SceneName = "Tut_01",
                    Position = new(0f, 0f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][]
                        {
                            ["Song Gate Tinker"], // TODO: What is this? Is it needed?
                            ["TileMap Render Data", "Scenemap", "Chunk 1 3"],
                            ["TileMap Render Data", "Scenemap", "Chunk 2 3"],
                            ["TileMap Render Data", "Scenemap", "Chunk 3 3"],
                            ["msk_generic_soft"],
                            ["msk_generic_soft (1)"],
                            ["SceneBorder"],
                            ["SceneBorder (1)"],
                        };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        // Remove lip on the right of the collider
                        Utils.UpdateTilemapPoints(
                            scene,
                            ["TileMap Render Data", "Scenemap", "Chunk 1 2"],
                            0,
                            points =>
                            {
                                points[3] = points[4] = new Vector2(32f, 2f);
                                return points;
                            }
                        );

                        // Remove wall on the right of the collider
                        Utils.UpdateTilemapPoints(
                            scene,
                            ["TileMap Render Data", "Scenemap", "Chunk 2 2"],
                            0,
                            points =>
                            {
                                points[3] =
                                    points[4] =
                                    points[5] =
                                    points[6] =
                                        new Vector2(32f, 23f);
                                return points;
                            }
                        );

                        // Remove part of collider overlapping the chapel area to the left
                        Utils.UpdateTilemapPoints(
                            scene,
                            ["TileMap Render Data", "Scenemap", "Chunk 3 0"],
                            0,
                            points =>
                            {
                                points[0] = points[1] = points[2] = points[17] = points[3];
                                points[16] = new Vector2(7f, 24f);
                                return points;
                            }
                        );
                    },
                },
                new()
                {
                    SceneName = "Tut_01b",
                    Position = new(29f, 0f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][]
                        {
                            ["SceneBorder"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 0"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 1"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 2"],
                            ["TileMap Render Data", "Scenemap", "Chunk 1 0"],
                            ["TileMap Render Data", "Scenemap", "Chunk 1 1"],
                            ["TileMap Render Data", "Scenemap", "Chunk 2 0"],
                            ["TileMap Render Data", "Scenemap", "Chunk 2 1"],
                            ["TileMap Render Data", "Scenemap", "Chunk 3 0"],
                            ["TileMap Render Data", "Scenemap", "Chunk 3 1"],
                            ["msk_generic_soft"],
                            ["msk_generic_soft (1)"],
                        };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);
                    },
                },
                new() { SceneName = "Tut_02", Position = new(-150f, 0f) },
                new()
                {
                    SceneName = "Tut_03",
                    Position = new(-112f, 94f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][] { ["church front collider"] };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        // Stop collider jutting into transition corridor
                        Utils.UpdateTilemapPoints(
                            scene,
                            ["TileMap Render Data", "Scenemap", "Chunk 0 3"],
                            2,
                            points =>
                            {
                                points[24] = new Vector2(24f, 7f);
                                points[25] = points[26] = new Vector2(30f, 16f);
                                return points;
                            }
                        );
                    },
                },
                // Weavenest Atla
                // TODO: Clean up
                new() { SceneName = "Weave_04", Position = new(189f, -33f) },
                new() { SceneName = "Weave_02", Position = new(280f, -118f) },
            ]
        );
}
