using UnityEngine;

public class PlayerKeyHolder : MonoBehaviour
{
    [SerializeField] private string heldKeyId;
    [SerializeField] private KeyPickup heldKey;

    public string HeldKeyId => heldKeyId;
    public KeyPickup HeldKey => heldKey;
    public Transform HeldKeyTransform => heldKey != null ? heldKey.transform : null;
    public bool HasKey => !string.IsNullOrEmpty(heldKeyId);

    public void HoldKey(KeyPickup key)
    {
        heldKey = key;
        heldKeyId = key != null ? key.KeyId : string.Empty;
    }

    public void HoldKey(string keyId)
    {
        heldKey = null;
        heldKeyId = keyId;
    }

    public bool HasMatchingKey(string keyId)
    {
        return !string.IsNullOrEmpty(keyId) && heldKeyId == keyId;
    }

    public void ClearKey()
    {
        heldKey = null;
        heldKeyId = string.Empty;
    }
}
