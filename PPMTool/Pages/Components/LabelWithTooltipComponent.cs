// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Pages.Components
{
    public class LabelWithTooltipComponent : RadzenLabel
    {
        [Inject]
        private TooltipService TooltipService { get; set; }

        [Parameter]
        public string Tooltip { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            MouseEnter = new EventCallback<ElementReference>(this, ShowTooltip);
        }

        private void ShowTooltip(ElementReference elementReference)
        {
            TooltipService.Open(elementReference, Tooltip, new TooltipOptions { Delay = 250 });
        }
    }
}
