using GameMain;
using TMPro;
using YangTools.Scripts.Core.YangAudio;
using YangTools.Scripts.Core.YangUGUI;

public sealed class WinWindow : UGUIPanelBase<WinWindowData>
{
    public TextMeshProUGUI leftText; 
    public TextMeshProUGUI rightText; 
        
    public UICustomButton doubleBtn;
    public UICustomButton noBtn;
    
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        YangAudioManager.Instance.PlaySoundAudio("Victory");
        doubleBtn.AddListener(OnDoubleClicked);
        noBtn.AddListener(NoBtnClicked);
    }
    
    private void OnDoubleClicked()
    {
        CloseSelfPanel();
        windowData.RestartAction?.Invoke();
    }
    private void NoBtnClicked()
    {
        CloseSelfPanel();
        windowData.RestartAction?.Invoke();
    }

    private void OnRestartClicked()
    {
        CloseSelfPanel();
        windowData.RestartAction?.Invoke();
    }
}
