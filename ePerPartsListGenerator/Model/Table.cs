﻿using System.Collections.Generic;

namespace ePerPartsListGenerator.Model
{
    public class Table
    {
        public int TableCode;
        public string FullCode;
        public string Description;
        internal List<Drawing> Drawings;
    }
}