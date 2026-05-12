using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace NoMoreRolling.Patches
{
    // Vanilla WakeUp() flow:
    //   1. State = Ragdoll  → constraints = None (free rotation)
    //   2. AddTorque around player's right axis  → the spin/front-flip
    //   3. AddForce up + back                    → the launch
    //   4. ragdollDuration later, DelayedDisableRagdoll sets State = Free again
    //
    // We override that with: snap State back to Free immediately (locks rotation,
    // triggers DORotate to upright — necessary because the spawn point has the
    // player lying down inside the coffin), then apply a flat forward push.
    //
    // The brief ~0.5s upright tween is unavoidable: that's the player standing
    // up from the lying-in-coffin pose. Killing it leaves the player horizontal.
    [HarmonyPatch(typeof(PlayerController), "WakeUp")]
    public static class NoSpinWakeUpPatch
    {
        const float ForwardSpeed = 5f;
        const float UpwardSpeed  = 1.5f;

        static readonly FieldInfo    _rbField   = AccessTools.Field(typeof(PlayerController), "_rb");
        static readonly FieldInfo    _headField = AccessTools.Field(typeof(PlayerController), "head");
        static readonly PropertyInfo _stateProp = AccessTools.Property(typeof(PlayerController), "State");

        [HarmonyPostfix]
        static void Postfix(PlayerController __instance)
        {
            var rb = _rbField?.GetValue(__instance) as Rigidbody;
            if (rb == null) return;

            // Clear the torque and up-launch impulse the original WakeUp applied.
            rb.angularVelocity = Vector3.zero;
            rb.linearVelocity  = Vector3.zero;

            // Force the player out of Ragdoll immediately. This re-applies
            // FreezeRotation constraints (no rolling/tumbling) and triggers the
            // DORotate tween that stands them up from the coffin's lying pose.
            _stateProp?.SetValue(__instance, PlayerController.PlayerState.Free);

            // Apply the forward push AFTER the state change (SetPlayerState
            // zeros linearVelocity internally).
            var head = _headField?.GetValue(__instance) as Component;
            Vector3 forward = head != null
                ? Vector3.ProjectOnPlane(head.transform.forward, Vector3.up).normalized
                : Vector3.ProjectOnPlane(__instance.transform.forward, Vector3.up).normalized;

            rb.AddForce(forward * ForwardSpeed + Vector3.up * UpwardSpeed, ForceMode.VelocityChange);
        }
    }
}
