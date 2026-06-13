using UnityEngine;

namespace XYBT
{
    public class XYBTSDK : MonoSingleton<XYBTSDK>
    {
        const string CONFIG_RESOURCE_PATH = "XYBTSDKConfig";

        public bool noAds { get; private set; } = false;
        public bool showDebugBannerView { get; private set; } = false;
        public float debugBannerHeightPx { get; private set; } = 100f;

        public static void Init()
        {
            Ins._Init();
        }

        public void _Init()
        {
            ApplyConfig(Resources.Load<XYBTSDKConfig>(CONFIG_RESOURCE_PATH));
            BannerAds.Ins.showDebugBannerView = showDebugBannerView;

            GameObject adapterEntry = Resources.Load<GameObject>("SDKAdapterEntry");

            if (adapterEntry)
            {
                Instantiate(adapterEntry);
            }
        }

        void ApplyConfig(XYBTSDKConfig config)
        {
            noAds = config.noAds;
            showDebugBannerView = config.showDebugBannerView;
            debugBannerHeightPx = config.debugBannerHeightPx;
        }
    }
}
