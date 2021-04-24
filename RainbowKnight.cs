using System;
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

        private readonly Dictionary<string, bool> _cooldownState = new Dictionary<string, bool>();

        private bool _chargingSuperDash;

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

        private void BooleanAnimationUpdate(bool condition, string state, Action animation)
        {
            var stateActive = _cooldownState.ContainsKey(state) && _cooldownState[state];

            switch (condition)
            {
                case true when !stateActive:
                    _cooldownState[state] = true;
                    animation();
                    return;
                case false when stateActive:
                    _cooldownState[state] = false;
                    _chromaHelper.PlayBackground();
                    return;
            }
        }

        private void ConditionalCooledDownExecution(string target, Action callback, int cooldown = 1000)
        {
            if (_cooldownState.ContainsKey(target) && _cooldownState[target]) return;

            _cooldownState[target] = true;
            ExecutionPlan.Delay(cooldown, () => _cooldownState[target] = false);
            callback();
        }

        private string OnLanguageGet(string key, string sheet)
        {
            // This is a "hack" to detect when we're back to the main menu, so that we can reset all lighting
            if (key == "MAIN_OPTIONS")
                _chromaHelper.PlayBackground();

            return Language.Language.GetInternal(key, sheet);
        }

        private void UpdateCrystalDashState()
        {
            var cDashState = HeroController.instance.superDash.ActiveStateName;

            switch (cDashState)
            {
                // We are charging a C-dash
                case "Ground Charge":
                case "Ground Charged":
                case "Wall Charge":
                case "Wall Charged":
                    _chargingSuperDash = true;
                    ConditionalCooledDownExecution("CDash_charge", _chromaHelper.PlayFullPinkLoad, 2000);
                    return;
                // We are in a C-dash (inflight, not charging)
                case "Dashing":
                case "Cancelable":
                    _chargingSuperDash = false;
                    ConditionalCooledDownExecution("CDash_inflight", _chromaHelper.PlayFullPinkFlash, 800);
                    return;
                // C-dash was probably being charged but was canceled, so stop its animation
                case "Inactive" when _chargingSuperDash:
                    _chargingSuperDash = false;
                    _chromaHelper.PlayBackground();
                    return;
            }
        }

        private void UpdateSpellState()
        {
            BooleanAnimationUpdate(
                HeroController.instance.spellControl.ActiveStateName != "Inactive" &&
                HeroController.instance.spellControl.ActiveStateName != "Button Down",
                "Spell",
                _chromaHelper.PlayColouredRing
            );
        }

        private void UpdateBenchState()
        {
            BooleanAnimationUpdate(
                HeroController.instance.playerData.atBench,
                "Benching",
                _chromaHelper.PlayFullWhite
            );
        }

        private void UpdateNailChargeState()
        {
            BooleanAnimationUpdate(
                HeroController.instance.cState.nailCharging,
                "Nail_charging",
                _chromaHelper.PlayFullWhite
            );
        }

        private void OnHeroUpdate()
        {
            // Don't update things too frequently, we don't want to burn people's CPUs
            if (++_frameCount < 10) return;

            _frameCount = 0;
            
            UpdateCrystalDashState();
            UpdateSpellState();
            UpdateBenchState();
            UpdateNailChargeState();
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