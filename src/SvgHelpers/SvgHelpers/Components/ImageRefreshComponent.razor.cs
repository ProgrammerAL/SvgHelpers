using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ProgrammerAl.SvgHelpers.Components;

public partial class ImageRefreshComponent : ComponentBase
{
    private static readonly TimeSpan RefreshLoopWaitTime = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ImageRefreshInterval = TimeSpan.FromSeconds(5);
    private const long MaxImageSize = 1024 * 1024 * 1024; //1 GB

    //private string? _lastImageUrl;

    //private string? EnteredImageUrl { get; set; }
    //private string? ImageUrl { get; set; }
    private DateTime LastImageRefreshTime { get; set; } = DateTime.MinValue;
    private IBrowserFile? ImageFile { get; set; } = null;

    private string? ImageDataUrl { get; set; }
    private MarkupString SvgHtml { get; set; }

    protected override void OnInitialized()
    {
        //Fire and forget
        _ = RefreshImageLoopAsync();

        base.OnInitialized();
    }

    private async Task SingleUploadAsync(InputFileChangeEventArgs e)
    {
        ImageFile = e.File;
        await RefreshImageAsync();
        //MemoryStream ms = new MemoryStream();
        //await e.File.OpenReadStream().CopyToAsync(ms);
        //var bytes = ms.ToArray();
        //do something with bytes
    }

    private async Task RefreshImageLoopAsync()
    {
        while (true)
        {
            await Task.Delay(RefreshLoopWaitTime);

            if (ImageFile != null
                && (DateTime.Now - LastImageRefreshTime) >= ImageRefreshInterval)
            {
                await RefreshImageAsync();
            }
        }
    }

    private async Task RefreshImageAsync()
    {
        if (ImageFile is null)
        {
            return;
        }

        try
        {
            var svgText = await new StreamReader(ImageFile.OpenReadStream(maxAllowedSize: MaxImageSize)).ReadToEndAsync();
            SvgHtml = new MarkupString(svgText);

            //using var ms = new MemoryStream();
            //await ImageFile.OpenReadStream(maxAllowedSize: MaxImageSize).CopyToAsync(ms);
            //var bytes = ms.ToArray();
            //var base64 = Convert.ToBase64String(bytes);
            //ImageDataUrl = string.Format("data:image/svg+xml;base64,{0}", base64);

            LastImageRefreshTime = DateTime.Now;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync(ex.ToString());
        }
    }
}
