using GameMain;
using TMPro;
using YangTools.Scripts.Core.YangAudio;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;

public sealed class LoseWindow : UGUIPanelBase<LoseWindowData>
{
    public TextMeshProUGUI moneyText; 
        
    public UICustomButton restartBtn;
    
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        YangAudioManager.Instance.PlaySoundAudio("Defeat");
        moneyText.text = $"${BagMgr.Instance.TryGetBagProp(1)?.PropCountString}";
        restartBtn.AddListener(OnRestartClicked);
    }
    
    private void OnRestartClicked()
    {
        CloseSelfPanel();
        windowData.RestartAction?.Invoke();
    }
}
