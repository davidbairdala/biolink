﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioLink.Data.Model {

    public class Multimedia : BiolinkDataObject {

        public string Name { get; set; }
        public string Number { get; set; }
        public string Artist { get; set; }
        public string DateRecorded { get; set; }
        public string Owner { get; set; }
        public string Copyright { get; set; }
        public string FileExtension { get; set; }

    }
}