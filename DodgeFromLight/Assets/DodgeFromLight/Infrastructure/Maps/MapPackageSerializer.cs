using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DodgeFromLight.Core.Model;
using DodgeFromLight.Core.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DodgeFromLight.Infrastructure.Maps
{
    public sealed class MapPackageSerializer
    {
        /// <summary>
        /// Serializes a map package using a stable, versioned JSON structure.
        /// </summary>
        public string Serialize(MapPackage package)
        {
            // Build JSON explicitly so property names and ordering remain intentional.
            if (package == null)
                throw new ArgumentNullException(nameof(package));

            var map = package.Map;
            var root = new JObject
            {
                ["schemaVersion"] = map.SchemaVersion,
                ["rulesetVersion"] = map.RulesetVersion,
                ["mapId"] = map.MapId,
                ["revisionId"] = map.RevisionId,
                ["contentHash"] = MapDefinitionHasher.Compute(map),
                ["title"] = map.Title,
                ["author"] = map.Author,
                ["width"] = map.Width,
                ["height"] = map.Height,
                ["start"] = SerializePosition(map.StartPosition),
                ["exit"] = SerializePosition(map.ExitPosition),
                ["cells"] = new JArray(map.Cells
                    .OrderBy(cell => cell.Position.Y)
                    .ThenBy(cell => cell.Position.X)
                    .Select(SerializeCell)),
                ["enemies"] = new JArray(map.Enemies.Select(SerializeEnemy)),
                ["movingPlatforms"] = new JArray(
                    map.MovingPlatforms.Select(SerializeMovingPlatform)),
                ["visuals"] = new JObject
                {
                    ["environment"] = ParseEnvironment(package.EnvironmentJson)
                }
            };

            return root.ToString(Formatting.Indented);
        }

        #region Deserialization

        /// <summary>
        /// Deserializes and verifies a target map package.
        /// </summary>
        public MapPackage Deserialize(string json)
        {
            // Reconstruct the immutable model, then verify its canonical content hash.
            var root = JObject.Parse(json ?? string.Empty);
            var map = new MapDefinition(
                ReadInteger(root, "schemaVersion"),
                ReadInteger(root, "rulesetVersion"),
                ReadString(root, "mapId"),
                ReadString(root, "revisionId"),
                ReadString(root, "title"),
                ReadString(root, "author"),
                ReadInteger(root, "width"),
                ReadInteger(root, "height"),
                DeserializePosition(RequireObject(root, "start")),
                DeserializePosition(RequireObject(root, "exit")),
                DeserializeCells(RequireArray(root, "cells")),
                DeserializeEnemies(RequireArray(root, "enemies")),
                DeserializeMovingPlatforms(RequireArray(root, "movingPlatforms")));

            var expectedHash = ReadString(root, "contentHash");
            var actualHash = MapDefinitionHasher.Compute(map);
            if (!string.Equals(expectedHash, actualHash, StringComparison.Ordinal))
                throw new InvalidDataException("The map package content hash is invalid.");

            var visuals = root["visuals"] as JObject;
            var environment = visuals?["environment"];
            return new MapPackage(
                map,
                environment == null || environment.Type == JTokenType.Null
                    ? "{}"
                    : environment.ToString(Formatting.None));
        }

        #endregion

        #region Map component conversion

        /// <summary>
        /// Serializes one target cell definition.
        /// </summary>
        private static JObject SerializeCell(CellDefinition cell)
        {
            // Write every authored value required for lossless target reloading.
            var parameters = new JObject();
            foreach (var parameter in cell.Parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                parameters[parameter.Key] = parameter.Value;

            return new JObject
            {
                ["x"] = cell.Position.X,
                ["y"] = cell.Position.Y,
                ["typeId"] = cell.TypeId,
                ["isWalkable"] = cell.IsWalkable,
                ["floorId"] = cell.FloorId,
                ["elementId"] = cell.ElementId,
                ["elementOrientationDegrees"] = cell.ElementOrientationDegrees,
                ["elementScale"] = cell.ElementScale,
                ["blocksMovement"] = cell.BlocksMovement,
                ["gearId"] = cell.GearId,
                ["parameters"] = parameters
            };
        }

        /// <summary>
        /// Deserializes all target cell definitions.
        /// </summary>
        private static IReadOnlyList<CellDefinition> DeserializeCells(JArray documents)
        {
            // Preserve explicit cell data and reject malformed entries.
            var cells = new List<CellDefinition>(documents.Count);
            foreach (var token in documents)
            {
                var document = token as JObject ??
                               throw new JsonException("A cell entry is not an object.");
                cells.Add(new CellDefinition(
                    DeserializePosition(document),
                    ReadString(document, "typeId"),
                    ReadBoolean(document, "isWalkable"),
                    ReadString(document, "floorId"),
                    ReadString(document, "elementId"),
                    ReadFloat(document, "elementOrientationDegrees"),
                    ReadFloat(document, "elementScale"),
                    ReadBoolean(document, "blocksMovement"),
                    ReadString(document, "gearId"),
                    DeserializeParameters(RequireObject(document, "parameters"))));
            }

            return cells;
        }

        /// <summary>
        /// Serializes one enemy placement.
        /// </summary>
        private static JObject SerializeEnemy(EnemyDefinition enemy)
        {
            // Preserve authored stats and waypoint ordering.
            return new JObject
            {
                ["enemyId"] = enemy.EnemyId,
                ["start"] = SerializePosition(enemy.StartPosition),
                ["direction"] = (int)enemy.StartDirection,
                ["sightRange"] = enemy.SightRange,
                ["actionsPerTurn"] = enemy.ActionsPerTurn,
                ["arguments"] = enemy.Arguments,
                ["waypoints"] = new JArray(enemy.Waypoints.Select(SerializePosition))
            };
        }

        /// <summary>
        /// Deserializes all enemy placements.
        /// </summary>
        private static IReadOnlyList<EnemyDefinition> DeserializeEnemies(JArray documents)
        {
            // Rebuild enemy definitions without instantiating presentation objects.
            var enemies = new List<EnemyDefinition>(documents.Count);
            foreach (var token in documents)
            {
                var document = token as JObject ??
                               throw new JsonException("An enemy entry is not an object.");
                enemies.Add(new EnemyDefinition(
                    ReadString(document, "enemyId"),
                    DeserializePosition(RequireObject(document, "start")),
                    (Direction)ReadInteger(document, "direction"),
                    DeserializePositions(RequireArray(document, "waypoints")),
                    ReadInteger(document, "sightRange"),
                    ReadInteger(document, "actionsPerTurn"),
                    ReadString(document, "arguments")));
            }

            return enemies;
        }

        /// <summary>
        /// Serializes one moving-platform placement.
        /// </summary>
        private static JObject SerializeMovingPlatform(MovingPlatformDefinition platform)
        {
            // Preserve authored platform state and waypoint ordering.
            return new JObject
            {
                ["start"] = SerializePosition(platform.StartPosition),
                ["direction"] = (int)platform.StartDirection,
                ["actionsPerTurn"] = platform.ActionsPerTurn,
                ["waypoints"] = new JArray(platform.Waypoints.Select(SerializePosition))
            };
        }

        /// <summary>
        /// Deserializes all moving-platform placements.
        /// </summary>
        private static IReadOnlyList<MovingPlatformDefinition> DeserializeMovingPlatforms(
            JArray documents)
        {
            // Rebuild platform definitions without starting runtime behavior.
            var platforms = new List<MovingPlatformDefinition>(documents.Count);
            foreach (var token in documents)
            {
                var document = token as JObject ??
                               throw new JsonException("A moving-platform entry is not an object.");
                platforms.Add(new MovingPlatformDefinition(
                    DeserializePosition(RequireObject(document, "start")),
                    (Direction)ReadInteger(document, "direction"),
                    DeserializePositions(RequireArray(document, "waypoints")),
                    ReadInteger(document, "actionsPerTurn")));
            }

            return platforms;
        }

        /// <summary>
        /// Serializes one logical grid position.
        /// </summary>
        private static JObject SerializePosition(GridPosition position)
        {
            // Use named axes to keep map files readable and migration-friendly.
            return new JObject
            {
                ["x"] = position.X,
                ["y"] = position.Y
            };
        }

        /// <summary>
        /// Deserializes one logical grid position.
        /// </summary>
        private static GridPosition DeserializePosition(JObject document)
        {
            // Require both axes so malformed coordinates fail at the boundary.
            return new GridPosition(
                ReadInteger(document, "x"),
                ReadInteger(document, "y"));
        }

        /// <summary>
        /// Deserializes an ordered position array.
        /// </summary>
        private static IReadOnlyList<GridPosition> DeserializePositions(JArray documents)
        {
            // Preserve authored route ordering exactly.
            var positions = new List<GridPosition>(documents.Count);
            foreach (var token in documents)
            {
                var document = token as JObject ??
                               throw new JsonException("A position entry is not an object.");
                positions.Add(DeserializePosition(document));
            }

            return positions;
        }

        /// <summary>
        /// Deserializes named cell parameters.
        /// </summary>
        private static IDictionary<string, string> DeserializeParameters(JObject document)
        {
            // Keep values as strings until their owning gameplay system validates them.
            var parameters = new Dictionary<string, string>();
            foreach (var property in document.Properties())
                parameters[property.Name] = property.Value.ToString();
            return parameters;
        }

        #endregion

        #region JSON helpers

        /// <summary>
        /// Parses preserved environment JSON for inclusion in the target package.
        /// </summary>
        private static JToken ParseEnvironment(string environmentJson)
        {
            // Keep environment data lossless while it remains outside the core model.
            return JToken.Parse(environmentJson ?? "{}");
        }

        /// <summary>
        /// Requires an object property from a target package.
        /// </summary>
        private static JObject RequireObject(JObject document, string propertyName)
        {
            // Reject malformed structured data before model construction.
            return document[propertyName] as JObject ??
                   throw new JsonException("Missing object property '" + propertyName + "'.");
        }

        /// <summary>
        /// Requires an array property from a target package.
        /// </summary>
        private static JArray RequireArray(JObject document, string propertyName)
        {
            // Reject missing collections instead of substituting incomplete content.
            return document[propertyName] as JArray ??
                   throw new JsonException("Missing array property '" + propertyName + "'.");
        }

        /// <summary>
        /// Reads a required integer property from a target package.
        /// </summary>
        private static int ReadInteger(JObject document, string propertyName)
        {
            // Parse invariant JSON numbers and reject invalid values.
            var token = document[propertyName];
            if (token == null || !int.TryParse(
                    token.ToString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                throw new JsonException("Missing integer property '" + propertyName + "'.");
            }

            return value;
        }

        /// <summary>
        /// Reads a required single-precision property from a target package.
        /// </summary>
        private static float ReadFloat(JObject document, string propertyName)
        {
            // Parse invariant JSON numbers and reject invalid values.
            var token = document[propertyName];
            if (token == null || !float.TryParse(
                    token.ToString(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                throw new JsonException("Missing number property '" + propertyName + "'.");
            }

            return value;
        }

        /// <summary>
        /// Reads a required boolean property from a target package.
        /// </summary>
        private static bool ReadBoolean(JObject document, string propertyName)
        {
            // Parse the native JSON boolean representation.
            var token = document[propertyName];
            if (token == null || !bool.TryParse(token.ToString(), out var value))
                throw new JsonException("Missing boolean property '" + propertyName + "'.");
            return value;
        }

        /// <summary>
        /// Reads a required string property from a target package.
        /// </summary>
        private static string ReadString(JObject document, string propertyName)
        {
            // Preserve empty optional content strings while requiring the property itself.
            var token = document[propertyName];
            if (token == null || token.Type == JTokenType.Null)
                throw new JsonException("Missing string property '" + propertyName + "'.");
            return token.ToString();
        }

        #endregion
    }
}
