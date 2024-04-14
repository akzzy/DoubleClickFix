namespace DoubleClickFix
{
    public partial class InteractiveForm : Form
    {
        public InteractiveForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int minValue;
            if (this.OnSave != null && int.TryParse(textBox1.Text, out minValue))
            {
                this.OnSave(minValue);
            }
        }
    }
}
