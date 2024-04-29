using System.IO;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;

using Microsoft.Extensions.Primitives;

using Svg;

namespace ProgrammerAl.SvgMover.SvgModifyUtilities;

/// <summary>
/// Custom logic to move elements inside an SVG string
/// This has to use custom logic because the input string may be an invalid SVG string, for example just a partial image the user will copy/paste back into their SVG file
/// </summary>
public class SvgMoverUtil
{
    private record AttributeModification(string AttributeName, int ModifyAmount);

    private readonly ImmutableDictionary<string, ImmutableArray<AttributeModification>> _simpleElementModifications;

    public SvgMoverUtil(string svgText, int xMove, int yMove, IModificationLogger logger)
    {
        OriginalSvgText = svgText;
        XMove = xMove;
        YMove = yMove;
        Logger = logger;

        _simpleElementModifications = new Dictionary<string, ImmutableArray<AttributeModification>>(StringComparer.OrdinalIgnoreCase)
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
            {
                "ellipse",
                [
                    new AttributeModification("cx", XMove),
                    new AttributeModification("cy", YMove)
                ]
            },
            {
                "text",
                [
                    new AttributeModification("x", XMove),
                    new AttributeModification("y", YMove)
                ]
            },
            {
                "tspan",
                [
                    new AttributeModification("x", XMove),
                    new AttributeModification("y", YMove)
                ]
            },
            {
                "line",
                [
                    new AttributeModification("x1", XMove),
                    new AttributeModification("x2", XMove),
                    new AttributeModification("y1", YMove),
                    new AttributeModification("y2", YMove),
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

            //TODO:
            //  Polygon and Polyline (pretty sure can be same method, both use points attribute)
            //  Path
            if (_simpleElementModifications.TryGetValue(elementName, out var simpleModifications))
            {
                MoveSimpleElementAttributes(elementName, elementNameEndIndex, simpleModifications);
            }
            else if (string.Equals("path", elementName, StringComparison.OrdinalIgnoreCase))
            {
                MovePathElementAttributes(elementNameEndIndex);
            }
            else if (string.Equals("polygon", elementName, StringComparison.OrdinalIgnoreCase)
                || string.Equals("polyline", elementName, StringComparison.OrdinalIgnoreCase))
            {
                MovePolyElementAttributes(elementNameEndIndex);
            }
            else
            {
                Logger.LogInfo($"Skipping SVG XML element with name '{elementName}' at index '{elementStartIndex}'");
            }

            //Since we did some parsing, reset the index to right after the element name
            //  Whatever we change is after the name, and the next iteration will look for the start of the next element, so it wil skip the attributes we edited
            elementStartIndex = elementNameEndIndex + 1;
        }

        return ModifiedSvgText;
    }

    private void MoveSimpleElementAttributes(string elementName, int attributesStartIndex, ImmutableArray<AttributeModification> modifications)
    {
        var index = attributesStartIndex - 1;
        var parseState = SimpleElementAttributeParseState.LookingForAttributeStart;
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
            else if (parseState == SimpleElementAttributeParseState.FoundInvalidAttributeLookingForNextSpace)
            {
                if (!char.IsWhiteSpace(character))
                {
                    continue;
                }
            }
            else if (parseState == SimpleElementAttributeParseState.LookingForAttributeStart)
            {
                if (char.IsWhiteSpace(character))
                {
                    continue;
                }
                else
                {
                    parseState = SimpleElementAttributeParseState.ParsingName;
                    attributeNameBuilder.Append(character);
                }
            }
            else if (parseState == SimpleElementAttributeParseState.ParsingName)
            {
                if (character == '=')
                {
                    var attributeName = attributeNameBuilder.ToString();
                    var attributeModifier = modifications.FirstOrDefault(x => string.Equals(x.AttributeName, attributeName, StringComparison.OrdinalIgnoreCase));
                    if (attributeModifier is null)
                    {
                        parseState = SimpleElementAttributeParseState.FoundInvalidAttributeLookingForNextSpace;
                        Logger.LogInfo($"Skipping attribute '{elementName}.{attributeName}' because there's no modification for it.");
                    }
                    else
                    {
                        parseState = SimpleElementAttributeParseState.ParsingValueOpenQuote;
                    }
                }
                else if (!char.IsLetter(character))
                {
                    //Invalid character, skip this attribute
                    parseState = SimpleElementAttributeParseState.FoundInvalidAttributeLookingForNextSpace;
                    Logger.LogError($"Found an invalid character '{character}' in the attribute name for an element type of '{elementName}' at string index '{index}'");

                    attributeNameBuilder.Clear();
                    attributeValueBuilder.Clear();
                }
                else
                {
                    attributeNameBuilder.Append(character);
                }
            }
            else if (parseState == SimpleElementAttributeParseState.ParsingValueOpenQuote)
            {
                if (character != '"')
                {
                    //Invalid character, skip this attribute
                    parseState = SimpleElementAttributeParseState.FoundInvalidAttributeLookingForNextSpace;
                    Logger.LogError($"Found an invalid character '{character}' for an element type of '{elementName}' at string index '{index}' when expecting to find an attribute opening double-quote");

                    attributeNameBuilder.Clear();
                    attributeValueBuilder.Clear();
                }
                else
                {
                    parseState = SimpleElementAttributeParseState.ParsingValue;
                }
            }
            else if (parseState == SimpleElementAttributeParseState.ParsingValue)
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

                    parseState = SimpleElementAttributeParseState.LookingForAttributeStart;
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

    private void MovePathElementAttributes(int attributesStartIndex)
    {
        var endElementIndex = ModifiedSvgText.IndexOf(">", attributesStartIndex);
        var dAttributeStartIndex = ModifiedSvgText.IndexOf("d=\"", attributesStartIndex, StringComparison.OrdinalIgnoreCase);

        if (dAttributeStartIndex == -1)
        {
            Logger.LogInfo($"Path element at index {attributesStartIndex} does not have a 'd' attribute. Skipping.");
            return;
        }
        else if (dAttributeStartIndex > endElementIndex)
        {
            //Found a 'd' attribute, but it's after the end of this element (for a different one), so don't modify anything here
            Logger.LogInfo($"Path element at index {attributesStartIndex} does not have a 'd' attribute. Skipping.");
            return;
        }

        var valueStartIndex = dAttributeStartIndex + 3; //Skip the 'd="' part
        var dEndIndex = ModifiedSvgText.IndexOf("\"", valueStartIndex);
        var dValue = ModifiedSvgText.Substring(valueStartIndex, dEndIndex - valueStartIndex);
        var newDValueBuilder = new StringBuilder();

        var dIndex = 0;
        var initialCharacter = dValue[dIndex];
        if (char.ToUpperInvariant(initialCharacter) != 'M')
        {
            Logger.LogInfo($"Path element at index {attributesStartIndex} does not start with 'M'. Skipping.");
            return;
        }
        if (dValue[dIndex + 1] != ' ')
        {
            Logger.LogInfo($"Path element at index {attributesStartIndex} does not have a space after the initial 'M'. Skipping.");
            return;
        }

        //d attribute has to start with 'M ' before numbers, so just do that check first
        dIndex++;
        newDValueBuilder.Append(initialCharacter);
        newDValueBuilder.Append(' ');

        bool IsStringAtEnd() => dIndex + 1 == dValue.Length;
        bool IsNumberComplete(char character) => char.IsWhiteSpace(character) || character == '"' || IsStringAtEnd();
        void AddSkippedCharacter(char character)
        {
            //Add the space that was skipped
            if (!IsStringAtEnd())
            {
                newDValueBuilder.Append(character);
            }
        }

        var parsedNumberBuilder = new StringBuilder();
        var parseState = PathElementAttributeParseState.ParsingMX;
        while (++dIndex < dValue.Length
            && parseState != PathElementAttributeParseState.Invalid)
        {
            var character = dValue[dIndex];
            //If we're looking at the final character in the string,
            //  add it now and pretend it was added in the previous loop
            //  This way the if-branch doesn't have to also check if it's the last character and add it for int processing
            if (IsStringAtEnd())
            {
                parsedNumberBuilder.Append(character);
            }

            if (parseState == PathElementAttributeParseState.ParsingMX)
            {
                if (IsNumberComplete(character))
                {
                    parseState = PathElementAttributeParseState.ParsingMY;

                    if (int.TryParse(parsedNumberBuilder.ToString(), out var number))
                    {
                        var newValue = number + XMove;
                        newDValueBuilder.Append(newValue);
                    }
                    else
                    {
                        Logger.LogError($"Could not parse int value from 'M X' attribute with string value '{parsedNumberBuilder}'");
                        newDValueBuilder.Append(parsedNumberBuilder.ToString());
                    }

                    AddSkippedCharacter(character);
                    parsedNumberBuilder.Clear();
                }
                else
                {
                    parsedNumberBuilder.Append(character);
                }
            }
            else if (parseState == PathElementAttributeParseState.ParsingMY)
            {
                if (IsNumberComplete(character))
                {
                    parseState = PathElementAttributeParseState.LookingForNextCommand;

                    if (int.TryParse(parsedNumberBuilder.ToString(), out var number))
                    {
                        var newValue = number + YMove;
                        newDValueBuilder.Append(newValue);
                    }
                    else
                    {
                        Logger.LogError($"Could not parse int value from 'M Y' attribute with string value '{parsedNumberBuilder}'");
                        newDValueBuilder.Append(parsedNumberBuilder.ToString());
                    }

                    AddSkippedCharacter(character);
                    parsedNumberBuilder.Clear();
                }
                else
                {
                    parsedNumberBuilder.Append(character);
                }
            }
            else if (parseState == PathElementAttributeParseState.LookingForNextCommand)
            {
                if (!char.IsWhiteSpace(character))
                {
                    var upperCharacter = char.ToUpperInvariant(character);
                    if (upperCharacter == 'L')
                    {
                        parseState = PathElementAttributeParseState.ParsingLX;
                        dIndex++;//Skip the next character, which is a space
                        newDValueBuilder.Append(character);
                        newDValueBuilder.Append(' ');
                    }
                    else if (upperCharacter == 'V')
                    {
                        parseState = PathElementAttributeParseState.ParsingV;
                        dIndex++;//Skip the next character, which is a space
                        newDValueBuilder.Append(character);
                        newDValueBuilder.Append(' ');
                    }
                    else if (upperCharacter == 'H')
                    {
                        parseState = PathElementAttributeParseState.ParsingH;
                        dIndex++;//Skip the next character, which is a space
                        newDValueBuilder.Append(character);
                        newDValueBuilder.Append(' ');
                    }
                    else
                    {
                        Logger.LogError($"Found an unexpected character '{character}' after a 'M' command in a path element at index {attributesStartIndex}");
                        parseState = PathElementAttributeParseState.Invalid;
                    }
                }
            }
            else if (parseState == PathElementAttributeParseState.ParsingLX)
            {
                if (IsNumberComplete(character))
                {
                    parseState = PathElementAttributeParseState.ParsingLY;

                    if (int.TryParse(parsedNumberBuilder.ToString(), out var number))
                    {
                        var newValue = number + XMove;
                        newDValueBuilder.Append(newValue);
                    }
                    else
                    {
                        Logger.LogError($"Could not parse int value from 'M L X' attribute with string value '{parsedNumberBuilder}'");
                        newDValueBuilder.Append(parsedNumberBuilder.ToString());
                    }

                    AddSkippedCharacter(character);
                    parsedNumberBuilder.Clear();
                }
                else
                {
                    parsedNumberBuilder.Append(character);
                }
            }
            else if (parseState == PathElementAttributeParseState.ParsingLY)
            {
                if (IsNumberComplete(character))
                {
                    parseState = PathElementAttributeParseState.LookingForNextCommand;

                    if (int.TryParse(parsedNumberBuilder.ToString(), out var number))
                    {
                        var newValue = number + YMove;
                        newDValueBuilder.Append(newValue);
                    }
                    else
                    {
                        Logger.LogError($"Could not parse int value from 'M L Y' attribute with string value '{parsedNumberBuilder}'");
                        newDValueBuilder.Append(parsedNumberBuilder.ToString());
                    }

                    AddSkippedCharacter(character);
                    parsedNumberBuilder.Clear();
                }
                else
                {
                    parsedNumberBuilder.Append(character);
                }
            }
            else if (parseState == PathElementAttributeParseState.ParsingV)
            {
                if (IsNumberComplete(character))
                {
                    parseState = PathElementAttributeParseState.LookingForNextCommand;

                    if (int.TryParse(parsedNumberBuilder.ToString(), out var number))
                    {
                        var newValue = number + YMove;
                        newDValueBuilder.Append(newValue);
                    }
                    else
                    {
                        Logger.LogError($"Could not parse int value from 'M V' attribute with string value '{parsedNumberBuilder}'");
                        newDValueBuilder.Append(parsedNumberBuilder.ToString());
                    }

                    AddSkippedCharacter(character);
                    parsedNumberBuilder.Clear();
                }
                else
                {
                    parsedNumberBuilder.Append(character);
                }
            }
            else if (parseState == PathElementAttributeParseState.ParsingH)
            {
                if (IsNumberComplete(character))
                {
                    parseState = PathElementAttributeParseState.LookingForNextCommand;

                    if (int.TryParse(parsedNumberBuilder.ToString(), out var number))
                    {
                        var newValue = number + XMove;
                        newDValueBuilder.Append(newValue);
                    }
                    else
                    {
                        Logger.LogError($"Could not parse int value from 'M H' attribute with string value '{parsedNumberBuilder}'");
                        newDValueBuilder.Append(parsedNumberBuilder.ToString());
                    }

                    AddSkippedCharacter(character);
                    parsedNumberBuilder.Clear();
                }
                else
                {
                    parsedNumberBuilder.Append(character);
                }
            }
        }

        if (parseState != PathElementAttributeParseState.Invalid)
        {
            var newDString = newDValueBuilder.ToString();
            ModifiedSvgText = ModifiedSvgText
                                .Remove(valueStartIndex, dEndIndex - valueStartIndex)
                                .Insert(valueStartIndex, newDString);
        }
    }

    private void MovePolyElementAttributes(int attributesStartIndex)
    {
        var endElementIndex = ModifiedSvgText.IndexOf(">", attributesStartIndex);
        var pointsAttributeStartIndex = ModifiedSvgText.IndexOf("points=\"", attributesStartIndex, StringComparison.OrdinalIgnoreCase);

        if (pointsAttributeStartIndex == -1)
        {
            Logger.LogInfo($"Path element at index {attributesStartIndex} does not have a 'points' attribute. Skipping.");
            return;
        }
        else if (pointsAttributeStartIndex > endElementIndex)
        {
            //Found a 'points' attribute in the string, but it's after the end of this element (for a different one), so don't modify anything here
            Logger.LogInfo($"Path element at index {attributesStartIndex} does not have a 'points' attribute. Skipping.");
            return;
        }

        var valueStartIndex = pointsAttributeStartIndex + 8; //Skip the 'points="' part
        var pointsEndIndex = ModifiedSvgText.IndexOf("\"", valueStartIndex);
        var pointsValue = ModifiedSvgText.Substring(valueStartIndex, pointsEndIndex - valueStartIndex);
        var newPointsValueBuilder = new StringBuilder();

        var index = -1;

        bool IsStringAtEnd() => index + 1 == pointsValue.Length;
        bool IsXNumberComplete(char character) => character == ',';
        bool IsYNumberComplete(char character) => char.IsWhiteSpace(character) || character == '"' || IsStringAtEnd();
        void AddSkippedCharacter(char character)
        {
            //Add the space that was skipped
            if (!IsStringAtEnd())
            {
                newPointsValueBuilder.Append(character);
            }
        }

        var parsedNumberBuilder = new StringBuilder();
        var parseState = PolyElementAttributeParseState.ParsingX;
        while (++index < pointsValue.Length)
        {
            var character = pointsValue[index];
            //If we're looking at the final character in the string,
            //  add it now and pretend it was added in the previous loop
            //  This way the if-branch doesn't have to also check if it's the last character and add it for int processing
            if (IsStringAtEnd())
            {
                parsedNumberBuilder.Append(character);
            }

            if (parseState == PolyElementAttributeParseState.ParsingX)
            {
                if (IsXNumberComplete(character))
                {
                    parseState = PolyElementAttributeParseState.ParsingY;

                    if (int.TryParse(parsedNumberBuilder.ToString(), out var number))
                    {
                        var newValue = number + XMove;
                        newPointsValueBuilder.Append(newValue);
                    }
                    else
                    {
                        Logger.LogError($"Could not parse int value from 'Points X' attribute with string value '{parsedNumberBuilder}'");
                        newPointsValueBuilder.Append(parsedNumberBuilder.ToString());
                    }

                    AddSkippedCharacter(character);
                    parsedNumberBuilder.Clear();
                }
                else
                {
                    parsedNumberBuilder.Append(character);
                }
            }
            else if (parseState == PolyElementAttributeParseState.ParsingY)
            {
                if (IsYNumberComplete(character))
                {
                    parseState = PolyElementAttributeParseState.LookingForNextPointStart;

                    if (int.TryParse(parsedNumberBuilder.ToString(), out var number))
                    {
                        var newValue = number + YMove;
                        newPointsValueBuilder.Append(newValue);
                    }
                    else
                    {
                        Logger.LogError($"Could not parse int value from 'Points Y' attribute with string value '{parsedNumberBuilder}'");
                        newPointsValueBuilder.Append(parsedNumberBuilder.ToString());
                    }

                    AddSkippedCharacter(character);
                    parsedNumberBuilder.Clear();
                }
                else
                {
                    parsedNumberBuilder.Append(character);
                }
            }
            else if (parseState == PolyElementAttributeParseState.LookingForNextPointStart)
            {
                if (!char.IsWhiteSpace(character))
                {
                    parseState = PolyElementAttributeParseState.ParsingX;
                    parsedNumberBuilder.Append(character);
                }
            }
        }

        var newPointsString = newPointsValueBuilder.ToString();
        ModifiedSvgText = ModifiedSvgText
                            .Remove(valueStartIndex, pointsEndIndex - valueStartIndex)
                            .Insert(valueStartIndex, newPointsString);
    }

    private enum SimpleElementAttributeParseState
    {
        LookingForAttributeStart,
        ParsingName,
        ParsingValueOpenQuote,
        ParsingValue,
        FoundInvalidAttributeLookingForNextSpace,
    }

    private enum PathElementAttributeParseState
    {
        ParsingMX,
        ParsingMY,
        ParsingLX,
        ParsingLY,
        ParsingH,
        ParsingV,
        LookingForNextCommand,
        Invalid
    }

    private enum PolyElementAttributeParseState
    {
        ParsingX,
        ParsingY,
        LookingForNextPointStart,
    }
}
