﻿using System;
using System.Collections.Generic;
using ChromaSDK;
using Modding;
using RazerAPI;
using UnityEngine;

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
            return "0.1.0";
        }

        public override void Initialize()
        {
            Log("Start RainbowKnight Init");

            Environment.SetEnvironmentVariable("PATH",
                Environment.GetEnvironmentVariable("PATH") + ";hollow_knight_Data\\Managed\\Mods\\RainbowKnight");

            _chromaHelper = new RainbowChromaHelper();
            _chromaHelper.Start();

            Log("Razer SDK init: " + RazerErrors.GetResultString(_chromaHelper.GetInitResult()));

            ModHooks.Instance.TakeHealthHook += OnTakeHealth;
            ModHooks.Instance.DashPressedHook += OnDashPressed;
            ModHooks.Instance.BeforePlayerDeadHook += OnPlayerDead;
            ModHooks.Instance.ApplicationQuitHook += OnApplicationQuit;
            ModHooks.Instance.HeroUpdateHook += OnHeroUpdate;
            ModHooks.Instance.LanguageGetHook += OnLanguageGet;

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
                HeroController.instance.superDash.ActiveStateName == "Dashing" ||
                HeroController.instance.superDash.ActiveStateName == "Cancelable",
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

        private bool UpdateBenchState()
        {
            return BooleanAnimationUpdate(
                HeroController.instance.playerData.atBench,
                "Benching",
                _chromaHelper.PlayFullWhite
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
        
        private bool UpdateDoubleJumpState()
        {
            return BooleanAnimationUpdate(
                HeroController.instance.cState.bouncing || HeroController.instance.cState.doubleJumping,
                "Double_Jumping",
                _chromaHelper.PlayWhiteBars
            );
        }

        private void OnHeroUpdate()
        {
            // Don't update things too frequently, we don't want to burn people's CPUs
            if (++_frameCount < 10) return;

            _frameCount = 0;

            // These "if → return" make sure we only trigger one animation per cycle.
            // Not that there is anything wrong with triggering several, but it allows to set a priority for which
            // animation should be triggered if its state is active
            if (UpdateDoubleJumpState()) return;
            if (UpdateCrystalDashFlyState()) return;
            if (UpdateCrystalDashLoadState()) return;
            if (UpdateSpellState()) return;
            if (UpdateNailChargeState()) return;
            if (UpdateBenchState()) return;

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

        private void OnPlayerDead()
        {
            _chromaHelper.PlayFullRed();
        }

        // Uninitialize Razer's SDK if the game gracefully exits
        private void OnApplicationQuit()
        {
            _chromaHelper.OnApplicationQuit();
        }


        private int OnTakeHealth(int damage)
        {
            _chromaHelper.PlayRedRing();
            return damage;
        }

        private bool OnDashPressed()
        {
            _chromaHelper.PlayWhiteBars();
            return false;
        }
    }
}