using AdaptiveGraphics.Config;
using AdaptiveGraphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

namespace AdaptiveGraphics.Systems
{
    public class AdaptiveGraphicsSystem
    {
        private static ICoreClientAPI capi;
        protected readonly AdaptiveGraphicsHud fpsHud;
        protected readonly DummyRenderer dummyRenderer;
        protected ModConfig config;

        protected Queue<int> _fpsSamples;
        protected List<int> _fpsOutliers;
        protected Dictionary<string, (int settingMin, int settingMax)> _rangeTable;
        protected TimeSpan _settleForSeconds;
        protected Stopwatch _timeSinceLastUpdate;
        protected Stopwatch _timeSinceLastEvaluate;
        protected Stopwatch _timeSinceLastSettle;
        protected Stopwatch _timeSinceLastAdjustment;

        protected bool _init = false;
        protected bool _toRecoverSSAOQuality = false;
        protected bool _toRecoverShadowQuality = false;
        protected int _frameCount;
        protected int _lowerFpsThreshold;
        protected int _upperFpsThreshold;
        protected int _fpsSampleSize;
        protected int _fpsTrendSize;
        protected int _lastCurrFps;
        protected int _lastSamplesAveFps;

        public AdaptiveGraphicsSystem(ICoreClientAPI api, AdaptiveGraphicsHud gui)
        {
            capi = api;
            fpsHud = gui;
            config = AdaptiveGraphicsModSystem.Config;

            Initialize();
            dummyRenderer = new DummyRenderer
            {
                action = new Action<float>(Update)
            };

            capi.Event.RegisterRenderer(dummyRenderer, EnumRenderStage.Done, typeof(AdaptiveGraphicsModSystem).Name);
            capi.Event.BlockTexturesLoaded += VerifyMaxViewDistance;
            ForceSettle(config.SettleInitial);
        }

        internal void Initialize()
        {
            // Stats
            if (sessionWatch != null && sessionWatch.IsRunning) ShowDebugStats();
            ResetStats();
            // Stats

            _init = false;
            config = AdaptiveGraphicsModSystem.Config;

            _fpsSampleSize = (int)(config.FpsSampleDuration / config.SamplingInterval);
            _fpsTrendSize = (int)(config.FpsTrendDuration / config.SamplingInterval);

            (_lowerFpsThreshold, _upperFpsThreshold) = Utils.GetBounds(config.TargetFPS, config.ToleranceFPS);
            _fpsSamples ??= new Queue<int>(_fpsSampleSize);
            _fpsOutliers ??= new List<int>(_fpsTrendSize);
            if (dummyRenderer != null)
            {
                VerifyMaxViewDistance();
            }

            if (config.MinViewDistance > 512 || config.MaxViewDistance > 512)
            {
                Utils.LogWarning($"View Distance is set more than 512, {Lang.Get("vram-warning")}");
            }

            _timeSinceLastUpdate = Stopwatch.StartNew();
            _timeSinceLastEvaluate = Stopwatch.StartNew();
            _timeSinceLastSettle = Stopwatch.StartNew();
            _timeSinceLastAdjustment = Stopwatch.StartNew();

            // Safeguards
            // Ensures all fps sampled use new settings
            config.SettleAfterAdjust = Math.Max(config.SettleAfterAdjust, config.FpsSampleDuration);

            ShowHud();
            ForceSettle(config.SettleAfterAdjust);
            Utils.LogNotification("Initialized Adaptive Graphics System");
        }

        protected void Update(float deltaTime)
        {
            if (!_init) return;

            _frameCount++;
            if (_timeSinceLastUpdate.ElapsedMilliseconds >= config.SamplingInterval * 1000f)
            {
                long actualSamplingInterval = _timeSinceLastUpdate.ElapsedMilliseconds; // stat
                int currentFps = Utils.GetFps(_frameCount, actualSamplingInterval / 1000f);
                if (currentFps < 1) return;

                fpsHud.UpdateText(currentFps);
                _lastCurrFps = currentFps;
                _frameCount = 0;

                if (!config.Enabled)
                {
                    _timeSinceLastUpdate.Restart();
                    return;
                }

                if (IsPaused())
                {
                    ForceSettle(config.SettleAfterPause);
                }

                // Stats collection
                if (currentFps >= _lowerFpsThreshold && currentFps <= _upperFpsThreshold) timeNearTarget += actualSamplingInterval;
                else timeNearTarget += timeOutsideTarget += actualSamplingInterval;
                fpsHistory.Add(currentFps);
                // Stats collection

                // Settling period
                if (_timeSinceLastSettle.Elapsed < _settleForSeconds || _fpsSamples.Count < _fpsSampleSize)
                {
                    _fpsSamples.AddSample(currentFps, _fpsSampleSize);
                    _timeSinceLastUpdate.Restart();
                    return;
                }

                // If an outlier, check if trend is existing before adding to fps samples
                var (lowerOutlierThreshold, upperOutlierThreshold) = Utils.GetBounds((int)_fpsSamples.Average(), config.OutlierTolerance);
                if (currentFps < lowerOutlierThreshold || currentFps > upperOutlierThreshold)
                {
                    _fpsOutliers.Add(currentFps);
                    CheckOutliers();
                }
                else
                {
                    _fpsSamples.AddSample(currentFps, _fpsSampleSize);
                    _fpsOutliers.Clear();
                    EvaluateAverage();
                }
                _timeSinceLastUpdate.Restart();
            }
        }

