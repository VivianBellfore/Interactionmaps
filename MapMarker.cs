using Newtonsoft.Json;

using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;



namespace Interactionmaps
{
    internal class Marker
    {
        public static readonly Dictionary<string, Color> predefinedGroupColors = new Dictionary<string, Color>
        {
            { "Default", Color.Magenta },
            { "Gang Hideouts", Color.Red },
            { "NCPD", Color.Blue },
            { "Club or Bar", Color.Green }
        };

        public static Dictionary<string, bool> groupVisibility = new Dictionary<string, bool>();
        public static List<MapMarker> markers = new List<MapMarker>();
        public static readonly Color fallbackGroupColor = Color.Magenta;
        public static float zoomFactor = 1.0f;


        public static double Distance(Point p1, Point p2)
        {
            int dx = p1.X - p2.X;
            int dy = p1.Y - p2.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static void ToggleGroupVisibility(string group, bool visible, PictureBox pictureBox1)
        {
            foreach (var marker in markers)
            {
                if (marker.Group == group)
                    marker.Visible = visible;
            }
            pictureBox1.Invalidate();
        }

        public static Point TranslateZoomMousePosition(Point coordinates, PictureBox pictureBox1)
        {
            if (pictureBox1.Image == null) return Point.Empty;

            int x = (int)(coordinates.X / zoomFactor);
            int y = (int)(coordinates.Y / zoomFactor);

            return new Point(x, y);
        }

        public static void InitializeGroupVisibilityFromComboBox(ComboBox comboBoxGroups)
        {
            groupVisibility.Clear();
            foreach (var item in comboBoxGroups.Items)
            {
                string group = item.ToString();
                if (!groupVisibility.ContainsKey(group))
                {
                    groupVisibility[group] = true;
                }
            }
        }

        public static MapMarker GetMarkerAt(Point imagePoint, int radius = 6)
        {
            foreach (var marker in markers)
            {
                var dx = marker.Location.X - imagePoint.X;
                var dy = marker.Location.Y - imagePoint.Y;
                var distSquared = dx * dx + dy * dy;

                if (distSquared <= radius * radius)
                    return marker;
            }

            return null;
        }

        /// <summary>
        /// Saves the image and a json file with marker data.
        /// </summary>
        public static void SaveMap(string folderPath, Image mapImage)
        {
            string imagePath = Path.Combine(folderPath, "mapImage.jpg");
            using (Bitmap clone = new Bitmap(mapImage))
            {
                clone.Save(imagePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            string markerJson = JsonConvert.SerializeObject(markers, Formatting.Indented);
            File.WriteAllText(Path.Combine(folderPath, "map.json"), markerJson);

            MessageBox.Show("Map was saved!");
        }

        public static void LoadMap(string folderPath, PictureBox pictureBox1, ComboBox comboBoxGroups, Button buttonA)
        {
            if (pictureBox1.Image != null)
                pictureBox1.Image.Dispose();

            string imagePath = Path.Combine(folderPath, "mapImage.jpg");
            string markerPath = Path.Combine(folderPath, "map.json");

            if (!File.Exists(imagePath) || !File.Exists(markerPath))
            {
                MessageBox.Show("Missing project files.");
                return;
            }

            using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                pictureBox1.Image = Image.FromStream(fs).Clone() as Image;
            }

            string json = File.ReadAllText(markerPath);
            Marker.markers = JsonConvert.DeserializeObject<List<MapMarker>>(json);

            pictureBox1.Invalidate();

            var groupsFromMarkers = Marker.markers.Select(m => m.Group).Distinct();
            foreach (var group in groupsFromMarkers)
            {
                bool exists = comboBoxGroups.Items.Cast<string>().Any(g => g == group);
                if (!exists)
                    AddGroup(group, comboBoxGroups, buttonA);
            }

            UpdatePictureBoxSize(pictureBox1);
        }

        public static void AddGroup(string groupName, ComboBox comboBoxGroups, Button buttonA)
        {
            if (!comboBoxGroups.Items.Contains(groupName))
            {
                comboBoxGroups.Items.Add(groupName);
            }

            if (!groupVisibility.ContainsKey(groupName))
            {
                groupVisibility[groupName] = true; // default visible
            }

            if (!predefinedGroupColors.ContainsKey(groupName))
                predefinedGroupColors[groupName] = fallbackGroupColor;

            UpdateGroupToggleButton(groupName, buttonA);
        }

        private static void UpdateGroupToggleButton(string groupName, Button buttonA)
        {
            bool visible = Marker.groupVisibility.ContainsKey(groupName) && Marker.groupVisibility[groupName];
            buttonA.Text = visible
                ? $"{groupName} is shown"
                : $"{groupName} is hidden";
        }

        public static void UpdatePictureBoxSize(PictureBox pictureBox1)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Width = (int)(pictureBox1.Image.Width * zoomFactor);
                pictureBox1.Height = (int)(pictureBox1.Image.Height * zoomFactor);

                pictureBox1.Size = new Size((int)(pictureBox1.Image.Width * zoomFactor), (int)(pictureBox1.Image.Height * zoomFactor)
);
            }
        }
    }



    internal class MapMarker
    {
        public Point Location { get; set; }
        public string Group { get; set; }
        public bool Visible { get; set; } = true;
        public string Label { get; set; } = "";
        public string LabelPosition { get; set; } = "Above";



        public MapMarker(Point location, string group)
        {
            Location = location;
            Group = group;
        }
    }
}
