# Adaptive Graphics
Automatically adjusts your graphics settings based on a desired target FPS! <br><br>
**WARNING**: The mod is currently in its testing phase. Unexpected results, and instability may occur. User feedback is highly appreciated! <br><br>
This mod aims to improve your Vintage Story experience by changing graphical settings to achieve a user-set target FPS, in the hopes of achieving a smoother gameplay based on changing circumstances. Included features are:
- Tracking of the game's Frames per Second to determine if certain settings should be decreased or increased
- Completely configurable according to your preferences and hardware
- Last resort option to bring down shader qualities one step further when the target FPS can't be achieved, at the cost of quality
- Minimal FPS counter on the bottom left of the screen

### Configuration
Recommended to be configured with [Config Lib](https://mods.vintagestory.at/configlib) by [Maltiez](https://mods.vintagestory.at/show/user/8F414EAEC3CB95A80138). <br>
Configurable by changing values in the provided config file `AdaptiveGraphics.json` inside `ModConfig` folder. <br><br>
- `TargetFPS` - The user desired FPS to achieve
- `ShowFPS` - Shows a simple FPS indicator on the bottom left
- `ToleranceFPS` - The lower/upper bounds of the target fps, dictates sensitivity to change
- `AllowLastResort` - If all options to lower settings have been exhausted, allow to lower each shader quality (SSAO, shadow) by one to try and achieve target fps
- `MinViewDistance`, `MaxViewDistance` - The distances in which the mod can automatically adjust to
- `ViewDistanceStep` - How near/far blocks are adjusted to the view distance when automatically adjusting
- `AllowChangeShaderSettings` - Allows to automatically change both shader settings (SSAO and Shadow). Typically causes stuttering as shaders are reloaded but less noticeable over time
- `AllowChangeShaderShadowSettings` - Allows to automatically change shadow quality settings. No effect if `AllowChangeShaderSettings` is disabled
- `BaseShadowQuality`, `MaxShadowQuality` - The base quality is what you set usually set as your default. The maximum tells the mod how high it can set shadows <br>0 - Off<br>1 - Low<br>2 - Medium<br>3 - High<br>4 - Very High
- `AllowChangeShaderSSAOSettings` - same as `AllowChangeShaderShadowSettings` but for SSAO quality
- `BaseSSAOQuality`, `MaxSSAOQuality` - same as `BaseShadowQuality`, `MaxShadowQuality` but for SSAO quality <br>0 - Off<br>1 - Medium<br>2 - High

The next set of configurations are advanced settings, they are generally not changed unless you understand the changes
- `FpsSampleDuration` - The duration (seconds) of the rolling window for average FPS, which will then compared against the Target FPS. Setting this lower cause more sudden changes, higher cause slower changes 
- `FpsTrendDuration` - The duration (seconds) to confirm a trend is sustained before an outlier FPS will be used in computing the average. Recommended is 3-5
- `OutlierTolerance` - FPS is considered an outlier outside these bounds. This is multiplied to the average of the FPS sampled within the `FpsSampleDuration`
- `SettleInitial` - Wait period (seconds) to let the FPS settle before making adjustments initially on game start
- `SettleAfterAdjust` - Wait period to let the FPS settle after making adjustments to the graphical settings
- `SettleAfterPause` - Wait period to let the FPS settle after pausing. Used for external settings such as an external frame limiter in use when game is out of focus
- `SamplingInterval` - How often (seconds) FPS calculated and collected

### Known Issues
- Flicker observed from the sky occurs when view distance is changed (limitation?)
- Stutters when changing settings that requires a shader reload (SSAO Quality, Shadow Quality). These lessen eventually as the stutters hit hardest when initially reloading the shaders for the set quality

### Future Plans
- Documentation
- Asymmetric tolerance (react quickly when fps drops, react conservatively when fps rises)
- Minimize oscillations if present
- Configuration presets
- Dynamic view distance steps
- Environment aware (Caves, Below ground, Day/Night)
- FPS Cap compatibility
- Testing on different averaging methods
- VRAM protection if the game provides the information

#### Feedbacks, suggestions, and support are all welcome and appreciated! You can reach me in the Vintage Story discord server.

### Credits
- [ResetXPDR](https://github.com/ResetXPDR) for his work on [MSFS AutoFPS](https://github.com/ResetXPDR/MSFS_AutoFPS), which this mod is heavily inspired from