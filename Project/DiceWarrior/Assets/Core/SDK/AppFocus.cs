using System;

namespace XYBT
{
    public class AppFocus : MonoSingleton<AppFocus>
    {
        public Action OnAppHide;
        public Action OnAppRestore;

        public void CallAppHide()
        {
            OnAppHide?.Invoke();
        }

        public void CallAppRestore()
        {
            OnAppRestore?.Invoke();
        }
    }
}
