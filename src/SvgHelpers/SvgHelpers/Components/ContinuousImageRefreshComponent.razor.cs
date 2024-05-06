using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using ProgrammerAl.SvgHelpers.LoggerUtils;

namespace ProgrammerAl.SvgHelpers.Components;

public partial class ContinuousImageRefreshComponent : ComponentBase
{
    private static readonly TimeSpan RefreshLoopWaitTime = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ImageRefreshInterval = TimeSpan.FromSeconds(1);
    private DateTime LastImageRefreshTime { get; set; } = DateTime.MinValue;
    private MarkupString SvgHtml { get; set; }

    private bool HasImage => !string.IsNullOrWhiteSpace(SvgHtml.Value);

    [Inject, NotNull]
    private IJSRuntime? JSRuntime { get; set; }

    [Inject, NotNull]
    private ISiteLogger? Logger { get; set; }

    private IJSObjectReference? _fileHandle;

    protected override void OnInitialized()
    {
        //Fire and forget
        _ = RefreshImageLoopAsync();

        base.OnInitialized();
    }

    private async Task HandleOpenFileAsync()
    {
        try
        {
            _fileHandle = await JSRuntime.InvokeAsync<IJSObjectReference>("loadContinuousFileReference");
            await RefreshImageAsync();
        }
        catch (Exception ex)
        {
            Logger.Log(ex.ToString());
        }
    }

    private async Task RefreshImageLoopAsync()
    {
        while (true)
        {
            await Task.Delay(RefreshLoopWaitTime);

            if ((DateTime.Now - LastImageRefreshTime) >= ImageRefreshInterval)
            {
                await RefreshImageAsync();
            }
        }
    }

    private async Task RefreshImageAsync()
    {
        if (_fileHandle is null)
        {
            return;
        }

        try
        {
            var svgText = await JSRuntime.InvokeAsync<string>("loadFileStringContent", _fileHandle);
            SvgHtml = new MarkupString(svgText);

            LastImageRefreshTime = DateTime.Now;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.Log(ex.ToString());
        }
    }
}
