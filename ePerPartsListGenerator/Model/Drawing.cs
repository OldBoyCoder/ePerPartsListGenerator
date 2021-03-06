/*
MIT License

Copyright (c) 2021 Christopher Reynolds

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ePerPartsListGenerator.Model
{
    /// <summary>
    /// A Drawings is the main entity in the parts book.
    /// It represents a single drawing of a section of the car
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    internal class Drawing
    {
        internal int DrawingNo;
        internal int Revision;
        internal int Variant;
        internal MemoryStream ImageStream;
        internal string TableCode;
        internal List<Part> Parts;
        internal readonly List<string> CompatibilityList = new List<string>();
        internal readonly List<string> ModificationList = new List<string>();
        internal string Modifications;
        internal string ValidFor;

        /// <summary>
        /// A list of any parts that have further expansion available.
        /// </summary>
        internal readonly List<Cliche> Cliches = new List<Cliche> ();

        internal short SgsCode;

        private string GetDebuggerDisplay()
        {
            return $"{TableCode} {SgsCode} {DrawingNo}";
        }
    }
}