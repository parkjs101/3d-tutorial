using UnityEngine;

public class CoinCounter : MonoBehaviour
{
    private static int coinCount;

    void Awake()
    {
        coinCount = 0;
    }

    public static void AddCoin()
    {
        coinCount++;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(10, 10, 160, 40), $"coin:{coinCount}", style);
    }
}
