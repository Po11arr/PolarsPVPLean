using System;
using Sandbox;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage.Game;
using VRage.Input;
using VRageMath;
using PolarsPVPLean.Settings.Tools;

namespace PolarsPVPLean;

/// <summary>
/// Drives the lean for the local character. The lean is a real sideways step made with the
/// normal strafe input, so the server sees the character, its hitbox and its gun move.
/// </summary>
internal static class LeanController
{
    private const float StepSeconds = 1f / 60f;
    private const float StrafeDeadZone = 0.2f;   // gamepad stick drift is not the player strafing
    private const ulong MaxGapFrames = 60;       // longer gaps (menus) are not counted as lean movement

    private static readonly LeanState State = new();
    private static long characterId;
    private static Vector3D lastPosition;
    private static ulong lastFrame;
    private static bool hasLastPosition;
    private static int toggledSide;

    /// <summary>Camera roll at the current lean, radians, + = right.</summary>
    public static float CurrentTilt { get; private set; }

    /// <summary>True while the game runs the player's input step (see LeanInputPatch).</summary>
    public static bool InPlayerInput;

    /// <summary>Set after an unexpected error: every patch does nothing from then on.</summary>
    public static bool Disabled { get; private set; }

    /// <summary>Turns the lean off for the rest of the session and logs why, once.</summary>
    public static void Disable(string reason, Exception e)
    {
        if (Disabled)
            return;
        Disabled = true;
        Reset();
        Log.Error(reason, e);
    }

    /// <summary>Called at the start of the game's player input step.</summary>
    public static void BeginPlayerInput()
    {
        InPlayerInput = true;

        // In a seat, or using a remote control, turret or camera: the character is not being
        // walked by the player, so end the lean where it is.
        var session = MySession.Static;
        if (session?.LocalCharacter != null && !ReferenceEquals(session.ControlledEntity, session.LocalCharacter))
            Drop();
    }

    /// <summary>Called from MyCharacter.MoveAndRotate for the local character.</summary>
    public static void Update(MyCharacter localCharacter, ref Vector3 moveIndicator, ref float rollIndicator)
    {
        var config = Config.Current;

        if (localCharacter.EntityId != characterId)
        {
            Reset();
            characterId = localCharacter.EntityId;
        }

        var lateralMove = MeasureLateralMove(localCharacter);

        // Jetpack, ladder, death or disabled: drop the lean where it is, let the tilt ease out.
        var mode = MyCharacterMovement.GetMode(localCharacter.CurrentMovementState);
        if (!config.Enabled || !CanLean(localCharacter, mode))
        {
            State.Reset();
            toggledSide = 0;
            CurrentTilt = LeanState.Ease(CurrentTilt, 0f, StepSeconds, TiltTimeConstant);
            return;
        }

        var side = ReadSide(config, out var leanKeyDown);
        State.SetSide(side);

        // Q/E are roll keys by default; keep them from rolling anything while used for leaning on foot.
        if (leanKeyDown)
            rollIndicator = 0f;

        // Briefly airborne (a jump, or a bump on uneven ground): hold the lean as it is until the
        // feet are back down, rather than losing track of it and leaning out a second time.
        if (mode == MyCharacterMovement.Standing || mode == MyCharacterMovement.Crouching)
        {
            // Walking sideways while holding the lean keeps it: the player's own strafe wins for movement.
            // Releasing the lean while walking ends it (LeanState.Step).
            var playerStrafe = moveIndicator.X > StrafeDeadZone ? 1 : moveIndicator.X < -StrafeDeadZone ? -1 : 0;
            var strafe = State.Step(lateralMove, StepSeconds, config.LeanDistance, playerStrafe);
            if (strafe != 0 && playerStrafe == 0)
                moveIndicator.X = strafe;
        }

        var targetTilt = State.TiltFraction(config.LeanDistance) * MathHelper.ToRadians(config.TiltAngle);
        CurrentTilt = LeanState.Ease(CurrentTilt, targetTilt, StepSeconds, TiltTimeConstant);
    }

