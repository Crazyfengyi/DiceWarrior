using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XYBT
{
    public class VibrateManager : MonoSingleton<VibrateManager>
    {
        public Action CallVibrateShort;
        public Action CallVibrateLong;

        public bool enableVibrate;
        public void VibrateShort()
        {
            if (!enableVibrate)
            {
                return;
            }
            CallVibrateShort?.Invoke();
        }

        public void VibrateLong()
        {
            if (!enableVibrate)
            {
                return;
            }
            CallVibrateLong?.Invoke();
        }
    }

}
