using Fu;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Rendering
{
    /// <summary>
    /// Draws a camera-relative compass with labelled viewpoints and an understandable top/orbit cube.
    /// </summary>
    internal static class MapCreatorAxisGizmo
    {
        /// <summary>
        /// Returns the clicked camera intention without accessing or mutating a scene camera.
        /// </summary>
        public static string Draw(FuDrawList drawList, Rect rect, Quaternion rotation, bool plan, bool transitioning, out string hint)
        {
            float scale = DrawListUi.GetScale();
            MapCreatorTheme.Surface(drawList, rect);
            Vector2 center = rect.center - new Vector2(0f, 5f * scale);
            Quaternion inverse = Quaternion.Inverse(rotation);
            Vector3[] directions = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
            string[] names = { "North", "East", "South", "West" };
            string[] labels = { "N", "E", "S", "O" };
            string[] tips = { "Observer depuis le Nord", "Observer depuis l'Est", "Observer depuis le Sud", "Observer depuis l'Ouest" };
            hint = string.Empty;
            string command = null;
            DrawListUi.DrawCenteredText(drawList, new Vector2(rect.x, rect.y + 7f * scale), rect.width, "Orientation", DrawListUi.TextSecondary, 12);
            // The four ground axes follow the animated camera, rather than snapping to the requested destination.
            for (int i = 0; i < directions.Length; i++)
            {
                Vector3 axis = inverse * directions[i];
                Vector2 projected = new Vector2(axis.x, -axis.y);
                Vector2 end = center + projected.normalized * Mathf.Max(30f, projected.magnitude * 43f) * scale;
                Color color = i % 2 == 0 ? new Color(0.60f, 0.72f, 0.84f) : new Color(0.88f, 0.58f, 0.42f);
                drawList.AddLine(center, end, color, 2f * scale);
                Rect hit = new Rect(end - Vector2.one * 12f * scale, Vector2.one * 24f * scale);
                bool hover = MapCreatorDrawListUi.IsHovered(hit);
                drawList.AddCircleFilled(end, 11f * scale, hover ? new Color(0.37f, 0.28f, 0.14f) : new Color(0.095f, 0.078f, 0.058f));
                drawList.AddCircle(end, 11f * scale, Fugui.GetColorU32(color), 20, scale);
                Vector2 measured = DrawListUi.MeasureText(labels[i], 12);
                DrawListUi.DrawText(drawList, end - measured * 0.5f, labels[i], color, 12);
                if (hover) hint = tips[i];
                if (MapCreatorDrawListUi.WasClicked(hit)) command = names[i];
            }
            Vector2 top = center + new Vector2(0f, -14f) * scale;
            Vector2 left = center + new Vector2(-16f, -5f) * scale;
            Vector2 right = center + new Vector2(16f, -5f) * scale;
            Vector2 middle = center + new Vector2(0f, 4f) * scale;
            drawList.AddQuadFilled(top, right, middle, left, Fugui.GetColorU32(new Color(0.72f, 0.59f, 0.36f)));
            drawList.AddQuadFilled(left, middle, center + new Vector2(0f, 20f) * scale, center + new Vector2(-16f, 11f) * scale, Fugui.GetColorU32(new Color(0.25f, 0.20f, 0.14f)));
            drawList.AddQuadFilled(middle, right, center + new Vector2(16f, 11f) * scale, center + new Vector2(0f, 20f) * scale, Fugui.GetColorU32(new Color(0.42f, 0.32f, 0.19f)));
            Rect cube = new Rect(center + new Vector2(-18f, -16f) * scale, new Vector2(36f, 38f) * scale);
            if (MapCreatorDrawListUi.IsHovered(cube)) hint = plan ? "Passer en perspective" : "Revenir à la vue de dessus";
            if (MapCreatorDrawListUi.WasClicked(cube)) command = plan ? "Perspective" : "Top";
            string action = plan ? "Voir en 3D" : "Vue de dessus";
            Rect caption = new Rect(rect.x + 5f * scale, rect.yMax - 27f * scale, rect.width - 10f * scale, 22f * scale);
            DrawListUi.DrawCenteredText(drawList, caption.position, caption.width, transitioning ? "Transition…" : action, DrawListUi.AccentBright, 12);
            if (MapCreatorDrawListUi.IsHovered(caption)) hint = plan ? "Passer en perspective" : "Revenir à la vue de dessus";
            if (MapCreatorDrawListUi.WasClicked(caption)) command = plan ? "Perspective" : "Top";
            return command;
        }
    }
}
