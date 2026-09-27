using UnityEngine;

//rules of placing items, returns bool whether item can be placed at that specific coordinate
public class LevelRules : MonoBehaviour
{
    [Header("Mario's abilities - measure these in play mode!")]
    public int maxStepUp = 4;           // highest ledge Mario can jump onto
    public int maxGap = 3;              // widest pit Mario can jump across

    [Header("Ground and pits")]
    public int minGap = 2;
    public int maxStepDown = 4;         // bigger drops are allowed physically, this just keeps it looking sane
    public int maxStepUpAcrossGap = 1;  // jumping a pit AND going up at the same time is harder
    public int minRunBeforeGap = 3;     // flat ground needed before/after a pit (run-up and landing)
    public int safeZoneLength = 8;      // nothing spawns in the first/last N columns

    [Header("Floating platforms")]
    public int maxGapWithPlatform = 7;  // pits wider than maxGap must have a platform in the middle
    public int platformClearance = 3;   // empty rows between the ground and a platform above it

    [Header("Bricks and ? blocks")]
    public int blockClearance = 3;      // empty rows between the ground and a block row
    public int maxBlockRowLength = 5;
    [Range(0, 1)] public float questionChance = 0.3f;

    [Header("Pipes")]
    public int minPipeHeight = 2;       // the pipe top sprite alone is 2 tiles tall
    public int maxPipeHeight = 3;
    public int minPipeSpacing = 4;


    [Header("Enemies")]
    public int goombaPatrol = 2;        // must match maxOffset in EnemyMovement.cs
    public int minEnemySpacing = 6;

    [Header("General spacing")]
    public int dangerSpacing = 2;       // min tiles between pits  / pipes
    public int edgeClearance = 2;       // columns after a step up / pit landing with nothing overhead, so Mario's jump isn't blocked


    //a landing edge is the first ground column after a step up or after a pit (any height)
    public bool IsLandingEdge(LevelData level, int x)
    {
        if (x <= 0 || x >= level.width) return false;
        int h = level.groundHeight[x];
        int before = level.groundHeight[x - 1];
        return h > 0 && (before == 0 || before < h);
    }

    //checks if any column fromX..toX is within edgeClearance columns after a landing edge
    public bool NearLandingEdge(LevelData level, int fromX, int toX)
    {
        for (int cx = fromX; cx <= toX; cx++)
        {
            // cx is too close if one of the edgeClearance columns ending at cx is a landing edge
            for (int e = cx - edgeClearance + 1; e <= cx; e++)
            {
                if (IsLandingEdge(level, e)) return true;
            }
        }
        return false;
    }

    //used for random walk to ensure no goomba spawns next to mario and triggers rewind immediately
    public bool InSafeZone(LevelData level, int fromX, int toX)
    {
        return fromX < safeZoneLength || toX >= level.width - safeZoneLength;
    }


    //used for random walk on how high up/down can the ground vary from
    public bool IsValidStep(int fromHeight, int toHeight)
    {
        int dy = toHeight - fromHeight;
        return dy <= maxStepUp && -dy <= maxStepDown;
    }

    //used for random gap to ensure pits (with and without platform) is not too far for mario
    public bool IsValidGap(int gapWidth, int fromHeight, int toHeight, bool hasPlatform)
    {
        int widest = hasPlatform ? maxGapWithPlatform : maxGap;
        if (gapWidth < minGap || gapWidth > widest) return false;
        if (toHeight - fromHeight > maxStepUpAcrossGap) return false;
        if (fromHeight - toHeight > maxStepDown) return false;
        return true;
    }

