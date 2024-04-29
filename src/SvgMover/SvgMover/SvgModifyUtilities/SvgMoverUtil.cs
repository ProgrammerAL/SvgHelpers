using System.IO;
using System.Xml;
using System.Xml.Linq;

using Microsoft.AspNetCore.Components.WebAssembly.Http;

using Svg;

namespace ProgrammerAl.SvgMover.SvgModifyUtilities;

/// <summary>
/// Custom logic to move elements inside an SVG string
/// This has to use custom logic because the input string may be an invalid SVG string, for example just a partial image the user will copy/paste back into their SVG file
/// </summary>
public class SvgMoverUtil(string SvgText, int XMove, int YMove, ISiteLogger Logger)
{
    private record ParsedElementInfo(
        string ElementName,
        int ElementStartIndex,
        int ElementEndIndex,
        ImmutableArray<ParsedElementInfo.ParsedAttributeInfo> Attributes,
        bool IsEndElement)
    {
        public record ParsedAttributeInfo(string Name, string Value, int ValueStartIndex, int ValueEndIndex)
        {
            public bool TryParseAttributeValue(out int outValue) => int.TryParse(Value, out outValue);
        }
    }

    private const string XEqualsStartPattern = "x=\"";
    private const string YEqualsStartPattern = "y=\"";

    public string MoveAllElements()
    {
        var elementStartIndex = -1;

        while ((elementStartIndex = SvgText.IndexOf("<")) > -1)
        {
            var elementEndIndex = SvgText.IndexOf(">", elementStartIndex);
            if (elementEndIndex == -1)
            {
                //Invalid string, just move on
                Logger.Log($"Found an element that does not have a closing angle backet (>). Skipping it.");
            }

            var elementLength = elementEndIndex - elementStartIndex;
            var elementString = SvgText.Substring(elementStartIndex, elementLength);
            var elementInfo = ParseElementInfo(elementString);
        }

        svgText = MoveElement(XEqualsStartPattern, svgText, xMove);
        svgText = MoveElement(YEqualsStartPattern, svgText, yMove);

        return svgText;
    }

    private ParsedElementInfo ParseElementInfo(string elementString)
    {
        return 1;
    }

    private static string MoveElement(string searchPattern, string svgText, int moveAmount)
    {
        int startIndex = 0;
        while ((startIndex = svgText.IndexOf(searchPattern, startIndex, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            var startNumberIndex = startIndex + searchPattern.Length;
            var preStartCharacter = svgText[startIndex - 1];
            if (char.IsWhiteSpace(preStartCharacter))
            {
                var endIndex = svgText.IndexOf("\"", startNumberIndex);
                if (endIndex > 0)
                {
                    var textLength = endIndex - startNumberIndex;
                    var numberText = svgText.Substring(startNumberIndex, textLength);
                    if (int.TryParse(numberText, out var number))
                    {
                        var newNumber = number + moveAmount;
                        svgText = svgText.Remove(startNumberIndex, textLength);
                        svgText = svgText.Insert(startNumberIndex, newNumber.ToString());
                    }
                }
            }

            //Move the start index 1 higher so we don't find the same element again on the next search
            startIndex++;
        }

        return svgText;
    }







    private void MoveSvgAllElements(IEnumerable<XElement> elements)
    {
        foreach (var element in elements)
        {
            if (element is null)
            {
                continue;
            }

            //Recursive so we get all elemets in the list
            if (element.HasElements)
            {
                MoveSvgAllElements(elements.Elements());
            }

            if (string.Equals(element.Name.LocalName, "rect", StringComparison.OrdinalIgnoreCase))
            {
                MoveRectangle(element);
            }
            //else if (element is SvgCircle circle)
            //{
            //    MoveCircle(circle);
            //}
            //else if (element is SvgPath path)
            //{
            //    MovePath(path);
            //}
            //else if (element is SvgEllipse ellipse)
            //{
            //    MoveEllipse(ellipse);
            //}
            else
            {
                Logger.Log($"Unhandled SVG XML element with name {element.Name.LocalName}");
            }
        }
    }

    private void MoveEllipse(SvgEllipse item)
    {
        var newX = item.CenterX.Value + XMove;
        item.CenterX = new SvgUnit(item.CenterX.Type, newX);

        var newY = item.CenterY.Value + YMove;
        item.CenterY = new SvgUnit(item.CenterY.Type, newY);
    }

    private void MovePath(SvgPath item)
    {
        foreach (var pathItem in item.PathData)
        {
            var newX = pathItem.End.X + XMove;
            var newY = pathItem.End.Y + YMove;
            pathItem.End = new System.Drawing.PointF(newX, newY);
        }
    }

    private void MoveCircle(SvgCircle item)
    {
        var newX = item.CenterX.Value + XMove;
        item.CenterX = new SvgUnit(item.CenterX.Type, newX);

        var newY = item.CenterY.Value + YMove;
        item.CenterY = new SvgUnit(item.CenterY.Type, newY);
    }

    private void MoveRectangle(XElement element)
    {
        var xElm = element.Attributes().FirstOrDefault(x => string.Equals(x.Name.LocalName, "x", StringComparison.OrdinalIgnoreCase));
        var yElm = element.Attributes().FirstOrDefault(x => string.Equals(x.Name.LocalName, "y", StringComparison.OrdinalIgnoreCase));

        UpdateElmValue(xElm, XMove);
        UpdateElmValue(yElm, YMove);
    }

    private void UpdateElmValue(XAttribute? attr, int modifyValue)
    {
        if (int.TryParse(attr?.Value, out var attrValue))
        {
            var newValue = attrValue + modifyValue;
            attr.SetValue(newValue.ToString());
        }
    }
}
