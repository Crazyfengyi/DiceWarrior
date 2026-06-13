using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniFramework.Machine;
using UnityEngine;
using YangTools;
using YangTools.Scripts.Core.ResourceManager;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;
using YooAsset;

namespace GameMain
{
    internal class FsmReadyStartGame : IStateNode
    {
        private StateMachine machine;

        public FsmReadyStartGame()
        {
        }

        void IStateNode.OnCreate(StateMachine machine)
        {
            this.machine = machine;
        }

        async void IStateNode.OnEnter()
        {
            UserResourcesReadyPress press = new UserResourcesReadyPress();
            press.press = 0.2f;
            press.SendEvent();
            Debug.LogWarning("准备游戏启动");
            //配置表
            await GameTableManager.Instance.LoadAllConfigs();
            await UniTask.Delay(500);
            press.press = 0.5f;
            press.SendEvent();
            await UniTask.Delay(1000);

            GameInit.Instance.InitializeAfterManagers();
            //预加载资源
            var preloadList = new List<string>()
            {
            };

            for (int i = 0; i < preloadList.Count; i++)
            {
                press.press = 0.5f + 0.5f / preloadList.Count * i;
                press.SendEvent();
                await ResourceManager.LoadAssetAsync<Object>(preloadList[i]);
            }

            LoadOnProgress temp = new LoadOnProgress();
            temp.OnEndLoad = async (sceneName) =>
            {
                if (sceneName == "MainGame")
                {
                    (int id, GameWindow panel) panelData = await UIMonoInstance.OpenPanel<GameWindow>(GroupType.中间);
                }
            };
            GameSceneManager.Instance.SetOnProgress(temp);

            if (YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>().isFirstEnter)
            {
                YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>().isFirstEnter = false;
                //TODO:进入游戏
                //await UIWindowTool.ChangeToBattleScene();
                GameSceneManager.Instance.Load("MainGame");
            }
            else
            {
                //await UIWindowTool.ChangeToMainUIScene(); 
                GameSceneManager.Instance.Load("MainGame");
            }

            press.press = 1f;
            press.SendEvent();
            await UniTask.WaitUntil(() => LoadScript.isLoadAniOver);
            await UniTask.Delay(1000);
            
            machine.ChangeState<FsmLoadDone>();
        }

        void IStateNode.OnUpdate()
        {
        }

        void IStateNode.OnExit()
        {
        }
    }
}