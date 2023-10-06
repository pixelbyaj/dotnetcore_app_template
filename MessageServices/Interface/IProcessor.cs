using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StartupServices.Interface
{
    public interface IProcessor
    {
        public void Connect();
        public void Subscribe<T>(string eventName);

        public void Unsubscribe();

        public void Publish();

        public bool Enabled { get; }
    }
}
