using System;
using HarmonyLib;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using VRageMath;

namespace PolarsPVPLean.Patches;

// The lean is injected as normal strafe input for the local character only, exactly like
// pressing A or D, so movement, hitbox and gun position replicate to the server as usual.
[HarmonyPatch(typeof(MyCharacter), nameof(MyCharacter.MoveAndRotate), typeof(Vector3), typeof(Vector2), typeof(float))]
// ReSharper disable once UnusedType.Global
public static class LeanMovePatch
{
    [HarmonyPrefix]
    // ReSharper disable once UnusedMember.Global
    public static void Prefix(MyCharacter __instance, ref Vector3 moveIndicator, ref float rollIndicator)
    {
        if (LeanController.Disabled)
            return;
        try
        {
            if (!LeanController.InPlayerInput)
                return;

            var session = MySession.Static;
            if (session == null || !ReferenceEquals(session.LocalCharacter, __instance))
                return;

            LeanController.Update(__instance, ref moveIndicator, ref rollIndicator);
        }
        catch (Exception e)
        {
            LeanController.Disable("Movement patch failed, the lean is off until restart", e);
        }
    }
}
