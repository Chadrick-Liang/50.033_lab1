using System.Collections.Generic;
using UnityEngine;

// Builds a random level when the scene starts:
// 1. random walk left -> right to make the ground heights and pits
// 2. tries to place pipes, enemies, blocks, platforms at random spots,
//    asking LevelRules each time whether that spot is allowed
// 3. spawns the prefabs
public class LevelGenerator : MonoBehaviour
{
    public LevelRules rules;

    [Header("Level size")]
    public int levelWidth = 150;
    public int levelHeight = 16;
    public int seed = 0;                // 0 = new random level every play, anything else = same level every time
    public int startHeight = 1;         // ground height at the start, 1 lines up with the old ground
    public int maxGroundHeight = 5;
    public float groundTopY = -4.5f;    // world y of the top of the starting ground

    [Header("Random walk")]
    public int minSegmentLength = 3;
    public int maxSegmentLength = 14;
    [Range(0, 1)] public float gapChance = 0.25f;
    [Range(0, 1)] public float bridgeChance = 0.3f;        // chance a pit is a wide one with a platform in the middle
    [Range(0, 1)] public float heightChangeChance = 0.5f;

    [Header("How many times to TRY placing each thing (rules reject a lot)")]
    public int pipeAttempts = 15;
    public int enemyAttempts = 30;
    public int blockRowAttempts = 30;
    public int platformAttempts = 15;

    [Header("Prefabs")]
    public GameObject groundPrefab;     // 1x1, tag Ground, layer Ground
    public GameObject brickPrefab;
    public GameObject questionPrefab;
    public GameObject pipeTopPrefab;    // pipe_green_top_up (2x2)
    public GameObject pipeBodyPrefab;   // pipe_green_body (1 tall)
    public GameObject platformPrefab;
    public GameObject goombaPrefab;

    [Header("Scene references")]
    public Transform player;            // Mario, the level starts just left of him
    public Transform enemiesParent;     // the "Enemies" object, PlayerMovement resets its children
    public Transform endLimit;          // moved to the end of the level for CameraController
    public GameObject rewindPanel;      // spawned goombas need this, EnemyMovement.Start() uses it
    public JumpOverGoomba jumpOverGoomba; // optional, pointed at the first spawned goomba

    [Header("Debug")]
    public bool printAscii = true;
    public bool drawWalk = true;        // draw the random walk as gizmos (Scene view, or Game view with Gizmos on)
    public float walkRevealSpeed = 3f;  // walk steps revealed per second while playing, 0 = show everything at once

    private LevelData level;
    private Vector2 origin;             // world position of the centre of tile (0, 0)
    private List<Vector3Int> pipes = new List<Vector3Int>(); // x = column, y = ground height, z = pipe height

    // every decision the random walk made, recorded so we can draw/log it
    private enum StepKind { Flat, StepUp, StepDown, Pit, Bridge, Rejected }
    private struct WalkStep
    {
        public StepKind kind;
        public int x;          // column where this step starts
        public int length;     // how many columns it covers (0 for rejected proposals)
        public int fromHeight;
        public int toHeight;
    }
    private List<WalkStep> walkSteps = new List<WalkStep>();
    private float generatedTime;

    // Awake (not Start) so the level exists before CameraController/EnemyMovement run their Start()
    void Awake()
    {
        CheckIsPrefabAsset(groundPrefab);
        CheckIsPrefabAsset(brickPrefab);
        CheckIsPrefabAsset(questionPrefab);
        CheckIsPrefabAsset(pipeTopPrefab);
        CheckIsPrefabAsset(pipeBodyPrefab);
        CheckIsPrefabAsset(platformPrefab);
        CheckIsPrefabAsset(goombaPrefab);

        if (seed == 0) seed = Random.Range(1, 100000);
        Random.InitState(seed);
        Debug.Log("Level seed: " + seed + " (put this in the seed field to replay this level)");

        level = new LevelData(levelWidth, levelHeight);
        // put Mario 3 tiles into the level, and line the start ground's top up with groundTopY
        origin = new Vector2(Mathf.Round(player.position.x) - 3, groundTopY - startHeight + 0.5f);

        GenerateGround();
        PlacePipes();
        PlaceEnemies();
        PlaceBlockRows();
        PlaceFloatingPlatforms();

        if (printAscii)
        {
            string ascii = level.ToAscii();
            Debug.Log(ascii);
            // the console font isn't monospaced, so also save it where it's easier to read
            System.IO.File.WriteAllText(Application.dataPath + "/../GeneratedLevel.txt", ascii);
        }

        SpawnAll();
    }


