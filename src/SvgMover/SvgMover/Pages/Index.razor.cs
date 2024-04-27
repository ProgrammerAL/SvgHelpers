using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

using Microsoft.AspNetCore.Components;

namespace ProgrammerAl.SvgMover.Pages;

public partial class Index : ComponentBase
{
    private string OriginalText { get; set; } = "";
    private string ModifiedText { get; set; } = "";

    private void MoveSvg()
    {
        ModifiedText = string.Empty;
        StateHasChanged();

        ModifiedText = "Some Test";
        StateHasChanged();
    }
}
