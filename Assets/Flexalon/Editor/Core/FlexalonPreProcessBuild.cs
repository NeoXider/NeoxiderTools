using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Flexalon
{
    public class PreProcessBuild :
        IPreprocessBuildWithReport
    {
        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.StandaloneOSX)
            {
                // Needed by GetPixelDensity.mm helper to retrieve the correct DPI on macOS
                PlayerSettings.SetAdditionalIl2CppArgs("--linker-flags=\"-framework AppKit\"");
            }
        }
    }
}