using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace AdaptiveGraphics.GUI
{
    public class AdaptiveGraphicsHud : HudElement
    {
        public double StrokeWidth = 2.0;
        public int FontSize = 16;
        public string FontName = "Lora";
        public string HexColor = "#FFFFFF";
        public string StrokeHexColor = "#000000";
        public EnumDialogArea DialogArea = EnumDialogArea.LeftBottom;
        public FontWeight FontWeight = FontWeight.Normal;
        public FontSlant FontSlant = FontSlant.Normal;
        public double FixedHeight => DialogArea.ToString() is "LeftBottom" or "RightBottom" ? 48.0 : 256.0;
        public double FixedWidth => DialogArea.ToString() is "LeftBottom" or "RightBottom" ? 130.0 : 400.0;

        public AdaptiveGraphicsHud(ICoreClientAPI capi) : base(capi)
        {
            ElementBounds elementBounds = ElementBounds.Fixed(DialogArea, 0, 0, FixedWidth, FixedHeight);

            SingleComposer = capi
                .Gui
                .CreateCompo("adaptivegraphics", elementBounds)
                .AddDynamicText(
                "",
                  CairoFont
                    .WhiteMediumText()
                    .WithFont(FontName)
                    .WithFontSize(FontSize)
                    .WithWeight(FontWeight)
                    .WithSlant(FontSlant)
                    .WithColor(ColorUtil.Hex2Doubles(HexColor))
                    .WithStroke(ColorUtil.Hex2Doubles(StrokeHexColor), StrokeWidth)
                    .WithOrientation(EnumTextOrientation.Justify),
                  elementBounds.ForkChild(),
                  "adaptivegraphics").Compose();
        }

        public void UpdateText(int aveFps)
        {
            SingleComposer.GetDynamicText("adaptivegraphics").SetNewText($"FPS: {aveFps}");
        }
    }
}
