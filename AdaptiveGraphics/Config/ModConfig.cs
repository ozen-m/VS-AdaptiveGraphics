namespace AdaptiveGraphics.Config;

public class ModConfig
{
    public const string ConfigName = "AdaptiveGraphics.json";

    // --- General ---
    public bool Enabled { get; set; } = true;
    public bool ShowFPS { get; set; } = true;
    public int TargetFPS { get; set; } = 120;
    public int ToleranceFPS { get; set; } = 10;
    public bool AllowLastResort { get; set; } = true;
    public bool DebugLogs { get; set; } = true;

    // --- Graphics Config ---
    // View Distance 32-1536
    public int MinViewDistance { get; set; } = 256;
    public int MaxViewDistance { get; set; } = 1024;
    public int ViewDistanceStep { get; set; } = 96;
    // Shadows 0-4
    public bool AllowChangeShaderSettings { get; set; } = true; // seconds
    public bool AllowChangeShaderShadowSettings { get; set; } = true; // seconds
    public int BaseShadowQuality { get; set; } = 2;
    public int MaxShadowQuality { get; set; } = 3;
    // SSAO 0-2
    public bool AllowChangeShaderSSAOSettings { get; set; } = true; // seconds
    public int BaseSSAOQuality { get; set; } = 1;
    public int MaxSSAOQuality { get; set; } = 2;

    // --- Advanced ---
    public int FpsSampleDuration { get; set; } = 5; // seconds
    public int FpsTrendDuration { get; set; } = 3; // seconds
    public float OutlierTolerance { get; set; } = 0.1f; // percent
    public int SettleInitial { get; set; } = 30; // seconds
    public int SettleAfterAdjust { get; set; } = 7; // seconds
    public int SettleAfterPause { get; set; } = 0; // seconds
    public float SamplingInterval { get; set; } = 0.5f; // seconds
    public int Version { get; set; } = 0;
}