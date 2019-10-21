namespace CoreControllers
{
    partial class ByteMask
    {
        /// <summary> 
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary> 
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.b0 = new System.Windows.Forms.CheckBox();
            this.b1 = new System.Windows.Forms.CheckBox();
            this.b2 = new System.Windows.Forms.CheckBox();
            this.b3 = new System.Windows.Forms.CheckBox();
            this.b4 = new System.Windows.Forms.CheckBox();
            this.b5 = new System.Windows.Forms.CheckBox();
            this.b6 = new System.Windows.Forms.CheckBox();
            this.b7 = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // b0
            // 
            this.b0.AutoSize = true;
            this.b0.Location = new System.Drawing.Point(129, 3);
            this.b0.Name = "b0";
            this.b0.Size = new System.Drawing.Size(15, 14);
            this.b0.TabIndex = 0;
            this.b0.UseVisualStyleBackColor = true;
            this.b0.CheckedChanged += new System.EventHandler(this.b0_CheckedChanged);
            // 
            // b1
            // 
            this.b1.AutoSize = true;
            this.b1.Location = new System.Drawing.Point(111, 3);
            this.b1.Name = "b1";
            this.b1.Size = new System.Drawing.Size(15, 14);
            this.b1.TabIndex = 1;
            this.b1.UseVisualStyleBackColor = true;
            this.b1.CheckedChanged += new System.EventHandler(this.b1_CheckedChanged);
            // 
            // b2
            // 
            this.b2.AutoSize = true;
            this.b2.Location = new System.Drawing.Point(93, 3);
            this.b2.Name = "b2";
            this.b2.Size = new System.Drawing.Size(15, 14);
            this.b2.TabIndex = 2;
            this.b2.UseVisualStyleBackColor = true;
            this.b2.CheckedChanged += new System.EventHandler(this.b2_CheckedChanged);
            // 
            // b3
            // 
            this.b3.AutoSize = true;
            this.b3.Location = new System.Drawing.Point(75, 3);
            this.b3.Name = "b3";
            this.b3.Size = new System.Drawing.Size(15, 14);
            this.b3.TabIndex = 3;
            this.b3.UseVisualStyleBackColor = true;
            this.b3.CheckedChanged += new System.EventHandler(this.b3_CheckedChanged);
            // 
            // b4
            // 
            this.b4.AutoSize = true;
            this.b4.Location = new System.Drawing.Point(57, 3);
            this.b4.Name = "b4";
            this.b4.Size = new System.Drawing.Size(15, 14);
            this.b4.TabIndex = 4;
            this.b4.UseVisualStyleBackColor = true;
            this.b4.CheckedChanged += new System.EventHandler(this.b4_CheckedChanged);
            // 
            // b5
            // 
            this.b5.AutoSize = true;
            this.b5.Location = new System.Drawing.Point(39, 3);
            this.b5.Name = "b5";
            this.b5.Size = new System.Drawing.Size(15, 14);
            this.b5.TabIndex = 5;
            this.b5.UseVisualStyleBackColor = true;
            this.b5.CheckedChanged += new System.EventHandler(this.b5_CheckedChanged);
            // 
            // b6
            // 
            this.b6.AutoSize = true;
            this.b6.Location = new System.Drawing.Point(21, 3);
            this.b6.Name = "b6";
            this.b6.Size = new System.Drawing.Size(15, 14);
            this.b6.TabIndex = 6;
            this.b6.UseVisualStyleBackColor = true;
            this.b6.CheckedChanged += new System.EventHandler(this.b6_CheckedChanged);
            // 
            // b7
            // 
            this.b7.AutoSize = true;
            this.b7.Location = new System.Drawing.Point(3, 3);
            this.b7.Name = "b7";
            this.b7.Size = new System.Drawing.Size(15, 14);
            this.b7.TabIndex = 7;
            this.b7.UseVisualStyleBackColor = true;
            this.b7.CheckedChanged += new System.EventHandler(this.b7_CheckedChanged);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(4, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(140, 16);
            this.label1.TabIndex = 8;
            this.label1.Text = "Двоичное: 00000000";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(4, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(139, 18);
            this.label2.TabIndex = 9;
            this.label2.Text = "Десятичное: 0";
            // 
            // ByteMask
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.b7);
            this.Controls.Add(this.b6);
            this.Controls.Add(this.b5);
            this.Controls.Add(this.b4);
            this.Controls.Add(this.b3);
            this.Controls.Add(this.b2);
            this.Controls.Add(this.b1);
            this.Controls.Add(this.b0);
            this.Name = "ByteMask";
            this.Size = new System.Drawing.Size(146, 68);
            this.Resize += new System.EventHandler(this.ByteMask_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox b0;
        private System.Windows.Forms.CheckBox b1;
        private System.Windows.Forms.CheckBox b2;
        private System.Windows.Forms.CheckBox b3;
        private System.Windows.Forms.CheckBox b4;
        private System.Windows.Forms.CheckBox b5;
        private System.Windows.Forms.CheckBox b6;
        private System.Windows.Forms.CheckBox b7;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}
