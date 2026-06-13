using System.Collections;
using UniFramework.Machine;
using UnityEngine;
using YooAsset;

namespace GameMain
{
    public class FsmDownloadPackageFiles : IStateNode
    {
        private StateMachine machine;

        public FsmDownloadPackageFiles() { }

        void IStateNode.OnCreate(StateMachine machine)
        {
            this.machine = machine;
        }
        void IStateNode.OnEnter()
        {
            PatchStepsChange temp = new PatchStepsChange
            {
                Tips = "开始下载补丁文件"
            };
            temp.SendEvent();
            GameInit.Instance.StartCoroutine(BeginDownload());
        }
        void IStateNode.OnUpdate()
        {
        }
        void IStateNode.OnExit()
        {
        }

        private IEnumerator BeginDownload()
        {
            ResourceDownloaderOperation downloader = (ResourceDownloaderOperation)machine.GetBlackboardValue("Downloader");
            downloader.DownloadProgressChanged += DownloadUpdate.SendEventMessage;
            downloader.DownloadError += WebFileDownloadFailed.SendEventMessage;
            downloader.StartDownload();
            yield return downloader;
            // 检测下载结果
            if (downloader.Status != EOperationStatus.Succeeded)
            {
                yield break;
            }
            machine.ChangeState<FsmDownloadPackageOver>();
        }
    }
}