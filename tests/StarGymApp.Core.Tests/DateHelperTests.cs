using StarGymApp.Core.Helpers;
using Xunit;

namespace StarGymApp.Core.Tests;

public class DateHelperTests
{
    [Fact]
    public void IsExpired_ReturnsFalse_OnExpiryDate()
    {
        var joinDate = DateTime.Today.AddDays(-30);

        var result = DateHelper.IsExpired(joinDate, 30);

        Assert.False(result);
    }

    [Fact]
    public void IsExpired_ReturnsTrue_AfterExpiryDate()
    {
        var joinDate = DateTime.Today.AddDays(-31);

        var result = DateHelper.IsExpired(joinDate, 30);

        Assert.True(result);
    }
}
