using DatabaseAccess.WebTeamRepository.Mappers;
using Entities.DbModels;
using FluentAssertions;

namespace WebApi.Tests.BusinessLogic.UnitTests.MapperTests;

[TestClass]
public class DbSeasonTeamToTeamMapperTests
{
    [TestMethod]
    // The NHL's placeName for the New York teams repeats the common name
    [DataRow("New York Rangers", "Rangers", "NY Rangers", "New York")]
    [DataRow("New York Islanders", "Islanders", "NY Islanders", "New York")]
    [DataRow("Montréal Canadiens", "Canadiens", "Montréal", "Montréal")]
    [DataRow("St. Louis Blues", "Blues", "St. Louis", "St. Louis")]
    [DataRow("Utah Mammoth", "Mammoth", "Utah", "Utah")]
    // A full name that doesn't end with the common name keeps placeName
    [DataRow("Some Team", "Other", "Somewhere", "Somewhere")]
    [DataRow("", "Rangers", "NY Rangers", "NY Rangers")]
    public void Map_LocationName_IsThePlacePartOfTheFullName(string name, string common, string place, string expected)
    {
        var result = DbSeasonTeamToTeamMapper.Map(new DbSeasonTeam
        {
            TeamId = 3,
            Name = name,
            CommonName = common,
            PlaceName = place,
        });

        result.LocationName.Should().Be(expected);
        result.TeamName.Should().Be(common);
    }
}
