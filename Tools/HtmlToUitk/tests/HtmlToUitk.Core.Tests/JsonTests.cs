using System;
using System.Linq;
using System.Text;
using HtmlToUitk.Core.Json;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class JsonTests
    {
        private static string RoundTrip(string text) => JsonWriter.Write(JsonReader.Parse(text), JsonWriteOptions.Compact);

        [TestCase("{\"a\":1}", "{\"a\":1}")]
        [TestCase(" [ 1 , 2 ] ", "[1,2]")]
        [TestCase("\"x\"", "\"x\"")]
        [TestCase("true", "true")]
        [TestCase("false", "false")]
        [TestCase("null", "null")]
        [TestCase("-0", "-0")]
        [TestCase("1.50", "1.50")]
        [TestCase("1e3", "1e3")]
        [TestCase("-1.5E-3", "-1.5E-3")]
        [TestCase("0", "0")]
        [TestCase("123456789012", "123456789012")]
        [TestCase("\"\\u0041\"", "\"A\"")]
        [TestCase("\"a\\/b\"", "\"a/b\"")]
        [TestCase("\"\\n\\t\\\"\\\\\"", "\"\\n\\t\\\"\\\\\"")]
        [TestCase("\"\\b\\f\\r\"", "\"\\b\\f\\r\"")]
        [TestCase("{\"a\":{\"b\":[{\"c\":null}]}}", "{\"a\":{\"b\":[{\"c\":null}]}}")]
        [TestCase("{}", "{}")]
        [TestCase("[]", "[]")]
        [TestCase("[[],{}]", "[[],{}]")]
        [TestCase("{\"b\":1,\"a\":2}", "{\"b\":1,\"a\":2}")]
        [TestCase("\"\\ud83d\\ude00\"", "\"\ud83d\ude00\"")]
        [TestCase("\"\u00e9\u4e2d\"", "\"\u00e9\u4e2d\"")]
        [TestCase("\"\\u0001\"", "\"\\u0001\"")]
        [TestCase("{\"\":1}", "{\"\":1}")]
        public void RoundTripsValidDocuments(string input, string expected)
        {
            Assert.That(RoundTrip(input), Is.EqualTo(expected));
        }

        [Test]
        public void SkipsByteOrderMark()
        {
            Assert.That(RoundTrip("\uFEFF{\"a\":1}"), Is.EqualTo("{\"a\":1}"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("{")]
        [TestCase("}")]
        [TestCase("[1,")]
        [TestCase("[1,]")]
        [TestCase("{\"a\"}")]
        [TestCase("{\"a\":}")]
        [TestCase("{\"a\":1,}")]
        [TestCase("{\"a\":1 \"b\":2}")]
        [TestCase("[1 2]")]
        [TestCase("01")]
        [TestCase("00")]
        [TestCase("1.")]
        [TestCase(".5")]
        [TestCase("+1")]
        [TestCase("-")]
        [TestCase("-a")]
        [TestCase("1e")]
        [TestCase("1e+")]
        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("-Infinity")]
        [TestCase("'a'")]
        [TestCase("\"abc")]
        [TestCase("\"a\\x\"")]
        [TestCase("\"\\u12\"")]
        [TestCase("\"\\u12G4\"")]
        [TestCase("tru")]
        [TestCase("nulll")]
        [TestCase("truee")]
        [TestCase("{\"a\":1}}")]
        [TestCase("[1]x")]
        [TestCase("{1:2}")]
        [TestCase("{'a':1}")]
        [TestCase("\"line\nbreak\"")]
        [TestCase("\"tab\there\"")]
        [TestCase("1e999")]
        [TestCase("-1e999")]
        [TestCase("{\"a\":1,\"a\":2}")]
        [TestCase("// c\n1")]
        [TestCase("/* c */1")]
        [TestCase("[1,,2]")]
        [TestCase("{,}")]
        [TestCase("\"\\")]
        [TestCase("[\"a\":1]")]
        public void RejectsMalformedDocuments(string input)
        {
            Assert.Throws<JsonParseException>(() => JsonReader.Parse(input));
        }

        [Test]
        public void ReportsLineAndColumn()
        {
            var ex = Assert.Throws<JsonParseException>(() => JsonReader.Parse("{\n  \"a\": 1,\n  \"b\": x\n}"));
            Assert.That(ex.Line, Is.EqualTo(3));
            Assert.That(ex.Column, Is.EqualTo(8));
        }

        [Test]
        public void TryParseReportsInsteadOfThrowing()
        {
            JsonValue value;
            string error;
            Assert.That(JsonReader.TryParse("[1,", out value, out error), Is.False);
            Assert.That(value, Is.Null);
            Assert.That(error, Does.Contain("line 1"));
            Assert.That(JsonReader.TryParse("[1]", out value, out error), Is.True);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void DepthLimitIsExactlySixtyFour()
        {
            string ok = new string('[', 64) + new string(']', 64);
            string tooDeep = new string('[', 65) + new string(']', 65);
            Assert.DoesNotThrow(() => JsonReader.Parse(ok));
            Assert.Throws<JsonParseException>(() => JsonReader.Parse(tooDeep));
        }

        [Test]
        public void HugeNestingFailsWithoutStackOverflow()
        {
            string hostile = new string('[', 200000);
            Assert.Throws<JsonParseException>(() => JsonReader.Parse(hostile));
            string objects = string.Concat(Enumerable.Repeat("{\"a\":", 100000));
            Assert.Throws<JsonParseException>(() => JsonReader.Parse(objects));
        }

        [Test]
        public void MaxLengthIsEnforced()
        {
            var options = new JsonReadOptions { MaxLength = 10 };
            Assert.Throws<JsonParseException>(() => JsonReader.Parse("[1,2,3,4,5,6,7,8]", options));
            Assert.DoesNotThrow(() => JsonReader.Parse("[1,2]", options));
        }

        [Test]
        public void CustomDepthLimit()
        {
            var options = new JsonReadOptions { MaxDepth = 2 };
            Assert.DoesNotThrow(() => JsonReader.Parse("[[1]]", options));
            Assert.Throws<JsonParseException>(() => JsonReader.Parse("[[[1]]]", options));
        }

        [Test]
        public void CommentsRequireTheOption()
        {
            var options = new JsonReadOptions { AllowComments = true };
            JsonValue value = JsonReader.Parse("{/* c */\"a\":1 // tail\n}", options);
            Assert.That(value.AsObject().GetInt("a"), Is.EqualTo(1));
            Assert.Throws<JsonParseException>(() => JsonReader.Parse("{\"a\":1 // x\n}"));
            Assert.Throws<JsonParseException>(() => JsonReader.Parse("/* open", options));
        }

        [Test]
        public void TrailingCommasRequireTheOption()
        {
            var options = new JsonReadOptions { AllowTrailingCommas = true };
            Assert.That(JsonWriter.Write(JsonReader.Parse("[1,2,]", options), JsonWriteOptions.Compact), Is.EqualTo("[1,2]"));
            Assert.That(JsonWriter.Write(JsonReader.Parse("{\"a\":1,}", options), JsonWriteOptions.Compact), Is.EqualTo("{\"a\":1}"));
            Assert.Throws<JsonParseException>(() => JsonReader.Parse("[1,2,]"));
        }

        [Test]
        public void DuplicateKeyPolicies()
        {
            string text = "{\"a\":1,\"a\":2}";
            Assert.That(JsonReader.Parse(text, new JsonReadOptions { DuplicateKeys = DuplicateKeyPolicy.LastWins }).AsObject().GetInt("a"), Is.EqualTo(2));
            Assert.That(JsonReader.Parse(text, new JsonReadOptions { DuplicateKeys = DuplicateKeyPolicy.FirstWins }).AsObject().GetInt("a"), Is.EqualTo(1));
            Assert.Throws<JsonParseException>(() => JsonReader.Parse(text));
        }

        [Test]
        public void ObjectsKeepInsertionOrder()
        {
            JsonObject obj = JsonReader.Parse("{\"z\":1,\"a\":2,\"m\":3}").AsObject();
            Assert.That(obj.Keys.ToArray(), Is.EqualTo(new[] { "z", "a", "m" }));
        }

        [Test]
        public void SortedWritingIsOrdinal()
        {
            JsonValue value = JsonReader.Parse("{\"b\":1,\"B\":2,\"a\":{\"z\":1,\"y\":2}}");
            string text = JsonWriter.Write(value, new JsonWriteOptions { Indent = 0, SortKeys = true });
            Assert.That(text, Is.EqualTo("{\"B\":2,\"a\":{\"y\":2,\"z\":1},\"b\":1}"));
        }

        [Test]
        public void PrettyOutputIsExact()
        {
            JsonValue value = JsonReader.Parse("{\"a\":[1,{\"b\":true}],\"c\":{},\"d\":[]}");
            string expected = "{\n  \"a\": [\n    1,\n    {\n      \"b\": true\n    }\n  ],\n  \"c\": {},\n  \"d\": []\n}\n";
            Assert.That(JsonWriter.Write(value, JsonWriteOptions.Pretty), Is.EqualTo(expected));
        }

        [Test]
        public void CanonicalWritingIsDeterministic()
        {
            string a = JsonWriter.Write(JsonReader.Parse("{\"x\":1,\"y\":[3,2,1]}"), JsonWriteOptions.Canonical);
            string b = JsonWriter.Write(JsonReader.Parse("{\"y\":[3,2,1],\"x\":1}"), JsonWriteOptions.Canonical);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Does.EndWith("}\n"));
            Assert.That(a, Does.Not.Contain("\r"));
        }

        [Test]
        public void CanonicalizeSortsNestedObjectsOnly()
        {
            JsonValue value = JsonObject.Canonicalize(JsonReader.Parse("{\"b\":[{\"y\":1,\"x\":2}],\"a\":0}"));
            Assert.That(value.ToString(), Is.EqualTo("{\"a\":0,\"b\":[{\"x\":2,\"y\":1}]}"));
        }

        [Test]
        public void EscapesControlCharactersAndLoneSurrogates()
        {
            string text = JsonWriter.Write(JsonValue.From("a\u0001b\ud800c"), JsonWriteOptions.Compact);
            Assert.That(text, Is.EqualTo("\"a\\u0001b\\ud800c\""));
        }

        [Test]
        public void WriterThrowsForNonFiniteNumbers()
        {
            Assert.Throws<InvalidOperationException>(() => JsonWriter.Write(JsonValue.From(double.NaN)));
            Assert.Throws<InvalidOperationException>(() => JsonWriter.Write(JsonValue.From(double.PositiveInfinity)));
        }

        [TestCase(1.5, "1.5")]
        [TestCase(2.0, "2")]
        [TestCase(-3.25, "-3.25")]
        [TestCase(0.1, "0.1")]
        [TestCase(1e20, "1E+20")]
        public void NumbersWithoutRawTextAreFormattedInvariantly(double value, string expected)
        {
            Assert.That(JsonWriter.Write(JsonValue.From(value), JsonWriteOptions.Compact), Is.EqualTo(expected));
        }

        [Test]
        public void TypedAccessors()
        {
            JsonObject obj = JsonReader.Parse("{\"s\":\"x\",\"b\":true,\"i\":7,\"d\":2.5,\"a\":[\"p\",\"q\",1],\"o\":{\"k\":null}}").AsObject();
            Assert.That(obj.GetString("s"), Is.EqualTo("x"));
            Assert.That(obj.GetString("missing", "fallback"), Is.EqualTo("fallback"));
            Assert.That(obj.GetBool("b"), Is.True);
            Assert.That(obj.GetInt("i"), Is.EqualTo(7));
            Assert.That(obj.GetInt("d", -1), Is.EqualTo(-1));
            Assert.That(obj.GetDouble("d"), Is.EqualTo(2.5));
            Assert.That(obj.GetStringList("a"), Is.EqualTo(new[] { "p", "q" }));
            Assert.That(obj.GetObject("o").Has("k"), Is.True);
            Assert.That(obj.GetObject("o")["k"].IsNull, Is.True);
            Assert.That(obj["nope"], Is.Null);
            Assert.That(obj.GetStringList("s"), Is.Null);
        }

        [Test]
        public void SetReplacesExistingMembersInPlace()
        {
            var obj = new JsonObject().Set("a", 1).Set("b", 2).Set("a", 3);
            Assert.That(obj.ToString(), Is.EqualTo("{\"a\":3,\"b\":2}"));
        }

        [Test]
        public void IntegerConversions()
        {
            Assert.That(JsonReader.Parse("5").AsInt(), Is.EqualTo(5));
            Assert.That(JsonReader.Parse("5.0").AsInt(), Is.EqualTo(5));
            Assert.That(JsonReader.Parse("5.5").AsInt(), Is.Null);
            Assert.That(JsonReader.Parse("3000000000").AsInt(), Is.Null);
            Assert.That(JsonReader.Parse("3000000000").AsLong(), Is.EqualTo(3000000000L));
            Assert.That(JsonReader.Parse("\"5\"").AsInt(), Is.Null);
        }

        [Test]
        public void LargeDocumentsRoundTrip()
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < 5000; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":").Append(i).Append(",\"name\":\"item").Append(i).Append("\"}");
            }
            sb.Append(']');
            string text = sb.ToString();
            Assert.That(RoundTrip(text), Is.EqualTo(text));
        }

        [Test]
        public void ParseThenWriteThenParseIsStable()
        {
            string source = "{\"a\":[1,2.50,\"\\u00e9\",null,true],\"b\":{\"c\":\"d\\n\"}}";
            string once = JsonWriter.Write(JsonReader.Parse(source), JsonWriteOptions.Canonical);
            string twice = JsonWriter.Write(JsonReader.Parse(once), JsonWriteOptions.Canonical);
            Assert.That(twice, Is.EqualTo(once));
        }
    }
}
