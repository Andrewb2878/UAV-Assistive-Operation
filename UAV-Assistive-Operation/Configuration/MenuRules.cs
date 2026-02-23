using System.Collections.Generic;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;

namespace UAV_Assistive_Operation.Configuration
{
    public static class MenuRules
    {
        public static readonly Dictionary<MenuRowOptions, MenuRulesModel> Rules =
            new Dictionary<MenuRowOptions, MenuRulesModel>
        {
            [MenuRowOptions.ReconfigController] = new MenuRulesModel
            {
                IsToggleButton = false,
                ButtonText = "Configure"
            },

            [MenuRowOptions.simulatorMode] = new MenuRulesModel
            {
                IsToggleButton = true
            }
        };
    }
}
