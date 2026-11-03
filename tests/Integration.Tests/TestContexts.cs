using DatabaseAccess;
using DatabaseAccess.WebBookmakerOddsRepository;
using DatabaseAccess.WebGameOddsRepository;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests;

/// <summary>The real repositories, wired to the test database like Program.cs wires them.</summary>
public static class TestContexts
{
    public static GameDbContext Web() =>
        new(new DbContextOptionsBuilder<GameDbContext>().UseNpgsql(TestDatabase.ConnectionString).Options);

    public static GameOddsRepository GameOdds(GameDbContext context) =>
        new(context, new WebBookmakerOddsRepository(context));
}
