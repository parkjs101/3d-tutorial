using UnityEngine;

public class CoinCounter : MonoBehaviour
{
    [SerializeField] private GameSession gameSession;

    public int CoinCount { get; private set; }

    void Awake()
    {
        CoinCount = 0;
    }

    public void AddCoin()
    {
        CoinCount++;

        GameSession targetSession = gameSession != null ? gameSession : GameSession.Current;
        if (targetSession != null)
        {
            targetSession.AddCoin();
        }
    }
}
