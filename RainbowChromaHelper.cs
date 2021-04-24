using ChromaSDK;
using System;
using RainbowKnight;

namespace RazerAPI
{
    public class RainbowChromaHelper
    {
        public const string ANIMATION_PATH = "hollow_knight_Data/Managed/Mods/RainbowKnight/Animations";

        private int _mResult;

        private ExecutionPlan _scheduledBackground;

        public int GetInitResult()
        {
            return _mResult;
        }

        public void Start()
        {
            APPINFOTYPE appInfo = new APPINFOTYPE();
            appInfo.Title = "RainbowKnight";
            appInfo.Description = "Hollow Knight RainbowKnight mod";

            appInfo.Author_Name = "Webcretaire";
            appInfo.Author_Contact = "https://github.com/Webcretaire/HollowKnight.RainbowKnight";

            //appInfo.SupportedDevice = 
            //    0x01 | // Keyboards
            //    0x02 | // Mice
            //    0x20   // ChromaLink devices
            appInfo.SupportedDevice = (0x01 | 0x02 | 0x20);
            //    0x01 | // Utility. (To specifiy this is an utility application)
            //    0x02   // Game. (To specifiy this is a game);
            appInfo.Category = 0x02;
            _mResult = ChromaAnimationAPI.InitSDK(ref appInfo);
            switch (_mResult)
            {
                case RazerErrors.RZRESULT_DLL_NOT_FOUND:
                    Console.Error.WriteLine("Chroma DLL is not found! {0}", RazerErrors.GetResultString(_mResult));
                    break;
                case RazerErrors.RZRESULT_DLL_INVALID_SIGNATURE:
                    Console.Error.WriteLine("Chroma DLL has an invalid signature! {0}",
                        RazerErrors.GetResultString(_mResult));
                    break;
                case RazerErrors.RZRESULT_SUCCESS:
                    break;
                default:
                    Console.Error.WriteLine("Failed to initialize Chroma! {0}", RazerErrors.GetResultString(_mResult));
                    break;
            }
        }

        public void OnApplicationQuit()
        {
            ChromaAnimationAPI.Uninit();
        }

        public void PlayBackground()
        {
            PlayAnimation("Background_Keyboard", 0, true);
            PlayAnimation("Background_Mouse", 0, true);
            PlayAnimation("Background_ChromaLink", 0, true);
        }

        public void PlayFullPinkLoad()
        {
            PlayAnimation("FullPinkLoad_Keyboard", 0);
        }
        
        public void PlayFullPinkFlash()
        {
            PlayAnimation("FullPinkFlash_Keyboard", 1500, true);
        }
        
        public void PlayColouredRing()
        {
            PlayAnimation("ColouredRing_Keyboard", 270);
        }

        public void PlayRedRing()
        {
            PlayAnimation("RedRing_Keyboard", 320);
            PlayAnimation("RedRing_Mouse", 320);
            PlayAnimation("RedRing_ChromaLink", 320);
        }

        public void PlayWhiteBars()
        {
            PlayAnimation("WhiteBars_Keyboard", 231);
        }

        public void PlayFullRed()
        {
            PlayAnimation("FullRed_Keyboard", 3000);
        }

        public void PlayFullWhite()
        {
            PlayAnimation("FullWhite_Keyboard", 0, true);
            PlayAnimation("FullWhite_Mouse", 0, true);
            PlayAnimation("FullWhite_ChromaLink", 0, true);
        }

        private void PlayAnimation(string name, int duration, bool loop = false)
        {
            if (_scheduledBackground != null)
            {
                _scheduledBackground.Dispose();
                _scheduledBackground = null;
            }

            // start with a blank animation
            string baseLayer = ANIMATION_PATH + "/" + name + ".chroma";
            // close the blank animation if it's already loaded, discarding any changes
            ChromaAnimationAPI.CloseAnimationName(baseLayer);
            // open the blank animation, passing a reference to the base animation when loading has completed
            ChromaAnimationAPI.GetAnimation(baseLayer);
            // play the animation on the dynamic canvas
            ChromaAnimationAPI.PlayAnimationName(baseLayer, loop);

            if (duration > 0)
                _scheduledBackground = ExecutionPlan.Delay(duration, PlayBackground);
        }
    }
}