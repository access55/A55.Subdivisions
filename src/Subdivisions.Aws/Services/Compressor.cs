using System.IO.Compression;
using System.Text;

namespace Subdivisions.Services;

interface ICompressor
{
    Task<string> Compress(string text);
    Task<string> Decompress(string text);
}

class GzipCompressor : ICompressor
{
    public async Task<string> Compress(string text)
    {
        var buffer = Encoding.UTF8.GetBytes(text);
        var memoryStream = new MemoryStream();
        await using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            await gZipStream.WriteAsync(buffer);
        memoryStream.Position = 0;

        var compressedData = new byte[memoryStream.Length];
        _ = await memoryStream.ReadAsync(compressedData);

        var gZipBuffer = new byte[compressedData.Length + 4];
        Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
        Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
        return Convert.ToBase64String(gZipBuffer);
    }

    public async Task<string> Decompress(string text)
    {
        var gZipBuffer = Convert.FromBase64String(text);
        using var memoryStream = new MemoryStream();
        int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
        await memoryStream.WriteAsync(gZipBuffer.AsMemory(4, gZipBuffer.Length - 4));

        var buffer = new byte[dataLength];
        memoryStream.Position = 0;
        await using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            _ = await gZipStream.ReadAsync(buffer);

        return Encoding.UTF8.GetString(buffer);
    }
}
