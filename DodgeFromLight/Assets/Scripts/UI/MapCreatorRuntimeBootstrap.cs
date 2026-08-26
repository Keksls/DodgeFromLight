using System;
using System.Collections.Generic;
using System.Linq;
using DodgeFromLight.FuguiUI.Rendering;
using DodgeFromLight.FuguiUI.Screens;
using DodgeFromLight.FuguiUI.ViewModels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DodgeFromLight.UnityPresentation.UI
{
    /// <summary>
    /// Attaches the foreground DrawList map creator when its local scene becomes active.
    /// </summary>
    public static class MapCreatorRuntimeBootstrap
    {
        private const string MapCreatorSceneName = "MapCreator";

        /// <summary>
        /// Registers the map-creator scene callback before the first scene begins loading.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // Re-registration remains safe when Play Mode starts without a domain reload.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Creates the DrawList map-creator bridge for the authored editor scene.
        /// </summary>
        private static void OnSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            // Every other scene keeps its own dedicated runtime presenter.
            if (scene.name != MapCreatorSceneName)
                return;

            MapCreatorController controller =
                UnityEngine.Object.FindFirstObjectByType<MapCreatorController>();
            if (controller == null)
            {
                Debug.LogError(
                    "The Fugui map creator could not find its scene controller.");
                return;
            }
            if (!FuguiRuntimeController.EnsureInitialized())
                return;
            if (controller.GetComponent<MapCreatorPresenter>() != null)
                return;

            MapCreatorViewModel viewModel =
                CreateViewModel(controller);
            MapCreatorPresenter presenter =
                controller.gameObject.AddComponent<MapCreatorPresenter>();
            if (presenter.Initialize(viewModel))
                return;

            UnityEngine.Object.Destroy(presenter);
            Debug.LogError(
                "The Fugui map-creator DrawList presenter could not be attached.");
        }

        /// <summary>
        /// Creates the presentation contract and guarded controller intentions.
        /// </summary>
        private static MapCreatorViewModel CreateViewModel(
            MapCreatorController controller)
        {
            // The adapter is the only layer translating mutable Unity domain objects into UI snapshots.
            var actions =
                new MapCreatorActions(
                    controller.SelectTool,
                    controller.SelectDecoration,
                    controller.SelectGear,
                    controller.SelectEnvironment,
                    controller.SetEnvironmentValue,
                    controller.SetEnvironmentColor,
                    controller.RenameMap,
                    controller.RequestSave,
                    controller.SaveAndClose,
                    controller.DiscardAndClose,
                    controller.PlayMap,
                    controller.Undo,
                    controller.Redo,
                    controller.DeleteSelection,
                    controller.FocusSelection,
                    controller.SetSelectionOrientation,
                    controller.RotateSelectedElement,
                    controller.ScaleSelectedElement,
                    controller.AdjustSelectedArgument,
                    controller.ToggleSelectedNeedle,
                    controller.BeginPathEditing,
                    controller.ConfirmPathEditing,
                    controller.CancelPathEditing,
                    controller.SetPointerOverUi,
                    controller.SetKeyboardCaptured);
            return new MapCreatorViewModel(
                () => CreateViewState(controller),
                CreatePaletteItems(),
                actions);
        }

        /// <summary>
        /// Creates one immutable view state from the current map-authoring controller.
        /// </summary>
        private static MapCreatorViewState CreateViewState(
            MapCreatorController controller)
        {
            // Missing map data produces a safe loading snapshot until controller Start completes.
            FullMap fullMap = GameContext.CurrentMap;
            Grid grid =
                fullMap != null ? fullMap.Grid : null;
            Map map =
                fullMap != null ? fullMap.Map : null;
            string activeToolId =
                controller.CurrentTool == ToolType.MapDeco
                    ? controller.CurrentDecorationId
                    : controller.CurrentTool.ToString();
            return new MapCreatorViewState(
                map != null ? map.Name : "Chargement…",
                grid != null ? grid.Width : 0,
                grid != null ? grid.Height : 0,
                activeToolId,
                controller.GetCurrentToolLabel(),
                controller.EnvironmentId,
                controller.EnvironmentLabel,
                controller.StatusMessage,
                controller.IsReady,
                controller.IsBusy,
                controller.IsDirty,
                UndoableActionsManager.CanUndo,
                UndoableActionsManager.CanRedo,
                controller.ValidationErrors.ToArray(),
                CreateSelectionState(controller, grid),
                CreateEnvironmentValues(grid),
                CreateEnvironmentColors(grid));
        }

        /// <summary>
        /// Creates one immutable inspector state from the selected domain cell.
        /// </summary>
        private static MapCreatorSelectionState CreateSelectionState(
            MapCreatorController controller,
            Grid grid)
        {
            // Selection entities remain controller-owned and are exposed only as stable display values.
            Cell cell = controller.SelectedCell;
            if (cell == null || grid == null)
                return CreateEmptySelection();

            string cellType = grid.IsStart(cell)
                ? "Départ"
                : grid.IsEnd(cell)
                    ? "Sortie"
                    : GetCellTypeLabel(cell.Type);
            Ennemy enemy = controller.SelectedEnnemy;
            MovingPlatform platform =
                controller.SelectedMovingPlatform;
            return new MapCreatorSelectionState(
                true,
                cell.X,
                cell.Y,
                cellType,
                cell.FloorID,
                cell.ElementID,
                cell.GearType.ToString(),
                enemy != null ? enemy.Type.ToString() : string.Empty,
                controller.GetSelectionOrientationLabel(),
                platform != null,
                controller.CanAssignGearToSelection(),
                controller.CanRotateSelectedElement(),
                controller.CanScaleSelectedElement(),
                enemy != null || platform != null,
                controller.CurrentTool == ToolType.Path,
                controller.GetSelectedPathPointCount(),
                cell.Arg1,
                cell.ElementScale);
        }

        /// <summary>
        /// Creates the empty inspector state used before a board cell is selected.
        /// </summary>
        private static MapCreatorSelectionState CreateEmptySelection()
        {
            // A concrete empty value avoids nullable inspector state in immediate-mode rendering.
            return new MapCreatorSelectionState(
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

        /// <summary>
        /// Creates all numeric environment controls from the active serialized environment.
        /// </summary>
        private static IReadOnlyList<MapCreatorEnvironmentValue> CreateEnvironmentValues(
            Grid grid)
        {
            // Bounds match the complete legacy editor capability while presentation is now data-driven.
            if (grid == null)
                return new MapCreatorEnvironmentValue[0];

            EnvironmentSettings environment =
                grid.GetEnvironment();
            return new[]
            {
                new MapCreatorEnvironmentValue("ParticleSize", "Taille", "Particules", environment.ParticleSize, 0.1f, 2f, "0.00"),
                new MapCreatorEnvironmentValue("ParticleDensity", "Densité", "Particules", environment.ParticleDensity, 0.1f, 2f, "0.00"),
                new MapCreatorEnvironmentValue("FoamScale", "Échelle de l’écume", "Eau", environment.FoamScale, 0.01f, 10f, "0.00"),
                new MapCreatorEnvironmentValue("SunIntensity", "Intensité solaire", "Éclairage", environment.SunIntensity, 0f, 2f, "0.00"),
                new MapCreatorEnvironmentValue("Seed", "Graine", "Ciel", environment.Seed, 0f, 1000f, "0"),
                new MapCreatorEnvironmentValue("Scale", "Échelle", "Ciel", environment.Scale, 0.001f, 10f, "0.000"),
                new MapCreatorEnvironmentValue("Contrast", "Contraste", "Ciel", environment.Contrast, 0.001f, 1f, "0.000"),
                new MapCreatorEnvironmentValue("AnimationSpeed", "Vitesse", "Ciel", environment.AnimationSpeed, 0f, 0.25f, "0.000"),
                new MapCreatorEnvironmentValue("DustAmount", "Poussière", "Ciel", environment.DustAmount, 0f, 1f, "0.00"),
                new MapCreatorEnvironmentValue("Nebular1Strength", "Nébuleuse I", "Nébuleuses", environment.Nebular1Strength, 0f, 1f, "0.00"),
                new MapCreatorEnvironmentValue("Nebular2Strength", "Nébuleuse II", "Nébuleuses", environment.Nebular2Strength, 0f, 1f, "0.00"),
                new MapCreatorEnvironmentValue("SunSize", "Taille du soleil", "Astre", environment.SunSize, 0f, 1f, "0.00"),
                new MapCreatorEnvironmentValue("SunSkyIntensity", "Éclat du soleil", "Astre", environment.SunSkyIntensity, 0.3f, 25f, "0.00"),
                new MapCreatorEnvironmentValue("SunHorizontal", "Azimut", "Astre", environment.SunHorizontal, 0f, 1f, "0.00"),
                new MapCreatorEnvironmentValue("SunUp", "Hauteur", "Astre", environment.SunUp, -1f, 1f, "0.00")
            };
        }

        /// <summary>
        /// Creates all color environment controls from the active serialized environment.
        /// </summary>
        private static IReadOnlyList<MapCreatorEnvironmentColor> CreateEnvironmentColors(
            Grid grid)
        {
            // Every color exposed by the original creator remains editable.
            if (grid == null)
                return new MapCreatorEnvironmentColor[0];

            EnvironmentSettings environment =
                grid.GetEnvironment();
            return new[]
            {
                new MapCreatorEnvironmentColor("FoamColor", "Écume", "Eau", GetEnvironmentColor(environment.FoamColor, Color.white)),
                new MapCreatorEnvironmentColor("DepthDeep", "Profondeur", "Eau", GetEnvironmentColor(environment.DepthDeep, Color.black)),
                new MapCreatorEnvironmentColor("DepthShallow", "Surface", "Eau", GetEnvironmentColor(environment.DepthShallow, Color.gray)),
                new MapCreatorEnvironmentColor("SunColor", "Soleil", "Éclairage", GetEnvironmentColor(environment.SunColor, Color.white)),
                new MapCreatorEnvironmentColor("SkyColor", "Ciel ambiant", "Éclairage", GetEnvironmentColor(environment.SkyColor, Color.gray)),
                new MapCreatorEnvironmentColor("EquatorColor", "Horizon", "Éclairage", GetEnvironmentColor(environment.EquatorColor, Color.gray)),
                new MapCreatorEnvironmentColor("GroundColor", "Sol ambiant", "Éclairage", GetEnvironmentColor(environment.GroundColor, Color.black)),
                new MapCreatorEnvironmentColor("SunSkyColor", "Astre", "Ciel", GetEnvironmentColor(environment.SunSkyColor, Color.white)),
                new MapCreatorEnvironmentColor("Nebular1ColorMain", "Nébuleuse I · cœur", "Nébuleuses", GetEnvironmentColor(environment.Nebular1ColorMain, Color.black)),
                new MapCreatorEnvironmentColor("Nebular1ColorMid", "Nébuleuse I · bord", "Nébuleuses", GetEnvironmentColor(environment.Nebular1ColorMid, Color.black)),
                new MapCreatorEnvironmentColor("Nebular2Color1", "Nébuleuse II · cœur", "Nébuleuses", GetEnvironmentColor(environment.Nebular2Color1, Color.black)),
                new MapCreatorEnvironmentColor("Nebular2Color2", "Nébuleuse II · bord", "Nébuleuses", GetEnvironmentColor(environment.Nebular2Color2, Color.black))
            };
        }

        /// <summary>
        /// Converts an optional serialized environment color into a safe Unity color.
        /// </summary>
        private static Color GetEnvironmentColor(
            Serializable_Vector4 value,
            Color fallback)
        {
            // Older local maps may omit environment fields introduced after they were authored.
            return value != null
                ? (Color)value.ToColor()
                : fallback;
        }

        /// <summary>
        /// Gets a concise French label for one serialized cell mechanic.
        /// </summary>
        private static string GetCellTypeLabel(
            CellType type)
        {
            // Temporary labels preserve stable domain enums until the localization catalogue lands.
            switch (type)
            {
                case CellType.Walkable:
                    return "Sol praticable";
                case CellType.NotWalkable:
                    return "Vide";
                case CellType.TP:
                    return "Téléporteur";
                case CellType.Collapsable:
                    return "Sol fragile";
                case CellType.FlipACoin:
                    return "Pile ou face";
                case CellType.CheckPoint:
                    return "Point de contrôle";
                case CellType.Spring:
                    return "Ressort";
                case CellType.Bombe:
                    return "Bombe temporisée";
                case CellType.Needle:
                    return "Pics";
                case CellType.SwordSocle:
                    return "Socle d’épée";
                default:
                    return type.ToString();
            }
        }

        /// <summary>
        /// Creates the complete retained authoring catalogue for tools, assets, gears, and environments.
        /// </summary>
        private static IReadOnlyList<MapCreatorPaletteItem> CreatePaletteItems()
        {
            // Authored previews are resolved once while dynamic decoration and gear databases provide their own.
            var items =
                new List<MapCreatorPaletteItem>();
            MapCreatorUiAssets assets =
                Resources.Load<MapCreatorUiAssets>(
                    "MapCreatorUiAssets");
            AddStructureItems(items, assets);
            AddEnemyItems(items, assets);
            AddDecorationItems(items);
            AddGearItems(items);
            AddEnvironmentItems(items);
            return items;
        }

        /// <summary>
        /// Adds all retained board and mechanic tools to the creator catalogue.
        /// </summary>
        private static void AddStructureItems(
            ICollection<MapCreatorPaletteItem> items,
            MapCreatorUiAssets assets)
        {
            // Ordering follows the natural authoring flow from navigation to structure to mechanics.
            AddTool(items, assets, ToolType.Select, "Sélection", "Inspecte et configure une case.", "Navigation", 0);
            AddTool(items, assets, ToolType.Walkable, "Sol", "Crée une case praticable.", "Terrain", 10);
            AddTool(items, assets, ToolType.UnWalkable, "Vide", "Retire le sol d’une case vide.", "Terrain", 20);
            AddTool(items, assets, ToolType.Start, "Départ", "Définit l’arrivée du joueur sur la carte.", "Objectif", 30);
            AddTool(items, assets, ToolType.End, "Sortie", "Définit la case qui termine le puzzle.", "Objectif", 40);
            AddTool(items, assets, ToolType.CheckPoint, "Checkpoint", "Ajoute un point de reprise.", "Objectif", 50);
            AddTool(items, assets, ToolType.TP, "Téléporteur", "Ajoute une cellule de téléportation.", "Mécanique", 60);
            AddTool(items, assets, ToolType.Collapsable, "Sol fragile", "Ajoute une case qui s’effondre.", "Mécanique", 70);
            AddTool(items, assets, ToolType.FlipACoin, "Pile ou face", "Ajoute la cellule aléatoire existante.", "Mécanique", 80);
            AddTool(items, assets, ToolType.Spring, "Ressort", "Projette le joueur sur une distance configurable.", "Mécanique", 90);
            AddTool(items, assets, ToolType.Bombe, "Bombe", "Explose après un nombre de tours configurable.", "Mécanique", 100);
            AddTool(items, assets, ToolType.Needle, "Pics", "Ajoute un piège à déclenchement configurable.", "Mécanique", 110);
            AddTool(items, assets, ToolType.MovingPlatform, "Plateforme", "Ajoute une plateforme au-dessus du vide.", "Mécanique", 120);
            AddTool(items, assets, ToolType.SwordSocle, "Socle d’épée", "Ajoute le socle utilisé par la téléportation à l’épée.", "Mécanique", 130);
        }

        /// <summary>
        /// Adds every retained enemy family to the creator catalogue.
        /// </summary>
        private static void AddEnemyItems(
            ICollection<MapCreatorPaletteItem> items,
            MapCreatorUiAssets assets)
        {
            // Enemy enum values and tool values intentionally share identifiers.
            foreach (EnnemyType enemy in
                     Enum.GetValues(typeof(EnnemyType)))
            {
                string label = GetEnemyLabel(enemy);
                items.Add(
                    new MapCreatorPaletteItem(
                        enemy.ToString(),
                        label,
                        "Place " + label + " sur une case praticable vide.",
                        IsHazardEnemy(enemy)
                            ? "Obstacle"
                            : "Patrouilleur",
                        MapCreatorPaletteGroup.Enemies,
                        assets != null
                            ? assets.GetTexture(enemy.ToString())
                            : null,
                        (int)enemy));
            }
        }

        /// <summary>
        /// Adds every authored floor and decoration resource to the searchable catalogue.
        /// </summary>
        private static void AddDecorationItems(
            ICollection<MapCreatorPaletteItem> items)
        {
            // Excluded database entries stay unavailable while all authored visible assets are retained.
            int order = 0;
            foreach (MapDecoData decoration in
                     GameContext.Databases.ResourcesData.Data)
            {
                if (decoration == null ||
                    string.IsNullOrWhiteSpace(decoration.ID) ||
                    string.IsNullOrWhiteSpace(decoration.Type) ||
                    decoration.Type == "Excluded")
                {
                    continue;
                }

                items.Add(
                    new MapCreatorPaletteItem(
                        decoration.ID,
                        GetReadableIdentifier(decoration.ID),
                        "Place la ressource " +
                        decoration.ID +
                        " depuis la catégorie " +
                        decoration.Type +
                        ".",
                        decoration.Type,
                        MapCreatorPaletteGroup.Decorations,
                        decoration.Texture,
                        order++));
            }
        }

        /// <summary>
        /// Adds every retained gear from the authoritative gear database.
        /// </summary>
        private static void AddGearItems(
            ICollection<MapCreatorPaletteItem> items)
        {
            // Database descriptions and images are reused directly so gear content cannot drift.
            int order = 0;
            foreach (GearType gear in
                     Enum.GetValues(typeof(GearType)))
            {
                GearDataAsset data =
                    GameContext.Databases.GearData.Gears
                        .FirstOrDefault(candidate =>
                            candidate.Type == gear);
                items.Add(
                    new MapCreatorPaletteItem(
                        gear.ToString(),
                        data != null &&
                        !string.IsNullOrWhiteSpace(data.Name)
                            ? data.Name
                            : GetReadableIdentifier(gear.ToString()),
                        data != null
                            ? data.Description
                            : "Retire le gear de cette case.",
                        "Gear",
                        MapCreatorPaletteGroup.Gears,
                        data != null ? data.Image : null,
                        order++));
            }
        }

        /// <summary>
        /// Adds every built-in environment preset to the creator catalogue.
        /// </summary>
        private static void AddEnvironmentItems(
            ICollection<MapCreatorPaletteItem> items)
        {
            // Preset identifiers match StreamingAssets environment filenames.
            int order = 0;
            foreach (BuildInEnvironments environment in
                     Enum.GetValues(typeof(BuildInEnvironments)))
            {
                items.Add(
                    new MapCreatorPaletteItem(
                        environment.ToString(),
                        GetEnvironmentLabel(environment),
                        "Applique tous les réglages visuels du preset.",
                        "Ambiance",
                        MapCreatorPaletteGroup.Environments,
                        null,
                        order++));
            }
        }

        /// <summary>
        /// Adds one static board tool with its authored preview texture.
        /// </summary>
        private static void AddTool(
            ICollection<MapCreatorPaletteItem> items,
            MapCreatorUiAssets assets,
            ToolType tool,
            string label,
            string description,
            string category,
            int order)
        {
            // Stable enum names connect the palette item to its guarded controller action.
            items.Add(
                new MapCreatorPaletteItem(
                    tool.ToString(),
                    label,
                    description,
                    category,
                    MapCreatorPaletteGroup.Structure,
                    assets != null
                        ? assets.GetTexture(tool.ToString())
                        : null,
                    order));
        }

        /// <summary>
        /// Gets a readable player-facing name for one enemy enum.
        /// </summary>
        private static string GetEnemyLabel(
            EnnemyType enemy)
        {
            // Established proper names are retained while correcting visible spelling where useful.
            switch (enemy)
            {
                case EnnemyType.Gloutton:
                    return "Glouton";
                case EnnemyType.Scoute:
                    return "Éclaireur";
                case EnnemyType.Thunderbird:
                    return "Oiseau-tonnerre";
                case EnnemyType.Axe:
                    return "Hache";
                case EnnemyType.Pillar:
                    return "Pilier";
                default:
                    return enemy.ToString();
            }
        }

        /// <summary>
        /// Reports whether one enemy is presented as a static obstacle.
        /// </summary>
        private static bool IsHazardEnemy(
            EnnemyType enemy)
        {
            // Hazards occupy the enemy palette but do not read as patrol characters.
            return enemy == EnnemyType.Trap ||
                   enemy == EnnemyType.Axe ||
                   enemy == EnnemyType.Hammer ||
                   enemy == EnnemyType.Catapult ||
                   enemy == EnnemyType.Pillar;
        }

        /// <summary>
        /// Converts a stable identifier into a readable fallback label.
        /// </summary>
        private static string GetReadableIdentifier(
            string identifier)
        {
            // Underscores are the only safe generic word boundary in authored resource IDs.
            return string.IsNullOrWhiteSpace(identifier)
                ? "Sans nom"
                : identifier.Replace('_', ' ');
        }

        /// <summary>
        /// Gets a French display label for one built-in environment.
        /// </summary>
        private static string GetEnvironmentLabel(
            BuildInEnvironments environment)
        {
            // Filename-facing enum identifiers stay unchanged behind localized labels.
            switch (environment)
            {
                case BuildInEnvironments.Clair:
                    return "Clair";
                case BuildInEnvironments.Fairy:
                    return "Féérique";
                case BuildInEnvironments.Hellish:
                    return "Infernal";
                case BuildInEnvironments.Holly:
                    return "Sacré";
                case BuildInEnvironments.Night:
                    return "Nuit";
                case BuildInEnvironments.Poison:
                    return "Poison";
                case BuildInEnvironments.Sunshine:
                    return "Solaire";
                default:
                    return "Par défaut";
            }
        }
    }
}
