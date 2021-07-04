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

namespace ePerPartsListGenerator.Model
{
    /// <summary>
    /// A cliche is the drawing of a part that is made up of other parts
    /// One example is the brake caliper which is shown as a part in 
    /// the drawings for the brake system as a single part but is then expanded in
    /// subsequent cliches.
    /// </summary>
    class Cliche
    {
        public string PartNo;
        public string Description; // The name of the parent part actually
        public string ClicheCode;
        public string ImagePath;
        public List<Part> Parts; // The sub-parts that make up the parent part

        public Cliche(string clicheCode)
        {
            ClicheCode = clicheCode;
        }
    }
}
