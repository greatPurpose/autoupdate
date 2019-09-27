using System.Collections.Generic;
using System.Diagnostics;

namespace ProclickEmployeeLib.Models
{
    public class ProcessesModel
    {
        public IEnumerable<string> Windows { get; set; }
        public IEnumerable<string> Background { get; set; }
    }
}
