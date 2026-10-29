using System;
using HarmonyLib;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.World;
using VRage.Game.Entity;
using VRageMath;

namespace PolarsPVPLean.Patches;

// Rolls the first-person gun with the camera so it stays put on screen while leaning,
// like a Rainbow Six view model. Visual only: the aim direction is the camera's forward,
// which the roll does not change.
[HarmonyPatch(typeof(MyCharacterWeaponPositionComponent), "UpdateGraphicalWeaponPosition")]
// ReSharper disable once UnusedType.Global
public static class LeanWeaponPatch
{
    [HarmonyPostfix]
    // ReSharper disable once UnusedMember.Global
    public static void Postfix(MyCharacterWeaponPositionComponent __instance)
    {
        if (LeanController.Disabled)
            return;
        try
        {
            var tilt = LeanController.CurrentTilt;
            if (tilt == 0f)
                return;

            var character = __instance.Character;
            if (character == null || !character.IsInFirstPersonView)
                return;

            var session = MySession.Static;
            if (session == null || !ReferenceEquals(session.LocalCharacter, character))
                return;

            if (character.CurrentWeapon is not MyEntity weapon)
                return;

            // Same roll as the camera: about the view axis through the eye.
            var head = character.GetHeadMatrix(false);
            weapon.WorldMatrix = LeanMath.RollAround(weapon.WorldMatrix, head.Translation, head.Forward, tilt);
        }
        catch (Exception e)
        {
            LeanController.Disable("Gun patch failed, the lean is off until restart", e);
        }
    }
}
