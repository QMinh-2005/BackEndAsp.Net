using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class RoleModuleFunction
{
    public int RoleId { get; set; }

    public int ModuleId { get; set; }

    public int FunctionId { get; set; }

    public virtual Function Function { get; set; } = null!;

    public virtual Module Module { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
