using OpenCvSharp;

namespace SteelAnalyze.Services
{
    public static class ImageColorCheck
    {
        public const double MaxGrayscaleColorfulness = 4.0;

        private const int SampleSize = 64;

        public static bool IsColored(byte[] image)
        {
            var colorfulness = MeasureColorfulness(image);
            return colorfulness != null && colorfulness > MaxGrayscaleColorfulness;
        }

        public static double? MeasureColorfulness(byte[] image)
        {
            Mat decoded;
            try
            {
                decoded = Cv2.ImDecode(image, ImreadModes.Color);
            }
            catch (OpenCVException)
            {
                return null;
            }

            using (decoded)
            {
                if (decoded.Empty()) return null;

                using var sample = new Mat();
                Cv2.Resize(decoded, sample, new Size(SampleSize, SampleSize), interpolation: InterpolationFlags.Area);

                var channels = Cv2.Split(sample);
                try
                {
                    using var max = new Mat();
                    using var min = new Mat();
                    Cv2.Max(channels[0], channels[1], max);
                    Cv2.Max(max, channels[2], max);
                    Cv2.Min(channels[0], channels[1], min);
                    Cv2.Min(min, channels[2], min);

                    using var spread = new Mat();
                    Cv2.Subtract(max, min, spread);
                    return Cv2.Mean(spread).Val0;
                }
                finally
                {
                    foreach (var channel in channels) channel.Dispose();
                }
            }
        }
    }
}
