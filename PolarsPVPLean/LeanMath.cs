using VRageMath;

namespace PolarsPVPLean;

internal static class LeanMath
{
    /// <summary>
    /// Rolls a view matrix by tilt radians around its forward axis (+ = head to the right).
    /// </summary>
    public static MatrixD RollView(MatrixD view, float tilt)
    {
        return view * MatrixD.CreateRotationZ(tilt);
    }

    /// <summary>
    /// Rotates a world matrix around an axis through a pivot, matching the camera roll from
    /// <see cref="RollView"/> when the axis is the camera's forward direction.
    /// </summary>
    public static MatrixD RollAround(MatrixD world, Vector3D pivot, Vector3D forward, float tilt)
    {
        var rotation = MatrixD.CreateFromAxisAngle(Vector3D.Normalize(forward), tilt);
        return world * MatrixD.CreateTranslation(-pivot) * rotation * MatrixD.CreateTranslation(pivot);
    }
}
