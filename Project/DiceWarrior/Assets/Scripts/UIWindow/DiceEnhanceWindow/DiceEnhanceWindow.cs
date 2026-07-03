using System;
using System.Collections.Generic;
using cfg;
using cfg.diceenhance;
using GameMain;
using TMPro;
using UnityEngine;
using YangTools.Scripts.Core.YangUGUI;

public sealed class DiceEnhanceWindow : UGUIPanelBase<DiceEnhanceWindowData>
{
    public TMP_FontAsset font;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI effectTitleText;
    [SerializeField] private TextMeshProUGUI effectDescText;
    [SerializeField] private TextMeshProUGUI previewNameText;
    [SerializeField] private TextMeshProUGUI previewDescText;
    [SerializeField] private TextMeshProUGUI rangeText;
    [SerializeField] private TextMeshProUGUI probabilityText;
    [SerializeField] private TextMeshProUGUI modeHintText;
    [SerializeField] private List<DiceEnhanceDiceItemUI> diceItems = new List<DiceEnhanceDiceItemUI>();
    [SerializeField] private List<DiceEnhanceFaceItemUI> faceItems = new List<DiceEnhanceFaceItemUI>();
    [SerializeField] private UICustomButton confirmButton;
    [SerializeField] private UICustomButton skipButton;
    [SerializeField] private UICustomButton closeButton;

    private IReadOnlyList<EquippedDiceSlotData> diceSlots;
    private DiceEnhanceConfig enhanceConfig;
    private int selectedDiceIndex;
    private int selectedFaceIndex;
    private bool hasSelectedFace;
    private bool resultHandled;
    private bool initialized;

    /// <summary>
    /// 打开强化页并初始化显示。
    /// </summary>
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ValidateBindings();

        if (windowData == null || windowData.EnhanceConfig == null || !windowData.EnhanceConfig.Enabled)
        {
            FloatTipWindow.Show("\u9ab0\u5b50\u5f3a\u5316\u914d\u7f6e\u9519\u8bef");
            resultHandled = true;
            CloseSelfPanel();
            return;
        }

        diceSlots = windowData.DiceSlots;
        enhanceConfig = windowData.EnhanceConfig;
        selectedDiceIndex = GetInitialDiceIndex();
        hasSelectedFace = false;
        selectedFaceIndex = 0;
        SyncSelectedFaceWithMode();

