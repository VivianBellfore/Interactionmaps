using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;



namespace Interactionmaps
{
    /// <summary>
    /// Main form that contains the map and all user functions.
    /// </summary>
    public partial class MainForm : Form
    {
        private HelpForm helpForm;
        private string currentGroup = "Default";
        


        /// <summary>
        /// Constructor and initializing the main form.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            Marker.InitializeGroupVisibilityFromComboBox(comboBoxGroups);

            pictureBox1.Paint += PictureBox1_Paint;
            pictureBox1.MouseClick += PictureBox1_MouseClick;
            
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left; // important: NO Right/Bottom anchor
            pictureBox1.SizeMode = PictureBoxSizeMode.Normal;

            buttonA.Click += GroupToggleButton_Click;

            comboBoxGroups.DrawItem += ComboBoxGroups_DrawItem;
            comboBoxGroups.SelectedIndexChanged += ComboBoxGroups_SelectedIndexChanged;

            comboBoxGroups.SelectedIndex = 0;
            comboBoxGroups.DrawMode = DrawMode.OwnerDrawFixed;

            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.AutoScroll = true;
        }



        /// <summary>
        /// Cheking if a group name is given and enabeling the button if so.
        /// </summary>
        private void textBoxNewGroup_TextChanged(object sender, EventArgs e)
        {
            buttonAddGroup.Enabled = !string.IsNullOrWhiteSpace(textBoxNewGroup.Text);
        }

