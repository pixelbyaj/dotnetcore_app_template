﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StartupServices.Interface
{
    public interface IKafkaProcessor: IProcessor
    {
        public void StartProcess(CancellationToken stoppingToken);

    }
}
