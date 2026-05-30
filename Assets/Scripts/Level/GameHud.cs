using UnityEngine;
using UnityEngine.UI;

public class GameHud : MonoBehaviour
{
    [SerializeField] private Text coinText;
    [SerializeField] private Text statusText;
    [SerializeField] private string coinFormat = "Coins: {0}";

    public void SetCoinCount(int coinCount)
    {
        if (coinText != null)
        {
            coinText.text = string.Format(coinFormat, coinCount);
        }
    }

    public void SetStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        statusText.enabled = true;
    }

    public void ClearStatus()
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = string.Empty;
        statusText.enabled = false;
    }
}
