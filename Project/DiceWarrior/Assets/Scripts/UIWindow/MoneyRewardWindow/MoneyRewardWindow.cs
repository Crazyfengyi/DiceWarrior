using GameMain;
using DG.Tweening;
using TMPro;
using UnityEngine;
using YangTools;
using YangTools.Scripts.Core.YangAudio;
using YangTools.Scripts.Core.YangUGUI;

public class MoneyRewardWindow : UGUIPanelBase<MoneyRewardWindowData>
{
    private const int MoneyPropId = 1;
    private const float DoubleNodeStepDelay = 0.12f;
    private const float DoubleNodeAppearDuration = 0.28f;
    private const float NormalGetButtonDelay = 1.5f;
    private const float AppearOffsetY = 80f;

    public GameObject node;
    public GameObject doubleGetNode;
    public TextMeshProUGUI moneyText1;
    public TextMeshProUGUI moneyText2;
    public TextMeshProUGUI doubleGetText;

    public UICustomButton getBtn;
    public UICustomButton doubleGetBtn;
    public UICustomButton getBtn2;

    private bool granted;
    private Sequence openSequence;

    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        granted = false;
        RefreshShow();
        PlayOpenAnimation();
        getBtn.AddListener(GrantAndClose);
        getBtn2.AddListener(GrantAndClose);
        doubleGetBtn.AddListener(DoubleGrantAndClose);
        YangAudioManager.Instance.PlaySoundAudio("Award_Coins");
    }

    public override void OnClose(bool isShutdown, object userData)
    {
        KillOpenAnimation();
        base.OnClose(isShutdown, userData);
    }

    private void RefreshShow()
    {
        if (moneyText1 == null || windowData == null)
        {
            return;
        }

        if (windowData.CanDoubleGet)
        {
            doubleGetNode.SetActive(true);
            node.SetActive(false);
        }
        else
        {
            doubleGetNode.SetActive(false);
            node.SetActive(true);
        }

        moneyText1.text = $"${windowData.GetRewardString()}";
        moneyText2.text = $"${windowData.GetRewardString()}";
        doubleGetText.text = $"${windowData.GetDoubleRewardString()}";
    }

    private void PlayOpenAnimation()
    {
        KillOpenAnimation();
        if (windowData == null)
        {
            return;
        }

        if (windowData.CanDoubleGet)
        {
            PlayDoubleModeAnimation();
        }
    }

    private void PlayDoubleModeAnimation()
    {
        if (doubleGetNode == null)
        {
            return;
        }

        HideNormalGetButton();
        Transform[] items = GetActiveChildren(doubleGetNode.transform);
        if (items.Length == 0)
        {
            InsertShowNormalGetButton();
            return;
        }

        openSequence = DOTween.Sequence().SetTarget(this);
        for (int i = 0; i < items.Length; i++)
        {
            Transform item = items[i];
            CanvasGroup canvasGroup = GetCanvasGroup(item);
            Vector3 endPosition = item.localPosition;
            item.localPosition = endPosition + Vector3.up * AppearOffsetY;
            canvasGroup.alpha = 0f;

            float startTime = i * DoubleNodeStepDelay;
            openSequence.Insert(startTime, canvasGroup.DOFade(1f, DoubleNodeAppearDuration));
            openSequence.Insert(startTime, item.DOLocalMove(endPosition, DoubleNodeAppearDuration).SetEase(Ease.OutCubic));
        }

        InsertShowNormalGetButton();
    }

    private void HideNormalGetButton()
    {
        if (getBtn2 == null)
        {
            return;
        }

        getBtn2.gameObject.SetActive(false);
    }

    private void InsertShowNormalGetButton()
    {
        if (getBtn2 == null)
        {
            return;
        }

        if (openSequence == null)
        {
            openSequence = DOTween.Sequence().SetTarget(this);
        }

        openSequence.InsertCallback(NormalGetButtonDelay, () => getBtn2.gameObject.SetActive(true));
    }

    private Transform[] GetActiveChildren(Transform root)
    {
        int childCount = root.childCount;
        Transform[] children = new Transform[childCount];
        int activeCount = 0;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (!child.gameObject.activeSelf)
            {
                continue;
            }

            children[activeCount++] = child;
        }

        if (activeCount == childCount)
        {
            SortChildrenTopToBottom(children, activeCount);
            return children;
        }

        Transform[] activeChildren = new Transform[activeCount];
        for (int i = 0; i < activeCount; i++)
        {
            activeChildren[i] = children[i];
        }

        SortChildrenTopToBottom(activeChildren, activeCount);
        return activeChildren;
    }

    private void SortChildrenTopToBottom(Transform[] children, int count)
    {
        for (int i = 0; i < count - 1; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                if (children[i].localPosition.y >= children[j].localPosition.y)
                {
                    continue;
                }

                Transform temp = children[i];
                children[i] = children[j];
                children[j] = temp;
            }
        }
    }

    private CanvasGroup GetCanvasGroup(Transform target)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private void KillOpenAnimation()
    {
        if (openSequence == null)
        {
            return;
        }

        openSequence.Kill();
        openSequence = null;
    }

    private void GrantAndClose()
    {
        if (!granted && windowData != null && windowData.RewardAmount > 0f)
        {
            granted = true;
            float rewardAmount = windowData.RewardAmount;
            string rewardString = windowData.GetRewardString();
            CloseSelfPanel();
            BagMgr.Instance.AddBagProp(MoneyPropId, rewardAmount);
            //FloatTipWindow.Show($"人民币+{rewardString}");
            return;
        }

        CloseSelfPanel();
    }

    private void DoubleGrantAndClose()
    {
        if (!granted && windowData != null && windowData.RewardAmount > 0f)
        {
            granted = true;
            float rewardAmount = windowData.RewardAmount * 2;
            string rewardString = windowData.GetDoubleRewardString();
            CloseSelfPanel();
            BagMgr.Instance.AddBagProp(MoneyPropId, rewardAmount);
            //FloatTipWindow.Show($"人民币+{rewardString}");
            return;
        }
        CloseSelfPanel();
    }
}

public class MoneyRewardWindowData : DefaultUGUIDataBase
{
    public float RewardAmount;
    public int DecimalPlaces;
    public bool CanDoubleGet;

    public MoneyRewardWindowData(float rewardAmount, int decimalPlaces, bool canDoubleGet)
    {
        RewardAmount = rewardAmount;
        DecimalPlaces = Mathf.Max(0, decimalPlaces);
        CanDoubleGet = canDoubleGet;
    }

    public string GetRewardString()
    {
        return RewardAmount.ToString($"F{DecimalPlaces}");
    }

    public string GetDoubleRewardString()
    {
        return (RewardAmount * 2).ToString($"F{DecimalPlaces}");
    }
}
