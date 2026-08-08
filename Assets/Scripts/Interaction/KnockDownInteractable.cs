using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Rigidbody))]
public class KnockDownInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private float interactRadius = 1.5f;

    [Header("Pull Animation")]
    [SerializeField] private AnimationClip startAnimation;
    [SerializeField, Min(0.01f)] private float animationSpeed = 1f;

    [Header("Pull Hand IK")]
    [SerializeField, Min(0f)] private float handSpacing = 0.25f;
    [SerializeField] private Vector3 leftHandEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 rightHandEulerAngles = Vector3.zero;

    [Header("Knock Down Force")]
    [SerializeField] private KnockDownForcePoint forceApplicationPoint;

    private Rigidbody targetRigidbody;
    private bool hasFallen;
    private PlayableGraph animationGraph;

    public static KnockDownInteractable ActivePull { get; private set; }
    public static bool IsAnyPulling => ActivePull != null;
    public Transform GripPoint => interactionPoint;
    public float HandSpacing => handSpacing;
    public Vector3 LeftHandEulerAngles => leftHandEulerAngles;
    public Vector3 RightHandEulerAngles => rightHandEulerAngles;

    private void Awake()
    {
        targetRigidbody = GetComponent<Rigidbody>();
        targetRigidbody.isKinematic = true;
        targetRigidbody.useGravity = true;

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    public bool CanInteract(Vector3 playerPosition)
    {
        return !hasFallen && interactionPoint != null &&
               Vector3.Distance(playerPosition, interactionPoint.position) <= interactRadius;
    }

    public bool Interact(PlayerMovement player)
    {
        if (hasFallen || targetRigidbody == null || interactionPoint == null ||
            forceApplicationPoint == null || player == null)
        {
            return false;
        }

        Animator animator = player.GetComponentInChildren<Animator>();
        if (animator == null || startAnimation == null)
        {
            Debug.LogError("KnockDownInteractable requires a Player Animator and pull start clip.", this);
            return false;
        }

        hasFallen = true;
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        StartCoroutine(PlayPullSequence(animator));
        return true;
    }

    private IEnumerator PlayPullSequence(Animator animator)
    {
        ActivePull = this;
        PlayClip(animator, startAnimation);
        yield return new WaitForSeconds(startAnimation.length / animationSpeed);

        StopAnimationGraph();
        ApplyKnockDownForce();
        animator.Rebind();
        animator.Update(0f);
        ActivePull = null;
    }

    private void ApplyKnockDownForce()
    {
        targetRigidbody.isKinematic = false;
        forceApplicationPoint.ApplyTo(targetRigidbody);
    }

    private void PlayClip(Animator animator, AnimationClip clip)
    {
        AnimationClipPlayable playable = AnimationPlayableUtilities.PlayClip(
            animator,
            clip,
            out animationGraph);
        playable.SetSpeed(animationSpeed);
    }

    private void StopAnimationGraph()
    {
        if (animationGraph.IsValid())
        {
            animationGraph.Destroy();
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(highlighted && !hasFallen);
        }
    }

    private void OnDisable()
    {
        StopAnimationGraph();
        if (ActivePull == this)
        {
            ActivePull = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (interactionPoint == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactionPoint.position, interactRadius);

    }
}
