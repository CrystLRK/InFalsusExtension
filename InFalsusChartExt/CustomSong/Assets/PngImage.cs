using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace InFalsusChartExt.CustomSong.Assets
{
    internal sealed class DecodedImage
    {
        internal int Width { get; }
        internal int Height { get; }
        internal byte[] RgbaBottomUp { get; }

        internal DecodedImage(
            int width,
            int height,
            byte[] rgbaBottomUp)
        {
            Width = width;
            Height = height;
            RgbaBottomUp = rgbaBottomUp;
        }
    }

    internal static class PngImage
    {
        private static readonly byte[] Signature =
        {
            137, 80, 78, 71, 13, 10, 26, 10,
        };

        internal static DecodedImage Decode(byte[] data)
        {
            if (data is null || data.Length < 33)
                throw new InvalidDataException("PNG is truncated");
            for (int i = 0; i < Signature.Length; i++)
                if (data[i] != Signature[i])
                    throw new InvalidDataException(
                        "PNG signature is invalid");

            int width = 0;
            int height = 0;
            byte bitDepth = 0;
            byte colorType = 0;
            byte interlace = 0;
            byte[] palette = null;
            byte[] transparency = null;
            using var compressed = new MemoryStream();

            int offset = 8;
            while (offset + 12 <= data.Length)
            {
                int length = ReadBigEndianInt32(data, offset);
                if (length < 0 ||
                    offset + 12L + length > data.Length)
                {
                    throw new InvalidDataException(
                        "PNG chunk length is invalid");
                }
                string type = System.Text.Encoding.ASCII.GetString(
                    data, offset + 4, 4);
                int dataStart = offset + 8;
                switch (type)
                {
                    case "IHDR":
                        if (length != 13)
                            throw new InvalidDataException(
                                "PNG IHDR length is invalid");
                        width = ReadBigEndianInt32(data, dataStart);
                        height = ReadBigEndianInt32(
                            data, dataStart + 4);
                        bitDepth = data[dataStart + 8];
                        colorType = data[dataStart + 9];
                        if (data[dataStart + 10] != 0 ||
                            data[dataStart + 11] != 0)
                        {
                            throw new InvalidDataException(
                                "PNG uses an unsupported codec");
                        }
                        interlace = data[dataStart + 12];
                        break;
                    case "PLTE":
                        palette = new byte[length];
                        Buffer.BlockCopy(
                            data, dataStart,
                            palette, 0, length);
                        break;
                    case "tRNS":
                        transparency = new byte[length];
                        Buffer.BlockCopy(
                            data, dataStart,
                            transparency, 0, length);
                        break;
                    case "IDAT":
                        compressed.Write(
                            data, dataStart, length);
                        break;
                    case "IEND":
                        offset = data.Length;
                        continue;
                }
                offset += 12 + length;
            }

            if (width <= 0 || height <= 0 ||
                width > 16384 || height > 16384)
            {
                throw new InvalidDataException(
                    "PNG dimensions are invalid");
            }
            if (bitDepth != 8 || interlace != 0)
            {
                throw new InvalidDataException(
                    "only non-interlaced 8-bit PNG is supported");
            }

            int channels = colorType switch
            {
                0 => 1,
                2 => 3,
                3 => 1,
                4 => 2,
                6 => 4,
                _ => throw new InvalidDataException(
                    "PNG color type is unsupported"),
            };
            int stride = checked(width * channels);
            int rawLength = checked((stride + 1) * height);
            byte[] filtered = Decompress(
                compressed.ToArray(), rawLength);
            byte[] pixels = Unfilter(
                filtered, width, height, channels);
            byte[] rgba = ToRgba(
                pixels, width, height,
                colorType, palette, transparency);
            FlipVertically(rgba, width, height);
            return new DecodedImage(width, height, rgba);
        }

        internal static Texture2D CreateTexture(byte[] pngBytes)
        {
            var engineTexture = new Texture2D(
                2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (ImageConversion.LoadImage(
                        engineTexture, pngBytes, false))
                {
                    engineTexture.Apply(false, false);
                    return engineTexture;
                }
            }
            catch
            {
            }
            UnityEngine.Object.Destroy(engineTexture);

            DecodedImage image = Decode(pngBytes);
            var texture = new Texture2D(
                image.Width,
                image.Height,
                TextureFormat.RGBA32,
                false);
            try
            {
                texture.LoadRawTextureData(image.RgbaBottomUp);
                texture.Apply(false, false);
                return texture;
            }
            catch
            {
                UnityEngine.Object.Destroy(texture);
                throw;
            }
        }

        private static byte[] Decompress(
            byte[] zlib,
            int expectedLength)
        {
            if (zlib.Length < 6 ||
                (zlib[0] & 0x0F) != 8)
            {
                throw new InvalidDataException(
                    "PNG zlib header is invalid");
            }
            using var input = new MemoryStream(
                zlib, 2, zlib.Length - 6, false);
            using var deflate = new DeflateStream(
                input, CompressionMode.Decompress);
            using var output = new MemoryStream(expectedLength);
            deflate.CopyTo(output);
            if (output.Length != expectedLength)
            {
                throw new InvalidDataException(
                    "PNG decompressed size is invalid");
            }
            return output.ToArray();
        }

        private static byte[] Unfilter(
            byte[] data,
            int width,
            int height,
            int channels)
        {
            int stride = checked(width * channels);
            var result = new byte[checked(stride * height)];
            int source = 0;
            for (int y = 0; y < height; y++)
            {
                byte filter = data[source++];
                int row = y * stride;
                int previous = row - stride;
                for (int x = 0; x < stride; x++)
                {
                    byte raw = data[source++];
                    byte left = x >= channels
                        ? result[row + x - channels]
                        : (byte)0;
                    byte up = y > 0
                        ? result[previous + x]
                        : (byte)0;
                    byte upLeft =
                        y > 0 && x >= channels
                            ? result[previous + x - channels]
                            : (byte)0;
                    result[row + x] = filter switch
                    {
                        0 => raw,
                        1 => (byte)(raw + left),
                        2 => (byte)(raw + up),
                        3 => (byte)(raw + ((left + up) >> 1)),
                        4 => (byte)(raw + Paeth(
                            left, up, upLeft)),
                        _ => throw new InvalidDataException(
                            "PNG filter type is invalid"),
                    };
                }
            }
            return result;
        }

        private static byte[] ToRgba(
            byte[] pixels,
            int width,
            int height,
            byte colorType,
            byte[] palette,
            byte[] transparency)
        {
            int pixelCount = checked(width * height);
            var rgba = new byte[checked(pixelCount * 4)];
            for (int i = 0; i < pixelCount; i++)
            {
                int destination = i * 4;
                switch (colorType)
                {
                    case 0:
                    {
                        byte gray = pixels[i];
                        rgba[destination] = gray;
                        rgba[destination + 1] = gray;
                        rgba[destination + 2] = gray;
                        rgba[destination + 3] = 255;
                        break;
                    }
                    case 2:
                    {
                        int source = i * 3;
                        rgba[destination] = pixels[source];
                        rgba[destination + 1] = pixels[source + 1];
                        rgba[destination + 2] = pixels[source + 2];
                        rgba[destination + 3] = 255;
                        break;
                    }
                    case 3:
                    {
                        if (palette is null)
                            throw new InvalidDataException(
                                "palette PNG has no PLTE");
                        int entry = pixels[i] * 3;
                        if (entry + 2 >= palette.Length)
                            throw new InvalidDataException(
                                "palette index is out of range");
                        rgba[destination] = palette[entry];
                        rgba[destination + 1] = palette[entry + 1];
                        rgba[destination + 2] = palette[entry + 2];
                        rgba[destination + 3] =
                            transparency is not null &&
                            pixels[i] < transparency.Length
                                ? transparency[pixels[i]]
                                : (byte)255;
                        break;
                    }
                    case 4:
                    {
                        int source = i * 2;
                        byte gray = pixels[source];
                        rgba[destination] = gray;
                        rgba[destination + 1] = gray;
                        rgba[destination + 2] = gray;
                        rgba[destination + 3] = pixels[source + 1];
                        break;
                    }
                    case 6:
                    {
                        int source = i * 4;
                        Buffer.BlockCopy(
                            pixels, source,
                            rgba, destination, 4);
                        break;
                    }
                }
            }
            return rgba;
        }

        private static void FlipVertically(
            byte[] rgba,
            int width,
            int height)
        {
            int stride = checked(width * 4);
            var row = new byte[stride];
            for (int y = 0; y < height / 2; y++)
            {
                int top = y * stride;
                int bottom = (height - 1 - y) * stride;
                Buffer.BlockCopy(rgba, top, row, 0, stride);
                Buffer.BlockCopy(
                    rgba, bottom, rgba, top, stride);
                Buffer.BlockCopy(
                    row, 0, rgba, bottom, stride);
            }
        }

        private static int Paeth(
            int left,
            int up,
            int upLeft)
        {
            int estimate = left + up - upLeft;
            int leftDistance = Math.Abs(estimate - left);
            int upDistance = Math.Abs(estimate - up);
            int upLeftDistance = Math.Abs(estimate - upLeft);
            if (leftDistance <= upDistance &&
                leftDistance <= upLeftDistance)
            {
                return left;
            }
            return upDistance <= upLeftDistance ? up : upLeft;
        }

        private static int ReadBigEndianInt32(
            byte[] bytes,
            int offset) =>
            (bytes[offset] << 24) |
            (bytes[offset + 1] << 16) |
            (bytes[offset + 2] << 8) |
            bytes[offset + 3];
    }
}