    // prefab slots must be dragged from the Project window, not the Hierarchy.
    // a scene object gets copied as-is, so if it's disabled every copy spawns disabled too
    void CheckIsPrefabAsset(GameObject prefab)
    {
        if (prefab != null && prefab.scene.IsValid())
        {
            Debug.LogWarning(prefab.name + " is a scene object, not a prefab asset. Drag it from the Project window (Assets/Prefabs) instead.");
        }
    }

    // ---------- step 1: random walk for ground and pits ----------
    void GenerateGround()
    {
        int x = 0;
        int h = startHeight;
        int segMin = Mathf.Max(minSegmentLength, rules.minRunBeforeGap);

        // flat safe zone at the start
        RecordStep(StepKind.Flat, x, rules.safeZoneLength, h, h);
        x = FillGround(x, rules.safeZoneLength, h);

        // stop early enough that the last segment/pit still fits before the end safe zone
        int stopX = levelWidth - rules.safeZoneLength - maxSegmentLength - rules.maxGapWithPlatform - rules.minRunBeforeGap;

        while (x < stopX)
        {
            if (Random.value < gapChance)
            {
                bool bridged = Random.value < bridgeChance;
                int gapWidth = bridged
                    ? Random.Range(rules.maxGap + 1, rules.maxGapWithPlatform + 1)
                    : Random.Range(rules.minGap, rules.maxGap + 1);
                int landingHeight = PickNextHeight(h);

                if (rules.IsValidGap(gapWidth, h, landingHeight, bridged))
                {
                    int pitStart = x;
                    RecordStep(bridged ? StepKind.Bridge : StepKind.Pit, x, gapWidth, h, landingHeight);
                    x = FillGround(x, gapWidth, 0);
                    if (bridged) AddBridge(pitStart, gapWidth, h);
                    h = landingHeight;
                    // landing run, also the run-up for any pit right after
                    RecordStep(StepKind.Flat, x, rules.minRunBeforeGap, h, h);
                    x = FillGround(x, rules.minRunBeforeGap, h);
                    continue;
                }
                // rules said no, fall through and make normal ground instead
                RecordStep(StepKind.Rejected, x, 0, h, landingHeight);
            }

            // change height at the START of a segment, so the column before a pit
            // always belongs to a full-length run at the current height
            int oldHeight = h;
            int nextHeight = PickNextHeight(h);
            if (Random.value < heightChangeChance)
            {
                if (rules.IsValidStep(h, nextHeight)) h = nextHeight;
                else RecordStep(StepKind.Rejected, x, 0, h, nextHeight);
            }
            int length = Random.Range(segMin, maxSegmentLength + 1);
            StepKind kind = h > oldHeight ? StepKind.StepUp : (h < oldHeight ? StepKind.StepDown : StepKind.Flat);
            RecordStep(kind, x, length, oldHeight, h);
            x = FillGround(x, length, h);
        }

        // flat safe zone at the end (whatever is left)
        RecordStep(StepKind.Flat, x, levelWidth - x, h, h);
        FillGround(x, levelWidth - x, h);

        LogWalk();
        generatedTime = Time.time;
    }

    void RecordStep(StepKind kind, int x, int length, int fromHeight, int toHeight)
    {
        WalkStep step = new WalkStep();
        step.kind = kind;
        step.x = x;
        step.length = length;
        step.fromHeight = fromHeight;
        step.toHeight = toHeight;
        walkSteps.Add(step);
    }

