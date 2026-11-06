using TurneraJardin.Api.Controllers;
using Xunit;

namespace TurneraJardin.Tests;

/// <summary>Cases U03–U06 of the QA report: slot generation, including ranges that end near midnight.</summary>
public class AppointmentSlotsTests
{
    [Fact]
    public void Morning_every_20_minutes_gives_12_slots()
    {
        var slots = AppointmentSlots.ForDay(new TimeOnly(8, 0), new TimeOnly(12, 0), 20)!;
        Assert.Equal(12, slots.Count);
        Assert.Equal(new TimeOnly(11, 40), slots[^1].Start);
        Assert.Equal(new TimeOnly(12, 0), slots[^1].End);
    }

    [Theory]
    [InlineData(23, 0, 23, 59, 60)]
    [InlineData(20, 0, 23, 59, 240)]
    public void Range_ending_near_midnight_does_not_loop(int sh, int sm, int eh, int em, int duration)
    {
        // Used to loop forever because TimeOnly.Add wraps 23:00 + 60 min to 00:00.
        Assert.Null(AppointmentSlots.ForDay(new TimeOnly(sh, sm), new TimeOnly(eh, em), duration));
    }

    [Fact]
    public void Last_slot_can_end_at_23_59()
    {
        var slots = AppointmentSlots.ForDay(new TimeOnly(22, 59), new TimeOnly(23, 59), 60)!;
        Assert.Single(slots);
        Assert.Equal(new TimeOnly(23, 59), slots[0].End);
    }

    [Fact]
    public void Duration_that_does_not_divide_the_range_drops_the_remainder()
    {
        var slots = AppointmentSlots.ForDay(new TimeOnly(8, 0), new TimeOnly(12, 0), 25)!;
        Assert.Equal(9, slots.Count);
        Assert.Equal(new TimeOnly(11, 45), slots[^1].End);
    }

    [Theory]
    [InlineData(12, 0, 8, 0, 20)]
    [InlineData(8, 0, 8, 0, 20)]
    [InlineData(8, 0, 8, 10, 20)]
    [InlineData(8, 0, 12, 0, 0)]
    public void Invalid_ranges_give_null(int sh, int sm, int eh, int em, int duration)
    {
        Assert.Null(AppointmentSlots.ForDay(new TimeOnly(sh, sm), new TimeOnly(eh, em), duration));
    }
}
