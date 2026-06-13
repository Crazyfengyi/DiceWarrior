using System.IO;
using System.Collections;
using System.Collections.Generic;
using UniFramework.Machine;
using UnityEngine;
using YooAsset;

namespace GameMain
{
    /// <summary>
    /// 初始化资源包
    /// </summary>
    internal class FsmInitializePackage : IStateNode
    {
        private StateMachine machine;

        public FsmInitializePackage()
        {
        }

        void IStateNode.OnCreate(StateMachine machine)
        {
            this.machine = machine;
        }

        void IStateNode.OnEnter()
        {
            PatchStepsChange temp = new PatchStepsChange
            {
                Tips = "初始化资源包！"
            };
            temp.SendEvent();
            GameInit.Instance.StartCoroutine(InitPackage());
        }

        void IStateNode.OnUpdate()
        {
        }

        void IStateNode.OnExit()
        {
        }

        private IEnumerator InitPackage()
        {
            var playMode = (EPlayMode) machine.GetBlackboardValue("PlayMode");
            var packageName = (string) machine.GetBlackboardValue("PackageName");

            // 创建资源包裹类
            bool package = YooAssets.TryGetPackage(packageName, out var packageInfo);
            if (!package)
            {
                packageInfo = YooAssets.CreatePackage(packageName);
            }

            // 编辑器下的模拟模式
            InitializePackageOperation initializationOperation = null;
            if (playMode == EPlayMode.EditorSimulateMode)
            {
                var buildResult = EditorSimulateBuildInvoker.Build(packageName, (int) EBundleType.VirtualAssetBundle);
                var packageRoot = buildResult.PackageRootDirectory;
                var createParameters = new EditorSimulateModeOptions()
                {
                    EditorFileSystemParameters =
                        FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot)
                };
                initializationOperation = packageInfo.InitializePackageAsync(createParameters);
            }

            // 单机运行模式
            if (playMode == EPlayMode.OfflinePlayMode)
            {
                var createParameters = new OfflinePlayModeOptions()
                {
                    BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters()
                };
                initializationOperation = packageInfo.InitializePackageAsync(createParameters);
            }

            // 联机运行模式
            if (playMode == EPlayMode.HostPlayMode)
            {
                string defaultHostServer = GetHostServerURL();
                string fallbackHostServer = GetHostServerURL();
                IRemoteService remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                var createParameters = new HostPlayModeOptions()
                {
                    BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                    CacheFileSystemParameters =
                        FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteServices)
                };
                initializationOperation = packageInfo.InitializePackageAsync(createParameters);
            }

            // WebGL运行模式
            if (playMode == EPlayMode.WebPlayMode)
            {
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
            var createParameters = new WebPlayModeOptions();
			string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE"; //注意：如果有子目录，请修改此处！
            IRemoteService remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            createParameters.WebRemoteFileSystemParameters =
 FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServices);
            createParameters.WebServerFileSystemParameters =
 FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
            initializationOperation = packageInfo.InitializePackageAsync(createParameters);
#else
                var createParameters = new WebPlayModeOptions()
                {
                    WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters()
                };
                initializationOperation = packageInfo.InitializePackageAsync(createParameters);
#endif
            }

            yield return initializationOperation;

            // 如果初始化失败弹出提示界面
            if (initializationOperation?.Status != EOperationStatus.Succeeded)
            {
                Debug.LogWarning($"初始化错误:{initializationOperation?.Error}");
                InitializeFailed temp = new InitializeFailed();
                temp.SendEvent();
            }
            else
            {
                machine.ChangeState<FsmRequestPackageVersion>();
            }
        }

        public static readonly string HostServerIP = "http://127.0.0.1";

        /// <summary>
        /// 获取资源服务器地址
        /// </summary>
        private string GetHostServerURL()
        {
            string appVersion = GameInit.Instance.appVersion;
#if UNITY_EDITOR
            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
                return $"{HostServerIP}/CDN/Android/{appVersion}";
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
                return $"{HostServerIP}/CDN/IPhone/{appVersion}";
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
                return $"{HostServerIP}/CDN/WebGL/{appVersion}";
            else
                return $"{HostServerIP}/CDN/PC/{appVersion}";
#else
            if (Application.platform == RuntimePlatform.Android)
                return $"{HostServerIP}/CDN/Android/{appVersion}";
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
                return $"{HostServerIP}/CDN/IPhone/{appVersion}";
            else if (Application.platform == RuntimePlatform.WebGLPlayer)
                return $"{HostServerIP}/CDN/WebGL/{appVersion}";
            else
                return $"{HostServerIP}/CDN/PC/{appVersion}";
#endif
        }

        /// <summary>
        /// 远端资源地址查询服务类
        /// </summary>
        private class RemoteServices : IRemoteService
        {
            private readonly string _defaultHostServer;
            private readonly string _fallbackHostServer;

            public RemoteServices(string defaultHostServer, string fallbackHostServer)
            {
                _defaultHostServer = defaultHostServer;
                _fallbackHostServer = fallbackHostServer;
            }

            public IReadOnlyList<string> GetRemoteUrls(string fileName)
            {
                return new List<string>
                {
                    $"{_defaultHostServer}/{fileName}",
                    $"{_fallbackHostServer}/{fileName}"
                };
            }
        }
    }
}