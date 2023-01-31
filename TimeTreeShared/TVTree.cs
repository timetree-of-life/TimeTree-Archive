using System;
using System.Collections.Generic;
using System.Text;

namespace TimeTreeShared
{
    public enum DisplayMode { BranchLengths, NodeHeights };
    public enum ValidationStatus
    {
        None,
        UserValidated,
        Validating,
        Validated,
        ValidationCancelled
    }
    

}
