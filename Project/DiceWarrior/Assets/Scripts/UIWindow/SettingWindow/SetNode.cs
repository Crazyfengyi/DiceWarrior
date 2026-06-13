using System;
using System.Collections;
using System.Collections.Generic;
using GameMain;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.YangAudio;
using YangTools.Scripts.Core.YangSaveData;

public class SetNode : MonoBehaviour
{
    public ExtendedSlider musicSlider;
    public ExtendedSlider soundSlider;
    public UICustomToggle soundToggle;
    public UICustomToggle musicToggle;
    public UICustomToggle mUICustomToggle_Shake;

    private void Awake()
    {
        musicSlider.onValueChanged.AddListener(OnMusicValueChang);
        soundSlider.onValueChanged.AddListener(OnSoundValueChange);
        soundSlider.pointerUp = OnSliderEndDrag;
        musicSlider.pointerUp = OnSliderEndDrag;
        musicSlider.endDrag = OnSliderEndDrag;
        soundSlider.endDrag = OnSliderEndDrag;
        mUICustomToggle_Shake.OnToggleClickCallback = OnShakeToggleClick;
        soundToggle.OnToggleClickCallback = OnSoundToggleClick;
        musicToggle.OnToggleClickCallback = OnMusicToggleClick;
    }

    private void Start()
    {
        Save_GameSet saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameSet>();
        YangAudioManager.Instance.BGMValue = saveData.musicValue;
        YangAudioManager.Instance.SoundValue = saveData.soundValue;
        musicSlider.value = YangAudioManager.Instance.BGMValue;
        soundSlider.value = YangAudioManager.Instance.SoundValue;

        CheckShowUpdate(); // 检查并显示更新信息
    }

    private void OnSliderEndDrag()
    {
        // Save_GameSet saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameSet>(true);
        // saveData.musicValue =  musicSlider.value;
        // YangAudioManager.Instance.BGMValue = saveData.musicValue;
        // saveData.soundValue = soundSlider.value;
        // YangAudioManager.Instance.SoundValue = saveData.soundValue;
    }

    private void OnMusicValueChang(float value)
    {
        Save_GameSet saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameSet>(true);
        saveData.musicValue = value;
        YangAudioManager.Instance.BGMValue = value;
        CheckShowUpdate();
    }

    private void OnSoundValueChange(float value)
    {
        Save_GameSet saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameSet>(true);
        saveData.soundValue = value;
        YangAudioManager.Instance.SoundValue = value;
        CheckShowUpdate();
    }

    private void OnSoundToggleClick(UICustomToggle clickTarget,bool isOn)
    {
        Save_GameSet saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameSet>(true);
        saveData.soundValue = isOn ? 0 : 1;
        YangAudioManager.Instance.SoundValue = saveData.soundValue;
        CheckShowUpdate();
    }

    private void OnMusicToggleClick(UICustomToggle clickTarget,bool isOn)
    {
        Save_GameSet saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameSet>(true);
        saveData.musicValue = isOn ? 0 : 1;
        YangAudioManager.Instance.BGMValue = saveData.musicValue;
        CheckShowUpdate();
    }

    private void OnShakeToggleClick(UICustomToggle clickTarget,bool isOn)
    {
        Save_GameSet saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameSet>(true);
        saveData.isOnShake = !isOn;
        CheckShowUpdate();
    }

    public void CheckShowUpdate()
    {
        Save_GameSet saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameSet>();
        mUICustomToggle_Shake.SetToggle(saveData.isOnShake);
        soundToggle.SetToggle(saveData.soundValue > 0);
        musicToggle.SetToggle(saveData.musicValue > 0);
    }
}