        protected void CheckOutliers()
        {
            if (_fpsOutliers.Count < 2) return;

            if (!IsConfirmedTrend())
            {
                _fpsOutliers.Clear();
                return;
            }

            if (_fpsOutliers.Count >= _fpsTrendSize)
            {
                confirmedOutliersTrendCount++; //stat

                // Trend confirmed and sustained for FpsTrendDuration
                _fpsSamples.Clear();
                foreach (var outlier in _fpsOutliers)
                {
                    _fpsSamples.AddSample(outlier, _fpsSampleSize);
                }
                _fpsOutliers.Clear();
            }
        }

        protected bool IsConfirmedTrend()
        {
            var isAllLower = _fpsOutliers.All(fps => fps < _lastSamplesAveFps);
            var isAllHigher = _fpsOutliers.All(fps => fps > _lastSamplesAveFps);
            return isAllLower || isAllHigher;
        }

        protected void EvaluateAverage()
        {
            if (_timeSinceLastEvaluate.ElapsedMilliseconds < 1000L) return; // only evaluate once a second

            try
            {
                int aveFps = (int)_fpsSamples.Average();
                _lastSamplesAveFps = aveFps;

                bool toIncrease;
                if (aveFps < _lowerFpsThreshold)
                {
                    if (_lastCurrFps > _lowerFpsThreshold)
                    {
                        Utils.LogWarning($"Tried to decrease but current FPS {_lastCurrFps} is more than threshold {_lowerFpsThreshold}. Last Average: {_lastSamplesAveFps}");
                        return;
                    }
                    toIncrease = false;
                }
                else if (aveFps > _upperFpsThreshold)
                {
                    if (_lastCurrFps < _upperFpsThreshold)
                    {
                        Utils.LogWarning($"Tried to increase but current FPS {_lastCurrFps} is less than threshold {_upperFpsThreshold}. Last Average: {_lastSamplesAveFps}");
                        return;
                    }
                    toIncrease = true;
                }
                else
                {
                    // No adjustment needed
                    //Utils.Debug("No changes");
                    return;
                }

                // Current Priorities: decrease -> shadow ssao viewdist lastresort vv
                if (toIncrease)
                {
                    // Undo Last Resort
                    if (config.AllowLastResort)
                    {
                        if (CheckAdjustment("ssaoQuality", toIncrease, 1, true)) return;
                        if (CheckAdjustment("shadowMapQuality", toIncrease, 1, true)) return;
                    }

                    if (CheckAdjustment("viewDistance", toIncrease, config.ViewDistanceStep)) return;
                    if (config.AllowChangeShaderSettings)
                    {
                        if (config.AllowChangeShaderSSAOSettings)
                        {
                            if (CheckAdjustment("ssaoQuality", toIncrease, 1)) return;
                        }
                        if (config.AllowChangeShaderShadowSettings)
                        {
                            if (CheckAdjustment("shadowMapQuality", toIncrease, 1)) return;
                        }
                    }
                    // Add adtl increase graphic settings if overhead? LOD qual
                }
                else
                {
                    if (config.AllowChangeShaderSettings)
                    {
                        if (config.AllowChangeShaderShadowSettings)
                        {
                            if (CheckAdjustment("shadowMapQuality", toIncrease, 1)) return;
                        }
                        if (config.AllowChangeShaderSSAOSettings)
                        {
                            if (CheckAdjustment("ssaoQuality", toIncrease, 1)) return;
                        }
                    }
                    if (CheckAdjustment("viewDistance", toIncrease, config.ViewDistanceStep)) return;

                    // Last Resort
                    if (config.AllowLastResort)
                    {
                        if (CheckAdjustment("shadowMapQuality", toIncrease, 1, true)) return;
                        if (CheckAdjustment("ssaoQuality", toIncrease, 1, true)) return;
                    }
                }
                //Utils.Debug("Exhausted all options");
            }
            finally
            {
                _timeSinceLastEvaluate.Restart();
            }
        }

