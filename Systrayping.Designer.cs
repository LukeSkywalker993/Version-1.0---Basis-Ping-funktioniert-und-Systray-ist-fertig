namespace Systrayping
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            button1 = new Button();
            numericUpDown1 = new NumericUpDown();
            numericUpDown2 = new NumericUpDown();
            numericUpDown3 = new NumericUpDown();
            IPAddress = new ComboBox();
            label1 = new Label();
            notifyIcon1 = new NotifyIcon(components);
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            PingMin = new NumericUpDown();
            label_PingMin = new Label();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)PingMin).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(3, 12);
            button1.Name = "button1";
            button1.Size = new Size(112, 34);
            button1.TabIndex = 0;
            button1.Text = "Start";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // numericUpDown1
            // 
            numericUpDown1.Location = new Point(94, 57);
            numericUpDown1.Name = "numericUpDown1";
            numericUpDown1.Size = new Size(180, 31);
            numericUpDown1.TabIndex = 1;
            numericUpDown1.Value = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown1.ValueChanged += numericUpDown1_ValueChanged;
            // 
            // numericUpDown2
            // 
            numericUpDown2.Location = new Point(94, 94);
            numericUpDown2.Name = "numericUpDown2";
            numericUpDown2.Size = new Size(180, 31);
            numericUpDown2.TabIndex = 2;
            numericUpDown2.Value = new decimal(new int[] { 2, 0, 0, 0 });
            numericUpDown2.ValueChanged += numericUpDown2_ValueChanged;
            // 
            // numericUpDown3
            // 
            numericUpDown3.Location = new Point(94, 131);
            numericUpDown3.Name = "numericUpDown3";
            numericUpDown3.Size = new Size(180, 31);
            numericUpDown3.TabIndex = 3;
            numericUpDown3.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // IPAddress
            // 
            IPAddress.FormattingEnabled = true;
            IPAddress.Items.AddRange(new object[] { "8.8.8.8", "192.168.23.200", "10.12.12.200", "119.235.99.163" });
            IPAddress.Location = new Point(92, 200);
            IPAddress.Name = "IPAddress";
            IPAddress.Size = new Size(182, 33);
            IPAddress.TabIndex = 4;
            IPAddress.Text = "8.8.8.8";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(94, 246);
            label1.Name = "label1";
            label1.Size = new Size(62, 25);
            label1.TabIndex = 5;
            label1.Text = "Latenz";
            // 
            // notifyIcon1
            // 
            notifyIcon1.Text = "notifyIcon1";
            notifyIcon1.Visible = true;
            notifyIcon1.MouseDoubleClick += Systrayping_MouseDoubleClick;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 63);
            label2.Name = "label2";
            label2.Size = new Size(49, 25);
            label2.TabIndex = 6;
            label2.Text = "grün";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 100);
            label3.Name = "label3";
            label3.Size = new Size(47, 25);
            label3.TabIndex = 7;
            label3.Text = "gelb";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 133);
            label4.Name = "label4";
            label4.Size = new Size(35, 25);
            label4.TabIndex = 8;
            label4.Text = "rot";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 203);
            label5.Name = "label5";
            label5.Size = new Size(27, 25);
            label5.TabIndex = 9;
            label5.Text = "IP";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(12, 246);
            label6.Name = "label6";
            label6.Size = new Size(62, 25);
            label6.TabIndex = 10;
            label6.Text = "Latenz";
            // 
            // PingMin
            // 
            PingMin.Location = new Point(94, 163);
            PingMin.Name = "PingMin";
            PingMin.Size = new Size(180, 31);
            PingMin.TabIndex = 11;
            PingMin.Value = new decimal(new int[] { 60, 0, 0, 0 });
            PingMin.ValueChanged += PingSec_ValueChanged;
            // 
            // label_PingMin
            // 
            label_PingMin.AutoSize = true;
            label_PingMin.Location = new Point(12, 165);
            label_PingMin.Name = "label_PingMin";
            label_PingMin.Size = new Size(77, 25);
            label_PingMin.TabIndex = 12;
            label_PingMin.Text = "PingMin";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(301, 292);
            Controls.Add(label_PingMin);
            Controls.Add(PingMin);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(IPAddress);
            Controls.Add(numericUpDown3);
            Controls.Add(numericUpDown2);
            Controls.Add(numericUpDown1);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown3).EndInit();
            ((System.ComponentModel.ISupportInitialize)PingMin).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private NumericUpDown numericUpDown1;
        private NumericUpDown numericUpDown2;
        private NumericUpDown numericUpDown3;
        private ComboBox IPAddress;
        private Label label1;
        private NotifyIcon notifyIcon1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private NumericUpDown PingMin;
        private Label label_PingMin;
    }
}
