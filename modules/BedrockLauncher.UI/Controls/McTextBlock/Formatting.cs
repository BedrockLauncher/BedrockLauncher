using System.Text.RegularExpressions;
using System.Windows.Media;

namespace BedrockLauncher.UI.Controls.McTextBlock {
    public static class Formatting {
        public static readonly Regex MinecraftFormattings = new Regex("§([0-9a-frlomnk])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public static class Colors {
        public static readonly Color Black = Color.FromRgb(0, 0, 0);
        public static readonly Color DarkBlue = Color.FromRgb(0, 0, 170);
        public static readonly Color DarkGreen = Color.FromRgb(0, 170, 0);
        public static readonly Color DarkAqua = Color.FromRgb(0, 170, 170);
        public static readonly Color DarkRed = Color.FromRgb(170, 0, 0);
        public static readonly Color Purple = Color.FromRgb(170, 0, 170);
        public static readonly Color Gold = Color.FromRgb(255, 170, 0);
        public static readonly Color Gray = Color.FromRgb(170, 170, 170);
        public static readonly Color DarkGray = Color.FromRgb(85, 85, 85);
        public static readonly Color Blue = Color.FromRgb(85, 85, 255);
        public static readonly Color Green = Color.FromRgb(85, 255, 85);
        public static readonly Color Aqua = Color.FromRgb(85, 255, 255);
        public static readonly Color Red = Color.FromRgb(255, 85, 85);
        public static readonly Color LightPurple = Color.FromRgb(255, 85, 255);
        public static readonly Color Yellow = Color.FromRgb(255, 255, 85);
        public static readonly Color White = Color.FromRgb(255, 255, 255);

        public static Color? FromChar(char c) {
            switch (c) {
                case '0':
                    return Black;
                case '1':
                    return DarkBlue;
                case '2':
                    return DarkGreen;
                case '3':
                    return DarkAqua;
                case '4':
                    return DarkRed;
                case '5':
                    return Purple;
                case '6':
                    return Gold;
                case '7':
                    return Gray;
                case '8':
                    return DarkGray;
                case '9':
                    return Blue;
                case 'a':
                case 'A':
                    return Green;
                case 'b':
                case 'B':
                    return Aqua;
                case 'c':
                case 'C':
                    return Red;
                case 'd':
                case 'D':
                    return LightPurple;
                case 'e':
                case 'E':
                    return Yellow;
                case 'f':
                case 'F':
                    return White;
                default:
                    return null;
            }
        }
    }
}
