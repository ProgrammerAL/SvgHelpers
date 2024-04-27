using System;

using ProgrammerAl.SvgMover.SvgModifyUtilities;

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
    public void WhenMovingX_AssertValuesMoved(string original, string expected, int moveAmount)
    {
        var modified = SvgMoveUtility.MoveAllElements(original, xMove: moveAmount, yMove: 0);
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
    public void WhenMovingY_AssertValuesMoved(string original, string expected, int moveAmount)
    {
        var modified = SvgMoveUtility.MoveAllElements(original, xMove: 0, yMove: moveAmount);
        modified.ShouldBe(expected);
    }

    //[Theory]
    //[InlineData("<rect x=\"10\" y=\"10\"></rect>", "<rect y=\"15\"></rect>", 5)]
    //[InlineData("<rect Y=\"10\"></rect>", "<rect Y=\"15\"></rect>", 5)]
    //[InlineData("<rect y=\"10\"></rect>", "<rect y=\"5\"></rect>", -5)]
    //[InlineData("<rect y=\"10\"></rect>", "<rect y=\"0\"></rect>", -10)]
    //[InlineData("<rect y=\"10\"></rect>", "<rect y=\"10\"></rect>", 0)]
    //[InlineData("<rect             y=\"10\"></rect>", "<rect             y=\"15\"></rect>", 5)]
    //public void WhenMovingXandY_AssertValuesMoved(string original, string expected, int moveAmount)
    //{
    //    var modified = SvgMoveUtility.MoveAllElements(original, xMove: moveAmount, yMove: 0);
    //    modified.ShouldBe(expected);
    //}
}