    // prints the walk as a list, e.g. "x=8   StepUp  +2 -> h=3, run 6"
    void LogWalk()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder("Random walk (" + walkSteps.Count + " steps):\n");
        foreach (WalkStep s in walkSteps)
        {
            int dy = s.toHeight - s.fromHeight;
            string change = (dy >= 0 ? "+" : "") + dy;
            switch (s.kind)
            {
                case StepKind.Pit:
                case StepKind.Bridge:
                    sb.Append("x=" + s.x + "  " + s.kind + " width " + s.length + ", land " + change + " -> h=" + s.toHeight + "\n");
                    break;
                case StepKind.Rejected:
                    sb.Append("x=" + s.x + "  REJECTED by rules: h " + s.fromHeight + " -> " + s.toHeight + "\n");
                    break;
                default:
                    sb.Append("x=" + s.x + "  " + s.kind + " " + change + " -> h=" + s.toHeight + ", run " + s.length + "\n");
                    break;
            }
        }
        Debug.Log(sb.ToString());
    }

    // propose a new height loosely, LevelRules decides if it's allowed
    int PickNextHeight(int h)
    {
        return Mathf.Clamp(h + Random.Range(-3, 4), 1, maxGroundHeight);
    }

    // fills `length` columns with ground of height h (h = 0 makes a pit), returns the next free column
    int FillGround(int x, int length, int h)
    {
        for (int i = 0; i < length && x < levelWidth; i++, x++)
        {
            level.groundHeight[x] = h;
            for (int y = 0; y < h; y++)
            {
                level.Set(x, y, TileType.Ground);
            }
        }
        return x;
    }

    // platform in the middle of a wide pit, leaving a jumpable gap on each side
    void AddBridge(int pitStart, int gapWidth, int takeoffHeight)
    {
        int sideGap = Mathf.Min(rules.maxGap, (gapWidth - 2) / 2); // at least 2 platform tiles
        int length = gapWidth - sideGap * 2;
        int y = takeoffHeight; // one step above the takeoff ground
        for (int i = 0; i < length; i++)
        {
            level.Set(pitStart + sideGap + i, y, TileType.Platform);
        }
    }


    // ---------- step 2: try placing things, rules decide ----------
    void PlacePipes()
    {
        for (int i = 0; i < pipeAttempts; i++)
        {
            int x = Random.Range(0, levelWidth);
            int pipeHeight = Random.Range(rules.minPipeHeight, rules.maxPipeHeight + 1);
            if (!rules.CanPlacePipe(level, x, pipeHeight)) continue;

            int h = level.groundHeight[x];
            for (int cx = x; cx < x + 2; cx++)
            {
                for (int y = h; y < h + pipeHeight; y++)
                {
                    level.Set(cx, y, TileType.Pipe);
                }
            }
            pipes.Add(new Vector3Int(x, h, pipeHeight));
        }
    }

    void PlaceEnemies()
    {
        for (int i = 0; i < enemyAttempts; i++)
        {
            int x = Random.Range(0, levelWidth);
            if (!rules.CanPlaceEnemy(level, x)) continue;
            level.Set(x, level.groundHeight[x], TileType.Enemy);
        }
    }

    void PlaceBlockRows()
    {
        for (int i = 0; i < blockRowAttempts; i++)
        {
            int x = Random.Range(0, levelWidth);
            int length = Random.Range(1, rules.maxBlockRowLength + 1);
            if (!rules.CanPlaceBlockRow(level, x, length)) continue;

            int y = level.groundHeight[x] + rules.blockClearance;
            for (int cx = x; cx < x + length; cx++)
            {
                level.Set(cx, y, rules.PickBlockType(length));
            }
        }
    }

    void PlaceFloatingPlatforms()
    {
        for (int i = 0; i < platformAttempts; i++)
        {
            int x = Random.Range(0, levelWidth);
            if (level.IsPit(x)) continue;
            int length = Random.Range(3, 6);
            int y = level.groundHeight[x] + rules.platformClearance;
            if (!rules.CanPlaceFloatingPlatform(level, x, y, length)) continue;

            for (int cx = x; cx < x + length; cx++)
            {
                level.Set(cx, y, TileType.Platform);
            }
        }
    }


    // ---------- step 3: spawn prefabs ----------
    Vector3 TileToWorld(int x, int y)
    {
        return new Vector3(origin.x + x, origin.y + y, 0);
    }

    void SpawnAll()
    {
        GameObject firstGoomba = null;

        for (int x = 0; x < levelWidth; x++)
        {
            for (int y = 0; y < levelHeight; y++)
            {
                Vector3 pos = TileToWorld(x, y);
                switch (level.Get(x, y))
                {
                    case TileType.Ground: Instantiate(groundPrefab, pos, Quaternion.identity, transform); break;
                    case TileType.Brick: Instantiate(brickPrefab, pos, Quaternion.identity, transform); break;
                    case TileType.Question: Instantiate(questionPrefab, pos, Quaternion.identity, transform); break;
                    case TileType.Platform: Instantiate(platformPrefab, pos, Quaternion.identity, transform); break;
                    case TileType.Enemy:
                        // parented under Enemies so PlayerMovement.ResetGame() resets them
                        GameObject goomba = Instantiate(goombaPrefab, pos, Quaternion.identity, enemiesParent);
                        goomba.GetComponent<EnemyMovement>().rewindPanel = rewindPanel;
                        if (firstGoomba == null) firstGoomba = goomba;
                        break;
                        // pipes are spawned below as whole pieces
                }
            }
        }

        foreach (Vector3Int p in pipes)
        {
            float centreX = origin.x + p.x + 0.5f; // pipes are 2 tiles wide
            // body pieces fill every row except the top two
            for (int y = p.y; y < p.y + p.z - 2; y++)
            {
                Instantiate(pipeBodyPrefab, new Vector3(centreX, origin.y + y, 0), Quaternion.identity, transform);
            }
            // the 2x2 top sits on the top two rows
            float topY = origin.y + p.y + p.z - 2 + 0.5f;
            Instantiate(pipeTopPrefab, new Vector3(centreX, topY, 0), Quaternion.identity, transform);
        }

        // camera stops scrolling at the end of the generated level
        endLimit.position = new Vector3(origin.x + levelWidth - 1, endLimit.position.y, endLimit.position.z);

        // JumpOverGoomba only tracks one enemy, give it the first one so it doesn't null-ref
        if (jumpOverGoomba != null && firstGoomba != null)
        {
            jumpOverGoomba.enemyLocation = firstGoomba.transform;
        }
    }


    // ---------- debug: draw the random walk ----------
    // green = flat, yellow = step up, cyan = step down, red = pit, magenta = pit with platform,
    // grey X = a proposal the rules rejected. white ball = the "walker" (latest revealed step)
    void OnDrawGizmos()
    {
        if (!drawWalk || walkSteps.Count == 0) return;

        // reveal the walk a few steps per second so you can watch it being built
        int count = walkSteps.Count;
        if (walkRevealSpeed > 0) count = Mathf.Min(count, (int)((Time.time - generatedTime) * walkRevealSpeed) + 1);

        Vector3 walker = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            WalkStep s = walkSteps[i];
            float left = origin.x + s.x - 0.5f;      // left edge of the first column
            float right = left + s.length;
            float fromY = SurfaceY(s.fromHeight);
            float toY = SurfaceY(s.toHeight);

            switch (s.kind)
            {
                case StepKind.Flat:
                case StepKind.StepUp:
                case StepKind.StepDown:
                    Gizmos.color = s.kind == StepKind.StepUp ? Color.yellow : (s.kind == StepKind.StepDown ? Color.cyan : Color.green);
                    Gizmos.DrawLine(new Vector3(left, fromY), new Vector3(left, toY));  // the vertical step
                    Gizmos.DrawLine(new Vector3(left, toY), new Vector3(right, toY));   // the flat run after it
                    walker = new Vector3(right, toY);
                    break;

                case StepKind.Pit:
                case StepKind.Bridge:
                    // draw the jump arc across the pit
                    Gizmos.color = s.kind == StepKind.Pit ? Color.red : Color.magenta;
                    Vector3 prev = new Vector3(left, fromY);
                    for (int j = 1; j <= 10; j++)
                    {
                        float t = j / 10f;
                        float arc = Mathf.Sin(t * Mathf.PI) * 2f;
                        Vector3 p = new Vector3(Mathf.Lerp(left, right, t), Mathf.Lerp(fromY, toY, t) + arc);
                        Gizmos.DrawLine(prev, p);
                        prev = p;
                    }
                    walker = new Vector3(right, toY);
                    break;

                case StepKind.Rejected:
                    // X at the height the walk wanted to go to
                    Gizmos.color = Color.grey;
                    Vector3 c = new Vector3(left, toY);
                    Gizmos.DrawLine(c + new Vector3(-0.3f, -0.3f), c + new Vector3(0.3f, 0.3f));
                    Gizmos.DrawLine(c + new Vector3(-0.3f, 0.3f), c + new Vector3(0.3f, -0.3f));
                    break;
            }

#if UNITY_EDITOR
            // small text label per step (only exists in the editor, so it's wrapped in #if)
            int dy = s.toHeight - s.fromHeight;
            string label = s.kind == StepKind.Rejected ? "x" : (s.kind == StepKind.Pit || s.kind == StepKind.Bridge ? "gap " + s.length : (dy == 0 ? "" : (dy > 0 ? "+" : "") + dy));
            if (label != "") UnityEditor.Handles.Label(new Vector3(left, Mathf.Max(fromY, toY) + 0.8f), label);
#endif
        }

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(walker, 0.3f);
    }

    // world y of the top surface of a column with height h (the line Mario walks on)
    float SurfaceY(int h)
    {
        return origin.y + h - 0.5f;
    }
}
