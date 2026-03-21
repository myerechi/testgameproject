using System.Collections.Generic;
using UnityEngine;

public enum PixelSpriteId
{
    HeroKnight = 0,
    Slime = 1,
    Goblin = 2,
    GroundA = 3,
    GroundB = 4,
    TunnelRim = 5,
    TunnelHole = 6,
    StageSky = 7,
    StageGround = 8,
    Pipe = 9,
    Goal = 10,
    ExpGem = 11,
    EnemyDart = 12,
    ShotPulse = 13,
    ShotSpread = 14,
    ShotPierce = 15,
    ShotNova = 16
}

public static class PixelArtFactory
{
    private static readonly Dictionary<PixelSpriteId, Sprite> Cache = new();

    public static Sprite Get(PixelSpriteId id)
    {
        if (Cache.TryGetValue(id, out var sprite))
        {
            return sprite;
        }

        sprite = id switch
        {
            PixelSpriteId.HeroKnight => BuildHeroKnight(),
            PixelSpriteId.Slime => BuildSlime(),
            PixelSpriteId.Goblin => BuildGoblin(),
            PixelSpriteId.GroundA => BuildGroundA(),
            PixelSpriteId.GroundB => BuildGroundB(),
            PixelSpriteId.TunnelRim => BuildTunnelRim(),
            PixelSpriteId.TunnelHole => BuildTunnelHole(),
            PixelSpriteId.StageSky => BuildStageSky(),
            PixelSpriteId.StageGround => BuildStageGround(),
            PixelSpriteId.Pipe => BuildPipe(),
            PixelSpriteId.Goal => BuildGoal(),
            PixelSpriteId.ExpGem => BuildExpGem(),
            PixelSpriteId.EnemyDart => BuildEnemyDart(),
            PixelSpriteId.ShotPulse => BuildShotPulse(),
            PixelSpriteId.ShotSpread => BuildShotSpread(),
            PixelSpriteId.ShotPierce => BuildShotPierce(),
            PixelSpriteId.ShotNova => BuildShotNova(),
            _ => BuildGroundA()
        };

        Cache[id] = sprite;
        return sprite;
    }

