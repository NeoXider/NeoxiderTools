using System;
using NUnit.Framework;
using Neo.HtmlToUitk.Json;

namespace Neo.HtmlToUitk.PureLogicTests
{
    public class JsonTests
    {
        [Test]
        public void ParsesNestedValuesAndKeepsMemberOrder()
        {
            JsonValue root = MiniJson.Parse("{\"b\": 1, \"a\": [true, null, \"x\\ny\", 2.5], \"c\": {\"z\": 0}}");
            Assert.That(root.Members[0].Key, Is.EqualTo("b"));
            Assert.That(root.Members[1].Key, Is.EqualTo("a"));
            Assert.That(root["a"][0].AsBool(), Is.True);
            Assert.That(root["a"][1].IsMissingOrNull, Is.True);
            Assert.That(root["a"][2].AsString(), Is.EqualTo("x\ny"));
            Assert.That(root["a"][3].AsNumber(), Is.EqualTo(2.5));
            Assert.That(root["c"]["z"].AsInt(), Is.EqualTo(0));
            Assert.That(root["missing"]["deeper"].IsMissingOrNull, Is.True);
        }

        [Test]
        public void RoundTripIsStable()
        {
            const string source = "{\"schema\":1,\"list\":[\"a\",\"b\"],\"obj\":{\"k\":\"v\\\"q\"},\"n\":-3.25,\"flag\":false}";
            JsonValue first = MiniJson.Parse(source);
            string written = MiniJson.Write(first, true);
            JsonValue second = MiniJson.Parse(written);
            Assert.That(MiniJson.Write(second, true), Is.EqualTo(written));
            Assert.That(MiniJson.Write(first, false), Is.EqualTo(source));
        }

        [Test]
        public void UnicodeEscapesAndBomAreHandled()
        {
            JsonValue root = MiniJson.Parse("\uFEFF{\"s\": \"\\u00e9\\u4e2d\"}");
            Assert.That(root["s"].AsString(), Is.EqualTo("é中"));
        }

        [TestCase("{\"a\": }")]
        [TestCase("{\"a\" 1}")]
        [TestCase("[1, 2")]
        [TestCase("{\"a\": 1} trailing")]
        [TestCase("\"unterminated")]
        public void MalformedInputThrowsWithPosition(string text)
        {
            JsonParseException exception = Capture(text);
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Line, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void ErrorLineNumbersCountNewlines()
        {
            JsonParseException exception = Capture("{\n  \"a\": 1,\n  \"b\": x\n}");
            Assert.That(exception.Line, Is.EqualTo(3));
        }

        private static JsonParseException Capture(string text)
        {
            try
            {
                MiniJson.Parse(text);
                return null;
            }
            catch (JsonParseException exception)
            {
                return exception;
            }
        }

        [Test]
        public void TryParseReportsErrorWithoutThrowing()
        {
            Assert.That(MiniJson.TryParse("{", out JsonValue value, out string error), Is.False);
            Assert.That(value, Is.Null);
            Assert.That(error, Does.Contain("line"));
        }

        [Test]
        public void IntegersAreWrittenWithoutFraction()
        {
            JsonValue obj = JsonValue.NewObject().Set("n", JsonValue.FromNumber(260)).Set("f", JsonValue.FromNumber(0.05));
            Assert.That(MiniJson.Write(obj, false), Is.EqualTo("{\"n\":260,\"f\":0.05}"));
        }

        [Test]
        public void SetReplacesExistingKeyInPlace()
        {
            JsonValue obj = JsonValue.NewObject().Set("a", JsonValue.FromNumber(1)).Set("b", JsonValue.FromNumber(2)).Set("a", JsonValue.FromNumber(3));
            Assert.That(obj.Count, Is.EqualTo(2));
            Assert.That(obj.Members[0].Key, Is.EqualTo("a"));
            Assert.That(obj["a"].AsInt(), Is.EqualTo(3));
        }
    }
}
