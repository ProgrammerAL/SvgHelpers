using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

using BlazorMonaco.Editor;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using ProgrammerAl.SvgMover.SvgModifyUtilities;

namespace ProgrammerAl.SvgMover.Pages;

public partial class Index : ComponentBase
{
    private const string EditorOriginalTextModel = "editor-original-text-model";
    private const string EditorModifiedTextModel = "editor-modified-text-model";

    private const string DefaultSvgText =
"""
<svg xmlns="http://www.w3.org/2000/svg"
    xmlns:xlink="http://www.w3.org/1999/xlink">
</svg>
""";

    private StandaloneDiffEditor _diffEditor = null!;

    [Inject, NotNull]
    private IJSRuntime? JsRuntime { get; set; }

    private string XAmount { get; set; } = "0";
    private string YAmount { get; set; } = "0";

    private StandaloneDiffEditorConstructionOptions DiffEditorConstructionOptions(StandaloneDiffEditor editor)
    {
        return new StandaloneDiffEditorConstructionOptions
        {
            OriginalEditable = true,
        };
    }

    private async Task EditorOnDidInit()
    {
        var originalModel = await GetOrCreateTextModelAsync(EditorOriginalTextModel);
        var modifiedModel = await GetOrCreateTextModelAsync(EditorModifiedTextModel);

        await _diffEditor.SetModel(new DiffEditorModel
        {
            Original = originalModel,
            Modified = modifiedModel
        });
    }

    private async Task<TextModel> GetOrCreateTextModelAsync(string textModelName)
    {
        var textValue = await BlazorMonaco.Editor.Global.GetModel(JsRuntime, textModelName);
        if (textValue is null)
        {
            textValue = await BlazorMonaco.Editor.Global.CreateModel(JsRuntime, DefaultSvgText, language: "xml", textModelName);
        }

        return textValue;
    }

    private async Task MoveSvgAsync()
    {
        var originalModel = await GetOrCreateTextModelAsync(EditorOriginalTextModel);
        var modifiedModel = await GetOrCreateTextModelAsync(EditorModifiedTextModel);

        var originalText = await originalModel.GetValue(eol: EndOfLinePreference.TextDefined, preserveBOM: false);
        var modifiedText = MoveSvgElements(originalText);

        await modifiedModel.SetValue(modifiedText);

        await _diffEditor.SetModel(new DiffEditorModel
        {
            Original = originalModel,
            Modified = modifiedModel
        });

        StateHasChanged();
    }

    private string MoveSvgElements(string svgText)
    {
        if (!int.TryParse(XAmount, out int xAmount))
        {
            xAmount = 0;
        }

        if (!int.TryParse(YAmount, out int yAmount))
        {
            yAmount = 0;
        }

        var logger = new ModificationLogger();
        var mover = new SvgMoverUtil(svgText, xAmount, yAmount, logger);

        return mover.MoveAllElements();
    }
}
