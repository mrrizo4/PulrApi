
using ImageMagick;
using System.IO;

namespace Core.Application.Helpers
{
    public static class ImageResizer
    {
        public static Stream Resize(Stream stream, int width, int height, Stream returnStream)
        {
            stream.Position = 0;
            using (MagickImage magick = new MagickImage(stream))
            {
                magick.Format = magick.Format;
                magick.Resize(width, height);
                magick.Write(returnStream);

                return returnStream;
            }
        }
    }
}
