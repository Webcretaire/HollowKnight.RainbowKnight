using System;
using ChromaSDK;
using Modding;
using RainbowKnight;

namespace RazerAPI
{
    public class RainbowChromaHelper : Loggable
    {
        public const string ANIMATION_PATH = "hollow_knight_Data/Managed/Mods/RainbowKnight/Animations";

        private int _mResult;

        private ExecutionPlan _scheduledBackground;

        public int GetInitResult()
        {
            return _mResult;
        }

        public bool Start()
        {
            try
            {
                var appInfo = new APPINFOTYPE
                {
                    Title = "RainbowKnight",
                    Description = "Hollow Knight RainbowKnight mod",
                    Author_Name = "Webcretaire",
                    Author_Contact = "https://github.com/Webcretaire/HollowKnight.RainbowKnight",
                    SupportedDevice = (int) ChromaAnimationAPI.Device.Keyboard |
                                      (int) ChromaAnimationAPI.Device.Mouse |
                                      (int) ChromaAnimationAPI.Device.ChromaLink,
                    Category = 0x02 // 0x01 = Utility ; 0x02 = Game
                };

                _mResult = ChromaAnimationAPI.InitSDK(ref appInfo);
                switch (_mResult)
                {
                    case RazerErrors.RZRESULT_DLL_NOT_FOUND:
                        LogError("Chroma DLL is not found! " + RazerErrors.GetResultString(_mResult));
                        return false;
                    case RazerErrors.RZRESULT_DLL_INVALID_SIGNATURE:
                        LogError("Chroma DLL has an invalid signature! " + RazerErrors.GetResultString(_mResult));
                        return false;
                    case RazerErrors.RZRESULT_SUCCESS:
                        return true;
                    default:
                        LogError("Failed to initialize Chroma! " + RazerErrors.GetResultString(_mResult));
                        return false;
                }
            }
            catch (Exception e)
            {
                LogError("Error during Chroma Helper Start: " + e.Message);
                return false;
            }
        }

        public void OnApplicationQuit()
        {
            if (ChromaAnimationAPI.IsInitialized())
                ChromaAnimationAPI.Uninit();
        }

        public void PlayBackground()
        {
            PlayAnimationAllDevices("Background", 0, true);
        }

        public void PlayFullPinkLoad()
        {
            PlayAnimationAllDevices("FullPinkLoad", 0);
        }

        public void PlayFullPinkFlash()
        {
            PlayAnimationAllDevices("FullPinkFlash", 0, true);
        }

        public void PlayColouredRing()
        {
            PlayAnimationAllDevices("ColouredRing", 10000, true);
        }

        public void PlayRedRing()
        {
            PlayAnimationAllDevices("RedRing", 320);
        }

        public void PlayWhiteBars()
        {
            PlayAnimationAllDevices("WhiteBars", 231);
        }

        public void PlayFullRed()
        {
            PlayAnimationAllDevices("FullRed", 3000);
        }

        public void PlayFullWhite()
        {
            PlayAnimationAllDevices("FullWhite", 0, true);
        }

        public void PlayYellowDiskLoad()
        {
            PlayAnimationAllDevices("YellowDiskLoad", 10000, true);
        }

        public void PlayYellowFlash()
        {
            PlayAnimationAllDevices("YellowFlash", 1000);
        }

        private void PlayAnimationAllDevices(string name, int duration, bool loop = false)
        {
            PlayAnimation(name + "_Keyboard", duration, loop);
            PlayAnimation(name + "_Mouse", duration, loop);
            PlayAnimation(name + "_ChromaLink", duration, loop);
        }

        private void PlayAnimation(string name, int duration, bool loop = false)
        {
            if (_scheduledBackground != null)
            {
                _scheduledBackground.Dispose();
                _scheduledBackground = null;
            }

            ChromaAnimationAPI.PlayAnimationName(ANIMATION_PATH + "/" + name + ".chroma", loop);

            if (duration > 0)
                _scheduledBackground = ExecutionPlan.Delay(duration, PlayBackground);
        }
    }
}