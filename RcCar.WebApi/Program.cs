using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RcCar.WebApi;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddCamera();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet(
    "/camera",
    async ([FromServices] CameraService cameraService, HttpContext context, CancellationToken cancellationToken) =>
    {
        await using var cameraReader = await cameraService.GetCameraReader(cancellationToken);
        if (cameraReader is null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Could not start camera", cancellationToken);
            return;
        }

        context.Response.Headers.ContentType = "multipart/x-mixed-replace;boundary=--FRAME";
        while (await cameraReader.ReadFrameAsync(cancellationToken) is var frame)
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