        /// <summary>
        /// Changing the group visibility.
        /// </summary>
        private void GroupToggleButton_Click(object sender, EventArgs e)
        {
            string selectedGroup = comboBoxGroups.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedGroup)) return;

            bool currentVisible = Marker.groupVisibility[selectedGroup];
            bool newVisible = !currentVisible;

            Marker.groupVisibility[selectedGroup] = newVisible;
            Marker.ToggleGroupVisibility(selectedGroup, newVisible, pictureBox1);

            buttonA.Text = newVisible ? $"{selectedGroup} is shown" : $"{selectedGroup} is hidden";
        }

        

        #region PICTUREBOX
        /// <summary>
        /// Draws marker and label on the map.
        /// </summary>
        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBox1.Image == null) return;

            foreach ( var marker in Marker.markers.Where(m => m.Visible))
            {
                Point drawPoint = new Point((int)(marker.Location.X * Marker.zoomFactor),(int)(marker.Location.Y * Marker.zoomFactor));

                Rectangle rect = new Rectangle(drawPoint.X - 5, drawPoint.Y - 5, 10, 10);

                Color groupColor = Marker.predefinedGroupColors.ContainsKey(marker.Group) ? Marker.predefinedGroupColors[marker.Group] : Marker.fallbackGroupColor;
                using (Brush brush = new SolidBrush(groupColor))
                {
                    e.Graphics.FillEllipse(brush, rect);
                }

                if (!string.IsNullOrWhiteSpace(marker.Label))
                {
                    using (var font = new Font("Segoe UI", 9))
                    using (var textBrush = new SolidBrush(Color.Black))
                    {
                        SizeF textSize = e.Graphics.MeasureString(marker.Label, font);

                        float labelX = 0;
                        float labelY = 0;

                        switch (marker.LabelPosition?.ToLowerInvariant())
                        {
                            case "above":
                                labelX = drawPoint.X - textSize.Width / 2;
                                labelY = drawPoint.Y - 5 - textSize.Height;
                                break;
                            case "below":
                                labelX = drawPoint.X - textSize.Width / 2;
                                labelY = drawPoint.Y + 5;
                                break;
                            case "left":
                                labelX = drawPoint.X - 5 - textSize.Width;
                                labelY = drawPoint.Y - textSize.Height / 2;
                                break;
                            case "right":
                                labelX = drawPoint.X + 5;
                                labelY = drawPoint.Y - textSize.Height / 2;
                                break;
                            default:
                                labelX = drawPoint.X - textSize.Width / 2;
                                labelY = drawPoint.Y - 5 - textSize.Height;
                                break;
                        }

                        RectangleF bgRect = new RectangleF(labelX, labelY, textSize.Width, textSize.Height);
                        using (Brush bgBrush = new SolidBrush(Color.FromArgb(200, Color.White)))
                        {
                            e.Graphics.FillRectangle(bgBrush, bgRect);
                        }

                        e.Graphics.DrawString(marker.Label, font, textBrush, labelX, labelY);
                    }
                }
            }
        }

        /// <summary>
        /// Adding new marker or label or remove marker from the map.
        /// </summary>
        private void PictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image == null)
                return;

            if (!Marker.groupVisibility.ContainsKey(currentGroup))
                Marker.groupVisibility[currentGroup] = true;

            Point mapLocation = Marker.TranslateZoomMousePosition(e.Location, pictureBox1);

            if (e.Button == MouseButtons.Left)
                Marker.markers.Add(new MapMarker(mapLocation, currentGroup));
            else if (e.Button == MouseButtons.Right)
            {
                var marker = Marker.GetMarkerAt(mapLocation);
                if (marker != null)
                    Marker.markers.Remove(marker);
            }
            else if (e.Button == MouseButtons.Middle)
            {
                var marker = Marker.markers.FirstOrDefault(m => m.Visible && Marker.Distance(m.Location, mapLocation) < 10);

                if (marker != null)
                {
                    using (var dlg = new LabelForm(marker.Label, marker.LabelPosition))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            marker.Label = dlg.MarkerLabel;
                            marker.LabelPosition = dlg.LabelPosition;
                            pictureBox1.Invalidate();
                        }
                    }
                }
            }

            pictureBox1.Invalidate();
        }
        #endregion



        #region COMBOBOX
        /// <summary>
        /// Set text in the group color for combobox items.
        /// </summary>
        private void ComboBoxGroups_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= comboBoxGroups.Items.Count)
                return;

            string groupName = comboBoxGroups.Items[e.Index].ToString();

            Color textColor = Marker.predefinedGroupColors.ContainsKey(groupName) ? Marker.predefinedGroupColors[groupName] : Marker.fallbackGroupColor;

            e.Graphics.FillRectangle(
                new SolidBrush(comboBoxGroups.BackColor),
                e.Bounds
            );

            using (Brush brush = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(groupName, e.Font, brush, e.Bounds);
            }
        }

        /// <summary>
        /// Set button group visibility text on context.
        /// </summary>
        private void ComboBoxGroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxGroups.SelectedItem != null)
            {
                currentGroup = comboBoxGroups.SelectedItem.ToString();

                if (Marker.groupVisibility.TryGetValue(currentGroup, out bool visible))
                    buttonA.Text = visible ? $"{currentGroup} is shown" : $"{currentGroup} is hidden";
                else
                    buttonA.Text = $"{currentGroup} (unknown visibility)";
            }
        }
        #endregion



        #region BUTTONS
        /// <summary>
        /// Load an image as map.
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Image map = Image.FromFile(openFileDialog.FileName);
                        pictureBox1.Image = map;

                        Marker.UpdatePictureBoxSize(pictureBox1);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error while loading map: " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Save current map and map marker.
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    Marker.SaveMap(fbd.SelectedPath, pictureBox1.Image);
                }
            }
        }

        /// <summary>
        /// Load an existing map with map marker.
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.LastFolderPath) && Directory.Exists(Properties.Settings.Default.LastFolderPath))
                    fbd.SelectedPath = Properties.Settings.Default.LastFolderPath;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    Properties.Settings.Default.LastFolderPath = fbd.SelectedPath;
                    Properties.Settings.Default.Save();

                    Marker.LoadMap(fbd.SelectedPath, pictureBox1, comboBoxGroups, buttonA);
                }
            }
        }

        /// <summary>
        /// Opens the help form.
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            if (helpForm == null || helpForm.IsDisposed)
            {
                helpForm = new HelpForm();
                helpForm.Show();
            }
            else
                helpForm.BringToFront();
        }

        /// <summary>
        /// Adds a new group to the combobox from a textbox input.
        /// </summary>
        private void buttonAddGroup_Click(object sender, EventArgs e)
        {
            string newGroup = textBoxNewGroup.Text.Trim();

            if (string.IsNullOrWhiteSpace(newGroup))
            {
                MessageBox.Show("Group name cannot be empty.");
                return;
            }

            if (newGroup.Length > 20)
            {
                MessageBox.Show("Group name must be 20 characters or fewer.");
                return;
            }

            if (!Regex.IsMatch(newGroup, @"^[a-zA-Z0-9 ]+$"))
            {
                MessageBox.Show("Group name can only contain letters, numbers, and spaces.");
                return;
            }

            if (!comboBoxGroups.Items.Contains(newGroup))
            {
                Marker.AddGroup(newGroup, comboBoxGroups, buttonA);
                comboBoxGroups.SelectedItem = newGroup;
                currentGroup = newGroup;
                textBoxNewGroup.Clear();
            }
            else
                MessageBox.Show("Group already exists.");
        }
        #endregion
    }
}
