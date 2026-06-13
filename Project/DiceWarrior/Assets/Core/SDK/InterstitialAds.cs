using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XYBT
{
    public class InterstitialAds : MonoSingleton<InterstitialAds>
    {
        public interface InterstitialAd
        {
            public bool IsReady { get; set; }
            public void Show();
            public Action<bool> OnClose { get; set; }
            public Action OnPaid { get; set; }
        }

        public bool registered = false;

        public InterstitialAd ad;

        public Action OnSuccess;
        public Action OnFailed;
        string _requestViewReason;

        bool _requesting = false;
        const float TIMEOUT = 4f;
        float _timeout = 0f;

        // 出于线程安全，回调不能直接在广告关闭回调中调用

        Action _adFinishCallbacks;

        public void Register(InterstitialAd ad)
        {
            this.ad = ad;
            ad.OnClose += HandleClose;
            ad.OnPaid += HandlePaid;
            registered = true;
        }

        public void RequestShowAd(Action OnSuccess = null, Action OnFailed = null, string viewReason = null)
        {
            bool editorMode = false;
#if UNITY_EDITOR
            editorMode = true;
#endif
            if (editorMode)
            {
                return;
            }
            if (XYBTSDK.Ins.noAds)
            {
                OnSuccess?.Invoke();
                return;
            }

            if (!registered)
            {
                OnFailed?.Invoke();
                return;
            }

            this.OnSuccess = OnSuccess;
            this.OnFailed = OnFailed;
            _requestViewReason = viewReason;
            _requesting = true;
        }

        void Update()
        {
            if (!registered)
            {
                return;
            }

            if (_requesting)
            {
                if (ad.IsReady)
                {
                    _requesting = false;
                    _timeout = 0f;
                    ad.Show();
                }
                else if (_timeout > TIMEOUT)
                {
                    _requesting = false;
                    _timeout = 0f;
                    _requestViewReason = null;
                    OnFailed?.Invoke();
                }
                else
                {
                    _timeout += Time.unscaledDeltaTime;
                }
            }

            if (_adFinishCallbacks != null)
            {
                _adFinishCallbacks();
                _adFinishCallbacks = null;
            }
        }

        void HandleClose(bool fullWatched)
        {
            if (fullWatched)
            {
                if (OnSuccess != null)
                {
                    _adFinishCallbacks += () => OnSuccess();
                }
            }
            else
            {
                if (OnFailed != null)
                {
                    _adFinishCallbacks += () => OnFailed();
                }
            }

            _requestViewReason = null;
        }

        void HandlePaid()
        {
            if (string.IsNullOrEmpty(_requestViewReason))
            {
                return;
            }
        }
    }
}