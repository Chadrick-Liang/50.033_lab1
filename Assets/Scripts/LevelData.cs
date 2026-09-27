using System.Text;
using UnityEngine;

//the types of available tiles in the grid
public enum TileType { Empty, Ground, Brick, Question, Pipe, Platform, Enemy }

//set level coordinates and sizes
public class LevelData
{
    public int width;
    public int height;
    public TileType[,] tiles;
    public int[] groundHeight; //how many ground tiles are stacked in each column, 0 = pit

    public LevelData(int width, int height)
    {
        this.width = width;
        this.height = height;
        tiles = new TileType[width, height];
        groundHeight = new int[width];
    }

    //checks whether coordinates for spawning object are within level boundaries
    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    //getter for tile type given coordinate
    public TileType Get(int x, int y)
    {
        if (!InBounds(x, y)) return TileType.Empty;
        return tiles[x, y];
    }

    //setter for tile type given coordinate
    public void Set(int x, int y, TileType type)
    {
        if (InBounds(x, y)) tiles[x, y] = type;
    }

    //checks if a column is a pit
    public bool IsPit(int x)
    {
        if (x < 0 || x >= width) return false;
        return groundHeight[x] == 0;
    }

    // true if columns x .. x+length-1 are all ground at the same height (no pits, no steps)
    public bool IsFlat(int x, int length)
    {
        if (x < 0 || x + length > width) return false;
        int h = groundHeight[x];
        if (h == 0) return false;
        for (int i = 1; i < length; i++)
        {
            if (groundHeight[x + i] != h) return false;
        }
        return true;
    }

    //checks if a tile of a specific type exists in the specified columns
    public bool AnyInColumns(TileType type, int fromX, int toX)
    {
        for (int x = fromX; x <= toX; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (Get(x, y) == type) return true;
            }
        }
        return false;
    }

    // text version of the level for debugging, top row first
    public string ToAscii()
    {
        StringBuilder sb = new StringBuilder();
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                sb.Append(Symbol(tiles[x, y]));
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }

    // returns the ASCII symbol for a tile type
    private char Symbol(TileType type)
    {
        switch (type)
        {
            case TileType.Ground: return '#';
            case TileType.Brick: return 'B';
            case TileType.Question: return '?';
            case TileType.Pipe: return 'P';
            case TileType.Platform: return '=';
            case TileType.Enemy: return 'E';
            default: return '.';
        }
    }
}
