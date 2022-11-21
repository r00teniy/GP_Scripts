
namespace GP_scripts
{
    partial class hCheckForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(hCheckForm));
            this.bSelfInt = new System.Windows.Forms.Button();
            this.bHatInt = new System.Windows.Forms.Button();
            this.errLabel = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.clearButton = new System.Windows.Forms.Button();
            this.bAddCheck = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // bSelfInt
            // 
            resources.ApplyResources(this.bSelfInt, "bSelfInt");
            this.bSelfInt.Name = "bSelfInt";
            this.bSelfInt.UseVisualStyleBackColor = true;
            this.bSelfInt.Click += new System.EventHandler(this.bSelfInt_Click);
            // 
            // bHatInt
            // 
            resources.ApplyResources(this.bHatInt, "bHatInt");
            this.bHatInt.Name = "bHatInt";
            this.bHatInt.UseVisualStyleBackColor = true;
            this.bHatInt.Click += new System.EventHandler(this.bHatInt_Click);
            // 
            // errLabel
            // 
            resources.ApplyResources(this.errLabel, "errLabel");
            this.errLabel.ForeColor = System.Drawing.Color.Red;
            this.errLabel.Name = "errLabel";
            // 
            // closeButton
            // 
            resources.ApplyResources(this.closeButton, "closeButton");
            this.closeButton.Name = "closeButton";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // clearButton
            // 
            resources.ApplyResources(this.clearButton, "clearButton");
            this.clearButton.Name = "clearButton";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.clearButton_Click);
            // 
            // bAddCheck
            // 
            resources.ApplyResources(this.bAddCheck, "bAddCheck");
            this.bAddCheck.Name = "bAddCheck";
            this.bAddCheck.UseVisualStyleBackColor = true;
            this.bAddCheck.Click += new System.EventHandler(this.bAddCheck_Click);
            // 
            // hCheckForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.Controls.Add(this.bAddCheck);
            this.Controls.Add(this.clearButton);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.errLabel);
            this.Controls.Add(this.bHatInt);
            this.Controls.Add(this.bSelfInt);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.Name = "hCheckForm";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bSelfInt;
        private System.Windows.Forms.Button bHatInt;
        private System.Windows.Forms.Label errLabel;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.Button bAddCheck;
    }
}