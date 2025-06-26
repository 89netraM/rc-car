using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();

app.MapGet(
    "/camera",
    async (HttpContext context, CancellationToken cancellationToken) =>
    {
        var process = Process.Start(
            new ProcessStartInfo("rpicam-vid", ["--output", "-", "--codec", "mjpeg", "--vflip", "--timeout", "0"])
            {
                RedirectStandardOutput = true,
            }
        );
        if (process is null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Could not start camera", cancellationToken);
            return;
        }
        context.Response.Headers.ContentType = "multipart/x-mixed-replace;boundary=--FRAME";
        while (ReadNextJPEGFrame(process.StandardOutput.BaseStream) is byte[] frame)
        {
            await context.Response.WriteAsync(
                $"""
                --FRAME
                Content-Type: image/jpeg
                Content-Length: {frame.Length}


                """,
                cancellationToken
            );
            await context.Response.Body.WriteAsync(frame, cancellationToken);
            await context.Response.WriteAsync("\r\n\r\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }
);

app.Run();

static byte[]? ReadNextJPEGFrame(Stream stream)
{
    using var ms = new MemoryStream();
    bool insideFrame = false;

    int prev = -1;
    while (true)
    {
        int b = stream.ReadByte();
        if (b is -1)
        {
            break;
        }

        if (!insideFrame)
        {
            if (prev is 0xFF && b is 0xD8)
            {
                insideFrame = true;
                ms.WriteByte((byte)prev);
                ms.WriteByte((byte)b);
            }
        }
        else
        {
            ms.WriteByte((byte)b);
            if (prev is 0xFF && b is 0xD9)
            {
                return ms.ToArray();
            }
        }

        prev = b;
    }

    return null;
}
