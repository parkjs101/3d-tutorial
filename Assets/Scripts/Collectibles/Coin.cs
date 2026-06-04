using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 180f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null)
        {
            return;
        }

        CoinCounter counter = FindFirstObjectByType<CoinCounter>();
        if (counter != null)
        {
            counter.AddCoin();
        }
        else if (GameSession.Current != null)
        {
            GameSession.Current.AddCoin();
        }

        Destroy(gameObject);
    }
}
