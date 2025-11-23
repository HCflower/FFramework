using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    public static class VectorDirectionUtility
    {
        public enum VectorDirection
        {
            Up,
            Down,
            Left,
            Right,
            Forward,
            Backward
        }

        /// <summary>
        /// Gets the Vector3 representation of a given direction.
        /// </summary>
        /// <param name="direction">The direction to convert.</param>
        /// <returns>A Vector3 representing the specified direction.</returns>
        public static Vector3 GetDirectionVector(VectorDirection direction)
        {
            switch (direction)
            {
                case VectorDirection.Up:
                    return Vector3.up;
                case VectorDirection.Down:
                    return Vector3.down;
                case VectorDirection.Left:
                    return Vector3.left;
                case VectorDirection.Right:
                    return Vector3.right;
                case VectorDirection.Forward:
                    return Vector3.forward;
                case VectorDirection.Backward:
                    return Vector3.back;
                default:
                    return Vector3.zero; // Default in case an undefined direction is used
            }
        }
    }
}
