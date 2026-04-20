using System;
using System.Collections.Generic;

namespace MyOwnLearning.Models;

public partial class Function
{
    public int FunctionId { get; set; }

    public string FunctionName { get; set; } = null!;

    public virtual ICollection<RoleModuleFunction> RoleModuleFunctions { get; set; } = new List<RoleModuleFunction>();
}
