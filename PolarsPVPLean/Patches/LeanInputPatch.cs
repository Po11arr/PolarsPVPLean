using System;
using HarmonyLib;
using Sandbox.Game.Gui;

namespace PolarsPVPLean.Patches;

// Marks the game's per-frame player input step. MyCharacter.MoveAndRotate is also called with
// zero input from MyCharacter.UpdateZeroMovement (parallel entity update) on frames where the
// input step sent nothing, so the lean only runs inside this method: once per frame, on the
// main thread, and only while the gameplay screen is taking input.
[HarmonyPatch(typeof(MyGuiScreenGamePlay), nameof(MyGuiScreenGamePlay.MoveAndRotatePlayerOrCamera))]
// ReSharper disable once UnusedType.Global
public static class LeanInputPatch
{
    [HarmonyPrefix]
    // ReSharper disable once UnusedMember.Global
    public static void Prefix()
    {
        if (LeanController.Disabled)
            return;
        try
        {
            LeanController.BeginPlayerInput();
        }
        catch (Exception e)
        {
            LeanController.Disable("Input patch failed, the lean is off until restart", e);
        }
    }

    [HarmonyFinalizer]
    // ReSharper disable once UnusedMember.Global
    public static void Finalizer()
    {
        LeanController.InPlayerInput = false;
    }
}
