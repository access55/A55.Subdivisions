using Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Subdivisions.Services;

namespace Subdivisions.Aws.Tests.Specs.Unit.Service;

public class CompressorTests : BaseTest
{
    [Test]
    public async Task ShouldCompressString()
    {
        var compressor = new GzipCompressor();
        var text = faker.Lorem.Paragraphs();
        var compressed = await compressor.Compress(text);
        compressed.Length.Should().BeLessThan(text.Length);
    }

    [Test]
    public async Task ShouldDecompressString()
    {
        var compressor = new GzipCompressor();
        var text = faker.Lorem.Paragraphs();
        var compressed = await compressor.Compress(text);
        var decompressed = await compressor.Decompress(compressed);
        decompressed.Should().Be(text);
    }
}
