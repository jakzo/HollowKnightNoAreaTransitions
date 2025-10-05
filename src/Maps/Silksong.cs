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
                    Position = new(-137f, 125f),
                    PlayableBounds = new(0f, 0f, 315f, 90f),
                    Colliders = [new(182f, -6f, 1f, 7f), new(189f, -6f, 1f, 7f)],
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

                        TilemapUtils.UpdateTilemapPoints(
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
                // TODO: Make bonetown door open on other side when bonegrave side opens
                new()
                {
                    SceneName = "Bonegrave",
                    Position = new(-452f, 112f),
                    PlayableBounds = new(0f, 0f, 315f, 82f),
                },
                new()
                {
                    SceneName = "Bone_01",
                    Position = new(178f, 125f),
                    PlayableBounds = new(0f, 6f, 130f, 85f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][]
                        {
                            ["TileMap Render Data", "Scenemap", "Chunk 2 0"],
                        };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        TilemapUtils.UpdateTilemapPoints(
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

                        TilemapUtils.ClampColliderPoints(
                            scene,
                            ["Roof Collider (33)"],
                            p => new(Mathf.Min(p.x, 27f), p.y)
                        );
                    },
                },
                new()
                {
                    SceneName = "Bone_01b",
                    Position = new(178f, 125f),
                    PlayableBounds = new(0f, 53f, 34f, 36f),
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

                        TilemapUtils.UpdateTilemapPoints(
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
                new()
                {
                    SceneName = "Bone_01c",
                    Position = new(178f, 125f),
                    PlayableBounds = new(0f, 6f, 198f, 65f),
                    // TODO: Stop collider intersecting room to left
                    OnLoad = scene =>
                    {
                        var toHide = new string[][]
                        {
                            ["TileMap Render Data", "Scenemap", "Chunk 0 0"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 1"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 2"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 3"],
                            ["TileMap Render Data", "Scenemap", "Chunk 1 0"],
                            ["TileMap Render Data", "Scenemap", "Chunk 1 1"],
                            ["TileMap Render Data", "Scenemap", "Chunk 1 2"],
                            ["TileMap Render Data", "Scenemap", "Chunk 1 3"],
                            ["TileMap Render Data", "Scenemap", "Chunk 2 0"],
                            ["TileMap Render Data", "Scenemap", "Chunk 2 1"],
                            ["TileMap Render Data", "Scenemap", "Chunk 2 2"],
                            ["TileMap Render Data", "Scenemap", "Chunk 2 3"],
                            ["msk_generic_soft"],
                            ["msk_generic_soft (1)"],
                            ["msk_generic_soft (2)"],
                            ["pipe_mask_02"],
                            ["pipe_mask_02 (1)"],
                            ["pipe_mask_02 (2)"],
                            ["pipe_mask_02 (3)"],
                            ["pipe_mask_02 (4)"],
                            ["pipe_mask_02 (5)"],
                            ["pipe_mask_02 (6)"],
                            ["pipe_mask_02 (7)"],
                            ["pipe_mask_02 (8)"],
                            ["fog (5)"],
                            ["fog (6)"],
                            ["fog (9)"],
                            ["SceneBorder"],
                            ["SceneBorder (1)"],
                            ["terrain collider non slider"],
                            ["terrain collider non slider (1)"],
                            ["terrain collider non slider (2)"],
                            ["Group", "black_fader_moon (10)"],
                            ["Group", "black_fader_moon (11)"],
                            ["Group", "black_fader_moon (12)"],
                            ["Group", "black_fader_moon (13)"],
                            ["Group", "black_fader_moon (14)"],
                            ["Group", "black_fader_moon (15)"],
                            ["Group (1)", "black_fader_moon (9)"],
                            ["Group (1)", "black_fader_moon (10)"],
                            ["Group (1)", "black_fader_moon (11)"],
                        };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        TilemapUtils.ClampColliderPoints(
                            scene,
                            ["Roof Collider (33)"],
                            p => new(Mathf.Clamp(p.x, 26f, 95f), p.y)
                        );
                    },
                },
                new()
                {
                    SceneName = "Bone_02",
                    Position = new(376f, 125f),
                    PlayableBounds = new(0f, 1f, 155f, 45f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][]
                        {
                            ["terrain collider non slider"],
                            ["terrain collider non slider (1)"],
                            ["terrain collider non slider (2)"],
                            ["Group (2)", "black_fader_moon (8)"],
                            ["Group (2)", "black_fader_moon (9)"],
                            ["Group (2)", "black_fader_moon (11)"],
                        };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        TilemapUtils.ClampColliderPoints(
                            scene,
                            ["Roof Collider (10)"],
                            p => new(Mathf.Max(p.x, -9f), Math.Max(p.y, -6.5f))
                        );

                        TilemapUtils.ClampColliderPoints(
                            scene,
                            ["Roof Collider"],
                            p => new(p.x, Math.Min(p.y, 10f))
                        );
                    },
                },
                new()
                {
                    SceneName = "Bone_03",
                    Position = new(381f, 171f),
                    PlayableBounds = new(0f, 0f, 60f, 138f),
                },
                new()
                {
                    SceneName = "Bone_08",
                    Position = new(662f, 167f),
                    PlayableBounds = new(0f, 0f, 40f, 94f),
                },
                new()
                {
                    SceneName = "Bone_09",
                    Position = new(666f, 122f),
                    PlayableBounds = new(0f, 0f, 164f, 45f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][] { ["SC_0047_sc_door (2)"] };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        // Ramp to higher floor in area on Dock_08
                        TilemapUtils.UpdateTilemapPoints(
                            scene,
                            ["TileMap Render Data", "Scenemap", "Chunk 0 5"],
                            0,
                            points =>
                            {
                                points[3] = new Vector2(4f, 7f);
                                return points;
                            }
                        );

                        TilemapUtils.ClampEdgeColliderPoints(
                            scene,
                            ["TileMap Render Data", "Scenemap", "Chunk 0 5"],
                            1,
                            p => new(p.x, Mathf.Max(p.y, 12f))
                        );

                        TilemapUtils.ClampColliderPoints(
                            scene,
                            ["Roof Collider_Basic (10)"],
                            p => new(Mathf.Min(p.x, 14f), Math.Max(p.y, -4f))
                        );
                    },
                },
                new()
                {
                    SceneName = "Bone_11b",
                    Position = new(126f, 201f),
                    PlayableBounds = new(2f, 2f, 50f, 16f),
                },
                new()
                {
                    SceneName = "Bone_15",
                    Position = new(535f, 195f),
                    PlayableBounds = new(0f, 0f, 110f, 27f),
                },
                new()
                {
                    SceneName = "Bone_16",
                    Position = new(531f, 120f),
                    PlayableBounds = new(0f, 6f, 135f, 69f),
                },
                // Deep Docks
                new()
                {
                    SceneName = "Dock_01",
                    Position = new(940f, 123f),
                    PlayableBounds = new(0f, 5f, 35f, 78f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][] { ["terrain collider"] };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);
                    },
                },
                new()
                {
                    SceneName = "Dock_08",
                    Position = new(830f, 123f),
                    PlayableBounds = new(0f, 0f, 110f, 33f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][] { ["Remasker New"] };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);
                    },
                },
                // Moss Grotto
                new()
                {
                    SceneName = "Tut_01",
                    Position = new(0f, 0f),
                    PlayableBounds = new(0f, 3f, 105f, 117f),
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
                            ["Group", "pipe_mask_02 (4)"],
                            ["Group (2)"],
                        };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        // Remove the black coverings behind the arch but keep the arch itself
                        var boneArch = Utils.FindGameObjectByPath(scene, ["bone_church_arch (7)"]);
                        for (int i = 6; i < boneArch.transform.childCount; i++)
                            boneArch.transform.GetChild(i).gameObject.SetActive(false);

                        // Remove lip on the right of the collider
                        TilemapUtils.UpdateTilemapPoints(
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
                        TilemapUtils.UpdateTilemapPoints(
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
                        TilemapUtils.UpdateTilemapPoints(
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
                    PlayableBounds = new(36f, 34f, 124f, 64f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][]
                        {
                            ["SceneBorder"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 0"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 1"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 2"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 3"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 4"],
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

                        TilemapUtils.ClampEdgeColliderPoints(
                            scene,
                            ["TileMap Render Data", "Scenemap", "Chunk 1 4"],
                            0,
                            p => new(p.x, Mathf.Max(p.y, 1f))
                        );
                    },
                },
                new()
                {
                    SceneName = "Tut_02",
                    Position = new(-150f, 0f),
                    PlayableBounds = new(2f, 3f, 148f, 54f),
                },
                new()
                {
                    SceneName = "Tut_03",
                    Position = new(-111f, 94f),
                    PlayableBounds = new(3f, 2f, 123f, 33f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][]
                        {
                            ["church front collider"],
                            ["bone_church_arch (4)"],
                            ["bone_church_04 (57)"],
                        };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        // Stop collider jutting into transition corridor
                        TilemapUtils.UpdateTilemapPoints(
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
                new()
                {
                    SceneName = "Weave_04",
                    Position = new(189f, -33f),
                    PlayableBounds = new(0f, 92f, 90f, 12f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][]
                        {
                            ["terrain collider"],
                            ["terrain collider (1)"],
                            ["terrain collider (2)"],
                            ["terrain collider (3)"],
                            ["terrain collider (4)"],
                            ["terrain collider (5)"],
                            ["terrain collider (6)"],
                            ["terrain collider (7)"],
                            ["terrain collider (8)"],
                            ["terrain collider (9)"],
                            ["terrain collider (10)"],
                            ["terrain collider (13)"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 0"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 1"],
                            ["TileMap Render Data", "Scenemap", "Chunk 0 2"],
                            ["TileMap Render Data", "Scenemap", "Chunk 1 0"],
                            ["TileMap Render Data", "Scenemap", "Chunk 1 1"],
                            ["TileMap Render Data", "Scenemap", "Chunk 1 2"],
                            ["Weaver Servitor (2)"],
                        };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        var chunksToClamp = new string[] { "Chunk 2 0", "Chunk 2 1", "Chunk 2 2" };
                        foreach (var chunkName in chunksToClamp)
                        {
                            TilemapUtils.ClampEdgeColliderPoints(
                                scene,
                                ["TileMap Render Data", "Scenemap", chunkName],
                                0,
                                p => new(p.x, Mathf.Max(p.y, 26f))
                            );
                        }
                    },
                },
                new()
                {
                    SceneName = "Weave_02",
                    Position = new(280f, -118f),
                    PlayableBounds = new(0f, 105f, 45f, 88f),
                },
                new()
                {
                    SceneName = "Weave_11",
                    Position = new(150f, 3f),
                    PlayableBounds = new(5f, 0f, 125f, 32f),
                    OnLoad = scene =>
                    {
                        var toHide = new string[][] { ["Remasker New Sharp Ultra"] };
                        foreach (var path in toHide)
                            Utils.FindGameObjectByPath(scene, path).SetActive(false);

                        TilemapUtils.ClampColliderPoints(
                            scene,
                            ["Roof Collider_Basic"],
                            p => new(p.x, Math.Min(p.y, 12f))
                        );

                        var boxColsToShrink = new string[][]
                        {
                            ["terrain collider (2)"],
                            ["terrain collider (3)"],
                        };
                        var amountToLower = 8f;
                        foreach (var path in boxColsToShrink)
                        {
                            var tr = Utils.FindGameObjectByPath(scene, path).transform;
                            tr.localScale = new(
                                tr.localScale.x,
                                tr.localScale.y - amountToLower,
                                tr.localScale.z
                            );
                            tr.localPosition = new(
                                tr.localPosition.x,
                                tr.localPosition.y - amountToLower / 2f,
                                tr.localPosition.z
                            );
                        }
                    },
                },
                new()
                {
                    SceneName = "Weave_14",
                    Position = new(204f, 35f),
                    PlayableBounds = new(29f, 0f, 45f, 18f),
                    OnLoad = scene =>
                    {
                        TilemapUtils.ClampColliderPoints(
                            scene,
                            ["Roof Collider_Basic (1)"],
                            p => new(p.x, Math.Min(p.y, 7f))
                        );
                    },
                },
                // ==================

                // TODO: Fix up chunks below
                new()
                {
                    SceneName = "Bone_East_01",
                    Position = new(975f, 105f),
                    PlayableBounds = new(0f, 0f, 47f, 81f),
                },
                new()
                {
                    SceneName = "Bellway_02",
                    Position = new(854f, 80f),
                    PlayableBounds = new(0f, 12f, 120f, 36f),
                },
                new()
                {
                    SceneName = "Dock_16",
                    Position = new(822f, 87f),
                    PlayableBounds = new(2f, 0f, 73f, 38f),
                },
                new()
                {
                    SceneName = "Bone_East_12",
                    Position = new(1022f, 105f),
                    PlayableBounds = new(0f, 0f, 180f, 30f),
                },
                new()
                {
                    SceneName = "Room_Forge",
                    Position = new(1018f, 50f),
                    PlayableBounds = new(0f, 1f, 140f, 68f),
                },
                new()
                {
                    SceneName = "Dock_04",
                    Position = new(988f, -39f),
                    PlayableBounds = new(0f, 0f, 30f, 103f),
                },
                new()
                {
                    SceneName = "Bone_East_13",
                    Position = new(1018f, 7f),
                    PlayableBounds = new(0f, 4f, 125f, 28f),
                },
                new()
                {
                    SceneName = "Dock_06_Church",
                    Position = new(913f, -39f),
                    PlayableBounds = new(2f, 0f, 73f, 33f),
                },
                new()
                {
                    SceneName = "Abyss_09",
                    Position = new(919f, -1064f),
                    PlayableBounds = new(2f, 0f, 54f, 1023f),
                },
                new()
                {
                    SceneName = "Dock_15",
                    Position = new(1052f, -81f),
                    PlayableBounds = new(0f, 1f, 29f, 74f),
                },
                new()
                {
                    SceneName = "Dock_13",
                    Position = new(1017f, -115f),
                    PlayableBounds = new(6f, 0f, 29f, 74f),
                },
                new()
                {
                    SceneName = "Dock_09",
                    Position = new(1081f, -28f),
                    PlayableBounds = new(0f, 3f, 77f, 27f),
                },
                new()
                {
                    SceneName = "Dock_14",
                    Position = new(1081f, -46f),
                    PlayableBounds = new(0f, 4f, 26f, 11f),
                },
                new()
                {
                    SceneName = "Dock_10",
                    Position = new(1017f, -61f),
                    PlayableBounds = new(0f, 27f, 35f, 15f),
                },
                new()
                {
                    SceneName = "Dock_11",
                    Position = new(1081f, -136f),
                    PlayableBounds = new(0f, 0f, 148f, 74f),
                },
                new()
                {
                    SceneName = "Dock_12",
                    Position = new(1229f, -100f),
                    PlayableBounds = new(0f, 16f, 44f, 49f),
                },
                new()
                {
                    SceneName = "Dock_02",
                    Position = new(1158f, -18f),
                    PlayableBounds = new(0f, 0f, 125f, 99f),
                },
                new()
                {
                    SceneName = "Dock_02b",
                    Position = new(1283f, -18f),
                    PlayableBounds = new(0f, 0f, 94f, 116f),
                },
                new()
                {
                    SceneName = "Dock_03c",
                    Position = new(1377f, -18f),
                    PlayableBounds = new(0f, 0f, 161f, 80f),
                },
                new()
                {
                    SceneName = "Dock_03",
                    Position = new(1377f, -18f),
                    PlayableBounds = new(0f, 0f, 168f, 116f),
                },
                new()
                {
                    SceneName = "Weave_12",
                    Position = new(325f, 50f),
                    PlayableBounds = new(0f, 7f, 52f, 13f),
                },
                new()
                {
                    SceneName = "Weave_13",
                    Position = new(325f, 34f),
                    PlayableBounds = new(0f, 7f, 60f, 11f),
                },
                new()
                {
                    SceneName = "Weave_08",
                    Position = new(353f, 4f),
                    PlayableBounds = new(0f, 13f, 77f, 52f),
                },
                new()
                {
                    SceneName = "Weave_05b",
                    Position = new(330f, 12f),
                    PlayableBounds = new(0f, 0f, 217f, 42f),
                },
                new()
                {
                    SceneName = "Weave_10",
                    Position = new(293f, -43f),
                    PlayableBounds = new(0f, 9f, 90f, 31f),
                },
                new()
                {
                    SceneName = "Weave_07",
                    Position = new(220f, -118f),
                    PlayableBounds = new(0f, 95f, 60f, 22f),
                },
                new()
                {
                    SceneName = "Weave_03",
                    Position = new(-64f, -38f),
                    PlayableBounds = new(2f, 0f, 282f, 33f),
                },
                new()
                {
                    SceneName = "Abyss_13",
                    Position = new(947f, -1100f),
                    PlayableBounds = new(0f, 5f, 240f, 31f),
                },
                new()
                {
                    SceneName = "Crawl_07",
                    Position = new(-250f, 194f),
                    PlayableBounds = new(0f, 0f, 82f, 58f),
                },
                new()
                {
                    SceneName = "Crawl_09",
                    Position = new(-400f, 194f),
                    PlayableBounds = new(0f, 2f, 150f, 52f),
                },
                new()
                {
                    SceneName = "Crawl_10",
                    Position = new(-441f, 216f),
                    PlayableBounds = new(8f, 6f, 45f, 12f),
                },
                new()
                {
                    SceneName = "Crawl_03b",
                    Position = new(-302f, 252f),
                    PlayableBounds = new(2f, 0f, 133f, 21f),
                },
                new()
                {
                    SceneName = "Crawl_05",
                    Position = new(-574f, 271f),
                    PlayableBounds = new(2f, 7f, 223f, 26f),
                },
                new()
                {
                    SceneName = "Crawl_02",
                    Position = new(-167f, 231f),
                    PlayableBounds = new(0f, 0f, 30f, 148f),
                },
                new()
                {
                    SceneName = "Crawl_03",
                    Position = new(-349f, 274f),
                    PlayableBounds = new(0f, 0f, 180f, 77f),
                },
                new()
                {
                    SceneName = "Crawl_08",
                    Position = new(-294f, 351f),
                    PlayableBounds = new(35f, 0f, 65f, 20f),
                },
                new()
                {
                    SceneName = "Crawl_04",
                    Position = new(-154f, 251f),
                    PlayableBounds = new(2f, 2f, 163f, 17f),
                },
                new()
                {
                    SceneName = "Crawl_06",
                    Position = new(-137f, 267f),
                    PlayableBounds = new(0f, 4f, 83f, 29f),
                },
                new()
                {
                    SceneName = "Crawl_01",
                    Position = new(-137f, 286f),
                    PlayableBounds = new(0f, 17f, 150f, 99f),
                },
                new()
                {
                    SceneName = "Bone_07",
                    Position = new(570f, 228f),
                    PlayableBounds = new(0f, 1f, 92f, 73f),
                },
                new()
                {
                    SceneName = "Bone_11",
                    Position = new(53f, 219f),
                    PlayableBounds = new(0f, 0f, 120f, 31f),
                },
                new()
                {
                    SceneName = "Bone_04",
                    Position = new(173f, 219f),
                    PlayableBounds = new(0f, 0f, 233f, 31f),
                },
                new()
                {
                    SceneName = "Bone_05",
                    Position = new(174f, 250f),
                    PlayableBounds = new(0f, 0f, 201f, 20f),
                },
                new()
                {
                    SceneName = "Mosstown_01",
                    Position = new(36f, 250f),
                    PlayableBounds = new(21f, 0f, 119f, 30f),
                },
                new()
                {
                    SceneName = "Bone_10",
                    Position = new(410f, 172f),
                    PlayableBounds = new(0f, 0f, 115f, 67f),
                },
                new()
                {
                    SceneName = "Bone_14",
                    Position = new(435f, 235f),
                    PlayableBounds = new(0f, 4f, 135f, 33f),
                },
                new()
                {
                    SceneName = "Ant_02",
                    Position = new(694f, 218f),
                    PlayableBounds = new(0f, 1f, 130f, 17f),
                },
                new()
                {
                    SceneName = "Belltown_basement_03",
                    Position = new(631f, 247f),
                    PlayableBounds = new(0f, 4f, 145f, 146f),
                },
                new()
                {
                    SceneName = "Ant_03",
                    Position = new(824f, 219f),
                    PlayableBounds = new(0f, 1f, 50f, 75f),
                },
                new()
                {
                    SceneName = "Ant_04_left",
                    Position = new(874f, 260f),
                    PlayableBounds = new(0f, 2f, 145f, 32f),
                },
                new()
                {
                    SceneName = "Ant_04_mid",
                    Position = new(879f, 260f),
                    PlayableBounds = new(0f, 3f, 235f, 23f),
                },
                new()
                {
                    SceneName = "Ant_04",
                    Position = new(884f, 260f),
                    PlayableBounds = new(0f, 1f, 384f, 32f),
                },
                new()
                {
                    SceneName = "Ant_14",
                    Position = new(1268f, 243f),
                    PlayableBounds = new(0f, 2f, 29f, 123f),
                },
                new()
                {
                    SceneName = "Ant_05b",
                    Position = new(1148f, 244f),
                    PlayableBounds = new(16f, 0f, 104f, 18f),
                },
                new()
                {
                    SceneName = "Bone_East_04b",
                    Position = new(1161f, 144f),
                    PlayableBounds = new(0f, 0f, 0f, 0f),
                },
                new()
                {
                    SceneName = "Bone_East_05",
                    Position = new(971f, 182f),
                    PlayableBounds = new(0f, 3f, 190f, 42f),
                },
                new()
                {
                    SceneName = "Dock_05",
                    Position = new(1022f, 136f),
                    PlayableBounds = new(0f, 2f, 28f, 13f),
                },
                new()
                {
                    SceneName = "Bone_East_03",
                    Position = new(1022f, 149f),
                    PlayableBounds = new(0f, 4f, 220f, 38f),
                },
                new()
                {
                    SceneName = "Bone_East_04",
                    Position = new(1169f, 144f),
                    PlayableBounds = new(0f, 0f, 83f, 100f),
                },
                new()
                {
                    SceneName = "Bellshrine_05",
                    Position = new(1181f, 108f),
                    PlayableBounds = new(0f, 3f, 43f, 14f),
                },
                new()
                {
                    SceneName = "Bone_East_02",
                    Position = new(1200f, 105f),
                    PlayableBounds = new(0f, 0f, 162f, 35f),
                },
                new()
                {
                    SceneName = "Bone_East_02b",
                    Position = new(1209f, 105f),
                    PlayableBounds = new(132f, 0f, 198f, 35f),
                },
                new()
                {
                    SceneName = "Bone_East_07",
                    Position = new(1539f, 50f),
                    PlayableBounds = new(0f, 0f, 40f, 190f),
                },
                new()
                {
                    SceneName = "Dock_03b",
                    Position = new(1372f, -12f),
                    PlayableBounds = new(20f, 78f, 148f, 36f),
                },
                new()
                {
                    SceneName = "Bone_East_08",
                    Position = new(1579f, 46f),
                    PlayableBounds = new(0f, 0f, 150f, 48f),
                },
                new()
                {
                    SceneName = "Bone_East_09",
                    Position = new(1729f, 34f),
                    PlayableBounds = new(0f, 0f, 100f, 97f),
                },
                new()
                {
                    SceneName = "Bone_East_14",
                    Position = new(1829f, 34f),
                    PlayableBounds = new(0f, 0f, 140f, 74f),
                },
                new()
                {
                    SceneName = "Bone_East_14b",
                    Position = new(1833f, 34f),
                    PlayableBounds = new(0f, 0f, 309f, 73f),
                },
                new()
                {
                    SceneName = "Bone_East_Weavehome",
                    Position = new(2128f, -50f),
                    PlayableBounds = new(0f, 55f, 205f, 50f),
                },
                new()
                {
                    SceneName = "Bone_East_09b",
                    Position = new(1729f, 34f),
                    PlayableBounds = new(0f, 0f, 96f, 200f),
                },
                new()
                {
                    SceneName = "Bone_East_20",
                    Position = new(1579f, 94f),
                    PlayableBounds = new(66f, 4f, 84f, 27f),
                },
                new()
                {
                    SceneName = "Bone_East_21",
                    Position = new(1579f, 97f),
                    PlayableBounds = new(0f, 4f, 25f, 8f),
                },
                new()
                {
                    SceneName = "Bellway_03",
                    Position = new(1579f, 126f),
                    PlayableBounds = new(0f, 4f, 150f, 53f),
                },
                new()
                {
                    SceneName = "Bone_06",
                    Position = new(305f, 309f),
                    PlayableBounds = new(0f, 0f, 112f, 48f),
                },
                new()
                {
                    SceneName = "Bone_05b",
                    Position = new(176f, 252f),
                    PlayableBounds = new(0f, 15f, 77f, 15f),
                },
                new()
                {
                    SceneName = "Mosstown_02",
                    Position = new(62f, 252f),
                    PlayableBounds = new(0f, 16f, 160f, 47f),
                },
                new()
                {
                    SceneName = "Mosstown_02c",
                    Position = new(222f, 282f),
                    PlayableBounds = new(0f, 2f, 53f, 11f),
                },
                new()
                {
                    SceneName = "Mosstown_03",
                    Position = new(271f, 289f),
                    PlayableBounds = new(3f, 27f, 31f, 119f),
                },
                new()
                {
                    SceneName = "Shellwood_25",
                    Position = new(305f, 387f),
                    PlayableBounds = new(0f, 4f, 283f, 28f),
                },
                new()
                {
                    SceneName = "Bone_18",
                    Position = new(417f, 311f),
                    PlayableBounds = new(0f, 5f, 45f, 26f),
                },
                new()
                {
                    SceneName = "Shellwood_03",
                    Position = new(264f, 435f),
                    PlayableBounds = new(0f, 0f, 40f, 132f),
                },
                new()
                {
                    SceneName = "Shellwood_16",
                    Position = new(304f, 437f),
                    PlayableBounds = new(0f, 0f, 85f, 23f),
                },
                new()
                {
                    SceneName = "Abyss_11",
                    Position = new(903f, -1130f),
                    PlayableBounds = new(16f, 0f, 28f, 55f),
                },
                new()
                {
                    SceneName = "Abyss_02b",
                    Position = new(769f, -1185f),
                    PlayableBounds = new(0f, 2f, 228f, 53f),
                },
                new()
                {
                    SceneName = "Abyss_01",
                    Position = new(719f, -1320f),
                    PlayableBounds = new(0f, 27f, 50f, 132f),
                },
                new()
                {
                    SceneName = "Abyss_04",
                    Position = new(769f, -1263f),
                    PlayableBounds = new(0f, 0f, 105f, 79f),
                },
                new()
                {
                    SceneName = "Abyss_06",
                    Position = new(659f, -1302f),
                    PlayableBounds = new(11f, 9f, 49f, 40f),
                },
                new()
                {
                    SceneName = "Abyss_07",
                    Position = new(769f, -1298f),
                    PlayableBounds = new(0f, 0f, 130f, 17f),
                },
                new()
                {
                    SceneName = "Abyss_12",
                    Position = new(899f, -1298f),
                    PlayableBounds = new(0f, 7f, 33f, 29f),
                },
                new()
                {
                    SceneName = "Abyss_05",
                    Position = new(932f, -1298f),
                    PlayableBounds = new(0f, 0f, 190f, 102f),
                },
                new()
                {
                    SceneName = "Abyss_08",
                    Position = new(1122f, -1298f),
                    PlayableBounds = new(0f, 0f, 161f, 105f),
                },
                new()
                {
                    SceneName = "Abyss_02",
                    Position = new(997f, -1179f),
                    PlayableBounds = new(0f, 4f, 192f, 33f),
                },
                new()
                {
                    SceneName = "Abyss_03",
                    Position = new(1189f, -1162f),
                    PlayableBounds = new(0f, 1f, 82f, 84f),
                },
                new()
                {
                    SceneName = "Shellwood_04b",
                    Position = new(34f, 446f),
                    PlayableBounds = new(0f, 0f, 230f, 26f),
                },
                new()
                {
                    SceneName = "Shellgrave",
                    Position = new(114f, 472f),
                    PlayableBounds = new(46f, 0f, 89f, 21f),
                },
                new()
                {
                    SceneName = "Shellwood_08c",
                    Position = new(22f, 450f),
                    PlayableBounds = new(0f, 3f, 42f, 19f),
                },
                new()
                {
                    SceneName = "Bone_17",
                    Position = new(346f, 203f),
                    PlayableBounds = new(5f, 5f, 30f, 10f),
                },
                new()
                {
                    SceneName = "Bellshrine",
                    Position = new(351f, 259f),
                    PlayableBounds = new(0f, 3f, 43f, 14f),
                },
                new()
                {
                    SceneName = "Bone_East_04c",
                    Position = new(1252f, 219f),
                    PlayableBounds = new(1f, 3f, 66f, 12f),
                },
                new()
                {
                    SceneName = "Bone_East_15",
                    Position = new(1252f, 138f),
                    PlayableBounds = new(0f, 1f, 173f, 103f),
                },
                new()
                {
                    SceneName = "Bone_East_16",
                    Position = new(1372f, 140f),
                    PlayableBounds = new(4f, 1f, 52f, 24f),
                },
                new()
                {
                    SceneName = "Bone_East_17b",
                    Position = new(1429f, 140f),
                    PlayableBounds = new(115f, 64f, -114f, -63f),
                },
                new()
                {
                    SceneName = "Bone_East_17",
                    Position = new(1429f, 140f),
                    PlayableBounds = new(1f, 61f, 114f, 43f),
                },
                new()
                {
                    SceneName = "Bone_East_22",
                    Position = new(1579f, 177f),
                    PlayableBounds = new(1f, 4f, 54f, 12f),
                },
                new()
                {
                    SceneName = "Bone_East_10",
                    Position = new(1579f, 229f),
                    PlayableBounds = new(1f, 3f, 129f, 33f),
                },
                new()
                {
                    SceneName = "Bone_East_11",
                    Position = new(1541f, 240f),
                    PlayableBounds = new(1f, 1f, 39f, 277f),
                },
                new()
                {
                    SceneName = "Bone_East_10_Church",
                    Position = new(1574f, 229f),
                    PlayableBounds = new(130f, 7f, 67f, 21f),
                },
                new()
                {
                    SceneName = "Bone_East_18c",
                    Position = new(1711f, 246f),
                    PlayableBounds = new(1f, 10f, 79f, 8f),
                },
                new()
                {
                    SceneName = "Bone_East_18",
                    Position = new(1726f, 246f),
                    PlayableBounds = new(67f, 2f, 111f, 60f),
                },
                new()
                {
                    SceneName = "Bone_East_18b",
                    Position = new(1904f, 181f),
                    PlayableBounds = new(1f, 4f, 321f, 104f),
                },
                new()
                {
                    SceneName = "Bone_East_26",
                    Position = new(1870f, 289f),
                    PlayableBounds = new(4f, 2f, 197f, 46f),
                },
                new()
                {
                    SceneName = "Bone_East_24",
                    Position = new(1600f, 308f),
                    PlayableBounds = new(1f, 1f, 262f, 72f),
                },
                new()
                {
                    SceneName = "Bone_East_27",
                    Position = new(1863f, 338f),
                    PlayableBounds = new(1f, 5f, 49f, 56f),
                },
                new()
                {
                    SceneName = "Bone_East_25",
                    Position = new(1913f, 338f),
                    PlayableBounds = new(1f, 6f, 153f, 19f),
                },
                new()
                {
                    SceneName = "Bone_19",
                    Position = new(500f, 302f),
                    PlayableBounds = new(18f, 1f, 125f, 24f),
                },
                new()
                {
                    SceneName = "Shellwood_02",
                    Position = new(389f, 436f),
                    PlayableBounds = new(1f, 0f, 78f, 100f),
                },
                new()
                {
                    SceneName = "Shellwood_01",
                    Position = new(469f, 436f),
                    PlayableBounds = new(2f, 0f, 128f, 90f),
                },
                new()
                {
                    SceneName = "Belltown_07",
                    Position = new(601f, 435f),
                    PlayableBounds = new(3f, 6f, 67f, 22f),
                },
                new()
                {
                    SceneName = "Belltown_Room_shellwood",
                    Position = new(582f, 465f),
                    PlayableBounds = new(16f, 6f, 27f, 12f),
                },
                new()
                {
                    SceneName = "Belltown",
                    Position = new(651f, 435f),
                    PlayableBounds = new(22f, 6f, 87f, 66f),
                },
                new()
                {
                    SceneName = "Belltown_06",
                    Position = new(760f, 435f),
                    PlayableBounds = new(1f, 3f, 77f, 77f),
                },
                new()
                {
                    SceneName = "Greymoor_08",
                    Position = new(840f, 438f),
                    PlayableBounds = new(7f, 0f, 154f, 38f),
                },
                new()
                {
                    SceneName = "Greymoor_16",
                    Position = new(1001f, 419f),
                    PlayableBounds = new(1f, 0f, 166f, 60f),
                },
                new()
                {
                    SceneName = "Ant_20",
                    Position = new(1297f, 245f),
                    PlayableBounds = new(1f, 2f, 215f, 44f),
                },
                new()
                {
                    SceneName = "Ant_05c",
                    Position = new(1297f, 301f),
                    PlayableBounds = new(1f, 3f, 83f, 36f),
                },
                new()
                {
                    SceneName = "Ant_09",
                    Position = new(1381f, 316f),
                    PlayableBounds = new(1f, 1f, 169f, 45f),
                },
                new()
                {
                    SceneName = "Greymoor_07",
                    Position = new(916f, 476f),
                    PlayableBounds = new(1f, 1f, 74f, 96f),
                },
                new()
                {
                    SceneName = "Greymoor_20b",
                    Position = new(866f, 514f),
                    PlayableBounds = new(2f, 3f, 48f, 14f),
                },
                new()
                {
                    SceneName = "Greymoor_06",
                    Position = new(991f, 460f),
                    PlayableBounds = new(0f, 27f, 40f, 180f),
                },
                new()
                {
                    SceneName = "Greymoor_05",
                    Position = new(1031f, 492f),
                    PlayableBounds = new(1f, 4f, 109f, 69f),
                },
                new()
                {
                    SceneName = "Bellway_04",
                    Position = new(1129f, 477f),
                    PlayableBounds = new(1f, 1f, 90f, 26f),
                },
                new()
                {
                    SceneName = "Greymoor_04",
                    Position = new(1141f, 523f),
                    PlayableBounds = new(0f, 10f, 40f, 160f),
                },
                new()
                {
                    SceneName = "Greymoor_03",
                    Position = new(1181f, 523f),
                    PlayableBounds = new(0f, 6f, 120f, 134f),
                },
                new()
                {
                    SceneName = "Greymoor_13",
                    Position = new(1301f, 532f),
                    PlayableBounds = new(1f, 0f, 179f, 21f),
                },
                new()
                {
                    SceneName = "Greymoor_21",
                    Position = new(1363f, 507f),
                    PlayableBounds = new(12f, 4f, 64f, 21f),
                },
                new()
                {
                    SceneName = "Greymoor_01",
                    Position = new(1481f, 532f),
                    PlayableBounds = new(1f, 3f, 129f, 47f),
                },
                new()
                {
                    SceneName = "Greymoor_02",
                    Position = new(1611f, 512f),
                    PlayableBounds = new(1f, 4f, 89f, 144f),
                },
                new()
                {
                    SceneName = "Bellshrine_02",
                    Position = new(1605f, 545f),
                    PlayableBounds = new(8f, 3f, 25f, 14f),
                },
                new()
                {
                    SceneName = "Greymoor_15",
                    Position = new(1701f, 506f),
                    PlayableBounds = new(1f, 0f, 77f, 83f),
                },
                new()
                {
                    SceneName = "Greymoor_15b",
                    Position = new(1706f, 506f),
                    PlayableBounds = new(75f, 2f, 145f, 139f),
                },
                new()
                {
                    SceneName = "Clover_01",
                    Position = new(1926f, 526f),
                    PlayableBounds = new(0f, 0f, 175f, 21f),
                },
                new()
                {
                    SceneName = "Clover_20",
                    Position = new(2101f, 525f),
                    PlayableBounds = new(1f, 3f, 137f, 43f),
                },
                new()
                {
                    SceneName = "Greymoor_22",
                    Position = new(1805f, 647f),
                    PlayableBounds = new(16f, 0f, 90f, 48f),
                },
                new()
                {
                    SceneName = "Greymoor_17",
                    Position = new(1701f, 604f),
                    PlayableBounds = new(3f, 24f, 50f, 41f),
                },
                new()
                {
                    SceneName = "Dust_11",
                    Position = new(1678f, 669f),
                    PlayableBounds = new(1f, 1f, 133f, 35f),
                },
                new()
                {
                    SceneName = "Dust_06",
                    Position = new(1648f, 669f),
                    PlayableBounds = new(1f, 7f, 28f, 181f),
                },
                new()
                {
                    SceneName = "Dust_12",
                    Position = new(1678f, 722f),
                    PlayableBounds = new(1f, 1f, 40f, 13f),
                },
                new()
                {
                    SceneName = "Ant_17",
                    Position = new(1208f, 306f),
                    PlayableBounds = new(7f, 5f, 53f, 10f),
                },
                new()
                {
                    SceneName = "Ant_Merchant",
                    Position = new(1116f, 324f),
                    PlayableBounds = new(6f, 7f, 146f, 18f),
                },
                new()
                {
                    SceneName = "Ant_21",
                    Position = new(1159f, 286f),
                    PlayableBounds = new(40f, 73f, 69f, 12f),
                },
                new()
                {
                    SceneName = "Greymoor_12",
                    Position = new(1301f, 559f),
                    PlayableBounds = new(1f, 0f, 179f, 31f),
                },
                new()
                {
                    SceneName = "Greymoor_24",
                    Position = new(1301f, 591f),
                    PlayableBounds = new(1f, 4f, 80f, 20f),
                },
                new()
                {
                    SceneName = "Dust_01",
                    Position = new(1280f, 625f),
                    PlayableBounds = new(0f, 2f, 155f, 24f),
                },
                new()
                {
                    SceneName = "Dust_02",
                    Position = new(1435f, 625f),
                    PlayableBounds = new(0f, 0f, 40f, 129f),
                },
                new()
                {
                    SceneName = "Dust_03",
                    Position = new(1475f, 628f),
                    PlayableBounds = new(1f, 1f, 139f, 39f),
                },
                new()
                {
                    SceneName = "Dust_Barb",
                    Position = new(1574f, 596f),
                    PlayableBounds = new(16f, 0f, 25f, 32f),
                },
                new()
                {
                    SceneName = "Dust_Chef",
                    Position = new(1582f, 668f),
                    PlayableBounds = new(2f, 1f, 54f, 67f),
                },
                new()
                {
                    SceneName = "Dust_04",
                    Position = new(1475f, 659f),
                    PlayableBounds = new(1f, 17f, 102f, 71f),
                },
                new()
                {
                    SceneName = "Dust_10",
                    Position = new(1275f, 658f),
                    PlayableBounds = new(16f, 3f, 144f, 75f),
                },
                new()
                {
                    SceneName = "Dust_05",
                    Position = new(1298f, 755f),
                    PlayableBounds = new(1f, 0f, 351f, 29f),
                },
                new()
                {
                    SceneName = "Shadow_05",
                    Position = new(1678f, 760f),
                    PlayableBounds = new(1f, 0f, 211f, 46f),
                },
            ]
        );
}
