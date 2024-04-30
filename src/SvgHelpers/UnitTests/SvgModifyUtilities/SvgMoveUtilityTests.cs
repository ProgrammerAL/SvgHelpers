using System;

using ProgrammerAl.SvgHelpers;
using ProgrammerAl.SvgHelpers.LoggerUtils;
using ProgrammerAl.SvgHelpers.SvgModifyUtilities;

using Shouldly;

using Xunit;

namespace UnitTests.SvgModifyUtilities;

public class SvgMoveUtilityTests
{
    [Theory]
    [InlineData("<rect x=\"10\"></rect>", "<rect x=\"15\"></rect>", 5)]
    [InlineData("<rect X=\"10\"></rect>", "<rect X=\"15\"></rect>", 5)]
    [InlineData("<rect x=\"10\"></rect>", "<rect x=\"5\"></rect>", -5)]
    [InlineData("<rect x=\"10\"></rect>", "<rect x=\"0\"></rect>", -10)]
    [InlineData("<rect x=\"10\"></rect>", "<rect x=\"10\"></rect>", 0)]
    [InlineData("<rect x=\"10000\"></rect>", "<rect x=\"19000\"></rect>", 9000)]
    [InlineData("<rect x=\"10\"></rect>", "<rect x=\"-90\"></rect>", -100)]
    [InlineData("<rect             x=\"10\"></rect>", "<rect             x=\"15\"></rect>", 5)]
    [InlineData("<rect x=\"10\"></rect><circle cx=\"10\"></circle>", "<rect x=\"15\"></rect><circle cx=\"15\"></circle>", 5)]
    public void WhenMovingSimpleElements_X_AssertValuesMoved(string original, string expected, int moveAmount)
    {
        var mover = new SvgHelpersUtil(original, xMove: moveAmount, yMove: 0, logger: new ModificationLogger());
        var modified = mover.MoveAllElements();
        modified.ShouldBe(expected);
    }

    [Theory]
    [InlineData("<rect y=\"10\"></rect>", "<rect y=\"15\"></rect>", 5)]
    [InlineData("<rect Y=\"10\"></rect>", "<rect Y=\"15\"></rect>", 5)]
    [InlineData("<rect y=\"10\"></rect>", "<rect y=\"5\"></rect>", -5)]
    [InlineData("<rect y=\"10\"></rect>", "<rect y=\"0\"></rect>", -10)]
    [InlineData("<rect y=\"10\"></rect>", "<rect y=\"10\"></rect>", 0)]
    [InlineData("<rect y=\"10000\"></rect>", "<rect y=\"19000\"></rect>", 9000)]
    [InlineData("<rect y=\"10\"></rect>", "<rect y=\"-90\"></rect>", -100)]
    [InlineData("<rect             y=\"10\"></rect>", "<rect             y=\"15\"></rect>", 5)]
    [InlineData("<rect y=\"10\"></rect><circle cy=\"10\"></circle>", "<rect y=\"15\"></rect><circle cy=\"15\"></circle>", 5)]
    public void WhenMovingSimpleElementsY_AssertValuesMoved(string original, string expected, int moveAmount)
    {
        var mover = new SvgHelpersUtil(original, xMove: 0, yMove: moveAmount, logger: new ModificationLogger());
        var modified = mover.MoveAllElements();
        modified.ShouldBe(expected);
    }

    [Theory]
    [InlineData("<path d=\"M 5 10 L 20 30 V 50 H 12345 V 12 H 65 L 30 30 L 40 50 L -20 -50 H 1000 V -20\"></path>", "<path d=\"M 10 20 L 25 40 V 60 H 12350 V 22 H 70 L 35 40 L 45 60 L -15 -40 H 1005 V -10\"></path>", 5, 10)]
    public void WhenMovingPath_AssertValuesMoved(string original, string expected, int xMove, int yMove)
    {
        var mover = new SvgHelpersUtil(original, xMove: xMove, yMove: yMove, logger: new ModificationLogger());
        var modified = mover.MoveAllElements();
        modified.ShouldBe(expected);
    }

    [Theory]
    [InlineData("<polygon points=\"0,100 55,25 -50,75 100,0\" />", "<polygon points=\"5,110 60,35 -45,85 105,10\" />", 5, 10)]
    [InlineData("<polyline points=\"0,100 55,25 -50,75 100,0\" />", "<polyline points=\"5,110 60,35 -45,85 105,10\" />", 5, 10)]
    public void WhenMovingPolyEement_AssertValuesMoved(string original, string expected, int xMove, int yMove)
    {
        var mover = new SvgHelpersUtil(original, xMove: xMove, yMove: yMove, logger: new ModificationLogger());
        var modified = mover.MoveAllElements();
        modified.ShouldBe(expected);
    }
}
