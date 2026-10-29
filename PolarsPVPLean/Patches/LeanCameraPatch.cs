using System;
using HarmonyLib;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using VRageMath;

namespace PolarsPVPLean.Patches;

// Rolls the first-person camera around its forward axis. Forward is unchanged, so the
// crosshair and aim direction stay exactly where the gun points. Visual only, local only.
[HarmonyPatch(typeof(MyCharacter), nameof(MyCharacter.GetViewMatrix))]
// ReSharper disable once UnusedType.Global
public static class LeanCameraPatch
{
    [HarmonyPostfix]
    // ReSharper disable once UnusedMember.Global
    public static void Postfix(MyCharacter __instance, ref MatrixD __result)
    {
        if (LeanController.Disabled)
            return;
        try
        {
            var tilt = LeanController.CurrentTilt;
            if (tilt == 0f || !__instance.IsInFirstPersonView)
                return;

            var session = MySession.Static;
            if (session == null || !ReferenceEquals(session.LocalCharacter, __instance))
                return;

            __result = LeanMath.RollView(__result, tilt);
        }
        catch (Exception e)
        {
            LeanController.Disable("Camera patch failed, the lean is off until restart", e);
        }
    }
}
