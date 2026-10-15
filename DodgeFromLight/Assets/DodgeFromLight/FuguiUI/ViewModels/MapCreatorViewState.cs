using System.Collections.Generic;

namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Carries one immutable map-creator frame from the Unity adapter to the Fugui presenter.
    /// </summary>
    public sealed class MapCreatorViewState
    {
        public MapCreatorViewportState Viewport { get; }
        public string MapName { get; }
        public int Width { get; }
        public int Height { get; }
        public string ActiveToolId { get; }
        public string ActiveToolLabel { get; }
        public string EnvironmentId { get; }
        public string EnvironmentLabel { get; }
        public string StatusMessage { get; }
        public bool IsReady { get; }
        public bool IsBusy { get; }
        public bool IsDirty { get; }
        public bool CanUndo { get; }
        public bool CanRedo { get; }
        public IReadOnlyList<string> ValidationErrors { get; }
        public MapCreatorSelectionState Selection { get; }
        public IReadOnlyList<MapCreatorEnvironmentValue> EnvironmentValues { get; }
        public IReadOnlyList<MapCreatorEnvironmentColor> EnvironmentColors { get; }

        /// <summary>
        /// Creates one complete editor state consumed by the foreground DrawList.
        /// </summary>
        public MapCreatorViewState(string mapName, int width, int height, string activeToolId, string activeToolLabel, string environmentId, string environmentLabel, string statusMessage, bool isReady, bool isBusy, bool isDirty, bool canUndo, bool canRedo, IReadOnlyList<string> validationErrors, MapCreatorSelectionState selection, IReadOnlyList<MapCreatorEnvironmentValue> environmentValues, IReadOnlyList<MapCreatorEnvironmentColor> environmentColors, MapCreatorViewportState viewport)
        {
            Viewport = viewport;
            // Null values are normalized so rendering never needs defensive branches.
            MapName = mapName ?? string.Empty;
            Width = width;
            Height = height;
            ActiveToolId = activeToolId ?? string.Empty;
            ActiveToolLabel = activeToolLabel ?? string.Empty;
            EnvironmentId = environmentId ?? string.Empty;
            EnvironmentLabel = environmentLabel ?? string.Empty;
            StatusMessage = statusMessage ?? string.Empty;
            IsReady = isReady;
            IsBusy = isBusy;
            IsDirty = isDirty;
            CanUndo = canUndo;
            CanRedo = canRedo;
            ValidationErrors =
                validationErrors ?? new string[0];
            EnvironmentValues =
                environmentValues ??
                new MapCreatorEnvironmentValue[0];
            EnvironmentColors =
                environmentColors ??
                new MapCreatorEnvironmentColor[0];
            Selection =
                selection ??
                new MapCreatorSelectionState(
                    false,
                    0,
                    0,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    0,
                    0,
                    1f);
        }
    }
}
