using System;
using System.Windows.Forms;



namespace Interactionmaps
{
    /// <summary>
    /// Dialog to set a label on a marker.
    /// </summary>
    public partial class LabelForm : Form
    {
        public string MarkerLabel => textBox1.Text.Trim();
        public string LabelPosition => comboBox1.SelectedItem?.ToString() ?? "Above";



        /// <summary>
        /// Constructor and initializing.
        /// </summary>
        public LabelForm(string initialLabel, string initialPosition = "Above")
        {
            InitializeComponent();

            textBox1.Text = initialLabel;
            comboBox1.Items.AddRange(new string[] { "Above", "Below", "Left", "Right" });

            if (!string.IsNullOrWhiteSpace(initialPosition))
                comboBox1.SelectedItem = initialPosition;
            else
                comboBox1.SelectedIndex = 0;
        }



        /// <summary>
        /// "Set label" button to save or replace a label.
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Label cannot be empty.");
                this.DialogResult = DialogResult.None;
                return;
            }
        }

        /// <summary>
        /// "Cancel" button to close the dialog form.
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// "Remove label" button to remove an existing label.
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            this.DialogResult = DialogResult.OK;
        }
    }
}
