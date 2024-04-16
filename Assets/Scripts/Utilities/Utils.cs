using UnityEngine;

public static class Utils
{
    public static bool IsNeighbour(this Tile currTile, Tile t, bool includeDiagonals = false)
    {
        Vector2Int diff = new Vector2Int(currTile.X - t.X, currTile.Y - t.Y);
        return diff.sqrMagnitude == 1 || (includeDiagonals && diff.sqrMagnitude == 2);
    }

    public static Vector3 V3(this Vector2Int v2, float z = 0)
    {
        return new Vector3(v2.x, v2.y, z);
    }
}