using System;

namespace PolarsPVPLean;

/// <summary>
/// Pure lean logic, no game types, so it can be unit tested.
/// Tracks the sideways offset the lean itself has moved the character (metres, + = right)
/// and decides how much strafe input to inject each simulation step.
/// </summary>
internal sealed class LeanState
{
    private const float Tolerance = 0.03f;       // close enough to the target offset (m)
    private const float StopLead = 0.05f;        // stop pushing this many seconds before reaching the target
    private const float BlockedSpeed = 0.15f;    // below this sideways speed (m/s) while pushing = blocked
    private const float BlockedTime = 0.12f;     // seconds of being blocked before giving up on more offset
    private const float TeleportDistance = 2f;   // larger jumps in one step reset the lean
    private const float PlayerMoveGrace = 0.15f; // seconds after the player's own strafe before resuming
    private const float ReturnTimeout = 1f;      // give up stepping back after this long (normal return ~0.25 s)

    /// <summary>Sideways offset made by the lean, + = right.</summary>
    public float Offset { get; private set; }

    /// <summary>-1 = lean left, 0 = straight, +1 = lean right.</summary>
    public int Side { get; private set; }

    private float blockedTimer;
    private float blockedLimit = float.NaN;   // offset at which a wall stopped us, for the current side
    private bool pushedLastStep;
    private float playerMoveTimer;
    private float returnTimer;

    public void Reset()
    {
        Offset = 0f;
        Side = 0;
        blockedTimer = 0f;
        blockedLimit = float.NaN;
        pushedLastStep = false;
        playerMoveTimer = 0f;
        returnTimer = 0f;
    }

    public void SetSide(int side)
    {
        side = Math.Sign(side);
        if (side == Side)
            return;
        Side = side;
        blockedTimer = 0f;
        blockedLimit = float.NaN;
    }

    /// <summary>
    /// One simulation step.
    /// lateralMove: sideways distance moved since the previous step (m, + = right).
    /// playerStrafe: the player's own strafe input this step, -1 (left), 0 or +1 (right).
    /// Returns the strafe input to inject: -1, 0 or +1.
    /// </summary>
    public int Step(float lateralMove, float stepSeconds, float distance, int playerStrafe = 0)
    {
        if (Math.Abs(lateralMove) > TeleportDistance)
        {
            Reset();
            return 0;
        }

        // The player's own sideways walking moves the whole lean with them: while the lean key is
        // held it is not counted as lean offset, and the lean waits until they stop and settle.
        if (playerStrafe != 0)
        {
            playerMoveTimer = PlayerMoveGrace;
            pushedLastStep = false;
            blockedTimer = 0f;
            returnTimer = 0f;

            // Lean released while walking: walking back toward the start undoes the lean as they go,
            // walking further out drops it (they are moving that way anyway).
            if (Side == 0 && Offset != 0f)
            {
                if (Math.Sign(playerStrafe) == Math.Sign(Offset))
                    Offset = 0f;
                else
                    ReturnBy(lateralMove);
            }
            return 0;
        }

        if (playerMoveTimer > 0f)
        {
            playerMoveTimer -= stepSeconds;
            if (Side == 0)
                ReturnBy(lateralMove);   // coasting back after the player stops counts too
            return 0;
        }

        if (Side != 0 || Math.Abs(Offset) > 0.0001f)
            Offset += lateralMove;

        var target = Side * distance;
        if (!float.IsNaN(blockedLimit))
            target = blockedLimit;

        var error = target - Offset;
        if (Math.Abs(error) < Tolerance)
        {
            pushedLastStep = false;
            blockedTimer = 0f;
            returnTimer = 0f;
            if (Side == 0 && Math.Abs(Offset) < Tolerance)
                Offset = 0f;
            return 0;
        }

        // Released but something keeps the character from getting back: drop the lean rather
        // than leave the camera tilted and the strafe pushing.
        if (Side == 0)
        {
            returnTimer += stepSeconds;
            if (returnTimer >= ReturnTimeout)
            {
                DropLean();
                return 0;
            }
        }
        else
        {
            returnTimer = 0f;
        }

        var direction = Math.Sign(error);
        var speed = stepSeconds > 0f ? lateralMove / stepSeconds : 0f;

        // Coast the last bit: the character keeps sliding briefly after input stops.
        if (Math.Sign(speed) == direction && Math.Abs(error) <= Math.Abs(speed) * StopLead)
        {
            pushedLastStep = false;
            return 0;
        }

        // Pushing but not moving: a wall. Leaning stops at the offset reached so far;
        // stepping back counts as back.
        if (pushedLastStep && Math.Abs(speed) < BlockedSpeed)
        {
            blockedTimer += stepSeconds;
            if (blockedTimer >= BlockedTime)
            {
                if (Side != 0)
                {
                    blockedLimit = Offset;
                    pushedLastStep = false;
                    blockedTimer = 0f;
                }
                else
                {
                    DropLean();
                }
                return 0;
            }
        }
        else
        {
            blockedTimer = 0f;
        }

        pushedLastStep = true;
        return direction;
    }

    // Moves the offset toward zero by any sideways movement in that direction, never past it.
    private void ReturnBy(float lateralMove)
    {
        if (Offset == 0f || Math.Sign(lateralMove) == Math.Sign(Offset))
            return;
        var next = Offset + lateralMove;
        Offset = Math.Sign(next) == Math.Sign(Offset) ? next : 0f;
    }

    private void DropLean()
    {
        Offset = 0f;
        pushedLastStep = false;
        blockedTimer = 0f;
        returnTimer = 0f;
    }

    /// <summary>Lean amount for the camera tilt, -1 (full left) to +1 (full right).</summary>
    public float TiltFraction(float distance)
    {
        if (distance <= 0f)
            return 0f;
        return Math.Max(-1f, Math.Min(1f, Offset / distance));
    }

    /// <summary>Eases a value toward a target with a time constant, frame-rate independent.</summary>
    public static float Ease(float current, float target, float stepSeconds, float timeConstant)
    {
        if (timeConstant <= 0f)
            return target;
        var k = 1f - (float)Math.Exp(-stepSeconds / timeConstant);
        var next = current + (target - current) * k;
        return Math.Abs(next - target) < 1e-4f ? target : next;
    }
}
