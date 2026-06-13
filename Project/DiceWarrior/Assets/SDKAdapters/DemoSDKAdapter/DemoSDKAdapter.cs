using System;
using UnityEngine;
using XYBT;

public class DemoSDKAdapter : MonoSingleton<DemoSDKAdapter>
{
    public override void Awake()
    {
        base.Awake();

        Vibration.Init();
        VibrateManager.Ins.CallVibrateShort += VibrateShort;
        VibrateManager.Ins.CallVibrateLong += VibrateLong;
    }

    void VibrateShort()
    {
        Vibration.VibratePop();
    }

    void VibrateLong()
    {
        Vibration.Vibrate();
    }
}
