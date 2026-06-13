using System;
using Manager;
using YangTools;
using YooAsset;
using StateMachine = UniFramework.Machine.StateMachine;

namespace GameMain
{
    public class PatchManager
    {
        private static StateMachine machine;
        private static StepsType stepsType = StepsType.None;

        public static void Create(string packageName, EPlayMode playMode)
        {
            if (!IsValidPlayMode(playMode))
            {
                throw new ArgumentException($"Invalid play mode: {playMode}.", nameof(playMode));
                return;
            }
                
            // 注册监听事件
            Extend.AddEventListener<UserTryInitialize>(GameInit.Instance.gameObject, OnHandleEventMessage);
            Extend.AddEventListener<UserBeginDownloadWebFiles>(GameInit.Instance.gameObject, OnHandleEventMessage);
            Extend.AddEventListener<UserTryUpdatePackageVersion>(GameInit.Instance.gameObject, OnHandleEventMessage);
            Extend.AddEventListener<UserTryUpdatePatchManifest>(GameInit.Instance.gameObject, OnHandleEventMessage);
            Extend.AddEventListener<UserTryDownloadWebFiles>(GameInit.Instance.gameObject, OnHandleEventMessage);

            // 创建状态机
            machine = new StateMachine(null);
            machine.AddNode(new FsmInitializePackage());
            machine.AddNode(new FsmRequestPackageVersion());
            machine.AddNode(new FsmUpdatePackageManifest());
            machine.AddNode(new FsmCreatePackageDownloader());
            machine.AddNode(new FsmDownloadPackageFiles());
            machine.AddNode(new FsmDownloadPackageOver());
            machine.AddNode(new FsmClearPackageCache());
            machine.AddNode(new FsmReadyStartGame());
            machine.AddNode(new FsmLoadDone());

            machine.SetBlackboardValue("PackageName", packageName);
            machine.SetBlackboardValue("PlayMode", playMode);
        }

        public static void Start()
        {
            stepsType = StepsType.Update;
            machine.Run<FsmInitializePackage>();
        }

        public static void Update()
        {
            if (stepsType is StepsType.None or StepsType.Done)
            {
                return;
            }

            if (stepsType == StepsType.Update)
            {
                machine.Update();
                if (machine.CurrentNode == typeof(FsmLoadDone).FullName)
                {
                    YangEventManager.Instance.Clear();
                    stepsType = StepsType.Done;
                }
            }
        }
        /// <summary>
        /// 接收事件
        /// </summary>
        private static void OnHandleEventMessage(EventData eventData)
        {
            if (eventData.Args is UserTryInitialize)
            {
                machine.ChangeState<FsmInitializePackage>();
            }
            else if (eventData.Args is UserBeginDownloadWebFiles)
            {
                machine.ChangeState<FsmDownloadPackageFiles>();
            }
            else if (eventData.Args is UserTryUpdatePackageVersion)
            {
                machine.ChangeState<FsmRequestPackageVersion>();
            }
            else if (eventData.Args is UserTryUpdatePatchManifest)
            {
                machine.ChangeState<FsmUpdatePackageManifest>();
            }
            else if (eventData.Args is UserTryDownloadWebFiles)
            {
                machine.ChangeState<FsmCreatePackageDownloader>();
            }
            else
            {
                throw new System.NotImplementedException($"错误:{eventData.Name}");
            }
        }
        
        private static bool IsValidPlayMode(EPlayMode playMode)
        {
            switch (playMode)
            {
                case EPlayMode.EditorSimulateMode:
                case EPlayMode.OfflinePlayMode:
                case EPlayMode.HostPlayMode:
                case EPlayMode.WebPlayMode:
                    return true;
                default:
                    return false;
            }
        }
    }
}