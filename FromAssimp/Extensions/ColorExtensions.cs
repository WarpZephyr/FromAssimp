using System.Drawing;
using Assimp;
using SoulsFormats;

namespace FromAssimp.Extensions
{
    internal static class ColorExtensions
    {
        /// <summary>
        /// Convert a <see cref="Color4D"/> to a <see cref="Color"/>.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>A color.</returns>
        public static Color ToColor(this Color4D color)
        {
            byte a = (byte)(color.A * 255);
            byte r = (byte)(color.R * 255);
            byte g = (byte)(color.G * 255);
            byte b = (byte)(color.B * 255);

            return Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// Convert a <see cref="Color4D"/> to a <see cref="FLVER.VertexColor"/>.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>A color.</returns>
        public static FLVER.VertexColor ToFlverVertexColor(this Color4D color)
        {
            return new FLVER.VertexColor(color.A, color.R, color.G, color.B);
        }
    }
}
