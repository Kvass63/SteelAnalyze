namespace SteelAnalyze.Services
{
    // Перевод эмбеддинга в набор байтов для хранения в базе и обратно
    public static class EmbeddingCodec
    {
        public static byte[] ToBytes(float[] embedding)
        {
            var bytes = new byte[embedding.Length * sizeof(float)];
            Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static float[] FromBytes(byte[] bytes)
        {
            var embedding = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
            return embedding;
        }
    }
}