    //checks if a pipe can be placed at the coordinate
    public bool CanPlacePipe(LevelData level, int x, int pipeHeight)
    {
        if (InSafeZone(level, x, x + 1)) return false;

        // must be low enough to jump over
        if (pipeHeight < minPipeHeight || pipeHeight > maxPipeHeight || pipeHeight > maxStepUp) return false;

        // flat under the pipe plus landing room on both sides (also keeps it away from pits and ledges)
        if (!level.IsFlat(x - dangerSpacing, 2 + dangerSpacing * 2)) return false;

        // not too close to other pipes or
        if (level.AnyInColumns(TileType.Pipe, x - minPipeSpacing, x + 1 + minPipeSpacing)) return false;

        // nothing floating above it, Mario gets wedged between pipe and block
        if (level.AnyInColumns(TileType.Brick, x - 1, x + 2)) return false;
        if (level.AnyInColumns(TileType.Question, x - 1, x + 2)) return false;
        if (level.AnyInColumns(TileType.Platform, x - 1, x + 2)) return false;

        // goombas are kinematic and walk straight through pipes, so keep pipes out of patrol routes
        if (level.AnyInColumns(TileType.Enemy, x - goombaPatrol - 1, x + 2 + goombaPatrol)) return false;

        return true;
    }




    // ---------- enemies ----------
    // goomba standing at column x
    public bool CanPlaceEnemy(LevelData level, int x)
    {
        // its whole patrol must be away from the start (don't hit Mario on spawn) and end
        if (InSafeZone(level, x - goombaPatrol, x + goombaPatrol)) return false;

        // goombas are kinematic: they float over pits and walk up/down through steps,
        // so the whole patrol x-patrol .. x+patrol must be flat ground
        if (!level.IsFlat(x - goombaPatrol, goombaPatrol * 2 + 1)) return false;

        // nothing to walk through on the patrol
        if (level.AnyInColumns(TileType.Pipe, x - goombaPatrol - 1, x + goombaPatrol + 1)) return false;

        // not bunched up with other enemies
        if (level.AnyInColumns(TileType.Enemy, x - minEnemySpacing, x + minEnemySpacing)) return false;

        return true;
    }


    // ---------- bricks and ? blocks ----------
    // a row of blocks over columns x .. x+length-1
    public bool CanPlaceBlockRow(LevelData level, int x, int length)
    {
        if (InSafeZone(level, x, x + length - 1)) return false;

        // must be over flat ground so Mario can stand under it and bump it (this also means no pits)
        if (!level.IsFlat(x, length)) return false;

        int y = level.groundHeight[x] + blockClearance;
        if (y + 1 >= level.height) return false;

        // the air around the row (1 tile each side, 1 row above/below) must be empty,
        // so rows don't merge with platforms, other rows or tall ground next to it
        for (int cx = x - 1; cx <= x + length; cx++)
        {
            for (int cy = y - 1; cy <= y + 1; cy++)
            {
                if (level.Get(cx, cy) != TileType.Empty) return false;
            }
        }

        // no pipes underneath (blocks the jump)
        if (level.AnyInColumns(TileType.Pipe, x - 1, x + length)) return false;

        // not right after a step up / pit, Mario would hit it mid-jump and fall short of the ledge
        if (NearLandingEdge(level, x, x + length - 1)) return false;

        return true;
    }

    // which block goes in a row
    public TileType PickBlockType(int rowLength)
    {
        // a lone brick is pointless, make it a ? block
        if (rowLength == 1) return TileType.Question;
        return Random.value < questionChance ? TileType.Question : TileType.Brick;
    }


    // ---------- floating platforms ----------
    // a platform over columns x .. x+length-1 at row y
    public bool CanPlaceFloatingPlatform(LevelData level, int x, int y, int length)
    {
        if (InSafeZone(level, x, x + length - 1)) return false;
        if (y + 1 >= level.height) return false;

        // keep empty air around it (2 tiles each side) so it doesn't touch blocks or other platforms
        for (int cx = x - 2; cx < x + length + 2; cx++)
        {
            for (int cy = y - 1; cy <= y + 1; cy++)
            {
                if (level.Get(cx, cy) != TileType.Empty) return false;
            }
        }

        for (int cx = x; cx < x + length; cx++)
        {
            int h = level.groundHeight[cx];
            if (h == 0) continue; // over a pit is fine
            if (y - h < platformClearance) return false; // room to walk underneath
            if (y + 1 - h > maxStepUp) return false;     // reachable by jumping from the ground below
        }

        // not above pipes, Mario gets wedged between the two
        if (level.AnyInColumns(TileType.Pipe, x - 1, x + length)) return false;

        // not right after a step up / pit, Mario would hit it mid-jump and fall short of the ledge
        if (NearLandingEdge(level, x, x + length - 1)) return false;

        return true;
    }
}
