using System;
using System.Globalization;
using Neo.Extensions;
class Program { 
    static void Main() { 
        var opts = new NumberFormatOptions(NumberNotation.IdleShort, 2, NumberRoundingMode.ToEven, false);
        foreach(double v in new double[]{ 999.9d, 999.99d, 999.995d, 999.999d, 1000d, 999999.99d, 999999.995d, 999e28}) {
            Console.WriteLine($"{v} => " + v.ToPrettyString(opts));
        }
    }
}
