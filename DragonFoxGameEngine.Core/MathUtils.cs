using Silk.NET.Maths;
using System;

namespace DragonGameEngine.Core
{
    public static class MathUtils
    {
        public static double ConvertDegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        /// <summary>
        /// Returns a forward vector relative to the provided matrix.
        /// </summary>
        /// <param name="matrix">The matrix from which to base the vector.</param>
        /// <returns>A 3-component directional vector.</returns>
        public static Vector3D<float> ForwardFromMatrix(Matrix4X4<float> matrix)
        {
            Vector3D<float> forward = Vector3D<float>.Zero;
            forward.X = -matrix.Row1[2];
            forward.Y = -matrix.Row2[2];
            forward.Z = -matrix.Row3[2];
            forward = Vector3D.Normalize(forward);
            return forward;
        }

        /// <summary>
        /// Returns a upward vector relative to the provided matrix.
        /// </summary>
        /// <param name="matrix">The matrix from which to base the vector.</param>
        /// <returns>A 3-component directional vector.</returns>
        public static Vector3D<float> UpFromMatrix(Matrix4X4<float> matrix)
        {
            Vector3D<float> up = Vector3D<float>.Zero;
            up.X = matrix.Row1[1];
            up.Y = matrix.Row2[1];
            up.Z = matrix.Row3[1];
            up = Vector3D.Normalize(up);
            return up;
        }

        /// <summary>
        /// Returns a right vector relative to the provided matrix.
        /// </summary>
        /// <param name="matrix">The matrix from which to base the vector.</param>
        /// <returns>A 3-component directional vector.</returns>
        public static Vector3D<float> RightFromMatrix(Matrix4X4<float> matrix)
        {
            Vector3D<float> right = Vector3D<float>.Zero;
            right.X = matrix.Row1[0];
            right.Y = matrix.Row2[0];
            right.Z = matrix.Row3[0];
            right = Vector3D.Normalize(right);
            return right;
        }
    }
}
