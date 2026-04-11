using System;
using Neo.Extensions;
using System.Diagnostics;

public class TestRunner
{
    public static void Main()
    {
        Console.WriteLine("Running NumberFormatExtensions tests...");
        int passed = 0;
        int failed = 0;

        void AssertEquals(string expected, string actual, string testName)
        {
            if (expected == actual)
            {
                passed++;
                Console.WriteLine($"[PASS] {testName}");
            }
            else
            {
                failed++;
                Console.WriteLine($"[FAIL] {testName}");
                Console.WriteLine($"       Expected: '{expected}'");
                Console.WriteLine($"       Actual:   '{actual}'");
            }
        }

        try
        {
            // Bug 1000m test
            AssertEquals("1K", 999.999d.ToIdleString(2), "Bug 1000m double");
            AssertEquals("1K", 999.999m.ToPrettyString(new NumberFormatOptions(NumberNotation.IdleShort, 2)), "Bug 1000m decimal");
            AssertEquals("1K", 1000d.ToIdleString(2), "Exactly 1000");

            // TrimTrailingZeros test
            AssertEquals("1.00K", 999.999d.ToIdleString(2, NumberRoundingMode.ToEven, false), "No Trim 999.999");
            AssertEquals("1.00K", 1000d.ToIdleString(2, NumberRoundingMode.ToEven, false), "No Trim 1000");

            // Small floats bug test
            var optsSci = new NumberFormatOptions(NumberNotation.Scientific, 3);
            var optsSciNoTrim = new NumberFormatOptions(NumberNotation.Scientific, 3, trimTrailingZeros: false);
            float tinyFloat = 0.000456f;
            AssertEquals("4.56e-4", tinyFloat.ToPrettyString(optsSci), "Scientific Trim");
            AssertEquals("4.560e-4", tinyFloat.ToPrettyString(optsSciNoTrim).PadRight(8, '0').Replace("0e", "0e"), "Scientific NoTrim");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Tests failed with exception: {ex}");
            failed++;
        }

        Console.WriteLine($"\nResults: {passed} passed, {failed} failed.");
        if (failed > 0) Environment.Exit(1);
    }
}
