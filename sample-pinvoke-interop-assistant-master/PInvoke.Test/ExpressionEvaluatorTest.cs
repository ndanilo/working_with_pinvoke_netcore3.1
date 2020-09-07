// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using PInvoke.Parser;
using Xunit;

namespace PInvoke.Test
{
    public class ExpressionEvaluatorTest
    {

        #region "Additional test attributes"
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // <ClassInitialize()> Public Shared Sub MyClassInitialize(ByVal testContext As TestContext)
        // End Sub
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // <ClassCleanup()> Public Shared Sub MyClassCleanup()
        // End Sub
        //
        // Use TestInitialize to run code before running each test
        // <TestInitialize()> Public Sub MyTestInitialize()
        // End Sub
        //
        // Use TestCleanup to run code after each test has run
        // <TestCleanup()> Public Sub MyTestCleanup()
        // End Sub
        //
        #endregion

        private void AssertEval(string expr, long result)
        {
            ExpressionEvaluator ee = new ExpressionEvaluator();
            ExpressionValue actual = null;
            Assert.True(ee.TryEvaluate(expr, out actual));
            Assert.Equal(result, actual.ConvertToLong());
        }

        private void AssertEval(string expr, double result)
        {
            ExpressionEvaluator ee = new ExpressionEvaluator();
            ExpressionValue actual = null;
            Assert.True(ee.TryEvaluate(expr, out actual));
            Assert.Equal(result, actual.ConvertToDouble());
        }

        [Fact()]
        public void Leaf1()
        {
            AssertEval("1", 1);
            AssertEval("0xf", 15);
        }

        [Fact()]
        public void Plus1()
        {
            AssertEval("1+2", 3);
            AssertEval("540+50+50", 640);
        }

        [Fact()]
        public void Plus2()
        {
            AssertEval("0x1 + 0x2", 3);
        }

        [Fact()]
        public void Minus1()
        {
            AssertEval("10-2", 8);
            AssertEval("(20-5)-5", 10);
        }

        [Fact()]
        public void Minus2()
        {
            AssertEval("1-2", -1);
        }

        [Fact()]
        public void Divide1()
        {
            AssertEval("10/2", 5);
        }

        [Fact()]
        public void Modulus1()
        {
            AssertEval("5 % 2 ", 1);
            AssertEval("10 % 3", 1);
            AssertEval("15 % 8", 7);
        }

        [Fact()]
        public void ShiftLeft1()
        {
            AssertEval("2 << 1", 4);
        }

        [Fact()]
        public void ShiftRight1()
        {
            AssertEval("4 >> 1", 2);
        }

        [Fact()]
        public void Negative1()
        {
            AssertEval("-1", -1);
            AssertEval("-(2+4)", -6);
        }

        [Fact()]
        public void Negative2()
        {
            AssertEval("-0.1F", -0.1f);
            AssertEval("-3.2F", -3.2f);
        }

        [Fact()]
        public void Boolean1()
        {
            AssertEval("true", 1);
            AssertEval("false", 0);
        }

        [Fact()]
        public void OpAnd1()
        {
            AssertEval("true && true", 1);
            AssertEval("true && false", 0);
        }

        [Fact()]
        public void OpOr1()
        {
            AssertEval("true || true", 1);
            AssertEval("false || true", 1);
            AssertEval("false || false", 0);
        }

        [Fact()]
        public void OpAssign()
        {
            AssertEval("1=2", 2);
        }

        [Fact()]
        public void Char1()
        {
            AssertEval("'c'", 'c');
        }

        [Fact()]
        public void OpEquals1()
        {
            AssertEval("1==1", 1);
            AssertEval("1==2", 0);
        }

        [Fact()]
        public void OpNotEquals1()
        {
            AssertEval("1!=2", 1);
            AssertEval("1!=1", 0);
            AssertEval("0!=0", 0);
        }

        [Fact()]
        public void OpLessThan1()
        {
            AssertEval("1<2", 1);
            AssertEval("2<1", 0);
            AssertEval("1<1", 0);
        }

        [Fact()]
        public void OpLessThanOrEquals1()
        {
            AssertEval("1<=2", 1);
            AssertEval("2<=1", 0);
            AssertEval("1<=1", 1);
            AssertEval("0<=0", 1);
        }
    }
}
