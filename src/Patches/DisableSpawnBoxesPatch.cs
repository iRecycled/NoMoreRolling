using DG.Tweening;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace NoMoreRolling.Patches
{
    // Extends DisableLidColliders to make ALL box colliders triggers so the player
    // can exit through any face.
    [HarmonyPatch(typeof(SpawnBoxPlayerRagdollTrigger), "UserCode_TargetDisableLids__NetworkConnection")]
    public static class DisableAllBoxCollidersOnClientPatch
    {
        [HarmonyPostfix]
        static void Postfix(SpawnBoxPlayerRagdollTrigger __instance)
        {
            foreach (var col in __instance.GetComponentsInChildren<Collider>())
                col.isTrigger = true;
        }
    }

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

            rb.angularVelocity = Vector3.zero;
            rb.linearVelocity  = Vector3.zero;

            float spawnY = __instance.transform.eulerAngles.y;

            // Transition to Free: re-applies FreezeRotation, starts DORotate(0,0,0, 0.5s).
            // SetPlayerState zeros linearVelocity here — force is applied after so that's fine.
            _stateProp?.SetValue(__instance, PlayerController.PlayerState.Free);

            // Replace the DORotate with a short version that preserves Y (no camera pan)
            // and completes in 0.1s instead of 0.5s so the stand-up is barely noticeable.
            __instance.transform.DOKill();
            __instance.transform.DORotate(new Vector3(0f, spawnY, 0f), 0.1f, RotateMode.Fast)
                .SetEase(Ease.OutCubic);

            var head = _headField?.GetValue(__instance) as Component;
            Vector3 forward = head != null
                ? Vector3.ProjectOnPlane(head.transform.forward, Vector3.up).normalized
                : Vector3.ProjectOnPlane(__instance.transform.forward, Vector3.up).normalized;

            rb.AddForce(forward * ForwardSpeed + Vector3.up * UpwardSpeed, ForceMode.VelocityChange);

        }
    }
}
