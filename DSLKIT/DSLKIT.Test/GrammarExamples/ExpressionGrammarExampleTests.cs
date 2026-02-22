using DSLKIT.GrammarExamples;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public class ExpressionGrammarExampleTests
    {
        [Theory]
        [InlineData("x=2+3*4;x+1", 15.0)]
        [InlineData("a=2;b=a**3;b-1", 7.0)]
        [InlineData("v=2**3**2;v", 512.0)]
        [InlineData("(2+3)*4", 20.0)]
        [InlineData("5/2", 2.5)]
        [InlineData("a=1.25;b=a+2;b", 3.25)]
        [InlineData("sin(0)", 0.0)]
        [InlineData("cos(0)", 1.0)]
        [InlineData("tg(0)", 0.0)]
        [InlineData("ctg(1)", 0.6420926159343306)]
        [InlineData("round(2.5)", 3.0)]
        [InlineData("round(-2.5)", -3.0)]
        [InlineData("trunc(2.9)", 2.0)]
        [InlineData("trunc(-2.9)", -2.0)]
        [InlineData("x=1;y=sin(x)**2+cos(x)**2;round(y)", 1.0)]
        [InlineData("x=0.75;y=tg(x)*ctg(x);round(y)", 1.0)]
        [InlineData("x=1;a=sin(x)**2+cos(x)**2;b=tg(x)*ctg(x);c=round(2.5)+trunc(3.9)-trunc(-3.9);a+b+c", 11.0)]
        [InlineData("p=2;q=3;r=(p+q)*(p**q)-round(2.5)+trunc(3.9);r", 40.0)]
        [InlineData("a=5;b=2;c=a/b;d=trunc(c)+round(c)+trunc(-c);d", 3.0)]
        [InlineData("round((sin(1)**2+cos(1)**2+tg(1)*ctg(1))*5/2)", 5.0)]
        public void Evaluate_ShouldReturnExpectedResult(string source, double expectedResult)
        {
            var actualResult = ExpressionGrammarExample.Evaluate(source);

            actualResult.Should().BeApproximately(expectedResult, 1e-9);
        }
    }
}
