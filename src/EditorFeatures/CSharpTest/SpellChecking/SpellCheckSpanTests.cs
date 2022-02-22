﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.SpellChecking;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SpellChecking
{

    [UseExportProvider]
    public class SpellCheckSpanTests : AbstractSpellCheckSpanTests
    {
        protected override TestWorkspace CreateWorkspace(string content)
            => TestWorkspace.CreateCSharp(content);

        [Fact]
        public async Task TestSingleLineComment1()
        {
            await TestAsync("//{|Comment: Goo|}");
        }

        [Fact]
        public async Task TestSingleLineComment2()
        {
            await TestAsync(@"
//{|Comment: Goo|}");
        }

        [Fact]
        public async Task TestMultiLineComment1()
        {
            await TestAsync("/*{|Comment: Goo |}*/");
        }

        [Fact]
        public async Task TestMultiLineComment2()
        {
            await TestAsync(@"
/*{|Comment:
   Goo
 |}*/");
        }

        [Fact]
        public async Task TestMultiLineComment3()
        {
            await TestAsync(@"
/*{|Comment:
   Goo
 |}");
        }

        [Fact]
        public async Task TestMultiLineComment4()
        {
            await TestAsync(@"
/**/");
        }

        [Fact]
        public async Task TestMultiLineComment5()
        {
            await TestAsync(@"
/*{|Comment:/|}");
        }

        [Fact]
        public async Task TestDocComment1()
        {
            await TestAsync(@"
///{|Comment:goo bar Comment:baz|}
class {|Identifier:C|}
{
}");
        }

        [Fact]
        public async Task TestDocComment2()
        {
            await TestAsync(@"
///{|Comment: |}<summary>{|Comment: goo bar baz |}</summary>
class {|Identifier:C|}
{
}");
        }

        [Fact]
        public async Task TestString1()
        {
            await TestAsync(@"""{|String: goo |}""");
        }

        [Fact]
        public async Task TestString2()
        {
            await TestAsync(@"""{|String: goo |}");
        }

        [Fact]
        public async Task TestString3()
        {
            await TestAsync(@"
""{|String: goo |}""");
        }

        [Fact]
        public async Task TestString4()
        {
            await TestAsync(@"
""{|String: goo |}");
        }

        [Fact]
        public async Task TestString5()
        {
            await TestAsync(@"
@""{|String: goo |}""");
        }

        [Fact]
        public async Task TestString6()
        {
            await TestAsync(@"
@""{|String: goo |}");
        }

        [Fact]
        public async Task TestString7()
        {
            await TestAsync(@"""""""{|String: goo |}""""""");
        }

        [Fact]
        public async Task TestString8()
        {
            await TestAsync(@"""""""{|String: goo |}""""");
        }

        [Fact]
        public async Task TestString9()
        {
            await TestAsync(@"""""""{|String: goo |}""");
        }

        [Fact]
        public async Task TestString10()
        {
            await TestAsync(@"""""""{|String: goo |}");
        }

        [Fact]
        public async Task TestString11()
        {
            await TestAsync(@"""""""{|String:
    goo 
    |}""""""");
        }

        [Fact]
        public async Task TestString12()
        {
            await TestAsync(@"""""""{|String:
    goo
    |}""""");
        }

        [Fact]
        public async Task TestString13()
        {
            await TestAsync(@"""""""{|String:
    goo
    |}""");
        }

        [Fact]
        public async Task TestString14()
        {
            await TestAsync(@"""""""{|String:
    goo
    |}");
        }

        [Fact]
        public async Task TestIdentifier1()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
}");
        }

        [Fact]
        public async Task TestIdentifier2()
        {
            await TestAsync(@"
record {|Identifier:C|}
{
}");
        }

        [Fact]
        public async Task TestIdentifier3()
        {
            await TestAsync(@"
record class {|Identifier:C|}
{
}");
        }

        [Fact]
        public async Task TestIdentifier4()
        {
            await TestAsync(@"
delegate void {|Identifier:C|}();");
        }

        [Fact]
        public async Task TestIdentifier5()
        {
            await TestAsync(@"
enum {|Identifier:C|} { }");
        }

        [Fact]
        public async Task TestIdentifier6()
        {
            await TestAsync(@"
enum {|Identifier:C|}
{
    {|Identifier:D|}
}");
        }

        [Fact]
        public async Task TestIdentifier7()
        {
            await TestAsync(@"
enum {|Identifier:C|}
{
    {|Identifier:D|}, {|Identifier:E|}
}");
        }

        [Fact]
        public async Task TestIdentifier8()
        {
            await TestAsync(@"
interface {|Identifier:C|} { }");
        }

        [Fact]
        public async Task TestIdentifier9()
        {
            await TestAsync(@"
struct {|Identifier:C|} { }");
        }

        [Fact]
        public async Task TestIdentifier10()
        {
            await TestAsync(@"
record struct {|Identifier:C|}() { }");
        }

        [Fact]
        public async Task TestIdentifier11()
        {
            await TestAsync(@"
class {|Identifier:C|}<{|Identifier:T|}> { }");
        }

        [Fact]
        public async Task TestIdentifier12()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    private int {|Identifier:X|};
}");
        }

        [Fact]
        public async Task TestIdentifier13()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    private int {|Identifier:X|}, {|Identifier:Y|};
}");
        }

        [Fact]
        public async Task TestIdentifier14()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    private const int {|Identifier:X|};
}");
        }

        [Fact]
        public async Task TestIdentifier15()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    private const int {|Identifier:X|}, {|Identifier:Y|};
}");
        }

        [Fact]
        public async Task TestIdentifier16()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    private int {|Identifier:X|} => 0;
}");
        }

        [Fact]
        public async Task TestIdentifier17()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    private event Action {|Identifier:X|};
}");
        }

        [Fact]
        public async Task TestIdentifier18()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    private event Action {|Identifier:X|}, {|Identifier:Y|};
}");
        }

        [Fact]
        public async Task TestIdentifier19()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    private event Action {|Identifier:X|} { add { } remove { } }
}");
        }

        [Fact]
        public async Task TestIdentifier20()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    void {|Identifier:D|}()
    {
        int {|Identifier:E|};
    }
}");
        }

        [Fact]
        public async Task TestIdentifier21()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    void {|Identifier:D|}()
    {
        int {|Identifier:E|}, {|Identifier:F|};
    }
}");
        }

        [Fact]
        public async Task TestIdentifier22()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    void {|Identifier:D|}()
    {
{|Identifier:E|}:
        return;
    }
}");
        }

        [Fact]
        public async Task TestIdentifier23()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    void {|Identifier:D|}(int {|Identifier:E|})
    {
    }
}");
        }

        [Fact]
        public async Task TestIdentifier24()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    void {|Identifier:D|}(int {|Identifier:E|})
    {
    }
}");
        }

        [Fact]
        public async Task TestIdentifier25()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    void {|Identifier:D|}(int {|Identifier:E|}, int {|Identifier:F|})
    {
    }
}");
        }

        [Fact]
        public async Task TestIdentifier26()
        {
            await TestAsync(@"
static class {|Identifier:C|}
{
    static void {|Identifier:D|}(this int {|Identifier:E|})
    {
    }
}");
        }

        [Fact]
        public async Task TestIdentifier27()
        {
            await TestAsync(@"
namespace {|Identifier:C|}
{
}");
        }

        [Fact]
        public async Task TestIdentifier28()
        {
            await TestAsync(@"
namespace {|Identifier:C|}.{|Identifier:D|}
{
}");
        }

        [Fact]
        public async Task TestIdentifier29()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    void {|Identifier:D|}()
    {
        for (int {|Identifier:E|} = 0; E < 10; E++)
        {
        }
    }
}");
        }

        [Fact]
        public async Task TestIdentifier30()
        {
            await TestAsync(@"
class {|Identifier:C|}
{
    void {|Identifier:D|}()
    {
        Goo(out var {|Identifier:E|});
    }
}");
        }
    }
}
