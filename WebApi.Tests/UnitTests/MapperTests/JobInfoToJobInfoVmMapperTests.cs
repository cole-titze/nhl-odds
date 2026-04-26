using Entities.Models.Web;
using FluentAssertions;
using WebApi.Mappers;

namespace WebApi.Tests.BusinessLogic.UnitTests.MapperTests;

[TestClass]
public class JobInfoToJobInfoVmMapperTests
{
    [TestMethod]
    public void Map_ShouldMapAllProperties()
    {
        var jobInfo = new JobInfo
        {
            Id = "data-collection",
            Name = "data-collection",
            Status = "completed",
            StartedAt = new DateTime(2024, 1, 15, 3, 0, 0),
            FinishedAt = new DateTime(2024, 1, 15, 3, 30, 0),
            Error = null,
            Output = "Processing...\nDone.",
        };

        var result = JobInfoToJobInfoVmMapper.Map(jobInfo);

        result.Id.Should().Be("data-collection");
        result.Name.Should().Be("data-collection");
        result.Status.Should().Be("completed");
        result.StartedAt.Should().Be(new DateTime(2024, 1, 15, 3, 0, 0));
        result.FinishedAt.Should().Be(new DateTime(2024, 1, 15, 3, 30, 0));
        result.Error.Should().BeNull();
        result.Output.Should().Be("Processing...\nDone.");
    }

    [TestMethod]
    public void Map_WithFailedJob_ShouldMapError()
    {
        var jobInfo = new JobInfo
        {
            Id = "prediction",
            Name = "prediction",
            Status = "failed",
            StartedAt = new DateTime(2024, 1, 15, 6, 0, 0),
            FinishedAt = new DateTime(2024, 1, 15, 6, 1, 0),
            Error = "Process exited with code 1",
            Output = "Error output",
        };

        var result = JobInfoToJobInfoVmMapper.Map(jobInfo);

        result.Status.Should().Be("failed");
        result.Error.Should().Be("Process exited with code 1");
    }

    [TestMethod]
    public void Map_WithIdleJob_ShouldMapDefaults()
    {
        var jobInfo = new JobInfo
        {
            Id = "test-job",
            Name = "test-job",
        };

        var result = JobInfoToJobInfoVmMapper.Map(jobInfo);

        result.Status.Should().Be("idle");
        result.StartedAt.Should().BeNull();
        result.FinishedAt.Should().BeNull();
        result.Error.Should().BeNull();
        result.Output.Should().BeEmpty();
    }
}