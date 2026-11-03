using System.Reflection;
using DatabaseAccess;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests;

/// <summary>
/// Reads one row from every DbSet of both contexts, so a table or column that exists in the
/// EF model but not in CreateTables.sql (renamed, dropped, misspelled) fails here instead of
/// at runtime. EF selects every mapped column, so even an empty table checks them all.
/// </summary>
[TestClass]
[TestCategory(TestDatabase.Category)]
public class SchemaTests
{
    [TestInitialize]
    public Task Setup() => TestDatabase.Reset();

    public static IEnumerable<object[]> GameDbContextSets() => DbSetNames(typeof(GameDbContext));
    public static IEnumerable<object[]> NhlDbContextSets() => DbSetNames(typeof(NhlDbContext));

    private static IEnumerable<object[]> DbSetNames(Type context) => context
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
        .Select(p => new object[] { p.Name });

    [TestMethod]
    [DynamicData(nameof(GameDbContextSets), DynamicDataSourceType.Method)]
    public async Task GameDbContext_DbSet_ShouldMatchTheSchema(string dbSet)
    {
        await using var context = TestContexts.Web();
        await ReadOne(context, dbSet);
    }

    [TestMethod]
    [DynamicData(nameof(NhlDbContextSets), DynamicDataSourceType.Method)]
    public async Task NhlDbContext_DbSet_ShouldMatchTheSchema(string dbSet)
    {
        await using var context = new NhlDbContext(TestDatabase.ConnectionString);
        await ReadOne(context, dbSet);
    }

    [TestMethod]
    public void BothContexts_ShouldHaveDbSets()
    {
        // Guards the two tests above against passing with nothing to check
        GameDbContextSets().Should().NotBeEmpty();
        NhlDbContextSets().Should().NotBeEmpty();
    }

    private static async Task ReadOne(DbContext context, string dbSet)
    {
        var query = (IQueryable<object>)context.GetType().GetProperty(dbSet)!.GetValue(context)!;
        var act = () => query.Take(1).ToListAsync();
        await act.Should().NotThrowAsync($"the {dbSet} model should match CreateTables.sql");
    }
}
