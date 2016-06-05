using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace PhotoManager.Providers {
    class CustomKeyboardCommandProvider : DefaultKeyboardCommandProvider {
        private readonly GridViewDataControl _parentGrid;
        public CustomKeyboardCommandProvider(GridViewDataControl grid)
            : base(grid) {
            _parentGrid = grid;
        }

        public override IEnumerable<ICommand> ProvideCommandsForKey(Key key) {
            List<ICommand> commandsToExecute = base.ProvideCommandsForKey(key).ToList();
            if (key == Key.Enter) {
                if (_parentGrid.CurrentCell.IsInEditMode) {
                    commandsToExecute.Clear();
                    commandsToExecute.Add(RadGridViewCommands.CommitEdit);
                }
            }
            return commandsToExecute;
        }
    }
}
