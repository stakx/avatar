﻿using System;

namespace Stunts.UnitTests.CodeAnalysis.ST005.Diagnostic
{
    public partial class MyClass
    {
        public MyClass()
        {
            var stunt = Stunt.Of<BaseType>();

            Console.WriteLine(stunt);
        }
    }

    public sealed class BaseType { }
}
