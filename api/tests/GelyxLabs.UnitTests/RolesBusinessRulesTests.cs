using GelyxLabs.Application.Features.Roles;
using Xunit;

namespace GelyxLabs.UnitTests;

public sealed class RolesBusinessRulesTests
{
    [Theory]
    [InlineData("Supervisor", true)]
    [InlineData("Admin", false)]
    [InlineData("admin", false)]
    [InlineData("ab", false)]
    [InlineData("A role name that is definitely longer than fifty characters", false)]
    [InlineData("   ", false)]
    public void IsValidName_EnforcesRoleNamingRules(string? name, bool expected) =>
        Assert.Equal(expected, RolesBusinessRules.IsValidName(name));
}
