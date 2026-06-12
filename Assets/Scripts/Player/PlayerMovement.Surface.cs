using System.Collections.Generic;
using UnityEngine;

public partial class PlayerMovement
{
    private bool hasWalkableContact;
    private WaypointFollower currentPlatform;
    private readonly HashSet<Collider> stairContacts = new HashSet<Collider>();

    public bool IsOnStairs => stairContacts.Count > 0;

    void OnCollisionStay(Collision collision)
    {
        if (!IsStandingOnCollision(collision))
        {
            return;
        }

        hasWalkableContact = true;

        WaypointFollower platform = collision.collider.GetComponentInParent<WaypointFollower>();
        if (platform != null)
        {
            currentPlatform = platform;
        }

        if (collision.collider.GetComponentInParent<StairSurface>() != null)
        {
            stairContacts.Add(collision.collider);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        hasWalkableContact = false;
        stairContacts.Remove(collision.collider);

        WaypointFollower platform = collision.collider.GetComponentInParent<WaypointFollower>();
        if (platform == currentPlatform)
        {
            currentPlatform = null;
        }
    }

    private bool IsStandingOnCollision(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                return true;
            }
        }

        return false;
    }
}
