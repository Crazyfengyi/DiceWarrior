using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XYBT
{
    public class RewardAds : MonoSingleton<RewardAds>
    {
        public interface RewardAd
        {
            public bool IsReady { get; }
            public void Show();
            public Action<bool> OnClose { get; set; }
            public Action OnPaid { get; set; }
        }

        public bool registered = false;

        public RewardAd ad;

        public Action OnSuccess;
        public Action OnFailed;
        string _requestViewReason;

        bool _requesting = false;
        const float TIMEOUT = 4f;
        float _timeout = 0f;

        // 出于线程安全，回调不能直接在广告关闭回调中调用

        Action _adFinishCallbacks;

        public void Register(RewardAd ad)
        {
            this.ad = ad;
            ad.OnClose += HandleClose;
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
                Debug.Log("RewardAds: Simulate Ad Success in Editor");
                OnSuccess?.Invoke();
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
            LogState("RequestShowAd.Queued");
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
                    LogState("Update.ShowRewarded");
                    ad.Show();
                }
                else if (_timeout > TIMEOUT)
                {
                    _requesting = false;
                    _timeout = 0f;
                    _requestViewReason = null;
                    LogState("Update.Timeout");
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
            LogState($"HandleClose.fullWatched={fullWatched}");

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


        void LogState(string stage)
        {
            Debug.Log($"RewardAds.{stage} | requesting={_requesting}, Registered={registered},");
        }
    }
}
