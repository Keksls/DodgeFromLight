using System;
using System.Collections.Generic;

namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Connects immutable editor state, authored-content catalogues, and guarded authoring intentions.
    /// </summary>
    public sealed class MapCreatorViewModel
    {
        private readonly Func<MapCreatorViewState> _stateProvider;
        private readonly IReadOnlyList<MapCreatorPaletteItem> _items;

        public MapCreatorActions Actions { get; }

        /// <summary>
        /// Creates the presentation contract for the Fugui map creator.
        /// </summary>
        public MapCreatorViewModel(
            Func<MapCreatorViewState> stateProvider,
            IReadOnlyList<MapCreatorPaletteItem> items,
            MapCreatorActions actions)
        {
            // A static catalogue and live state provider keep frame allocations predictable.
            _stateProvider =
                stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
        }

        /// <summary>
        /// Gets the latest immutable state of the authored map.
        /// </summary>
        public MapCreatorViewState GetState()
        {
            // The controller remains authoritative for every board and persistence value.
            return _stateProvider();
        }

        /// <summary>
        /// Gets the complete immutable palette catalogue.
        /// </summary>
        public IReadOnlyList<MapCreatorPaletteItem> GetItems()
        {
            // Filtering and search remain presentation concerns over this stable catalogue.
            return _items;
        }
    }
}