    private static Sprite BuildHeroKnight()
    {
        return BuildSprite(
            "HeroKnight",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['s'] = new Color32(197, 165, 120, 255),
                ['h'] = new Color32(115, 63, 33, 255),
                ['a'] = new Color32(198, 205, 216, 255),
                ['b'] = new Color32(74, 104, 176, 255),
                ['d'] = new Color32(42, 58, 99, 255)
            },
            new[]
            {
                "................",
                "......hhhh......",
                ".....hssssh.....",
                "....hssssssh....",
                "...hhshsshsdh...",
                "...haaaaassdh...",
                "...haaaaassdh...",
                "...hbbbbbbbdh...",
                "...hbbbbbbbdh...",
                "...hbbbaabbdh...",
                "...hbbb..bbdh...",
                "...haaa..aaah...",
                "...haaa..aaah...",
                "...haaa..aaah...",
                "....aa....aa....",
                "................"
            });
    }

    private static Sprite BuildSlime()
    {
        return BuildSprite(
            "Slime",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['o'] = new Color32(28, 115, 77, 255),
                ['g'] = new Color32(71, 211, 132, 255),
                ['m'] = new Color32(38, 156, 98, 255),
                ['e'] = new Color32(250, 250, 250, 255),
                ['x'] = new Color32(16, 26, 18, 255)
            },
            new[]
            {
                "................",
                "................",
                ".....oooooo.....",
                "....oggggggo....",
                "...oggggggggo...",
                "...oggeggeggo...",
                "...oggggggggo...",
                "..omggmmggmmmo..",
                "..ommmmmmmmmmo..",
                "....ommmmmmo....",
                ".....oooooo.....",
                "......oxxo......",
                "................",
                "................",
                "................",
                "................"
            });
    }

    private static Sprite BuildGoblin()
    {
        return BuildSprite(
            "Goblin",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['g'] = new Color32(94, 190, 84, 255),
                ['d'] = new Color32(39, 109, 47, 255),
                ['e'] = new Color32(250, 250, 250, 255),
                ['x'] = new Color32(20, 24, 20, 255),
                ['c'] = new Color32(120, 80, 45, 255),
                ['l'] = new Color32(54, 67, 89, 255)
            },
            new[]
            {
                "................",
                "......d..d......",
                ".....dggggd.....",
                "....dggggggd....",
                "...dggexxeggd...",
                "...dggggggggd...",
                "...dgggccccgd...",
                "...dgggccccgd...",
                "...dlllccccld...",
                "...dlll....ld...",
                "...dlll....ld...",
                "....ll......ll..",
                "....ll......ll..",
                "................",
                "................",
                "................"
            });
    }

    private static Sprite BuildGroundA()
    {
        return BuildSprite(
            "GroundA",
            new Dictionary<char, Color32>
            {
                ['a'] = new Color32(31, 42, 54, 255),
                ['b'] = new Color32(37, 51, 66, 255),
                ['c'] = new Color32(24, 33, 44, 255)
            },
            new[]
            {
                "ababaabb",
                "babbabca",
                "abcaabbb",
                "bababbca",
                "abbacaba",
                "bbabacaa",
                "abacbabb",
                "bbaabaca"
            },
            8f);
    }

    private static Sprite BuildGroundB()
    {
        return BuildSprite(
            "GroundB",
            new Dictionary<char, Color32>
            {
                ['a'] = new Color32(26, 37, 48, 255),
                ['b'] = new Color32(33, 47, 61, 255),
                ['c'] = new Color32(20, 29, 38, 255)
            },
            new[]
            {
                "baababca",
                "abacabba",
                "bacabbaa",
                "ababbcaa",
                "bbacaabb",
                "abbbacaa",
                "bacaabab",
                "aabacabb"
            },
            8f);
    }

    private static Sprite BuildTunnelRim()
    {
        return BuildSprite(
            "TunnelRim",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['r'] = new Color32(131, 89, 51, 255),
                ['h'] = new Color32(161, 120, 76, 255),
                ['d'] = new Color32(78, 52, 31, 255)
            },
            new[]
            {
                "................",
                "......rrrr......",
                "....rrhhhhrr....",
                "...rhhhhhhhhr...",
                "..rhhhddddhhhr..",
                "..rhhddddddhhr..",
                ".rhhddd..dddhhr.",
                ".rhhdd....ddhhr.",
                ".rhhdd....ddhhr.",
                ".rhhddd..dddhhr.",
                "..rhhddddddhhr..",
                "..rhhhddddhhhr..",
                "...rhhhhhhhhr...",
                "....rrhhhhrr....",
                "......rrrr......",
                "................"
            });
    }

    private static Sprite BuildTunnelHole()
    {
        return BuildSprite(
            "TunnelHole",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['d'] = new Color32(16, 12, 20, 255),
                ['b'] = new Color32(8, 7, 10, 255)
            },
            new[]
            {
                "................",
                "................",
                ".....dddddd.....",
                "...dddddddddd...",
                "..ddbbbbbbbbdd..",
                "..ddbbbbbbbbdd..",
                ".ddbbbbbbbbbbdd.",
                ".ddbbbbbbbbbbdd.",
                ".ddbbbbbbbbbbdd.",
                ".ddbbbbbbbbbbdd.",
                "..ddbbbbbbbbdd..",
                "..ddbbbbbbbbdd..",
                "...dddddddddd...",
                ".....dddddd.....",
                "................",
                "................"
            });
    }

    private static Sprite BuildStageSky()
    {
        return BuildSprite(
            "StageSky",
            new Dictionary<char, Color32>
            {
                ['a'] = new Color32(29, 45, 84, 255),
                ['b'] = new Color32(35, 58, 104, 255),
                ['c'] = new Color32(44, 73, 126, 255)
            },
            new[]
            {
                "abcccbba",
                "bcccccba",
                "cccccccb",
                "cccccccb",
                "bcccccba",
                "abcccbaa",
                "aabbbbaa",
                "aaaaaaba"
            },
            8f);
    }

    private static Sprite BuildStageGround()
    {
        return BuildSprite(
            "StageGround",
            new Dictionary<char, Color32>
            {
                ['a'] = new Color32(95, 66, 35, 255),
                ['b'] = new Color32(131, 95, 52, 255),
                ['c'] = new Color32(69, 46, 24, 255)
            },
            new[]
            {
                "bbbbbbbb",
                "baccacbb",
                "bcabbcbb",
                "bbaccabb",
                "bcbbacbb",
                "bbacccbb",
                "bccabacb",
                "bbbbbbbb"
            },
            8f);
    }

    private static Sprite BuildPipe()
    {
        return BuildSprite(
            "Pipe",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['h'] = new Color32(52, 148, 63, 255),
                ['b'] = new Color32(39, 115, 48, 255),
                ['d'] = new Color32(25, 79, 33, 255)
            },
            new[]
            {
                "................",
                "...hhhhhhhhhh...",
                "..hbbbbbbbbbbh..",
                "..hbbbbbbbbbbh..",
                "...hbbbbbbbbh...",
                "...hbbbbbbbbh...",
                "...hbbbbbbbbh...",
                "...hbbbbbbbbh...",
                "...hbbbbbbbbh...",
                "...hbbbbbbbbh...",
                "...hbbbbbbbbh...",
                "...hbbbbbbbbh...",
                "...hbbbbbbbbh...",
                "...hbbbbbbbbh...",
                "...hddddddddh...",
                "................"
            });
    }

    private static Sprite BuildGoal()
    {
        return BuildSprite(
            "Goal",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['p'] = new Color32(210, 210, 210, 255),
                ['y'] = new Color32(255, 224, 84, 255),
                ['o'] = new Color32(198, 145, 30, 255)
            },
            new[]
            {
                "...p............",
                "...p..yyyy......",
                "...p.yyyyyo.....",
                "...p.yyyyyo.....",
                "...p..yyyy......",
                "...p............",
                "...p............",
                "...p............",
                "...p............",
                "...p............",
                "...p............",
                "...p............",
                "...p............",
                "...p............",
                "...p............",
                "...p............"
            });
    }

    private static Sprite BuildExpGem()
    {
        return BuildSprite(
            "ExpGem",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['c'] = new Color32(72, 227, 233, 255),
                ['h'] = new Color32(195, 250, 255, 255),
                ['d'] = new Color32(28, 145, 168, 255)
            },
            new[]
            {
                "........",
                "...cc...",
                "..chhc..",
                ".chhhhc.",
                ".chhhhc.",
                "..chhc..",
                "...dd...",
                "........"
            },
            8f);
    }

    private static Sprite BuildEnemyDart()
    {
        return BuildSprite(
            "EnemyDart",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['s'] = new Color32(209, 209, 214, 255),
                ['d'] = new Color32(112, 112, 116, 255)
            },
            new[]
            {
                "...s....",
                "..sss...",
                ".ssssd..",
                "..sss...",
                "...s....",
                "........",
                "........",
                "........"
            },
            8f);
    }

    private static Sprite BuildShotPulse()
    {
        return BuildSprite(
            "ShotPulse",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['y'] = new Color32(255, 224, 95, 255),
                ['h'] = new Color32(255, 244, 188, 255)
            },
            new[]
            {
                "........",
                "...yy...",
                "..yhhy..",
                "..yhhy..",
                "...yy...",
                "........",
                "........",
                "........"
            },
            8f);
    }

    private static Sprite BuildShotSpread()
    {
        return BuildSprite(
            "ShotSpread",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['o'] = new Color32(255, 139, 60, 255),
                ['h'] = new Color32(255, 210, 142, 255)
            },
            new[]
            {
                "........",
                "...oo...",
                "..ohho..",
                "..ohho..",
                "...oo...",
                "........",
                "........",
                "........"
            },
            8f);
    }

    private static Sprite BuildShotPierce()
    {
        return BuildSprite(
            "ShotPierce",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['c'] = new Color32(87, 233, 255, 255),
                ['h'] = new Color32(209, 252, 255, 255)
            },
            new[]
            {
                "........",
                "...cc...",
                "..chhc..",
                "..chhc..",
                "...cc...",
                "........",
                "........",
                "........"
            },
            8f);
    }

    private static Sprite BuildShotNova()
    {
        return BuildSprite(
            "ShotNova",
            new Dictionary<char, Color32>
            {
                ['.'] = new Color32(0, 0, 0, 0),
                ['p'] = new Color32(255, 112, 220, 255),
                ['h'] = new Color32(255, 210, 246, 255)
            },
            new[]
            {
                "........",
                "...pp...",
                "..phhp..",
                "..phhp..",
                "...pp...",
                "........",
                "........",
                "........"
            },
            8f);
    }

    private static Sprite BuildSprite(
        string name,
        IReadOnlyDictionary<char, Color32> palette,
        IReadOnlyList<string> rows,
        float pixelsPerUnit = 16f)
    {
        int height = rows.Count;
        int width = rows[0].Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            string row = rows[height - 1 - y];
            for (int x = 0; x < width; x++)
            {
                char key = row[x];
                if (!palette.TryGetValue(key, out var color))
                {
                    color = new Color32(0, 0, 0, 0);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
        sprite.name = name;
        return sprite;
    }
}
