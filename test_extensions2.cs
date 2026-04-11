using System;
using System.Globalization;
using Neo.Extensions;
class Program { 
    static void Main() { 
        var opts = new NumberFormatOptions(NumberNotation.IdleShort, 2, NumberRoundingMode.ToEven, false);
        double[] tests = { 999.9, 999.99, 999.995, 999.999, 1000.0, 999999.0, 999999.995, 999999.999, 1000000.0, 999999999.999 };
        foreach(var v in tests) {
            Console.WriteLine(string.Format("{0} -> {1}", v, v.ToPrettyString(opts)));
        }
        decimal[] testd = { 999.9m, 999.99m, 999.995m, 999.999m, 1000.0m, 999999.0m, 999999.995m, 999999.999m, 1000000.0m, 999999999.999m };
        foreach(var v in testd) {
            Console.WriteLine(string.Format("{0} -> {1}", v, v.ToPrettyString(opts)));
        }
    }
}
