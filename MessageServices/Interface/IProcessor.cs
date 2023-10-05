using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageServices.Interface
{
    public interface IProcessor
    {
        public void Bootstrap();
        public void StartProcess();
        public bool Enabled { get; }
    }
}
