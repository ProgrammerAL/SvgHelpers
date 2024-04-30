using Pulumi;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ProgrammerAl.SvgHelpers.IaC.Utilities;

public record CompressedStringContent(
    string Content,
    byte[] BrotliContent,
    byte[] GZipContent);

public record CompressedStringContentBase64(
    string Content,
    string ContentBase64,
    string BrotliBase64,
    string GZipBase64);

public static class StringContentUtilities
{
    public static CompressedStringContent GenerateCompressedStringContent(JsonNode node)
    {
        var content = node.ToJsonString();
        return GenerateCompressedStringContent(content);
    }

    public static CompressedStringContent GenerateCompressedStringContent(string content)
    {
        var contentBytes = Encoding.UTF8.GetBytes(content);

        var brotliContent = OutputBrotliCompressed(contentBytes);
        var gzipContent = OutputGZipCompressed(contentBytes);

        return new CompressedStringContent(content, brotliContent, gzipContent);
    }

    public static CompressedStringContentBase64 GenerateCompressedStringContentBase64(JsonNode node)
    {
        var content = node.ToJsonString();
        return GenerateCompressedStringContentBase64(content);
    }

    public static CompressedStringContentBase64 GenerateCompressedStringContentBase64(string content)
    {
        var contentBytes = Encoding.UTF8.GetBytes(content);

        var contentBase64 = OutputStringBase64(contentBytes);
        var brotliBase64 = OutputBrotliCompressedBase64(contentBytes);
        var gzipBase64 = OutputGZipCompressedBase64(contentBytes);

        return new CompressedStringContentBase64(content, contentBase64, brotliBase64, gzipBase64);
    }

    public static string OutputStringBase64(byte[] contentBytes)
    {
        return Convert.ToBase64String(contentBytes);
    }

    public static byte[] OutputBrotliCompressed(byte[] contentBytes)
    {
        using (var outStream = new MemoryStream())
        {
            using (var compressor = new BrotliStream(outStream, CompressionLevel.Optimal))
            {
                compressor.Write(contentBytes, 0, contentBytes.Length);
                compressor.Flush();

                var outBuffer = outStream.ToArray();
                return outBuffer;
            }
        }
    }

    public static string OutputBrotliCompressedBase64(byte[] contentBytes)
    {
        using (var outStream = new MemoryStream())
        {
            using (var compressor = new BrotliStream(outStream, CompressionLevel.Optimal))
            {
                compressor.Write(contentBytes, 0, contentBytes.Length);
                compressor.Flush();

                var outBuffer = outStream.ToArray();

                return Convert.ToBase64String(outBuffer);
            }
        }
    }

    public static byte[] OutputGZipCompressed(byte[] contentBytes)
    {
        using (var outStream = new MemoryStream())
        {
            using (var compressor = new GZipStream(outStream, CompressionLevel.Optimal))
            {
                compressor.Write(contentBytes, 0, contentBytes.Length);
                compressor.Flush();

                var outBuffer = outStream.ToArray();
                return outBuffer;
            }
        }
    }

    public static string OutputGZipCompressedBase64(byte[] contentBytes)
    {
        using (var outStream = new MemoryStream())
        {
            using (var compressor = new GZipStream(outStream, CompressionLevel.Optimal))
            {
                compressor.Write(contentBytes, 0, contentBytes.Length);
                compressor.Flush();

                var outBuffer = outStream.ToArray();

                return Convert.ToBase64String(outBuffer);
            }
        }
    }
}
