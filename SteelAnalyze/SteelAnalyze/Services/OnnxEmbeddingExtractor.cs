using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace SteelAnalyze.Services
{
    // Нейросеть (EfficientNet-B0 + надстройка), обученная в training/ и сохранённая в формате ONNX
    public class OnnxEmbeddingExtractor : IEmbeddingExtractor, IDisposable
    {
        private const int ImageSize = 224;
        private const string InputName = "image";
        private const string OutputName = "embedding";

        private readonly InferenceSession _session;

        public OnnxEmbeddingExtractor(string modelPath)
        {
            _session = new InferenceSession(modelPath);
            ModelVersion = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(modelPath)))[..16].ToLowerInvariant();
        }

        public bool IsAvailable => true;

        public string ModelVersion { get; }

        public float[] Extract(byte[] image)
        {
            var input = NamedOnnxValue.CreateFromTensor(InputName, ToTensor(image));
            using var results = _session.Run([input]);

            return results.First(r => r.Name == OutputName).AsEnumerable<float>().ToArray();
        }

        // Подготовка снимка точно как при обучении сети:
        // цветной снимок -> RGB -> 224x224 без сохранения пропорций (билинейно) -> значения 0..1, порядок осей канал-высота-ширина.
        // Используется та же библиотека OpenCV, что и при обучении: другие библиотеки сжимают снимок иначе, и эмбеддинги расходятся
        private static DenseTensor<float> ToTensor(byte[] image)
        {
            using var decoded = DecodeImage(image);
            using var rgb = new Mat();
            Cv2.CvtColor(decoded, rgb, ColorConversionCodes.BGR2RGB);
            using var resized = new Mat();
            Cv2.Resize(rgb, resized, new Size(ImageSize, ImageSize), interpolation: InterpolationFlags.Linear);

            var pixels = new byte[ImageSize * ImageSize * 3];
            Marshal.Copy(resized.Data, pixels, 0, pixels.Length);

            var tensor = new DenseTensor<float>([1, 3, ImageSize, ImageSize]);
            for (var y = 0; y < ImageSize; y++)
            for (var x = 0; x < ImageSize; x++)
            for (var c = 0; c < 3; c++)
                tensor[0, c, y, x] = pixels[(y * ImageSize + x) * 3 + c] / 255f;

            return tensor;
        }

        private static Mat DecodeImage(byte[] image)
        {
            Mat decoded;
            try
            {
                decoded = Cv2.ImDecode(image, ImreadModes.Color);
            }
            catch (OpenCVException)
            {
                throw new InvalidDataException("Файл не является изображением");
            }

            if (decoded.Empty())
            {
                decoded.Dispose();
                throw new InvalidDataException("Файл не является изображением");
            }
            return decoded;
        }

        public void Dispose() => _session.Dispose();
    }
}
