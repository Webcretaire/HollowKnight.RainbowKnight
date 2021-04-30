using System;
using System.Collections.Generic;
using ChromaSDK;
using Modding;
using RazerAPI;

namespace RainbowKnight
{
    public class RainbowKnight : Mod
    {
        private RainbowChromaHelper _chromaHelper;

        private readonly Dictionary<string, bool> _animState = new Dictionary<string, bool>();

        private bool _stateRequestsBackground;

        private int _frameCount;

        public RainbowKnight() : base("RainbowKnight")
        {
        }

        public override string GetVersion()
        {
            return "0.2.0";
        }

        public override void Initialize()
        {
            Log("Start RainbowKnight Init");

            Environment.SetEnvironmentVariable(
                "PATH",
                Environment.GetEnvironmentVariable("PATH") + ";hollow_knight_Data\\Managed\\Mods\\RainbowKnight"
            );

            _chromaHelper = new RainbowChromaHelper();

            if (!_chromaHelper.Start()) return; // There was an error at startup, don't register any hook

            Log("Razer SDK init: " + RazerErrors.GetResultString(_chromaHelper.GetInitResult()));

            ModHooks.Instance.TakeHealthHook += OnTakeHealth;
            ModHooks.Instance.BeforePlayerDeadHook += OnPlayerDead;
            ModHooks.Instance.ApplicationQuitHook += OnApplicationQuit;
            ModHooks.Instance.HeroUpdateHook += OnHeroUpdate;
            ModHooks.Instance.LanguageGetHook += OnLanguageGet;
            ModHooks.Instance.SetPlayerBoolHook += OnSetPlayerBool;

            _chromaHelper.PlayBackground();
        }

        /// <summary>
        /// Triggers or stops an animation when the condition is toggled
        /// </summary>
        /// <param name="condition">Condition to test</param>
        /// <param name="state">An arbitrary string key to differentiate calls to this function</param>
        /// <param name="animation">The animation to be triggered when `condition` becomes true</param>
        /// <returns>true if the animation was triggered, false if nothing was done or Background was resumed</returns>
        private bool BooleanAnimationUpdate(bool condition, string state, Action animation)
        {
            var stateActive = _animState.ContainsKey(state) && _animState[state];

            switch (condition)
            {
                case true when !stateActive:
                    LogDebug("Triggering " + state + " animation");
                    _animState[state] = true;
                    _stateRequestsBackground = false; // Cancel any in-progress "background resume" request
                    animation();
                    return true;
                case false when stateActive:
                    LogDebug("Requesting background resume " + state + " ended");
                    _animState[state] = false;
                    _stateRequestsBackground = true;
                    return false;
            }

            return false;
        }

        private string OnLanguageGet(string key, string sheet)
        {
            // This is a "hack" to detect when we're back to the main menu, so that we can reset all lighting
            if (key == "MAIN_OPTIONS")
                _chromaHelper.PlayBackground();

            return Language.Language.GetInternal(key, sheet);
        }

        private bool UpdateCrystalDashLoadState()
        {
            return BooleanAnimationUpdate(
                HeroController.instance.superDash.ActiveStateName.StartsWith("Ground Charge") ||
                HeroController.instance.superDash.ActiveStateName.StartsWith("Wall Charge"),
                "Cdash_charge",
                _chromaHelper.PlayFullPinkLoad
            );
        }


        private bool UpdateCrystalDashFlyState()
        {
            return BooleanAnimationUpdate(
                HeroController.instance.cState.superDashing,
                "Cdash_fly",
                _chromaHelper.PlayFullPinkFlash
            );
        }

        private bool UpdateSpellState()
        {
            return BooleanAnimationUpdate(
                HeroController.instance.spellControl.ActiveStateName != "Inactive" &&
                HeroController.instance.spellControl.ActiveStateName != "Button Down",
                "Spell",
                _chromaHelper.PlayColouredRing
            );
        }

        private bool UpdateNailChargeState()
        {
            return BooleanAnimationUpdate(
                HeroController.instance.cState.nailCharging,
                "Nail_charging",
                _chromaHelper.PlayFullWhite
            );
        }

        private bool UpdateMovementState()
        {
            return BooleanAnimationUpdate(
                HeroController.instance.cState.bouncing ||
                HeroController.instance.cState.doubleJumping ||
                HeroController.instance.cState.dashing ||
                HeroController.instance.cState.shroomBouncing,
                "Double_Jumping",
                _chromaHelper.PlayWhiteBars
            );
        }

        /**
         * Updates for things I didn't find a cleaner trigger for (either a dedicated hook or an Int / Bool value) 
         */
        private void OnHeroUpdate()
        {
            // Don't update things too frequently, we don't want to burn people's CPUs
            if (++_frameCount < 10) return;

            _frameCount = 0;

            // These "if → return" make sure we only trigger one animation per cycle.
            // Not that there is anything wrong with triggering several, but it allows to set a priority for which
            // animation should be triggered if its state is active
            if (UpdateMovementState()) return;
            if (UpdateCrystalDashFlyState()) return;
            if (UpdateCrystalDashLoadState()) return;
            if (UpdateSpellState()) return;
            if (UpdateDreamNailSlashState()) return;
            if (UpdateDreamNailChargeState()) return;
            if (UpdateNailChargeState()) return;

            // Resuming the background should have the absolute lowest priority, which is why we don't resume it in
            // each `Update*` function directly, but only ask for it to be resumed (which might end up not being 
            // necessary if another animation needs to run instead)
            if (_stateRequestsBackground && !_animState.ContainsValue(true))
            {
                LogDebug("Effectively resuming background animation");
                _stateRequestsBackground = false;
                _chromaHelper.PlayBackground();
            }
        }
        
        private bool UpdateDreamNailChargeState()
        {
            return BooleanAnimationUpdate(
                HeroController.instance.gameObject.LocateMyFSM("Dream Nail").ActiveStateName.Contains("Charge"),
                "Dream_Nail_charging",
                _chromaHelper.PlayYellowDiskLoad
            );
        }

        private bool UpdateDreamNailSlashState()
        {
            return BooleanAnimationUpdate(
                HeroController.instance.gameObject.LocateMyFSM("Dream Nail").ActiveStateName == "Slash",
                "Dream_Nailing_slashing",
                _chromaHelper.PlayYellowFlash
            );
        }

        private void OnPlayerDead()
        {
            _chromaHelper.PlayFullRed();
        }

        private int OnTakeHealth(int damage)
        {
            _chromaHelper.PlayRedRing();
            return damage;
        }

        private void OnSetPlayerBool(string set, bool value)
        {
            BooleanAnimationUpdate(
                set == "atBench",
                "Benching",
                _chromaHelper.PlayFullWhite
            );
            PlayerData.instance.SetBoolInternal(set, value);
        }

        // Uninitialize Razer's SDK if the game gracefully exits
        private void OnApplicationQuit()
        {
            _chromaHelper.OnApplicationQuit();
        }
    }
}