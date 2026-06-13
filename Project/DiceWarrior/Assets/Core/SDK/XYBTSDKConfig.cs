using UnityEngine;

namespace XYBT
{
    [CreateAssetMenu(fileName = "XYBTSDKConfig", menuName = "XYBT/SDK Config")]
    public class XYBTSDKConfig : ScriptableObject
    {
        [Header("Debug Options")]
        public bool noAds = false;
        public bool showDebugBannerView = false;
        public float debugBannerHeightPx = 100f;
    }
}
