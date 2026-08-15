using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Collider))]
public class BreakablePunchInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactRadius = 1.1f;
    [SerializeField, Min(0.01f)] private float turnSharpness = 7f;
    [SerializeField, Range(0.1f, 15f)] private float facingTolerance = 1f;

    [Header("Punch")]
    [SerializeField] private AnimationClip punchAnimation;
    [SerializeField] private HumanBodyBones punchHand = HumanBodyBones.RightHand;
    [SerializeField] private bool detectOppositeHand = true;
    [SerializeField, Range(0f, 1f)] private float earliestHitNormalizedTime = 0.5f;
    [SerializeField, Min(0.01f)] private float handContactDistance = 0.12f;

    [Header("Debris")]
    [SerializeField] private GameObject debrisRoot;
    [SerializeField, Min(0f)] private float explosionForce = 3f;
    [SerializeField, Min(0.01f)] private float explosionRadius = 1.2f;
    [SerializeField] private float upwardModifier = 0.15f;

    [Header("Dust Dissolve")]
    [SerializeField] private ParticleSystem dustVfxPrefab;
    [SerializeField] private Shader dissolveShader;
    [SerializeField, Min(0f)] private float debrisSettleDelay = 2f;
    [SerializeField, Min(0.01f)] private float dissolveDuration = 0.8f;
    [SerializeField, Min(0f)] private float debrisRiseDistance = 0.35f;
    [SerializeField, Min(0f)] private float debrisDriftDistance = 0.15f;

    private Collider intactCollider;
    private bool isBroken;
    private PlayableGraph animationGraph;
    private readonly List<DebrisPiece> debrisPieces = new List<DebrisPiece>();

    public static BreakablePunchInteractable ActivePunch { get; private set; }
    public static bool IsAnyPunching => ActivePunch != null;

    private void Awake()
    {
        intactCollider = GetComponent<Collider>();
        PrepareDebris();
    }

    public bool CanInteract(Vector3 playerPosition)
    {
        return !isBroken && !IsAnyPunching && intactCollider != null &&
               Vector3.Distance(playerPosition, intactCollider.ClosestPoint(playerPosition)) <= interactRadius;
    }

    public bool Interact(PlayerMovement player)
    {
        if (!CanInteract(player != null ? player.transform.position : Vector3.zero) || player == null ||
            punchAnimation == null || debrisRoot == null)
        {
            return false;
        }

        Animator animator = player.GetComponentInChildren<Animator>();
        PlayerAnimatorBridge animatorBridge = player.GetComponent<PlayerAnimatorBridge>();
        if (animator == null || animatorBridge == null || !animator.isHuman)
        {
            Debug.LogError("BreakablePunchInteractable requires a Humanoid Player Animator and PlayerAnimatorBridge.", this);
            return false;
        }

        Transform primaryHand = animator.GetBoneTransform(punchHand);
        HumanBodyBones oppositeHandBone = punchHand == HumanBodyBones.RightHand
            ? HumanBodyBones.LeftHand
            : HumanBodyBones.RightHand;
        Transform oppositeHand = detectOppositeHand ? animator.GetBoneTransform(oppositeHandBone) : null;
        if (primaryHand == null)
        {
            Debug.LogError($"BreakablePunchInteractable could not find the configured punch hand '{punchHand}'.", this);
            return false;
        }

        StartCoroutine(TurnPunchAndBreak(animator, animatorBridge, primaryHand, oppositeHand));
        return true;
    }

    public void SetHighlighted(bool highlighted)
    {
    }

    private IEnumerator TurnPunchAndBreak(
        Animator animator,
        PlayerAnimatorBridge animatorBridge,
        Transform primaryHand,
        Transform oppositeHand)
    {
        ActivePunch = this;

        Vector3 targetDirection = GetTargetDirection(animator.transform.position);
        animatorBridge.SetForcedFacingDirection(targetDirection, turnSharpness);
        while (!animatorBridge.IsFacingDirection(targetDirection, facingTolerance))
        {
            yield return null;
        }

        AnimationClipPlayable playable = AnimationPlayableUtilities.PlayClip(
            animator,
            punchAnimation,
            out animationGraph);
        playable.SetSpeed(1f);

        float elapsed = 0f;
        float hitStartTime = punchAnimation.length * earliestHitNormalizedTime;
        while (elapsed < punchAnimation.length)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= hitStartTime && TryGetHandContact(primaryHand, oppositeHand, out Vector3 impactPosition))
            {
                Break(impactPosition, animator, animatorBridge);
                yield break;
            }

            yield return null;
        }

        StopPunch(animator, animatorBridge);
        Debug.LogWarning("Punch animation ended before the hand reached the breakable object. Move closer or increase Hand Contact Distance.", this);
    }

    private Vector3 GetTargetDirection(Vector3 playerPosition)
    {
        Vector3 targetPoint = intactCollider.ClosestPoint(playerPosition);
        Vector3 direction = targetPoint - playerPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.position - playerPosition;
            direction.y = 0f;
        }

        return direction.sqrMagnitude <= 0.001f ? Vector3.forward : direction.normalized;
    }

    private bool TryGetHandContact(Transform primaryHand, Transform oppositeHand, out Vector3 impactPosition)
    {
        if (IsHandTouching(primaryHand))
        {
            impactPosition = primaryHand.position;
            return true;
        }

        if (oppositeHand != null && IsHandTouching(oppositeHand))
        {
            impactPosition = oppositeHand.position;
            return true;
        }

        impactPosition = default;
        return false;
    }

    private bool IsHandTouching(Transform hand)
    {
        Vector3 closestPoint = intactCollider.ClosestPoint(hand.position);
        return (closestPoint - hand.position).sqrMagnitude <= handContactDistance * handContactDistance;
    }

    private void Break(Vector3 impactPosition, Animator animator, PlayerAnimatorBridge animatorBridge)
    {
        isBroken = true;

        foreach (DebrisPiece piece in debrisPieces)
        {
            piece.Release(impactPosition, explosionForce, explosionRadius, upwardModifier);
        }

        StopPunch(animator, animatorBridge);
        HideIntactObject();
        StartCoroutine(DissolveDebrisAfterDelay());
    }

    private void PrepareDebris()
    {
        if (debrisRoot == null)
        {
            return;
        }

        debrisPieces.Clear();
        foreach (Rigidbody debrisRigidbody in debrisRoot.GetComponentsInChildren<Rigidbody>(includeInactive: true))
        {
            if (debrisRigidbody.transform == transform || debrisRigidbody.transform.IsChildOf(transform))
            {
                continue;
            }

            DebrisPiece piece = new DebrisPiece(
                debrisRigidbody,
                debrisRigidbody.GetComponent<Collider>(),
                debrisRigidbody.GetComponent<Renderer>());
            piece.ConfigureDissolveMaterial(dissolveShader);
            piece.Prepare();
            debrisPieces.Add(piece);
        }
    }

    private void HideIntactObject()
    {
        intactCollider.enabled = false;
        foreach (Renderer intactRenderer in GetComponentsInChildren<Renderer>())
        {
            intactRenderer.enabled = false;
        }
    }

    private IEnumerator DissolveDebrisAfterDelay()
    {
        yield return new WaitForSeconds(debrisSettleDelay);

        Bounds debrisBounds = GetDebrisBounds();
        SpawnDust(debrisBounds);

        foreach (DebrisPiece piece in debrisPieces)
        {
            piece.StartDissolve();
        }

        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / dissolveDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);

            foreach (DebrisPiece piece in debrisPieces)
            {
                piece.UpdateDissolve(easedProgress, debrisRiseDistance, debrisDriftDistance);
            }

            yield return null;
        }

        foreach (DebrisPiece piece in debrisPieces)
        {
            piece.Hide();
        }
    }

    private Bounds GetDebrisBounds()
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.zero);

        foreach (DebrisPiece piece in debrisPieces)
        {
            if (piece.Renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = piece.Renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(piece.Renderer.bounds);
            }
        }

        return bounds;
    }

    private void SpawnDust(Bounds debrisBounds)
    {
        if (dustVfxPrefab == null)
        {
            return;
        }

        ParticleSystem dust = Instantiate(dustVfxPrefab, debrisBounds.center, Quaternion.identity);
        CubeDustVfxQuality quality = dust.GetComponent<CubeDustVfxQuality>();
        if (quality != null)
        {
            quality.Configure(debrisBounds.size);
        }
        else
        {
            ParticleSystem.ShapeModule shape = dust.shape;
            shape.scale = debrisBounds.size;
        }

        dust.Play(withChildren: true);
    }

    private void StopPunch(Animator animator, PlayerAnimatorBridge animatorBridge)
    {
        if (animationGraph.IsValid())
        {
            animationGraph.Destroy();
        }

        animatorBridge.ClearForcedFacingDirection();
        animator.Rebind();
        animator.Update(0f);

        if (ActivePunch == this)
        {
            ActivePunch = null;
        }

    }

    private void OnDisable()
    {
        if (animationGraph.IsValid())
        {
            animationGraph.Destroy();
        }

        if (ActivePunch == this)
        {
            ActivePunch = null;
        }

        foreach (DebrisPiece piece in debrisPieces)
        {
            piece.Dispose();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }

    private sealed class DebrisPiece
    {
        public readonly Rigidbody Rigidbody;
        public readonly Collider Collider;
        public readonly Renderer Renderer;
        private Material dissolveMaterial;
        private Vector3 startPosition;
        private Vector3 driftDirection;

        public DebrisPiece(Rigidbody rigidbody, Collider collider, Renderer renderer)
        {
            Rigidbody = rigidbody;
            Collider = collider;
            Renderer = renderer;
        }

        public void ConfigureDissolveMaterial(Shader shader)
        {
            if (Renderer == null || shader == null)
            {
                return;
            }

            Material sourceMaterial = Renderer.sharedMaterial;
            dissolveMaterial = new Material(shader)
            {
                name = $"{Rigidbody.name} Dissolve Instance"
            };

            if (sourceMaterial != null)
            {
                Texture baseMap = sourceMaterial.HasProperty("_BaseMap")
                    ? sourceMaterial.GetTexture("_BaseMap")
                    : sourceMaterial.mainTexture;
                Color baseColor = sourceMaterial.HasProperty("_BaseColor")
                    ? sourceMaterial.GetColor("_BaseColor")
                    : sourceMaterial.color;
                dissolveMaterial.SetTexture("_BaseMap", baseMap);
                dissolveMaterial.SetColor("_BaseColor", baseColor);
            }

            Renderer.material = dissolveMaterial;
            SetDissolveAmount(0f);
        }

        public void Prepare()
        {
            Rigidbody.isKinematic = true;
            Rigidbody.useGravity = true;
            Hide();
        }

        public void Release(Vector3 impactPosition, float explosionForce, float explosionRadius, float upwardModifier)
        {
            if (Renderer != null)
            {
                Renderer.enabled = true;
            }

            if (Collider != null)
            {
                Collider.enabled = true;
            }

            Rigidbody.isKinematic = false;
            Rigidbody.useGravity = true;
            SetDissolveAmount(0f);
            Rigidbody.AddExplosionForce(
                explosionForce,
                impactPosition,
                explosionRadius,
                upwardModifier,
                ForceMode.Impulse);
        }

        public void Hide()
        {
            if (Collider != null)
            {
                Collider.enabled = false;
            }

            if (Renderer != null)
            {
                Renderer.enabled = false;
            }
        }

        public void StartDissolve()
        {
            Rigidbody.isKinematic = true;
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            if (Collider != null)
            {
                Collider.enabled = false;
            }

            startPosition = Rigidbody.position;
            driftDirection = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)).normalized;
        }

        public void UpdateDissolve(float progress, float riseDistance, float driftDistance)
        {
            Rigidbody.position = startPosition + Vector3.up * riseDistance * progress +
                                 driftDirection * driftDistance * progress;
            SetDissolveAmount(progress);
        }

        public void Dispose()
        {
            if (dissolveMaterial != null)
            {
                Object.Destroy(dissolveMaterial);
            }
        }

        private void SetDissolveAmount(float amount)
        {
            if (dissolveMaterial != null)
            {
                dissolveMaterial.SetFloat("_DissolveAmount", amount);
            }
        }
    }
}
