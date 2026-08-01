using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class WaterSceneReset : MonoBehaviour
{
    private bool isReloading;

    void OnTriggerEnter(Collider other)
    {
        if (isReloading || other.GetComponentInParent<PlayerMovement>() == null)
        {
            return;
        }

        isReloading = true;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }
}
