using System.Numerics;
using Neo.Extensions;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Neo.Tests.Edit
{
    public class SetTextTests
    {
        private GameObject _go;
        private SetText _setText;
        private TextMeshProUGUI _textMesh;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("SetTextTest");
            _textMesh = _go.AddComponent<TextMeshProUGUI>();
            _setText = _go.AddComponent<SetText>();
            _setText.text = _textMesh;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_go);
        }

        [Test]
        public void Set_StringValue_UpdatesText()
        {
            _setText.Set("TestValue");
            Assert.AreEqual("TestValue", _textMesh.text);
        }

        [Test]
        public void SetBigInteger_FormatsCorrectly()
        {
            _setText.NumberNotationStyle = NumberNotation.Grouped;
            _setText.Separator = ",";
            _setText.SetBigInteger(new BigInteger(1500000));
            
            Assert.AreEqual("1,500,000", _textMesh.text);
        }

        [Test]
        public void SetPercentage_UpdatesTextWithSuffix()
        {
            _setText.DecimalPlaces = 1;
            
            _setText.SetPercentage(50.5f, true);
            
            Assert.IsTrue(_textMesh.text.Contains("%"));
            Assert.IsTrue(_textMesh.text.Contains("50"));
        }

        [Test]
        public void SetCurrency_UpdatesTextWithPrefix()
        {
            _setText.DecimalPlaces = 2;

            _setText.SetCurrency(99.99f, "$");
            
            Assert.IsTrue(_textMesh.text.Contains("$"));
            Assert.IsTrue(_textMesh.text.Contains("99"));
        }

        [Test]
        public void Clear_SetsTextToEmpty()
        {
            _setText.Set("Initial");
            Assert.IsNotEmpty(_textMesh.text);
            
            _setText.Clear();
            Assert.IsEmpty(_textMesh.text);
        }
    }
}
