using System;
using NUnit.Framework;
using Shouldly;

namespace Unity.Core.Tests
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [Test]
        public void Left_InBounds_ReturnsSubstring()
        {
            "".Left(0).ShouldBe("");
            "abc".Left(2).ShouldBe("ab");
            "abc".Left(0).ShouldBe("");
        }

        [Test]
        public void Left_OutOfBounds_ClampsProperly()
        {
            "".Left(10).ShouldBe("");
            "abc".Left(10).ShouldBe("abc");
        }

        [Test]
        public void Left_BadInput_Throws()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            Should.Throw<Exception>(() => ((string)null).Left(1));
            Should.Throw<Exception>(() => "abc".Left(-1));
        }

        [Test]
        public void Mid_InBounds_ReturnsSubstring()
        {
            "".Mid(0, 0).ShouldBe("");
            "abc".Mid(0, 3).ShouldBe("abc");
            "abc".Mid(0).ShouldBe("abc");
            "abc".Mid(0, -2).ShouldBe("abc");
            "abc".Mid(1, 1).ShouldBe("b");
            "abc".Mid(3, 0).ShouldBe("");
            "abc".Mid(0, 0).ShouldBe("");
        }

        [Test]
        public void Mid_OutOfBounds_ClampsProperly()
        {
            "".Mid(10, 5).ShouldBe("");
            "abc".Mid(0, 10).ShouldBe("abc");
            "abc".Mid(1, 10).ShouldBe("bc");
            "abc".Mid(10, 5).ShouldBe("");
        }

        [Test]
        public void Mid_BadInput_Throws()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            Should.Throw<Exception>(() => ((string)null).Mid(1, 2));
            Should.Throw<Exception>(() => "abc".Mid(-1));
        }

        [Test]
        public void Right_InBounds_ReturnsSubstring()
        {
            "".Right(0).ShouldBe("");
            "abc".Right(2).ShouldBe("bc");
            "abc".Right(0).ShouldBe("");
        }

        [Test]
        public void Right_OutOfBounds_ClampsProperly()
        {
            "".Right(10).ShouldBe("");
            "abc".Right(10).ShouldBe("abc");
        }

        [Test]
        public void Right_BadInput_Throws()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            Should.Throw<Exception>(() => ((string)null).Right(1));
            Should.Throw<Exception>(() => "abc".Right(-1));
        }

        [Test]
        public void StringJoin_WithEmpty_ReturnsEmptyString()
        {
            var enumerable = new object[0];

            enumerable.StringJoin(", ").ShouldBe("");
            enumerable.StringJoin(';').ShouldBe("");
            enumerable.StringJoin(o => o, ", ").ShouldBe("");
            enumerable.StringJoin(o => o, ';').ShouldBe("");
        }

        [Test]
        public void StringJoin_WithSingle_ReturnsNoSeparators()
        {
            var enumerable = new[] { "abc" };

            enumerable.StringJoin(", ").ShouldBe("abc");
            enumerable.StringJoin(';').ShouldBe("abc");
            enumerable.StringJoin(o => o, ", ").ShouldBe("abc");
            enumerable.StringJoin(o => o, ';').ShouldBe("abc");
        }

        [Test]
        public void StringJoin_WithMultiple_ReturnsJoined()
        {
            var enumerable = new object[] { "abc", 0b111001, -14, 'z' };

            enumerable.StringJoin(" ==> ").ShouldBe("abc ==> 57 ==> -14 ==> z");
            enumerable.StringJoin('\n').ShouldBe("abc\n57\n-14\nz");
            enumerable.StringJoin(o => o, " <> ").ShouldBe("abc <> 57 <> -14 <> z");
            enumerable.StringJoin(o => o, ';').ShouldBe("abc;57;-14;z");
        }

        [Test]
        public void StringJoin_WithSelectorAndSimpleEnumerable_ReturnsSelectedJoined()
        {
            var enumerable = new[] { "hi", "there", "this", "", "is", "some", "stuff" };

            int Selector(string value) { return value.Length; }

            enumerable.StringJoin(Selector, ", ").ShouldBe("2, 5, 4, 0, 2, 4, 5");
            enumerable.StringJoin(Selector, ';').ShouldBe("2;5;4;0;2;4;5");
        }

        [Test]
        public void StringJoin_WithSelectorAndComplexEnumerable_ReturnsSelectedJoined()
        {
            var enumerable = new object[] { "abc", 123, null, ("hi", 1.23) };

            string Selector(object value) {  return value?.GetType().Name ?? "(null)"; }

            enumerable.StringJoin(Selector, " ** ").ShouldBe("String ** Int32 ** (null) ** ValueTuple`2");
            enumerable.StringJoin(Selector, '?').ShouldBe("String?Int32?(null)?ValueTuple`2");
        }
    }
}
