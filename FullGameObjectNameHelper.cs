using UnityEngine;

public static class FullGameObjectNameHelper
{
    public static string GetFullName(this GameObject gameObject)
    {
        string path = "!" + gameObject.name;
        while (gameObject.transform.parent != null)
        {
            gameObject = gameObject.transform.parent.gameObject;
            path = "!" + gameObject.name + path;
        }
        return path;
    }
}
