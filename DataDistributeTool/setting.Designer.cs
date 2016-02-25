namespace DataCheckToolAuxiliary
{
    partial class setting
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
            this.label1 = new System.Windows.Forms.Label();
            this.sourceDbName_txt = new System.Windows.Forms.TextBox();
            this.browse_btn = new System.Windows.Forms.Button();
            this.bufferDistance_txt = new System.Windows.Forms.TextBox();
            this.btnExit = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "数据源路径：";
            // 
            // sourceDbName_txt
            // 
            this.sourceDbName_txt.Location = new System.Drawing.Point(96, 23);
            this.sourceDbName_txt.Name = "sourceDbName_txt";
            this.sourceDbName_txt.Size = new System.Drawing.Size(157, 21);
            this.sourceDbName_txt.TabIndex = 1;
            // 
            // browse_btn
            // 
            this.browse_btn.Location = new System.Drawing.Point(276, 21);
            this.browse_btn.Name = "browse_btn";
            this.browse_btn.Size = new System.Drawing.Size(75, 23);
            this.browse_btn.TabIndex = 2;
            this.browse_btn.Text = "浏览...";
            this.browse_btn.UseVisualStyleBackColor = true;
            this.browse_btn.Click += new System.EventHandler(this.browse_btn_Click);
            // 
            // bufferDistance_txt
            // 
            this.bufferDistance_txt.Location = new System.Drawing.Point(96, 65);
            this.bufferDistance_txt.Name = "bufferDistance_txt";
            this.bufferDistance_txt.Size = new System.Drawing.Size(129, 21);
            this.bufferDistance_txt.TabIndex = 4;
            this.bufferDistance_txt.Text = "0";
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(276, 65);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 23);
            this.btnExit.TabIndex = 5;
            this.btnExit.Text = "确定";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 65);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 12);
            this.label2.TabIndex = 6;
            this.label2.Text = "Buffer半径：";
            // 
            // setting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(370, 106);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.bufferDistance_txt);
            this.Controls.Add(this.browse_btn);
            this.Controls.Add(this.sourceDbName_txt);
            this.Controls.Add(this.label1);
            this.Name = "setting";
            this.Text = "数据分割参数设置";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.setting_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.setting_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox sourceDbName_txt;
        private System.Windows.Forms.Button browse_btn;
        private System.Windows.Forms.TextBox bufferDistance_txt;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Label label2;
    }
}