using UnityEngine;

namespace Xabblll.DoomInInspector
{
    [DisallowMultipleComponent]
    public class DoomedComponent : MonoBehaviour
    {
        public bool LockKeyboard = true;
        public float TickTime = 0.028571f;
        [HideInInspector] public string WadPath;
        [HideInInspector] public string SfPath;

        public Object Wad;
        public Object SoundFont;
    }
}
