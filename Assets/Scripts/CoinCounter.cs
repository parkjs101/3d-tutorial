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
}
