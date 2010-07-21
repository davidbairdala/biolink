﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BioLink.Client.Utilities {

    /// <summary>
    /// Facade over thread pool, just in case we want to take more control over the threading later
    /// </summary>
    public class JobExecutor {

        public static void QueueJob(WaitCallback job, object state) {            
            ThreadPool.QueueUserWorkItem(job, state);
        }

        public static void QueueJob(Action job) {
            ThreadPool.QueueUserWorkItem((ignored) => { job(); } );
        }

    }
}