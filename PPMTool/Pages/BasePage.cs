using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace PPMTool.Pages
{
    public class BasePage : ComponentBase
    {
        [Inject]
        protected ILogger<AddPerson> Logger { get; set; }
    }
}
