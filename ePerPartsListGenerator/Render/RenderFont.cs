using PdfSharp.Drawing;

namespace ePerPartsListGenerator.Render
{
    class RenderFont
    {
        internal XFont Font;

        public RenderFont(XFont xFont)
        {
            Font = xFont;
        }

        internal double Height(XGraphics gfx)
        {
            return gfx.MeasureString("Ay", Font).Height + 1;
        }
    }
}