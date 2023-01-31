using System;
using System.Collections.Generic;
using System.Text;

namespace TimeTreeShared
{

    public interface ICommand
    {
        void Execute();
        void UnExecute();
    }

}
