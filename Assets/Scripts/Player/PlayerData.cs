using UnityEngine;

public static class PlayerData
{
    public static bool hasCheckPoint;
    public static Vector3 checkPoint;
    
    private static readonly Vector3 defaultPosition = new Vector3(-188.2f, -34.4f, -1.334f);

    public static void Reset()
    {
        hasCheckPoint = false;
        checkPoint = defaultPosition;
    }
    
    public static void SetCheckPoint(Vector3 position)
    {
        hasCheckPoint = true;
        checkPoint = position;
    }
}