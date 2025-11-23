#if DOTWEEN_ENABLED
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    /// <summary>
    /// Provides methods for comparing software version numbers using Semantic Versioning(X.Y.Z).
    /// </summary>
    public static class VersionComparator
    {
        /// <summary>
        /// Enum representing the result of a version comparison.
        /// </summary>
        public enum VersionComparisonResult
        {
            /// <summary>
            /// Indicates an incorrect version format.
            /// </summary>
            IncorrectFormat,
            /// <summary>
            /// Indicates that the first version is lesser than the second version.
            /// </summary>
            Lesser,
            /// <summary>
            /// Indicates that the two versions are equal.
            /// </summary>
            Equal,
            /// <summary>
            /// Indicates that the first version is greater than the second version.
            /// </summary>
            Greater
        }

        /// <summary>
        /// Compares two versions and returns the comparison result.
        /// </summary>
        /// <param name="versionA">The first version to compare.</param>
        /// <param name="versionB">The second version to compare.</param>
        /// <returns>The result of the version comparison.</returns>
        public static VersionComparisonResult Compare(string versionA, string versionB)
        {
            // Validate the version format for versionA.
            if (!IsValidVersionFormat(versionA))
            {
                Debug.LogError("Version A has incorrect version format. It should be X.Y.Z");
                return VersionComparisonResult.IncorrectFormat;
            }

            // Validate the version format for versionB.
            if (!IsValidVersionFormat(versionB))
            {
                Debug.LogError("Version B has incorrect version format. It should be X.Y.Z");
                return VersionComparisonResult.IncorrectFormat;
            }

            // Split the versions into numbers.
            string[] versionANumbers = versionA.Split('.');
            string[] versionBNumbers = versionB.Split('.');

            // Compare the versions.
            for (int i = 0; i < 3; i++)
            {
                int versionANumber = int.Parse(versionANumbers[i]);
                int versionBNumber = int.Parse(versionBNumbers[i]);

                if (versionANumber < versionBNumber)
                    return VersionComparisonResult.Lesser;
                else if (versionANumber > versionBNumber)
                    return VersionComparisonResult.Greater;
            }

            return VersionComparisonResult.Equal;
        }

        private static bool IsValidVersionFormat(string version)
        {
            string[] versionNumbers = version.Split('.');
            if (versionNumbers.Length != 3)
                return false;

            foreach (string number in versionNumbers)
            {
                if (!int.TryParse(number, out int _))
                    return false;
            }

            return true;
        }
    }
}
#endif