        protected bool CheckAdjustment(string setting, bool increase, int step, bool lastResort = false)
        {
            var currSetting = capi.Settings.Int.Get(setting);
            int newSetting = currSetting + ((increase ? 1 : -1) * step);

            if (newSetting < 0) return false;

            if (!increase && lastResort)
            {
                switch (setting)
                {
                    case "shadowMapQuality":
                        if (_toRecoverShadowQuality) return false;

                        _toRecoverShadowQuality = true;
                        break;
                    case "ssaoQuality":
                        if (_toRecoverSSAOQuality) return false;

                        _toRecoverSSAOQuality = true;
                        break;
                }
                Utils.LogDebug($"Adjusting {setting}, {(increase ? "increase" : "decrease")} from {currSetting} to {newSetting} as a last resort. Average FPS: {_lastSamplesAveFps}, Current FPS: {_lastCurrFps}. Adjustment last made: {(int)_timeSinceLastAdjustment.Elapsed.TotalSeconds}s ago");
                SetAdjustment(setting, newSetting, increase);
                return true;
            }
            else if (increase && lastResort)
            {
                switch (setting)
                {
                    case "shadowMapQuality":
                        if (!_toRecoverShadowQuality) return false;

                        _toRecoverShadowQuality = false;
                        break;
                    case "ssaoQuality":
                        if (!_toRecoverSSAOQuality) return false;

                        _toRecoverSSAOQuality = false;
                        break;
                }
                Utils.LogDebug($"Adjusting {setting}, {(increase ? "increase" : "decrease")} from {currSetting} to {newSetting} as a last resort. Average FPS: {_lastSamplesAveFps}, Current FPS: {_lastCurrFps}. Adjustment last made: {(int)_timeSinceLastAdjustment.Elapsed.TotalSeconds}s ago");
                SetAdjustment(setting, newSetting, increase);
                return true;
            }

            var (settingMin, settingMax) = _rangeTable[setting];
            // For view distance
            newSetting = increase ? (currSetting < settingMax && newSetting > settingMax ? Math.Min(newSetting, settingMax) : newSetting) : (currSetting > settingMin && newSetting < settingMin ? Math.Max(newSetting, settingMin) : newSetting);

            if (!increase && (newSetting >= settingMin) || (increase && newSetting <= settingMax))
            {
                Utils.LogDebug($"Adjusting {setting}, {(increase ? "increase" : "decrease")} from {currSetting} to {newSetting}. Average FPS: {_lastSamplesAveFps}, Current FPS: {_lastCurrFps}. Adjustment last made: {(int)_timeSinceLastAdjustment.Elapsed.TotalSeconds}s ago");
                SetAdjustment(setting, newSetting, increase);

                return true;
            }
            return false;
        }

        protected void SetAdjustment(string setting, int newSetting, bool increase)
        {
            // Statistics
            if (lastAdjustmentDirection != null && increase != lastAdjustmentDirection && _timeSinceLastAdjustment.Elapsed.TotalSeconds <= 10d) immediateReversalCount++;
            lastAdjustmentDirection = increase;
            adjustments.Add(new Adjustment((int)_timeSinceLastAdjustment.Elapsed.TotalSeconds, increase ? 1 : -1));
            // Statistics

            switch (setting)
            {
                case "viewDistance":
                case "ssaoQuality":
                    capi.Settings.Int.Set(setting, newSetting, true);
                    break;
                case "shadowMapQuality":
                    capi.Settings.Int.Set(setting, newSetting, true);
                    ScreenManager.Platform.RebuildFrameBuffers();
                    capi.Shader.ReloadShaders(); // capi.Settings.Int.Set does not reload shaders for shadows
                    break;
                default:
                    throw new ArgumentException($"AdaptiveGraphicsSystem::SetSetting Setting \"{setting}\" not recognized.");
            }
            ForceSettle(config.SettleAfterAdjust);
            _timeSinceLastAdjustment.Restart();
        }

        /// <summary>
        /// Pause from Evaluate()
        /// </summary>
        /// <param name="seconds"></param>
        /// <exception cref="ArgumentException"></exception>
        protected void ForceSettle(int seconds)
        {
            if (seconds > 0)
            {
                _settleForSeconds = TimeSpan.FromSeconds(seconds);
            }
            else if (seconds == 0) return;
            else
            {
                throw new ArgumentException($"AdaptiveGraphicsSystem::ForceSettle Invalid argument {seconds}, must be >= 0");
            }
            _fpsSamples.Clear();
            _timeSinceLastSettle.Restart();
        }

