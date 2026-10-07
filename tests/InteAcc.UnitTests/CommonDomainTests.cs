using InteAcc.Common.Domain.Common;
using Xunit;

namespace InteAcc.UnitTests;

public class CommonDomainTests
{
    private class TestEntity : Entity<int>
    {
        public TestEntity(int id) : base(id) { }
    }

    [Fact]
    public void Result_Success_ShouldHaveIsSuccessTrue()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Result_Failure_ShouldHaveError()
    {
        var result = Result.Failure("Invalid operation");
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Invalid operation", result.Error);
    }

    [Fact]
    public void ResultT_Success_ShouldReturnValue()
    {
        var result = Result.Success(42);
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Entity_Equality_ShouldBeEqualWhenIdsMatch()
    {
        var entity1 = new TestEntity(10);
        var entity2 = new TestEntity(10);
        var entity3 = new TestEntity(20);

        Assert.Equal(entity1, entity2);
        Assert.NotEqual(entity1, entity3);
    }
}
