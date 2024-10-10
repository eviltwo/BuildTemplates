using UnityEngine;

namespace BuildTemplates
{
    [CreateAssetMenu(fileName = "BuildTemplate", menuName = "Build Template")]
    public class BuildTemplate : ScriptableObject
    {
        public bool Development;
        public bool AutoconnectProfiler;
        public bool AllowDebugging;
        public string[] ExtraScriptingDefines = new string[0];
    }
}