        RegisterEventsIfNeeded();
        RefreshView();
    }

    /// <summary>
    /// 关闭窗口时兜底回调取消逻辑。
    /// </summary>
    public override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        if (!resultHandled)
        {
            windowData?.OnCancel?.Invoke();
            resultHandled = true;
        }
    }

    /// <summary>
    /// 检查并补齐窗口控件引用。
    /// </summary>
    private void ValidateBindings()
    {
        if (titleText != null)
        {
            return;
        }

        titleText = FindText("WindowRoot/Title");
        effectTitleText = FindText("WindowRoot/EffectPanel/EffectTitle");
        effectDescText = FindText("WindowRoot/EffectPanel/EffectDesc");
        previewNameText = FindText("WindowRoot/PreviewPanel/PreviewName");
        previewDescText = FindText("WindowRoot/PreviewPanel/PreviewDesc");
        rangeText = FindText("WindowRoot/PreviewPanel/RangeText");
        probabilityText = FindText("WindowRoot/PreviewPanel/ProbabilityText");
        modeHintText = FindText("WindowRoot/PreviewPanel/ModeHint");
        confirmButton ??= FindButton("WindowRoot/BottomBar/ConfirmButton");
        skipButton ??= FindButton("WindowRoot/BottomBar/SkipButton");
        closeButton ??= FindButton("WindowRoot/CloseButton");

        if (diceItems.Count == 0)
        {
            diceItems.AddRange(GetComponentsInChildren<DiceEnhanceDiceItemUI>(true));
            diceItems.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        }

        if (faceItems.Count == 0)
        {
            faceItems.AddRange(GetComponentsInChildren<DiceEnhanceFaceItemUI>(true));
            faceItems.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        }
    }

    /// <summary>
    /// 注册按钮和列表点击事件。
    /// </summary>
    private void RegisterEventsIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        for (int i = 0; i < diceItems.Count; i++)
        {
            if (diceItems[i] != null)
            {
                diceItems[i].Init(i, OnDiceItemClicked);
            }
        }

        for (int i = 0; i < faceItems.Count; i++)
        {
            if (faceItems[i] != null)
            {
                faceItems[i].Init(i, OnFaceItemClicked);
            }
        }

        confirmButton?.AddListener(OnConfirmClicked);
        skipButton?.AddListener(OnSkipClicked);
        closeButton?.AddListener(OnSkipClicked);
        initialized = true;
    }

    /// <summary>
    /// 刷新整页显示。
    /// </summary>
    private void RefreshView()
    {
        if (titleText != null)
        {
            titleText.text = "\u9ab0\u5b50\u5f3a\u5316";
        }

        if (effectTitleText != null)
        {
            effectTitleText.text = enhanceConfig.Name;
        }

        if (effectDescText != null)
        {
            effectDescText.text = enhanceConfig.Desc;
        }

        if (modeHintText != null)
        {
            modeHintText.text = enhanceConfig.TargetMode == EDiceEnhanceTargetMode.WholeDice
                ? "\u672c\u6b21\u53ea\u80fd\u5f3a\u5316\u6574\u9897\u9ab0\u5b50"
                : "\u672c\u6b21\u53ea\u80fd\u5f3a\u5316\u5355\u4e2a\u9762";
        }

        RefreshDiceList();
        RefreshPreview();
    }

    /// <summary>
    /// 刷新左侧骰子列表。
    /// </summary>
    private void RefreshDiceList()
    {
        for (int i = 0; i < diceItems.Count; i++)
        {
            EquippedDiceSlotData dice = diceSlots != null && i < diceSlots.Count ? diceSlots[i] : null;
            bool interactable = dice != null && !dice.IsEmpty;
            bool selected = i == selectedDiceIndex && interactable;
            if (diceItems[i] != null)
            {
                diceItems[i].Refresh(dice, selected, interactable);
            }
        }
    }

    /// <summary>
    /// 刷新中部预览信息。
    /// </summary>
    private void RefreshPreview()
    {
        EquippedDiceSlotData selectedDice = GetSelectedDice();
        DiceEnhancePreviewModel preview = new DiceEnhancePreviewModel(selectedDice, enhanceConfig,
            enhanceConfig.TargetMode == EDiceEnhanceTargetMode.SingleFace && hasSelectedFace ? selectedFaceIndex : null);

        if (previewNameText != null)
        {
            previewNameText.text = selectedDice == null || selectedDice.IsEmpty ? "\u6682\u65e0\u9ab0\u5b50" : selectedDice.Name;
        }

        if (previewDescText != null)
        {
            previewDescText.text = selectedDice == null || selectedDice.IsEmpty
                ? "\u5f53\u524d\u6ca1\u6709\u53ef\u5f3a\u5316\u7684\u9ab0\u5b50"
                : enhanceConfig.TargetMode == EDiceEnhanceTargetMode.WholeDice
                    ? "\u70b9\u51fb\u5de6\u4fa7\u9ab0\u5b50\u5361\u7247\u5207\u6362\u6574\u9897\u5f3a\u5316\u76ee\u6807"
                    : "\u5148\u9009\u5de6\u4fa7\u9ab0\u5b50\uff0c\u518d\u70b9\u4e0b\u65b9\u9762\u503c\u9009\u62e9\u5355\u9762\u5f3a\u5316";
        }

        if (rangeText != null)
        {
            rangeText.text = preview.HasValidTarget ? preview.RangeText : "\u6781\u9650\u533a\u95f4  -";
        }

        if (probabilityText != null)
        {
            probabilityText.text = preview.HasValidTarget ? preview.ProbabilityText : "\u6682\u65e0\u6982\u7387\u6570\u636e";
        }

        RefreshFaceItems(preview);
        SetConfirmState(preview.HasValidTarget);
    }

    /// <summary>
    /// 刷新骰面格子显示。
    /// </summary>
    private void RefreshFaceItems(DiceEnhancePreviewModel preview)
    {
        for (int i = 0; i < faceItems.Count; i++)
        {
            bool visible = preview.PreviewFaces != null && i < preview.PreviewFaces.Count;
            bool selected = enhanceConfig.TargetMode == EDiceEnhanceTargetMode.SingleFace && hasSelectedFace &&
                i == selectedFaceIndex;
            bool interactable = visible && enhanceConfig.TargetMode == EDiceEnhanceTargetMode.SingleFace;
            if (faceItems[i] != null)
            {
                faceItems[i].Refresh(visible ? preview.PreviewFaces[i] : 0, selected, visible, interactable);
            }
        }
    }

    /// <summary>
    /// 切换当前选中的骰子。
    /// </summary>
    private void OnDiceItemClicked(int index)
    {
        EquippedDiceSlotData dice = diceSlots != null && index >= 0 && index < diceSlots.Count ? diceSlots[index] : null;
        if (dice == null || dice.IsEmpty)
        {
            return;
        }

        selectedDiceIndex = index;
        SyncSelectedFaceWithMode();
        RefreshView();
    }

    /// <summary>
    /// 切换当前选中的骰面。
    /// </summary>
    private void OnFaceItemClicked(int index)
    {
        if (enhanceConfig.TargetMode != EDiceEnhanceTargetMode.SingleFace)
        {
            return;
        }

        EquippedDiceSlotData dice = GetSelectedDice();
        if (dice == null || dice.IsEmpty || index < 0 || index >= dice.Faces.Count)
        {
            return;
        }

        selectedFaceIndex = index;
        hasSelectedFace = true;
        RefreshPreview();
    }

    /// <summary>
    /// 确认强化并回调外部逻辑。
    /// </summary>
    private void OnConfirmClicked()
    {
        if (resultHandled)
        {
            return;
        }

        EquippedDiceSlotData dice = GetSelectedDice();
        if (dice == null || dice.IsEmpty)
        {
            return;
        }

        if (enhanceConfig.TargetMode == EDiceEnhanceTargetMode.SingleFace && !hasSelectedFace)
        {
            return;
        }

        resultHandled = true;
        windowData?.OnConfirm?.Invoke(selectedDiceIndex,
            enhanceConfig.TargetMode == EDiceEnhanceTargetMode.SingleFace ? selectedFaceIndex : (int?)null);
        CloseSelfPanel();
    }

    /// <summary>
    /// 放弃强化并关闭窗口。
    /// </summary>
    private void OnSkipClicked()
    {
        if (resultHandled)
        {
            return;
        }

        resultHandled = true;
        windowData?.OnCancel?.Invoke();
        CloseSelfPanel();
    }

    /// <summary>
    /// 更新确认按钮可用状态。
    /// </summary>
    private void SetConfirmState(bool canConfirm)
    {
        if (confirmButton == null || confirmButton.TargetButton == null)
        {
            return;
        }

        confirmButton.TargetButton.interactable = canConfirm;
        confirmButton.SetGray(!canConfirm);
    }

    /// <summary>
    /// 获取当前选中的骰子。
    /// </summary>
    private EquippedDiceSlotData GetSelectedDice()
    {
        return diceSlots != null && selectedDiceIndex >= 0 && selectedDiceIndex < diceSlots.Count
            ? diceSlots[selectedDiceIndex]
            : null;
    }

    /// <summary>
    /// 计算初始默认选中的骰子索引。
    /// </summary>
    private int GetInitialDiceIndex()
    {
        if (diceSlots == null || diceSlots.Count == 0)
        {
            return 0;
        }

        if (windowData.InitialSelectedDiceIndex >= 0 && windowData.InitialSelectedDiceIndex < diceSlots.Count &&
            diceSlots[windowData.InitialSelectedDiceIndex] != null &&
            !diceSlots[windowData.InitialSelectedDiceIndex].IsEmpty)
        {
            return windowData.InitialSelectedDiceIndex;
        }

        for (int i = 0; i < diceSlots.Count; i++)
        {
            if (diceSlots[i] != null && !diceSlots[i].IsEmpty)
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    /// 根据模式同步单面选择状态。
    /// </summary>
    private void SyncSelectedFaceWithMode()
    {
        if (enhanceConfig == null || enhanceConfig.TargetMode != EDiceEnhanceTargetMode.SingleFace)
        {
            hasSelectedFace = false;
            selectedFaceIndex = 0;
            return;
        }

        EquippedDiceSlotData dice = GetSelectedDice();
        if (dice == null || dice.IsEmpty || dice.Faces.Count == 0)
        {
            hasSelectedFace = false;
            selectedFaceIndex = 0;
            return;
        }

        if (!hasSelectedFace || selectedFaceIndex < 0 || selectedFaceIndex >= dice.Faces.Count)
        {
            selectedFaceIndex = 0;
            hasSelectedFace = true;
        }
    }

    /// <summary>
    /// 按路径查找文本组件。
    /// </summary>
    private TextMeshProUGUI FindText(string path)
    {
        Transform target = transform.Find(path);
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }

    /// <summary>
    /// 按路径查找按钮组件。
    /// </summary>
    private UICustomButton FindButton(string path)
    {
        Transform target = transform.Find(path);
        return target != null ? target.GetComponent<UICustomButton>() : null;
    }
}
