using System;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Menus;
using Codigames.Modules.UI;
using VContainer.Unity;

namespace Codigames.Game.UI.Presenters
{
    // A building picked in the build menu goes to placement, which opens over the menu so its close comes back
    // to it.
    public class BuildPlacementFlow : IStartable, IDisposable
    {
        private readonly BuildMenuPresenter _buildMenu;
        private readonly UIManager _ui;

        public BuildPlacementFlow(BuildMenuPresenter buildMenu, UIManager ui)
        {
            _buildMenu = buildMenu;
            _ui = ui;
        }

        public void Start() => _buildMenu.Picked += OnPicked;

        public void Dispose() => _buildMenu.Picked -= OnPicked;

        private void OnPicked(string definitionId) => _ = _ui.ShowMenu<PlacementMenu, PlacementOrder>(PlacementOrder.Build(definitionId));
    }
}