    // Sideways distance the character moved since the last update, relative to what it stands on,
    // so a moving or turning ship does not count as leaning.
    private static float MeasureLateralMove(MyCharacter localCharacter)
    {
        var position = localCharacter.PositionComp.GetPosition();
        var frame = MySandboxGame.Static?.SimulationFrameCounter ?? 0;
        var frames = frame - lastFrame;
        var counted = hasLastPosition && frame > lastFrame && frames <= MaxGapFrames;

        var lateralMove = 0f;
        if (counted)
        {
            var delta = position - lastPosition;
            var proxy = localCharacter.Physics?.CharacterProxy;
            if (proxy != null && proxy.Supported)
                delta -= (Vector3D)proxy.GroundVelocity * (frames * StepSeconds);
            lateralMove = (float)Vector3D.Dot(delta, localCharacter.WorldMatrix.Right);
        }

        lastPosition = position;
        lastFrame = frame;
        hasLastPosition = true;
        return lateralMove;
    }

    // Camera and gun roll follow the physical lean through this smoothing, so nothing snaps.
    private const float TiltTimeConstant = 0.06f;

    /// <summary>Clears all state. Also runs on world unload (MySession.OnUnloading).</summary>
    public static void Reset()
    {
        characterId = 0;
        Drop();
    }

    // Ends the lean without stepping back.
    private static void Drop()
    {
        State.Reset();
        hasLastPosition = false;
        toggledSide = 0;
        CurrentTilt = 0f;
    }

    // On foot, including briefly airborne. Not in a jetpack, on a ladder, seated or dead.
    private static bool CanLean(MyCharacter localCharacter, ushort mode)
    {
        if (localCharacter.IsDead || localCharacter.JetpackRunning || localCharacter.IsOnLadder)
            return false;

        return mode == MyCharacterMovement.Standing
               || mode == MyCharacterMovement.Crouching
               || mode == MyCharacterMovement.Falling
               || mode == MyCharacterMovement.Jump;
    }

    private static int ReadSide(Config config, out bool leanKeyDown)
    {
        leanKeyDown = false;

        // Only read keys while playing, not while typing in chat or using a menu or terminal.
        if (MyScreenManager.GetScreenWithFocus() is not MyGuiScreenGamePlay || MyGuiScreenGamePlay.ActiveGameplayScreen != null)
            return config.ToggleMode ? toggledSide : 0;

        var input = MyInput.Static;
        var left = IsDown(input, config.LeanLeft);
        var right = IsDown(input, config.LeanRight);
        leanKeyDown = left || right;

        if (!config.ToggleMode)
            return left == right ? 0 : left ? -1 : 1;

        if (IsNewPress(input, config.LeanLeft))
            toggledSide = toggledSide == -1 ? 0 : -1;
        else if (IsNewPress(input, config.LeanRight))
            toggledSide = toggledSide == 1 ? 0 : 1;
        return toggledSide;
    }

    // Modifiers are only checked when the binding uses them, so leaning works while holding Shift or Ctrl.
    private static bool IsDown(IMyInput input, Binding binding)
    {
        return binding.Key != MyKeys.None && ModifiersHeld(input, binding) && input.IsKeyPress(binding.Key);
    }

    private static bool IsNewPress(IMyInput input, Binding binding)
    {
        return binding.Key != MyKeys.None && ModifiersHeld(input, binding) && input.IsNewKeyPressed(binding.Key);
    }

    private static bool ModifiersHeld(IMyInput input, Binding binding)
    {
        return (!binding.Ctrl || input.IsAnyCtrlKeyPressed())
               && (!binding.Alt || input.IsAnyAltKeyPressed())
               && (!binding.Shift || input.IsAnyShiftKeyPressed());
    }
}
