// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;

namespace Buildalon.Editor.BuildPipeline
{
    public static class BuildOptionsExtensions
    {
        public static BuildOptions SetFlag(this BuildOptions a, BuildOptions b) => a | b;

        public static BuildOptions UnsetFlag(this BuildOptions a, BuildOptions b) => a & (~b);

        public static bool HasFlag(this BuildOptions a, BuildOptions b) => (a & b) == b;

        public static BuildOptions ToggleFlag(this BuildOptions a, BuildOptions b) => a ^ b;
    }
}
