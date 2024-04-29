using System.IO;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;

using Svg;

namespace ProgrammerAl.SvgMover.SvgModifyUtilities;

/// <summary>
/// Custom logic to move elements inside an SVG string
/// This has to use custom logic because the input string may be an invalid SVG string, for example just a partial image the user will copy/paste back into their SVG file
/// </summary>
public class SvgMoverUtil
{
    private record AttributeModification(string AttributeName, int ModifyAmount);

    private readonly ImmutableDictionary<string, ImmutableArray<AttributeModification>> _modifications;

    public SvgMoverUtil(string svgText, int xMove, int yMove, IModificationLogger logger)
    {
        OriginalSvgText = svgText;
        XMove = xMove;
        YMove = yMove;
        Logger = logger;

        _modifications = new Dictionary<string, ImmutableArray<AttributeModification>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "rect",
                [
                    new AttributeModification("x", XMove),
                    new AttributeModification("y", YMove)
                ]
            },
            {
                "circle",
                [
                    new AttributeModification("cx", XMove),
                    new AttributeModification("cy", YMove)
                ]
            },
        }.ToImmutableDictionary();
    }

    public string OriginalSvgText { get; }
    public int XMove { get; }
    public int YMove { get; }
    public IModificationLogger Logger { get; }

    public string ModifiedSvgText { get; private set; } = string.Empty;

    public string MoveAllElements()
    {
        ModifiedSvgText = OriginalSvgText;
        var elementStartIndex = 0;

        while ((elementStartIndex = ModifiedSvgText.IndexOf("<", elementStartIndex)) > -1)
        {
            var elementEndIndex = ModifiedSvgText.IndexOf(">", elementStartIndex);
            if (elementEndIndex == -1)
            {
                //Invalid string, just move on
                Logger.LogError($"Found an element that does not have a closing angle backet (>). Skipping it.");
                elementStartIndex++;
                continue;
            }
            else if (ModifiedSvgText.Length < elementStartIndex + 1)
            {
                //At the end of the string, no more elements to parse
                elementStartIndex++;
                continue;
            }
            else if (ModifiedSvgText[elementStartIndex + 1] == '/')
            {
                //A closing element, skip it
                elementStartIndex++;
                continue;
            }

            //Full length of the element, including the start and end tags
            var elementLength = elementEndIndex - elementStartIndex + 1;
            //var elementString = ModifiedSvgText.Substring(elementStartIndex, elementLength);

            var elementNameEndIndex = ModifiedSvgText.IndexOf(" ", elementStartIndex, StringComparison.OrdinalIgnoreCase);
            if (elementNameEndIndex == -1)
            {
                //It's possible the element doesn't have any attributes, so we need to find the end of the element name
                elementNameEndIndex = ModifiedSvgText.IndexOf(">", elementStartIndex, StringComparison.OrdinalIgnoreCase);
            }

            var nameLength = elementNameEndIndex - elementStartIndex - 1;
            var elementName = ModifiedSvgText.Substring(elementStartIndex + 1, nameLength);

            if (_modifications.TryGetValue(elementName, out var modifications))
            {
                MoveElementAttributes(elementName, elementNameEndIndex, modifications);
            }
            else
            {
                Logger.LogError($"Unhandled SVG XML element with name '{elementName}' at index '{elementStartIndex}'");
            }

            //Since we did some parsing, reset the index to right after the element name
            //  Whatever we change is after the name, and the next iteration will look for the start of the next element, so it wil skip the attributes we edited
            elementStartIndex = elementNameEndIndex + 1;
        }

        return ModifiedSvgText;
    }

    private void MoveElementAttributes(string elementName, int attributesStartIndex, ImmutableArray<AttributeModification> modifications)
    {
        var index = attributesStartIndex - 1;
        var parseState = AttributeParseState.LookingForAttributeStart;
        var attributeNameBuilder = new StringBuilder();
        var attributeValueBuilder = new StringBuilder();
        while (++index < ModifiedSvgText.Length)
        {
            var character = ModifiedSvgText[index];
            if (character == '>')
            {
                //End of the element, we're done here
                return;
            }
            else if (parseState == AttributeParseState.FoundInvalidAttributeLookingForNextSpace)
            {
                if (!char.IsWhiteSpace(character))
                {
                    continue;
                }
            }
            else if (parseState == AttributeParseState.LookingForAttributeStart)
            {
                if (char.IsWhiteSpace(character))
                {
                    continue;
                }
                else
                {
                    parseState = AttributeParseState.ParsingName;
                    attributeNameBuilder.Append(character);
                }
            }
            else if (parseState == AttributeParseState.ParsingName)
            {
                if (character == '=')
                {
                    var attributeName = attributeNameBuilder.ToString();
                    var attributeModifier = modifications.FirstOrDefault(x => string.Equals(x.AttributeName, attributeName, StringComparison.OrdinalIgnoreCase));
                    if (attributeModifier is null)
                    {
                        parseState = AttributeParseState.FoundInvalidAttributeLookingForNextSpace;
                        Logger.LogInfo($"Skipping attribute '{elementName}.{attributeName}' because there's no modification for it.");
                    }
                    else
                    {
                        parseState = AttributeParseState.ParsingValueOpenQuote;
                    }
                }
                else if (!char.IsLetter(character))
                {
                    //Invalid character, skip this attribute
                    parseState = AttributeParseState.FoundInvalidAttributeLookingForNextSpace;
                    Logger.LogError($"Found an invalid character '{character}' in the attribute name for an element type of '{elementName}' at string index '{index}'");

                    attributeNameBuilder.Clear();
                    attributeValueBuilder.Clear();
                }
                else
                {
                    attributeNameBuilder.Append(character);
                }
            }
            else if (parseState == AttributeParseState.ParsingValueOpenQuote)
            {
                if (character != '"')
                {
                    //Invalid character, skip this attribute
                    parseState = AttributeParseState.FoundInvalidAttributeLookingForNextSpace;
                    Logger.LogError($"Found an invalid character '{character}' for an element type of '{elementName}' at string index '{index}' when expecting to find an attribute opening double-quote");

                    attributeNameBuilder.Clear();
                    attributeValueBuilder.Clear();
                }
                else
                {
                    parseState = AttributeParseState.ParsingValue;
                }
            }
            else if (parseState == AttributeParseState.ParsingValue)
            {
                if (character == '"')
                {
                    var attributeName = attributeNameBuilder.ToString();
                    var attributeModifier = modifications.First(x => string.Equals(x.AttributeName, attributeName, StringComparison.OrdinalIgnoreCase));

                    var attributeString = attributeValueBuilder.ToString();
                    if (int.TryParse(attributeString, out var attributeValue))
                    {
                        var newValue = attributeValue + attributeModifier.ModifyAmount;
                        var newValueString = newValue.ToString();
                        var attributeStartIndex = index - attributeString.Length;

                        ModifiedSvgText = ModifiedSvgText
                            .Remove(attributeStartIndex, attributeString.Length)
                            .Insert(attributeStartIndex, newValueString);

                        //We just moved the string, adjust the index to the double-quote in the new string
                        index = attributeStartIndex + newValueString.Length;
                    }
                    else
                    {
                        Logger.LogError($"Could not parse int value from attribute '{elementName}.{attributeName}' with string value '{attributeString}'");
                    }

                    parseState = AttributeParseState.LookingForAttributeStart;
                    attributeNameBuilder.Clear();
                    attributeValueBuilder.Clear();
                }
                else
                {
                    attributeValueBuilder.Append(character);
                }
            }
        }
    }

    //private void MoveElement(ParsedElementInfo elementInfo)
    //{
    //    if (string.Equals("rect", elementInfo.ElementName, StringComparison.OrdinalIgnoreCase))
    //    {
    //        MoveRectangle(elementInfo);
    //    }
    //    else
    //    {
    //        Logger.Log($"Unhandled SVG XML element with name '{elementInfo.ElementName}' at index '{elementInfo.ElementStartIndex}'");
    //    }
    //}

    private enum AttributeParseState
    {
        LookingForAttributeStart,
        ParsingName,
        ParsingValueOpenQuote,
        ParsingValue,
        FoundInvalidAttributeLookingForNextSpace,
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
                Logger.LogError($"Unhandled SVG XML element with name {element.Name.LocalName}");
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
