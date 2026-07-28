using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DodgeFromLight.Core.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DodgeFromLight.Infrastructure.Maps
{
    public sealed class LegacyMapImporter
    {
        public const int TargetSchemaVersion = 2;
        public const int TargetRulesetVersion = 1;

        /// <summary>
        /// Converts legacy grid and metadata JSON into a non-mutating target map package.
        /// </summary>
        public LegacyMapImportResult Import(
            string gridJson,
            string metadataJson,
            string sourceLabel)
        {
            // Parse source text in memory so the importer never modifies legacy files.
            var issues = new List<LegacyMapImportIssue>();
            try
            {
                var gridDocument = JObject.Parse(gridJson ?? string.Empty);
                var metadataDocument = JObject.Parse(metadataJson ?? string.Empty);
                var width = ReadRequiredInteger(gridDocument, "Width");
                var height = ReadRequiredInteger(gridDocument, "Height");
                var mapId = ReadRequiredString(metadataDocument, "ID");
                var title = ReadOptionalString(metadataDocument, "Name");
                var author = ReadOptionalString(metadataDocument, "Author");

                CheckMetadataDimensions(metadataDocument, width, height, issues);

                var cells = ParseCells(gridDocument, width, height, issues);
                var enemies = ParseEnemies(gridDocument, issues);
                var platforms = ParseMovingPlatforms(gridDocument, issues);
                var startPosition = ParsePosition(
                    RequireObject(gridDocument, "StartCell"),
                    "StartCell");
                var exitPosition = ParsePosition(
                    RequireObject(gridDocument, "EndCell"),
                    "EndCell");

                var revisionId = "legacy." + NormalizeId(mapId) + ".1";
                var map = new MapDefinition(
                    TargetSchemaVersion,
                    TargetRulesetVersion,
                    mapId,
                    revisionId,
                    title,
                    author,
                    width,
                    height,
                    startPosition,
                    exitPosition,
                    cells,
                    enemies,
                    platforms);

                if (ReadOptionalBoolean(gridDocument, "LobbyMap"))
                {
                    issues.Add(new LegacyMapImportIssue(
                        LegacyMapImportIssueSeverity.Warning,
                        "legacy.lobby-map",
                        "The source is marked as a lobby map and requires a later product decision."));
                }

                var environment = gridDocument["Environment"] as JObject;
                var environmentJson = environment == null
                    ? "{}"
                    : environment.ToString(Formatting.None);
                return new LegacyMapImportResult(
                    new MapPackage(map, environmentJson),
                    issues);
            }
            catch (Exception exception)
            {
                issues.Add(new LegacyMapImportIssue(
                    LegacyMapImportIssueSeverity.Error,
                    "legacy.import-failed",
                    (sourceLabel ?? "Legacy map") + ": " + exception.Message));
                return new LegacyMapImportResult(null, issues);
            }
        }

        #region Legacy document parsing

        /// <summary>
        /// Parses and validates every cell in the legacy two-dimensional array.
        /// </summary>
        private static IReadOnlyList<CellDefinition> ParseCells(
            JObject gridDocument,
            int width,
            int height,
            ICollection<LegacyMapImportIssue> issues)
        {
            // Preserve every authored cell and convert generic arguments to named parameters.
            var columns = gridDocument["Cells"] as JArray;
            if (columns == null || columns.Count != width)
                throw new JsonException("Cells must contain one column for every X coordinate.");

            var cells = new List<CellDefinition>(width * height);
            for (var x = 0; x < width; x++)
            {
                var column = columns[x] as JArray;
                if (column == null || column.Count != height)
                    throw new JsonException("Cell column " + x + " has an invalid height.");

                for (var y = 0; y < height; y++)
                {
                    var cellDocument = column[y] as JObject;
                    if (cellDocument == null)
                        throw new JsonException("Cell " + x + "," + y + " is not an object.");

                    var authoredPosition = new GridPosition(
                        ReadRequiredInteger(cellDocument, "X"),
                        ReadRequiredInteger(cellDocument, "Y"));
                    if (authoredPosition != new GridPosition(x, y))
                    {
                        issues.Add(new LegacyMapImportIssue(
                            LegacyMapImportIssueSeverity.Warning,
                            "legacy.cell-coordinate",
                            "Cell array index " + x + "," + y +
                            " contains authored position " + authoredPosition + "."));
                    }

                    cells.Add(ParseCell(
                        cellDocument,
                        new GridPosition(x, y),
                        authoredPosition,
                        issues));
                }
            }

            return cells;
        }

        /// <summary>
        /// Converts one legacy cell into a typed target definition.
        /// </summary>
        private static CellDefinition ParseCell(
            JObject cellDocument,
            GridPosition normalizedPosition,
            GridPosition authoredPosition,
            ICollection<LegacyMapImportIssue> issues)
        {
            // Translate numeric enums while retaining source identifiers in parameters.
            var legacyType = ReadRequiredInteger(cellDocument, "Type");
            var typeId = MapCellTypeId(legacyType, issues);
            var legacyFloorId = ReadOptionalString(cellDocument, "FloorID");
            var legacyElementId = ReadOptionalString(cellDocument, "ElementID");
            var parameters = new Dictionary<string, string>
            {
                ["floorDirection"] = MapDirection(
                    ReadOptionalInteger(cellDocument, "FloorOrientation", 1),
                    issues,
                    "Cell.FloorOrientation").ToString()
            };

            if (authoredPosition != normalizedPosition)
            {
                parameters["legacy.authoredX"] =
                    authoredPosition.X.ToString(CultureInfo.InvariantCulture);
                parameters["legacy.authoredY"] =
                    authoredPosition.Y.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrEmpty(legacyFloorId))
                parameters["legacy.floorId"] = legacyFloorId;
            if (!string.IsNullOrEmpty(legacyElementId))
                parameters["legacy.elementId"] = legacyElementId;

            AddTypedCellArgument(
                typeId,
                ReadOptionalInteger(cellDocument, "Arg1", 0),
                parameters);

            return new CellDefinition(
                normalizedPosition,
                typeId,
                ReadOptionalBoolean(cellDocument, "Walkable"),
                MapResourceId("decor", legacyFloorId),
                MapResourceId("decor", legacyElementId),
                ReadOptionalFloat(cellDocument, "ElementOrientation", 0f),
                ReadOptionalFloat(cellDocument, "ElementScale", 0f),
                ReadOptionalBoolean(cellDocument, "HasElement"),
                MapGearId(ReadOptionalInteger(cellDocument, "GearType", 0), issues),
                parameters);
        }

        /// <summary>
        /// Converts legacy enemy placements without simulating their behavior yet.
        /// </summary>
        private static IReadOnlyList<EnemyDefinition> ParseEnemies(
            JObject gridDocument,
            ICollection<LegacyMapImportIssue> issues)
        {
            // Preserve stats, arguments, phase orientation, and waypoint order.
            var enemies = new List<EnemyDefinition>();
            var enemyDocuments = gridDocument["Ennemies"] as JArray;
            if (enemyDocuments == null)
                return enemies;

            foreach (var token in enemyDocuments)
            {
                var enemyDocument = token as JObject;
                if (enemyDocument == null)
                    throw new JsonException("An enemy entry is not an object.");

                var waypoints = ParsePositions(enemyDocument["WayPoints"] as JArray, "Enemy.WayPoints");
                enemies.Add(new EnemyDefinition(
                    MapEnemyId(ReadRequiredInteger(enemyDocument, "Type"), issues),
                    ParsePosition(RequireObject(enemyDocument, "StartPos"), "Enemy.StartPos"),
                    MapDirection(
                        ReadRequiredInteger(enemyDocument, "StartOrientation"),
                        issues,
                        "Enemy.StartOrientation"),
                    waypoints,
                    ReadOptionalInteger(enemyDocument, "PO", 0),
                    ReadOptionalInteger(enemyDocument, "TA", 0),
                    ReadOptionalString(enemyDocument, "Args")));
            }

            return enemies;
        }

        /// <summary>
        /// Converts legacy moving-platform placements without simulating them yet.
        /// </summary>
        private static IReadOnlyList<MovingPlatformDefinition> ParseMovingPlatforms(
            JObject gridDocument,
            ICollection<LegacyMapImportIssue> issues)
        {
            // Preserve platform routes for the dedicated parity phase.
            var platforms = new List<MovingPlatformDefinition>();
            var platformDocuments = gridDocument["MovingPlatforms"] as JArray;
            if (platformDocuments == null)
                return platforms;

            foreach (var token in platformDocuments)
            {
                var platformDocument = token as JObject;
                if (platformDocument == null)
                    throw new JsonException("A moving-platform entry is not an object.");

                platforms.Add(new MovingPlatformDefinition(
                    ParsePosition(
                        RequireObject(platformDocument, "StartPos"),
                        "MovingPlatform.StartPos"),
                    MapDirection(
                        ReadRequiredInteger(platformDocument, "StartOrientation"),
                        issues,
                        "MovingPlatform.StartOrientation"),
                    ParsePositions(
                        platformDocument["WayPoints"] as JArray,
                        "MovingPlatform.WayPoints"),
                    ReadOptionalInteger(platformDocument, "TA", 0)));
            }

            return platforms;
        }

        /// <summary>
        /// Parses an ordered array of legacy grid positions.
        /// </summary>
        private static IReadOnlyList<GridPosition> ParsePositions(
            JArray documents,
            string fieldName)
        {
            // Preserve route ordering because it affects patrol phase and replay results.
            var positions = new List<GridPosition>();
            if (documents == null)
                return positions;

            foreach (var token in documents)
            {
                var positionDocument = token as JObject;
                if (positionDocument == null)
                    throw new JsonException(fieldName + " contains a non-object entry.");
                positions.Add(ParsePosition(positionDocument, fieldName));
            }

            return positions;
        }

        /// <summary>
        /// Parses one legacy grid position.
        /// </summary>
        private static GridPosition ParsePosition(JObject document, string fieldName)
        {
            // Require both integer axes so invalid positions cannot be normalized silently.
            if (document == null)
                throw new JsonException(fieldName + " is missing.");
            return new GridPosition(
                ReadRequiredInteger(document, "X"),
                ReadRequiredInteger(document, "Y"));
        }

        #endregion

        #region Legacy value mapping

        /// <summary>
        /// Converts the generic legacy cell argument into a named parameter.
        /// </summary>
        private static void AddTypedCellArgument(
            string typeId,
            int argument,
            IDictionary<string, string> parameters)
        {
            // Name known arguments while retaining unknown non-zero values explicitly.
            switch (typeId)
            {
                case "cell.spring":
                    parameters["distance"] = argument.ToString(CultureInfo.InvariantCulture);
                    break;
                case "cell.timed_bomb":
                    parameters["turns"] = argument.ToString(CultureInfo.InvariantCulture);
                    break;
                case "cell.needles":
                    parameters["initialPhase"] = argument.ToString(CultureInfo.InvariantCulture);
                    break;
                default:
                    if (argument != 0)
                        parameters["legacy.arg1"] = argument.ToString(CultureInfo.InvariantCulture);
                    break;
            }
        }

        /// <summary>
        /// Converts a legacy cell enum value to a stable string ID.
        /// </summary>
        private static string MapCellTypeId(
            int legacyType,
            ICollection<LegacyMapImportIssue> issues)
        {
            // Keep the complete known enum mapping independent of simulation support.
            switch (legacyType)
            {
                case 0:
                    return "cell.floor";
                case 1:
                    return "cell.void";
                case 2:
                    return "cell.teleporter";
                case 3:
                    return "cell.collapse";
                case 4:
                    return "cell.speed_boost";
                case 5:
                    return "cell.checkpoint";
                case 6:
                    return "cell.spring";
                case 7:
                    return "cell.timed_bomb";
                case 8:
                    return "cell.needles";
                case 9:
                    return "cell.sword_socket";
                default:
                    issues.Add(new LegacyMapImportIssue(
                        LegacyMapImportIssueSeverity.Error,
                        "legacy.unknown-cell",
                        "Unknown legacy cell type " + legacyType + "."));
                    return "legacy.cell." + legacyType;
            }
        }

        /// <summary>
        /// Converts a legacy gear enum value to a stable string ID.
        /// </summary>
        private static string MapGearId(
            int legacyType,
            ICollection<LegacyMapImportIssue> issues)
        {
            // Preserve every known gear even when its simulation phase has not started.
            switch (legacyType)
            {
                case 0:
                    return string.Empty;
                case 1:
                    return "gear.bomb";
                case 2:
                    return "gear.disco_ball";
                case 3:
                    return "gear.grapple";
                case 4:
                    return "gear.invisibility_cloak";
                case 5:
                    return "gear.mirror";
                case 6:
                    return "gear.powder_of_darkness";
                case 7:
                    return "gear.remote_control";
                case 8:
                    return "gear.speed_boots";
                case 9:
                    return "gear.time_controller";
                default:
                    issues.Add(new LegacyMapImportIssue(
                        LegacyMapImportIssueSeverity.Error,
                        "legacy.unknown-gear",
                        "Unknown legacy gear type " + legacyType + "."));
                    return "legacy.gear." + legacyType;
            }
        }

        /// <summary>
        /// Converts a legacy enemy enum value to a stable string ID.
        /// </summary>
        private static string MapEnemyId(
            int legacyType,
            ICollection<LegacyMapImportIssue> issues)
        {
            // Preserve all twelve authored enemy identities.
            switch (legacyType)
            {
                case 0:
                    return "enemy.cyclops";
                case 1:
                    return "enemy.gloutton";
                case 2:
                    return "enemy.keeper";
                case 3:
                    return "enemy.knight";
                case 4:
                    return "enemy.scout";
                case 5:
                    return "enemy.trap";
                case 6:
                    return "enemy.lizardman";
                case 7:
                    return "enemy.thunderbird";
                case 8:
                    return "enemy.axe";
                case 9:
                    return "enemy.hammer";
                case 10:
                    return "enemy.catapult";
                case 11:
                    return "enemy.pillar";
                default:
                    issues.Add(new LegacyMapImportIssue(
                        LegacyMapImportIssueSeverity.Error,
                        "legacy.unknown-enemy",
                        "Unknown legacy enemy type " + legacyType + "."));
                    return "legacy.enemy." + legacyType;
            }
        }

        /// <summary>
        /// Converts a legacy orientation enum value to the core direction enum.
        /// </summary>
        private static Direction MapDirection(
            int legacyDirection,
            ICollection<LegacyMapImportIssue> issues,
            string fieldName)
        {
            // Keep valid established values and report invalid orientations explicitly.
            switch (legacyDirection)
            {
                case 1:
                    return Direction.Up;
                case 2:
                    return Direction.Right;
                case 3:
                    return Direction.Down;
                case 4:
                    return Direction.Left;
                default:
                    issues.Add(new LegacyMapImportIssue(
                        LegacyMapImportIssueSeverity.Error,
                        "legacy.unknown-direction",
                        fieldName + " contains unknown direction " + legacyDirection + "."));
                    return Direction.None;
            }
        }

        /// <summary>
        /// Creates a stable target resource ID while preserving the legacy ID separately.
        /// </summary>
        private static string MapResourceId(string category, string legacyId)
        {
            // Empty resource references remain empty instead of creating fake catalog entries.
            return string.IsNullOrWhiteSpace(legacyId)
                ? string.Empty
                : category + "." + NormalizeId(legacyId);
        }

        /// <summary>
        /// Normalizes arbitrary legacy names into stable lowercase identifiers.
        /// </summary>
        private static string NormalizeId(string value)
        {
            // Replace runs of punctuation and whitespace with a single underscore.
            var builder = new StringBuilder();
            var previousWasSeparator = false;
            foreach (var character in value ?? string.Empty)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                    previousWasSeparator = false;
                }
                else if (!previousWasSeparator && builder.Length > 0)
                {
                    builder.Append('_');
                    previousWasSeparator = true;
                }
            }

            return builder.ToString().Trim('_');
        }

        #endregion

        #region Legacy JSON reading

        /// <summary>
        /// Warns when redundant metadata dimensions disagree with the grid.
        /// </summary>
        private static void CheckMetadataDimensions(
            JObject metadata,
            int width,
            int height,
            ICollection<LegacyMapImportIssue> issues)
        {
            // Treat the grid as authoritative while reporting metadata inconsistencies.
            var metadataWidth = ReadOptionalInteger(metadata, "Width", width);
            var metadataHeight = ReadOptionalInteger(metadata, "Height", height);
            if (metadataWidth != width || metadataHeight != height)
            {
                issues.Add(new LegacyMapImportIssue(
                    LegacyMapImportIssueSeverity.Warning,
                    "legacy.metadata-size",
                    "Metadata dimensions do not match the grid dimensions."));
            }
        }

        /// <summary>
        /// Requires an object property from a JSON document.
        /// </summary>
        private static JObject RequireObject(JObject document, string propertyName)
        {
            // Fail fast when required structured data is absent.
            var value = document[propertyName] as JObject;
            if (value == null)
                throw new JsonException("Missing object property '" + propertyName + "'.");
            return value;
        }

        /// <summary>
        /// Reads a required integer property.
        /// </summary>
        private static int ReadRequiredInteger(JObject document, string propertyName)
        {
            // Convert both legacy numeric tokens and numeric strings.
            var token = document[propertyName];
            if (token == null || !int.TryParse(
                    token.ToString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                throw new JsonException("Missing or invalid integer property '" + propertyName + "'.");
            }

            return value;
        }

        /// <summary>
        /// Reads an optional integer property with a fallback value.
        /// </summary>
        private static int ReadOptionalInteger(
            JObject document,
            string propertyName,
            int fallback)
        {
            // Preserve the fallback when an optional legacy field is absent or invalid.
            var token = document[propertyName];
            return token != null && int.TryParse(
                token.ToString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var value)
                ? value
                : fallback;
        }

        /// <summary>
        /// Reads an optional single-precision floating-point property.
        /// </summary>
        private static float ReadOptionalFloat(
            JObject document,
            string propertyName,
            float fallback)
        {
            // Parse with invariant culture because JSON always uses a period separator.
            var token = document[propertyName];
            return token != null && float.TryParse(
                token.ToString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value)
                ? value
                : fallback;
        }

        /// <summary>
        /// Reads a required non-empty string property.
        /// </summary>
        private static string ReadRequiredString(JObject document, string propertyName)
        {
            // Reject empty identifiers before constructing an immutable map.
            var value = ReadOptionalString(document, propertyName);
            if (string.IsNullOrWhiteSpace(value))
                throw new JsonException("Missing string property '" + propertyName + "'.");
            return value;
        }

        /// <summary>
        /// Reads an optional string property.
        /// </summary>
        private static string ReadOptionalString(JObject document, string propertyName)
        {
            // Normalize JSON null tokens to an empty string.
            var token = document[propertyName];
            return token == null || token.Type == JTokenType.Null
                ? string.Empty
                : token.ToString();
        }

        /// <summary>
        /// Reads an optional boolean property.
        /// </summary>
        private static bool ReadOptionalBoolean(JObject document, string propertyName)
        {
            // Accept the native JSON boolean representation used by legacy maps.
            var token = document[propertyName];
            return token != null &&
                   bool.TryParse(token.ToString(), out var value) &&
                   value;
        }

        #endregion
    }
}
