using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XYBT
{
    public class BannerAds : MonoSingleton<BannerAds>
    {
        public interface BannerAd
        {
            public bool IsReady { get; }
            public bool IsLoading { get; }
            public bool IsShowing { get; }
            // Layout reservation height in screen pixels. This is allowed to be a stable fallback
            // before the SDK reports the final rendered banner height.
            public float ReservedHeightPx { get; }

            public void Load();
            public void Show();
            public void Hide();
        }

        public bool registered = false;

        public BannerAd ad;

        public bool RequestedVisible => !NoAdsEnabled && _requestVisible;
        public bool IsReady => !NoAdsEnabled && registered && ad.IsReady;
        public bool IsLoading => !NoAdsEnabled && registered && ad.IsLoading;
        public bool IsShowing => !NoAdsEnabled && registered && ad.IsShowing;
        // Exposes the layout reservation height instead of the runtime visibility state.
        public float ReservedHeightPx => !NoAdsEnabled && registered ? ad.ReservedHeightPx : 0f;
        public bool showDebugBannerView
        {
            get { return _showDebugBannerView; }
            set
            {
                _showDebugBannerView = value;
                SyncDebugBannerViewState();
            }
        }

        const float LOAD_RETRY_INTERVAL = 3f;
        const string DEBUG_BANNER_CANVAS_RESOURCE_PATH = "BannerAdDebugCanvas";
        const string DEBUG_BANNER_VIEW_NAME = "View_BannerAd";

        bool _requestVisible = false;
        bool _requestLoad = false;
        float _loadRetryTimer = 0f;
        bool _showDebugBannerView = false;
        GameObject _debugBannerCanvasRoot;
        Canvas _debugBannerCanvas;
        RectTransform _debugBannerRectTransform;

        bool NoAdsEnabled => XYBTSDK.Ins.noAds;

        public void Register(BannerAd ad)
        {
            this.ad = ad;
            registered = true;
            _requestVisible = false;
            _requestLoad = false;
            _loadRetryTimer = 0f;

            LogState("Register");
            SyncDebugBannerViewState();
        }

        public void RequestLoadAd()
        {
            if (NoAdsEnabled)
            {
                ApplyNoAdsState();
                LogState("RequestLoadAd.NoAds");
                return;
            }

#if UNITY_EDITOR
            return;
#endif

            if (!registered)
            {
                return;
            }

            _requestLoad = true;
            LogState("RequestLoadAd");
        }

        public void RequestShowAd()
        {
            if (NoAdsEnabled)
            {
                ApplyNoAdsState();
                LogState("RequestShowAd.NoAds");
                return;
            }

#if UNITY_EDITOR
            _requestVisible = true;
            ShowDebugBannerView();
            LogState("RequestShowAd.Editor");
            return;
#endif

            if (!registered)
            {
                return;
            }

            _requestVisible = true;
            _requestLoad = true;
            LogState("RequestShowAd");
        }

        public void RequestHideAd()
        {
            if (NoAdsEnabled)
            {
                ApplyNoAdsState();
                LogState("RequestHideAd.NoAds");
                return;
            }

            _requestVisible = false;

#if UNITY_EDITOR
            HideDebugBannerView();
            LogState("RequestHideAd.Editor");
            return;
#endif

            if (!registered)
            {
                return;
            }

            // Hide must not depend on the adapter's local IsShowing flag because the SDK runtime
            // visibility can drift from our cached state after full-screen ad flows.
            HideBannerAd();
            LogState("RequestHideAd");
        }

        public void RequestCloseAd()
        {
            RequestHideAd();
        }

        void Update()
        {
            if (NoAdsEnabled)
            {
                ApplyNoAdsState();
                return;
            }

#if UNITY_EDITOR
            SyncDebugBannerViewState();
            return;
#endif

            if (!registered)
            {
                return;
            }

            if (_loadRetryTimer > 0f)
            {
                _loadRetryTimer -= Time.unscaledDeltaTime;
            }

            if (_requestLoad && !ad.IsReady && !ad.IsLoading && _loadRetryTimer <= 0f)
            {
                _loadRetryTimer = LOAD_RETRY_INTERVAL;
                LogState("Update.Load");
                ad.Load();
            }

            if (_requestVisible)
            {
                if (ad.IsReady && !ad.IsShowing)
                {
                    LogState("Update.Show");
                    ShowBannerAd();
                }
            }
            else
            {
                if (ad.IsShowing)
                {
                    LogState("Update.Hide");
                    HideBannerAd();
                }
            }
        }

        void ShowBannerAd()
        {
            ad.Show();
            ShowDebugBannerView();
        }

        void HideBannerAd()
        {
            ad.Hide();
            HideDebugBannerView();
        }

        void ApplyNoAdsState()
        {
            _requestVisible = false;
            _requestLoad = false;

            if (registered && ad != null && ad.IsShowing)
            {
                HideBannerAd();
                return;
            }

            HideDebugBannerView();
        }

        void SyncDebugBannerViewState()
        {
            if (!ShouldShowDebugBannerView())
            {
                HideDebugBannerView();
                return;
            }

            ShowDebugBannerView();
        }

        void ShowDebugBannerView()
        {
            if (!_showDebugBannerView || NoAdsEnabled)
            {
                return;
            }

            EnsureDebugBannerView();
            _debugBannerCanvasRoot.transform.localScale = Vector3.one;
            _debugBannerCanvasRoot.SetActive(true);
            SyncDebugBannerViewSize();
        }

        void HideDebugBannerView()
        {
            if (_debugBannerCanvasRoot == null)
            {
                return;
            }

            _debugBannerCanvasRoot.SetActive(false);
        }

        void EnsureDebugBannerView()
        {
            if (_debugBannerCanvasRoot != null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(DEBUG_BANNER_CANVAS_RESOURCE_PATH);
            _debugBannerCanvasRoot = Instantiate(prefab);
            _debugBannerCanvasRoot.name = DEBUG_BANNER_CANVAS_RESOURCE_PATH;
            DontDestroyOnLoad(_debugBannerCanvasRoot);

            _debugBannerCanvas = _debugBannerCanvasRoot.GetComponent<Canvas>();
            _debugBannerRectTransform = _debugBannerCanvasRoot.transform.Find(DEBUG_BANNER_VIEW_NAME) as RectTransform;
            _debugBannerCanvasRoot.SetActive(false);
        }

        void SyncDebugBannerViewSize()
        {
            float bannerHeightPixels = GetDebugBannerHeightPx();
            float bannerHeightUI = bannerHeightPixels / _debugBannerCanvas.scaleFactor;
            _debugBannerRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bannerHeightUI);
        }

        bool ShouldShowDebugBannerView()
        {
            if (!_showDebugBannerView || NoAdsEnabled)
            {
                return false;
            }

#if UNITY_EDITOR
            return _requestVisible;
#endif

            return registered && ad != null && ad.IsShowing;
        }

        float GetDebugBannerHeightPx()
        {
            if (!registered || ad == null)
            {
                return XYBTSDK.Ins.debugBannerHeightPx;
            }

            return ad.ReservedHeightPx;
        }

        void LogState(string stage)
        {
            Debug.Log($"bannerAds.{stage} | {GetStateSummary()}");
        }

        string GetStateSummary()
        {
            if (!registered || ad == null)
            {
                return $"noAds={NoAdsEnabled}, registered={registered}, requestVisible={_requestVisible}, requestLoad={_requestLoad}, loadRetryTimer={_loadRetryTimer:F2}";
            }

            return $"noAds={NoAdsEnabled}, registered={registered}, requestVisible={_requestVisible}, requestLoad={_requestLoad}, isReady={IsReady}, isLoading={IsLoading}, isShowing={IsShowing}, reservedHeightPx={ReservedHeightPx:F1}, loadRetryTimer={_loadRetryTimer:F2}";
        }
    }
}
