using System.Collections;
using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Current { get; private set; }

    [SerializeField] private GameHud hud;
    [SerializeField] private string defaultGameOverMessage = "game over";
    [SerializeField] private string defaultClearMessage = "game clear";

    private Coroutine statusRoutine;

    public int CoinCount { get; private set; }
    public bool IsGameCleared { get; private set; }

    void Awake()
    {
        if (Current != null && Current != this)
        {
            Destroy(gameObject);
            return;
        }

        Current = this;

        if (hud == null)
        {
            hud = FindFirstObjectByType<GameHud>();
        }

        SetCoinCount(0);
        ClearStatus();
    }

    void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }
    }

    public void AddCoin(int amount = 1)
    {
        SetCoinCount(CoinCount + amount);
    }

    public void ShowGameOver(float duration, string message = null)
    {
        ShowTemporaryStatus(string.IsNullOrWhiteSpace(message) ? defaultGameOverMessage : message, duration);
    }

    public void ClearLevel(PlayerMovement player, string message = null)
    {
        if (IsGameCleared)
        {
            return;
        }

        IsGameCleared = true;
        StopStatusRoutine();
        SetStatus(string.IsNullOrWhiteSpace(message) ? defaultClearMessage : message);
        StopPlayer(player);
    }

    private void SetCoinCount(int value)
    {
        CoinCount = value;

        if (hud != null)
        {
            hud.SetCoinCount(CoinCount);
        }
    }

    private void ShowTemporaryStatus(string message, float duration)
    {
        StopStatusRoutine();
        statusRoutine = StartCoroutine(TemporaryStatusRoutine(message, duration));
    }

    private IEnumerator TemporaryStatusRoutine(string message, float duration)
    {
        SetStatus(message);
        yield return new WaitForSecondsRealtime(duration);

        if (!IsGameCleared)
        {
            ClearStatus();
        }

        statusRoutine = null;
    }

    private void StopStatusRoutine()
    {
        if (statusRoutine == null)
        {
            return;
        }

        StopCoroutine(statusRoutine);
        statusRoutine = null;
    }

    private void SetStatus(string message)
    {
        Debug.Log(message);

        if (hud != null)
        {
            hud.SetStatus(message);
        }
    }

    private void ClearStatus()
    {
        if (hud != null)
        {
            hud.ClearStatus();
        }
    }

    private void StopPlayer(PlayerMovement player)
    {
        if (player == null)
        {
            return;
        }

        player.enabled = false;

        FallGameOver fallGameOver = player.GetComponent<FallGameOver>();
        if (fallGameOver != null)
        {
            fallGameOver.enabled = false;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}
