using System.Collections;
using System.Collections.Generic;
using UniFramework.Machine;
using UnityEngine;
using YooAsset;

namespace GameMain
{
    public class FsmUpdatePackageManifest : IStateNode
    {
        private StateMachine _machine;

        public FsmUpdatePackageManifest() { }

        void IStateNode.OnCreate(StateMachine machine)
        {
            _machine = machine;
        }

        void IStateNode.OnEnter()
        {
            Debug.LogWarning("更新资源清单");
            PatchStepsChange temp = new PatchStepsChange
            {
                Tips = "更新资源清单！"
            };
            temp.SendEvent();
            GameInit.Instance.StartCoroutine(UpdateManifest());
        }

        void IStateNode.OnUpdate()
        {
        }

        void IStateNode.OnExit()
        {
        }

        private IEnumerator UpdateManifest()
        {
            yield return null;

            var packageName = (string)_machine.GetBlackboardValue("PackageName");
            var packageVersion = (string)_machine.GetBlackboardValue("PackageVersion");
            
            LoadPackageManifestOptions tempData = new LoadPackageManifestOptions(packageVersion,10);
            var package = YooAssets.GetPackage(packageName);
            var operation = package.LoadPackageManifestAsync(tempData);
            yield return operation;

            if (operation.Status != EOperationStatus.Succeeded)
            {
                Debug.LogWarning(operation.Error);
                PackageManifestUpdateFailed temp = new PackageManifestUpdateFailed();
                temp.SendEvent();
                yield break;
            }
            else
            {
                _machine.ChangeState<FsmCreatePackageDownloader>();
            }
        }
    }
}