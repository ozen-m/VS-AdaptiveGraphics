using AdaptiveGraphics.Config;
using AdaptiveGraphics.GUI;
using AdaptiveGraphics.Systems;
using ConfigLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AdaptiveGraphics
{
    public class AdaptiveGraphicsModSystem : ModSystem
    {
        public static ModConfig Config { get; private set; }
        public static ILogger Logger { get; private set; }

        private AdaptiveGraphicsSystem adaptiveGraphicsSystem;
        private AdaptiveGraphicsHud adaptiveGraphicsHud;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            Logger = capi.Logger;
            try
            {
                Config = capi.LoadModConfig<ModConfig>(ModConfig.ConfigName) ?? new ModConfig();
                capi.StoreModConfig(Config, ModConfig.ConfigName);
            }
            catch (Exception e)
            {
                Utils.LogError($"Failed to load config: {e}");
                Config = new ModConfig();
            }

            if (capi.ModLoader.IsModEnabled("configlib"))
            {
                SubscribeToConfigChange(capi);
            }
            adaptiveGraphicsHud = new AdaptiveGraphicsHud(capi);
            adaptiveGraphicsSystem = new AdaptiveGraphicsSystem(capi, adaptiveGraphicsHud);
        }

        private void SubscribeToConfigChange(ICoreAPI api)
        {
            ConfigLibModSystem configLibModSystem = api.ModLoader.GetModSystem<ConfigLibModSystem>();
            configLibModSystem.SettingChanged += (domain, config, setting) =>
            {
                if (domain != "adaptivegraphics") return;

                setting.AssignSettingValue(Config);
                adaptiveGraphicsSystem?.Initialize();
            };
        }

        public override double ExecuteOrder()
        {
            return 1;
        }

        public override void Dispose()
        {
            adaptiveGraphicsSystem.Dispose();
            adaptiveGraphicsHud.Dispose();
            base.Dispose();
        }
    }
}
