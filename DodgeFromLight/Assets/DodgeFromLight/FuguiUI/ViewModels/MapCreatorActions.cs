using System;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Groups guarded authoring intentions exposed to the map creator presenter.
    /// </summary>
    public sealed class MapCreatorActions
    {
        public Action<int, int, bool> CreateMap { get; }
        public Action<string> SetBrushMode { get; }
        public Action<bool> SetPlanView { get; }
        public Action<string> SetCameraView { get; }
        public Action<bool> SetEnvironmentPreview { get; }
        public Action ToggleGrid { get; }
        public Action RotatePlacement { get; }
        public Action<string> SelectTool { get; }
        public Action<string> SelectDecoration { get; }
        public Action<string> SelectGear { get; }
        public Action<string> SelectEnvironment { get; }
        public Action<string, float> SetEnvironmentValue { get; }
        public Action<string, Color> SetEnvironmentColor { get; }
        public Action<string> RenameMap { get; }
        public Action Save { get; }
        public Action SaveAndClose { get; }
        public Action DiscardAndClose { get; }
        public Action Playtest { get; }
        public Action Undo { get; }
        public Action Redo { get; }
        public Action DeleteSelection { get; }
        public Action FocusSelection { get; }
        public Action<int> SetOrientation { get; }
        public Action<float> RotateElement { get; }
        public Action<float> ScaleElement { get; }
        public Action<int> AdjustArgument { get; }
        public Action ToggleNeedle { get; }
        public Action BeginPath { get; }
        public Action ConfirmPath { get; }
        public Action CancelPath { get; }
        public Action<bool> SetPointerOverUi { get; }
        public Action<bool> SetKeyboardCaptured { get; }

        /// <summary>
        /// Creates the complete intention surface implemented by the Unity map-creator adapter.
        /// </summary>
        public MapCreatorActions(Action<string> selectTool, Action<string> selectDecoration, Action<string> selectGear, Action<string> selectEnvironment, Action<string, float> setEnvironmentValue, Action<string, Color> setEnvironmentColor, Action<string> renameMap, Action save, Action saveAndClose, Action discardAndClose, Action playtest, Action undo, Action redo, Action deleteSelection, Action focusSelection, Action<int> setOrientation, Action<float> rotateElement, Action<float> scaleElement, Action<int> adjustArgument, Action toggleNeedle, Action beginPath, Action confirmPath, Action cancelPath, Action<bool> setPointerOverUi, Action<bool> setKeyboardCaptured, Action<string> setBrushMode, Action<bool> setPlanView, Action toggleGrid, Action rotatePlacement, Action<int, int, bool> createMap, Action<bool> setEnvironmentPreview, Action<string> setCameraView)
        {
            SetEnvironmentPreview = setEnvironmentPreview ?? throw new ArgumentNullException(nameof(setEnvironmentPreview));
            SetCameraView = setCameraView ?? throw new ArgumentNullException(nameof(setCameraView));
            CreateMap = createMap ?? throw new ArgumentNullException(nameof(createMap));
            SetBrushMode = setBrushMode ?? throw new ArgumentNullException(nameof(setBrushMode));
            SetPlanView = setPlanView ?? throw new ArgumentNullException(nameof(setPlanView));
            ToggleGrid = toggleGrid ?? throw new ArgumentNullException(nameof(toggleGrid));
            RotatePlacement = rotatePlacement ?? throw new ArgumentNullException(nameof(rotatePlacement));
            // Every delegate is required so the presenter never guesses fallback behavior.
            SelectTool = selectTool ?? throw new ArgumentNullException(nameof(selectTool));
            SelectDecoration =
                selectDecoration ?? throw new ArgumentNullException(nameof(selectDecoration));
            SelectGear = selectGear ?? throw new ArgumentNullException(nameof(selectGear));
            SelectEnvironment =
                selectEnvironment ?? throw new ArgumentNullException(nameof(selectEnvironment));
            SetEnvironmentValue =
                setEnvironmentValue ??
                throw new ArgumentNullException(nameof(setEnvironmentValue));
            SetEnvironmentColor =
                setEnvironmentColor ??
                throw new ArgumentNullException(nameof(setEnvironmentColor));
            RenameMap = renameMap ?? throw new ArgumentNullException(nameof(renameMap));
            Save = save ?? throw new ArgumentNullException(nameof(save));
            SaveAndClose =
                saveAndClose ?? throw new ArgumentNullException(nameof(saveAndClose));
            DiscardAndClose =
                discardAndClose ?? throw new ArgumentNullException(nameof(discardAndClose));
            Playtest = playtest ?? throw new ArgumentNullException(nameof(playtest));
            Undo = undo ?? throw new ArgumentNullException(nameof(undo));
            Redo = redo ?? throw new ArgumentNullException(nameof(redo));
            DeleteSelection =
                deleteSelection ?? throw new ArgumentNullException(nameof(deleteSelection));
            FocusSelection =
                focusSelection ?? throw new ArgumentNullException(nameof(focusSelection));
            SetOrientation =
                setOrientation ?? throw new ArgumentNullException(nameof(setOrientation));
            RotateElement =
                rotateElement ?? throw new ArgumentNullException(nameof(rotateElement));
            ScaleElement =
                scaleElement ?? throw new ArgumentNullException(nameof(scaleElement));
            AdjustArgument =
                adjustArgument ?? throw new ArgumentNullException(nameof(adjustArgument));
            ToggleNeedle =
                toggleNeedle ?? throw new ArgumentNullException(nameof(toggleNeedle));
            BeginPath = beginPath ?? throw new ArgumentNullException(nameof(beginPath));
            ConfirmPath = confirmPath ?? throw new ArgumentNullException(nameof(confirmPath));
            CancelPath = cancelPath ?? throw new ArgumentNullException(nameof(cancelPath));
            SetPointerOverUi =
                setPointerOverUi ?? throw new ArgumentNullException(nameof(setPointerOverUi));
            SetKeyboardCaptured =
                setKeyboardCaptured ?? throw new ArgumentNullException(nameof(setKeyboardCaptured));
        }
    }
}
