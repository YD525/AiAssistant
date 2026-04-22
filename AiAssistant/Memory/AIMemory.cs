using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AiAssistant.Memory
{
    public class MemoryEntry
    {
        public int StepNumber { get; set; }
        public string Action { get; set; }
        public string Parameters { get; set; }
        public string Result { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
    }

    public class AIMemory
    {
        public List<MemoryEntry> Memory = new List<MemoryEntry>();
    }
}