        /// <summary>
        /// Get the max ViewDistance from the server if on multiplayer
        /// </summary>
        protected void VerifyMaxViewDistance()
        {
            if (!capi.IsSinglePlayer)
            {
                config.MaxViewDistance = Math.Min((capi.World as ClientMain).WorldMap.MaxViewDistance, config.MaxViewDistance);
                Utils.LogNotification($"Set config.MaxViewDistance to {config.MaxViewDistance} from the server");

                var currViewDist = capi.Settings.Int.Get("viewDistance");
                if (currViewDist > config.MaxViewDistance)
                {
                    Utils.LogWarning($"Current ViewDistance ({currViewDist}) is over the maximum set by the server ({config.MaxViewDistance}) from the server... adjusting.");
                    capi.Settings.Int.Set("viewDistance", config.MaxViewDistance, false);
                }
            }

            var viewDist = capi.Settings.Int.Get("viewDistance");
            if (viewDist < config.MinViewDistance || viewDist > config.MaxViewDistance)
            {
                Utils.LogWarning($"Current ViewDistance ({viewDist}) is not within the bounds set in the config ({config.MinViewDistance}-{config.MaxViewDistance})... adjusting.");
                capi.Settings.Int.Set("viewDistance", config.MaxViewDistance, false);
            }
            UpdateRangeTable();
            _init = true;
        }

        protected void UpdateRangeTable()
        {
            if (_rangeTable == null)
            {
                _rangeTable = new Dictionary<string, (int Min, int Max)>()
                {
                    { "viewDistance",  (config.MinViewDistance, config.MaxViewDistance)},
                    { "ssaoQuality",  (config.BaseSSAOQuality, config.MaxSSAOQuality)},
                    { "shadowMapQuality",  (config.BaseShadowQuality, config.MaxShadowQuality)}
                };
            }
            else
            {
                _rangeTable["viewDistance"] = (config.MinViewDistance, config.MaxViewDistance);
                _rangeTable["ssaoQuality"] = (config.BaseSSAOQuality, config.MaxSSAOQuality);
                _rangeTable["shadowMapQuality"] = (config.BaseShadowQuality, config.MaxShadowQuality);
            }
        }

        public void ShowHud()
        {
            if (config.ShowFPS)
            {
                fpsHud.TryOpen();
                return;
            }
            fpsHud.TryClose();
        }

        public void Dispose()
        {
            ShowDebugStats();

            capi.Event.BlockTexturesLoaded -= VerifyMaxViewDistance;
            capi.Event.UnregisterRenderer(dummyRenderer, EnumRenderStage.Done);
            dummyRenderer.Dispose();
            capi = null;
        }

        /// <summary>
        /// Pause collection when is paused (singleplayer) or game is out of focus
        /// </summary>
        /// <returns></returns>
        public static bool IsPaused()
        {
            if (!ClientProgram.screenManager.GamePlatform.IsFocused) return true;
            if (capi.IsGamePaused) return true;
            return false;
        }

        // Statistics
        protected List<int> fpsHistory;
        protected Stopwatch sessionWatch;
        protected int confirmedOutliersTrendCount;
        protected int immediateReversalCount;
        protected bool? lastAdjustmentDirection;
        protected float timeOutsideTarget;
        protected float timeNearTarget;
        public record Adjustment(int Seconds, int Direction);
        protected List<Adjustment> adjustments;

        public void ResetStats()
        {
            fpsHistory = [];
            sessionWatch = Stopwatch.StartNew();
            confirmedOutliersTrendCount = 0;
            immediateReversalCount = 0;
            lastAdjustmentDirection = null;
            timeOutsideTarget = 0;
            timeNearTarget = 0;
            adjustments = [];
        }

        /// <summary>
        /// Log basic statistics
        /// </summary>
        public void ShowDebugStats()
        {
            if (fpsHistory.Count < 1 || adjustments.Count < 1) return;
            var meanFps = fpsHistory.Average();
            var medianFps = Utils.GetMedian(fpsHistory);
            Utils.LogDebug($"""

-------------------------------------
Session Duration: {sessionWatch.Elapsed}
Mean FPS: {meanFps}
Mean Target Delta: {meanFps - config.TargetFPS}
Median FPS: {medianFps}
Median Target Delta: {medianFps - config.TargetFPS}
Min FPS: {fpsHistory.Min()}
Max FPS: {fpsHistory.Max()}
Time Near Target: {(timeNearTarget / (timeNearTarget + timeOutsideTarget)) * 100}% - {timeNearTarget / 1000f}s
Time Outside Target: {(timeOutsideTarget / (timeNearTarget + timeOutsideTarget)) * 100}% - {timeOutsideTarget / 1000f}s
Adjustment Count: {adjustments.Count}
Average time between adjustments: {adjustments.Select(x => x.Seconds).Average()}
Increased Quality Count: {adjustments.Where(x => x.Direction > 0).Count()}
Decreased Quality Count: {adjustments.Where(x => x.Direction < 0).Count()}
Immediate Reversal Count: {immediateReversalCount}
-------------------------------------

""");
        }
    }
}
