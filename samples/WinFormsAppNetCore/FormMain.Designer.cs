namespace WinFormsAppNetCore
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            labelDate = new System.Windows.Forms.Label();
            labelTime = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // labelDate
            // 
            labelDate.Dock = System.Windows.Forms.DockStyle.Top;
            labelDate.Font = new System.Drawing.Font("Segoe UI", 64.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)204));
            labelDate.Location = new System.Drawing.Point(0, 0);
            labelDate.Name = "labelDate";
            labelDate.Size = new System.Drawing.Size(1000, 403);
            labelDate.TabIndex = 0;
            labelDate.Text = "Date";
            labelDate.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // labelTime
            // 
            labelTime.Dock = System.Windows.Forms.DockStyle.Fill;
            labelTime.Font = new System.Drawing.Font("Segoe UI", 127.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)204));
            labelTime.Location = new System.Drawing.Point(0, 403);
            labelTime.Name = "labelTime";
            labelTime.Size = new System.Drawing.Size(1000, 197);
            labelTime.TabIndex = 1;
            labelTime.Text = "Time";
            labelTime.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1000, 600);
            Controls.Add(labelTime);
            Controls.Add(labelDate);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = ((System.Drawing.Icon)resources.GetObject("$this.Icon"));
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Text = "Clock";
            TransparencyKey = System.Drawing.Color.Transparent;
            WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ResumeLayout(false);
        }

        private System.Windows.Forms.Label labelDate;
        private System.Windows.Forms.Label labelTime;

        #endregion
    }
}