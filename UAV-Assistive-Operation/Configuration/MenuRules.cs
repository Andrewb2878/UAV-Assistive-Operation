using System.Collections.Generic;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;

namespace UAV_Assistive_Operation.Configuration
{
    public static class MenuRules
    {
        public static readonly Dictionary<MenuOptions, MenuRulesModel> Rules =
            new Dictionary<MenuOptions, MenuRulesModel>
        {
            [MenuOptions.ReconfigController] = new MenuRulesModel
            {
                IsToggleButton = false,
                ButtonText = "Configure"
            },

            [MenuOptions.simulatorMode] = new MenuRulesModel
            {
                IsToggleButton = true
            }
        };
    }
}
