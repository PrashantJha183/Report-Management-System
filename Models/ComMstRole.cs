using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Report.Models
{
    public class ComMstRole
    {
        public string Name { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
