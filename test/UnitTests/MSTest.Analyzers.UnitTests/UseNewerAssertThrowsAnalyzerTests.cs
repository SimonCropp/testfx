﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = MSTest.Analyzers.Test.CSharpCodeFixVerifier<
    MSTest.Analyzers.UseNewerAssertThrowsAnalyzer,
    MSTest.Analyzers.UseNewerAssertThrowsFixer>;

namespace MSTest.Analyzers.Test;

[TestClass]
public sealed class UseNewerAssertThrowsAnalyzerTests
{
    [TestMethod]
    public async Task WhenAssertThrowsException_Diagnostic()
    {
        string code = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public class MyTestClass
            {
                [TestMethod]
                public void MyTestMethod()
                {
                    // action only overload
                    [|Assert.ThrowsException<Exception>(() => Console.WriteLine())|];
                    [|Assert.ThrowsException<Exception>(action: () => Console.WriteLine())|];
                    [|Assert.ThrowsExceptionAsync<Exception>(() => Task.CompletedTask)|];
                    [|Assert.ThrowsExceptionAsync<Exception>(action: () => Task.CompletedTask)|];

                    // action and message overload
                    [|Assert.ThrowsException<Exception>(() => Console.WriteLine(), "Message")|];
                    [|Assert.ThrowsException<Exception>(action: () => Console.WriteLine(), message: "Message")|];
                    [|Assert.ThrowsException<Exception>(message: "Message", action: () => Console.WriteLine())|];
                    [|Assert.ThrowsException<Exception>(action: () => Console.WriteLine(), "Message")|];
                    [|Assert.ThrowsException<Exception>(() => Console.WriteLine(), message: "Message")|];
                    [|Assert.ThrowsExceptionAsync<Exception>(() => Task.CompletedTask, "Message")|];
                    [|Assert.ThrowsExceptionAsync<Exception>(action: () => Task.CompletedTask, message: "Message")|];
                    [|Assert.ThrowsExceptionAsync<Exception>(message: "Message", action: () => Task.CompletedTask)|];
                    [|Assert.ThrowsExceptionAsync<Exception>(action: () => Task.CompletedTask, "Message")|];
                    [|Assert.ThrowsExceptionAsync<Exception>(() => Task.CompletedTask, message: "Message")|];

                    // action, message, and parameters overload
                    [|Assert.ThrowsException<Exception>(() => Console.WriteLine(), "Message", "A", "B", "C")|];
                    [|Assert.ThrowsException<Exception>(() => Console.WriteLine(), "Message", parameters: new object[] { "A", "B", "C" })|];
                    [|Assert.ThrowsExceptionAsync<Exception>(() => Task.CompletedTask, "Message", "A", "B", "C")|];
                    [|Assert.ThrowsExceptionAsync<Exception>(() => Task.CompletedTask, "Message", parameters: new object[] { "A", "B", "C" })|];
                }
            }
            """;

        string fixedCode = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public class MyTestClass
            {
                [TestMethod]
                public void MyTestMethod()
                {
                    // action only overload
                    Assert.ThrowsExactly<Exception>(() => Console.WriteLine());
                    Assert.ThrowsExactly<Exception>(action: () => Console.WriteLine());
                    Assert.ThrowsExactlyAsync<Exception>(() => Task.CompletedTask);
                    Assert.ThrowsExactlyAsync<Exception>(action: () => Task.CompletedTask);
            
                    // action and message overload
                    Assert.ThrowsExactly<Exception>(() => Console.WriteLine(), "Message");
                    Assert.ThrowsExactly<Exception>(action: () => Console.WriteLine(), message: "Message");
                    Assert.ThrowsExactly<Exception>(message: "Message", action: () => Console.WriteLine());
                    Assert.ThrowsExactly<Exception>(action: () => Console.WriteLine(), "Message");
                    Assert.ThrowsExactly<Exception>(() => Console.WriteLine(), message: "Message");
                    Assert.ThrowsExactlyAsync<Exception>(() => Task.CompletedTask, "Message");
                    Assert.ThrowsExactlyAsync<Exception>(action: () => Task.CompletedTask, message: "Message");
                    Assert.ThrowsExactlyAsync<Exception>(message: "Message", action: () => Task.CompletedTask);
                    Assert.ThrowsExactlyAsync<Exception>(action: () => Task.CompletedTask, "Message");
                    Assert.ThrowsExactlyAsync<Exception>(() => Task.CompletedTask, message: "Message");

                    // action, message, and parameters overload
                    Assert.ThrowsExactly<Exception>(() => Console.WriteLine(), "Message", "A", "B", "C");
                    Assert.ThrowsExactly<Exception>(() => Console.WriteLine(), "Message", messageArgs: new object[] { "A", "B", "C" });
                    Assert.ThrowsExactlyAsync<Exception>(() => Task.CompletedTask, "Message", "A", "B", "C");
                    Assert.ThrowsExactlyAsync<Exception>(() => Task.CompletedTask, "Message", messageArgs: new object[] { "A", "B", "C" });
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [TestMethod]
    public async Task WhenAssertThrowsExceptionFuncOverloadExpressionBody_Diagnostic()
    {
        string code = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public class MyTestClass
            {
                [TestMethod]
                public void MyTestMethod()
                {
                    [|Assert.ThrowsException<Exception>(() => 5)|];
                }
            }
            """;

        // NOTE: The discard is needed to avoid CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
        // This is because ThrowsException has a Func<object> overload that is being used in the original code.
        // But ThrowsExactly only has an Action overload.
        string fixedCode = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public class MyTestClass
            {
                [TestMethod]
                public void MyTestMethod()
                {
                    Assert.ThrowsExactly<Exception>(() => _ = 5);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [TestMethod]
    public async Task WhenAssertThrowsExceptionFuncOverloadComplexBody_Diagnostic()
    {
        string code = """
            using System;
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public class MyTestClass
            {
                [TestMethod]
                public void MyTestMethod()
                {
                    [|Assert.ThrowsException<Exception>(() =>
                    {
                        Console.WriteLine();
                        Func<object> x = () =>
                        {
                            int LocalFunction()
                            {
                                // This shouldn't be touched.
                                return 0;
                            }

                            // This shouldn't be touched.
                            return LocalFunction();
                        };

                        if (true)
                        {
                            return 1;
                        }
                        else if (true)
                            return 2;

                        return 3;
                    })|];
                }
            }
            """;

        string fixedCode = """
            using System;
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            
            [TestClass]
            public class MyTestClass
            {
                [TestMethod]
                public void MyTestMethod()
                {
                    Assert.ThrowsExactly<Exception>(() =>
                    {
                        Console.WriteLine();
                        Func<object> x = () =>
                        {
                            int LocalFunction()
                            {
                                // This shouldn't be touched.
                                return 0;
                            }
            
                            // This shouldn't be touched.
                            return LocalFunction();
                        };
            
                        if (true)
                        {
                            _ = 1;
                            return;
                        }
                        else if (true)
                        {
                            _ = 2;
                            return;
                        }

                        _ = 3;
                        return;
                    });
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [TestMethod]
    public async Task WhenAssertThrowsExceptionFuncOverloadVariable_Diagnostic()
    {
        string code = """
            using System;
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public class MyTestClass
            {
                [TestMethod]
                public void MyTestMethod()
                {
                    Func<object> action = () => _ = 5;
                    [|Assert.ThrowsException<Exception>(action)|];
                }
            }
            """;

        string fixedCode = """
            using System;
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            
            [TestClass]
            public class MyTestClass
            {
                [TestMethod]
                public void MyTestMethod()
                {
                    Func<object> action = () => _ = 5;
                    Assert.ThrowsExactly<Exception>(() => _ = action());
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [TestMethod]
    public async Task WhenAssertThrowsExceptionFuncOverloadBinaryExpression_Diagnostic()
    {
        string code = """
            using System;
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public class MyTestClass
            {
                [TestMethod]
                public void MyTestMethod()
                {
                    Func<object> action = () => _ = 5;
                    [|Assert.ThrowsException<Exception>(action + action)|];
                }
            }
            """;

        string fixedCode = """
            using System;
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            
            [TestClass]
            public class MyTestClass
            {
                [TestMethod]
                public void MyTestMethod()
                {
                    Func<object> action = () => _ = 5;
                    Assert.ThrowsExactly<Exception>(() => _ = (action + action)());
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }
}