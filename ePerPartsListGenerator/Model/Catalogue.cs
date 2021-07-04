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
    /// Catalogue represents a single model in the ePer system.  Here it is used as the root to 
    /// the class hierarchy.  We also hold the details of all the modifications and variants
    /// used across the whole catalogue to save having to get them for each drawing
    /// </summary>
    class Catalogue
    {
        //public List<Drawing> Drawings;
        /// <summary>
        /// Maintain a list of the distinct groups used in the catalogue.  It is used to
        /// draw the quick access tabs down the side of the page
        /// </summary>
        public List<Group> Groups;
        public string Description;
        public string CatCode;
        public Dictionary<string, string> AllModifications;
        public Dictionary<string, string> AllVariants;
        internal string ImagePath;
    }
}
