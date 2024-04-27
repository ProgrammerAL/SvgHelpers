namespace ProgrammerAl.SvgMover.SvgModifyUtilities;

public static class SvgMoveUtility
{
    private const string XEqualsStartPattern = "x=\"";
    private const string YEqualsStartPattern = "y=\"";
    public static string MoveAllElements(string svgText, int xMove, int yMove)
    {
        svgText = MoveElement(XEqualsStartPattern, svgText, xMove);
        svgText = MoveElement(YEqualsStartPattern, svgText, yMove);

        return svgText;
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
}
