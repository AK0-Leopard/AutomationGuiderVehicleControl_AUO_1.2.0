namespace com.mirle.ibg3k0.bc.winform.UI
{
    partial class ReserveSectionInfoForm
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.num_vh_angle = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.btn_resetReservedSectionByVh = new System.Windows.Forms.Button();
            this.txt_y = new System.Windows.Forms.TextBox();
            this.txt_x = new System.Windows.Forms.TextBox();
            this.btn_set_vh_by_axis = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.cmb_vh_fork_dir = new System.Windows.Forms.ComboBox();
            this.cmb_vh_sensor_dir = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cmb_fork_dir = new System.Windows.Forms.ComboBox();
            this.cmb_sensor_dir = new System.Windows.Forms.ComboBox();
            this.btn_reset_reserve_all = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.cmb_reserve_section = new System.Windows.Forms.ComboBox();
            this.btn_reserve_section = new System.Windows.Forms.Button();
            this.btn_set_vh = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cmb_adr_id = new System.Windows.Forms.ComboBox();
            this.cmb_vh_ids = new System.Windows.Forms.ComboBox();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.uctlReserveSectionView1 = new com.mirle.ibg3k0.bc.winform.UI.Components.WPFComponents.uctlReserveSectionView();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_vh_angle)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 88.15052F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.84948F));
            this.tableLayoutPanel1.Controls.Add(this.panel1, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.elementHost1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1249, 558);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.num_vh_angle);
            this.panel1.Controls.Add(this.label10);
            this.panel1.Controls.Add(this.btn_resetReservedSectionByVh);
            this.panel1.Controls.Add(this.txt_y);
            this.panel1.Controls.Add(this.txt_x);
            this.panel1.Controls.Add(this.btn_set_vh_by_axis);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.label9);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.cmb_vh_fork_dir);
            this.panel1.Controls.Add(this.cmb_vh_sensor_dir);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.cmb_fork_dir);
            this.panel1.Controls.Add(this.cmb_sensor_dir);
            this.panel1.Controls.Add(this.btn_reset_reserve_all);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.cmb_reserve_section);
            this.panel1.Controls.Add(this.btn_reserve_section);
            this.panel1.Controls.Add(this.btn_set_vh);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.cmb_adr_id);
            this.panel1.Controls.Add(this.cmb_vh_ids);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(1103, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(143, 552);
            this.panel1.TabIndex = 0;
            // 
            // num_vh_angle
            // 
            this.num_vh_angle.Location = new System.Drawing.Point(12, 335);
            this.num_vh_angle.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
            this.num_vh_angle.Minimum = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
            this.num_vh_angle.Name = "num_vh_angle";
            this.num_vh_angle.Size = new System.Drawing.Size(120, 22);
            this.num_vh_angle.TabIndex = 26;
            this.num_vh_angle.Visible = false;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(11, 322);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(33, 12);
            this.label10.TabIndex = 25;
            this.label10.Text = "Angle";
            this.label10.Visible = false;
            // 
            // btn_resetReservedSectionByVh
            // 
            this.btn_resetReservedSectionByVh.ForeColor = System.Drawing.Color.Red;
            this.btn_resetReservedSectionByVh.Location = new System.Drawing.Point(11, 80);
            this.btn_resetReservedSectionByVh.Name = "btn_resetReservedSectionByVh";
            this.btn_resetReservedSectionByVh.Size = new System.Drawing.Size(121, 23);
            this.btn_resetReservedSectionByVh.TabIndex = 24;
            this.btn_resetReservedSectionByVh.Text = "Reser Reserve By Vh";
            this.btn_resetReservedSectionByVh.UseVisualStyleBackColor = true;
            this.btn_resetReservedSectionByVh.Click += new System.EventHandler(this.btn_resetReservedSectionByVh_Click);
            // 
            // txt_y
            // 
            this.txt_y.Location = new System.Drawing.Point(11, 297);
            this.txt_y.Name = "txt_y";
            this.txt_y.Size = new System.Drawing.Size(121, 22);
            this.txt_y.TabIndex = 23;
            this.txt_y.Visible = false;
            // 
            // txt_x
            // 
            this.txt_x.Location = new System.Drawing.Point(11, 259);
            this.txt_x.Name = "txt_x";
            this.txt_x.Size = new System.Drawing.Size(121, 22);
            this.txt_x.TabIndex = 22;
            this.txt_x.Visible = false;
            // 
            // btn_set_vh_by_axis
            // 
            this.btn_set_vh_by_axis.Location = new System.Drawing.Point(11, 358);
            this.btn_set_vh_by_axis.Name = "btn_set_vh_by_axis";
            this.btn_set_vh_by_axis.Size = new System.Drawing.Size(121, 23);
            this.btn_set_vh_by_axis.TabIndex = 21;
            this.btn_set_vh_by_axis.Text = "Set";
            this.btn_set_vh_by_axis.UseVisualStyleBackColor = true;
            this.btn_set_vh_by_axis.Visible = false;
            this.btn_set_vh_by_axis.Click += new System.EventHandler(this.btn_set_vh_by_axis_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(11, 246);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(13, 12);
            this.label8.TabIndex = 20;
            this.label8.Text = "X";
            this.label8.Visible = false;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(11, 284);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(13, 12);
            this.label9.TabIndex = 19;
            this.label9.Text = "Y";
            this.label9.Visible = false;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(11, 145);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(54, 12);
            this.label6.TabIndex = 16;
            this.label6.Text = "Sensor Dir";
            this.label6.Visible = false;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(11, 183);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(45, 12);
            this.label7.TabIndex = 15;
            this.label7.Text = "Fork Dir";
            this.label7.Visible = false;
            // 
            // cmb_vh_fork_dir
            // 
            this.cmb_vh_fork_dir.FormattingEnabled = true;
            this.cmb_vh_fork_dir.Location = new System.Drawing.Point(11, 198);
            this.cmb_vh_fork_dir.Name = "cmb_vh_fork_dir";
            this.cmb_vh_fork_dir.Size = new System.Drawing.Size(121, 20);
            this.cmb_vh_fork_dir.TabIndex = 14;
            this.cmb_vh_fork_dir.Visible = false;
            // 
            // cmb_vh_sensor_dir
            // 
            this.cmb_vh_sensor_dir.FormattingEnabled = true;
            this.cmb_vh_sensor_dir.Location = new System.Drawing.Point(11, 160);
            this.cmb_vh_sensor_dir.Name = "cmb_vh_sensor_dir";
            this.cmb_vh_sensor_dir.Size = new System.Drawing.Size(121, 20);
            this.cmb_vh_sensor_dir.TabIndex = 13;
            this.cmb_vh_sensor_dir.Visible = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(11, 430);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(54, 12);
            this.label5.TabIndex = 12;
            this.label5.Text = "Sensor Dir";
            this.label5.Visible = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 468);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 12);
            this.label4.TabIndex = 11;
            this.label4.Text = "Fork Dir";
            this.label4.Visible = false;
            // 
            // cmb_fork_dir
            // 
            this.cmb_fork_dir.FormattingEnabled = true;
            this.cmb_fork_dir.Location = new System.Drawing.Point(11, 483);
            this.cmb_fork_dir.Name = "cmb_fork_dir";
            this.cmb_fork_dir.Size = new System.Drawing.Size(121, 20);
            this.cmb_fork_dir.TabIndex = 10;
            this.cmb_fork_dir.Visible = false;
            // 
            // cmb_sensor_dir
            // 
            this.cmb_sensor_dir.FormattingEnabled = true;
            this.cmb_sensor_dir.Location = new System.Drawing.Point(11, 445);
            this.cmb_sensor_dir.Name = "cmb_sensor_dir";
            this.cmb_sensor_dir.Size = new System.Drawing.Size(121, 20);
            this.cmb_sensor_dir.TabIndex = 9;
            this.cmb_sensor_dir.Visible = false;
            // 
            // btn_reset_reserve_all
            // 
            this.btn_reset_reserve_all.ForeColor = System.Drawing.Color.Red;
            this.btn_reset_reserve_all.Location = new System.Drawing.Point(11, 3);
            this.btn_reset_reserve_all.Name = "btn_reset_reserve_all";
            this.btn_reset_reserve_all.Size = new System.Drawing.Size(121, 23);
            this.btn_reset_reserve_all.TabIndex = 8;
            this.btn_reset_reserve_all.Text = "Reset Reserve All";
            this.btn_reset_reserve_all.UseVisualStyleBackColor = true;
            this.btn_reset_reserve_all.Click += new System.EventHandler(this.button2_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 384);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 12);
            this.label3.TabIndex = 7;
            this.label3.Text = "Section ID";
            this.label3.Visible = false;
            // 
            // cmb_reserve_section
            // 
            this.cmb_reserve_section.FormattingEnabled = true;
            this.cmb_reserve_section.Location = new System.Drawing.Point(15, 399);
            this.cmb_reserve_section.Name = "cmb_reserve_section";
            this.cmb_reserve_section.Size = new System.Drawing.Size(121, 20);
            this.cmb_reserve_section.TabIndex = 6;
            this.cmb_reserve_section.Visible = false;
            // 
            // btn_reserve_section
            // 
            this.btn_reserve_section.Location = new System.Drawing.Point(13, 514);
            this.btn_reserve_section.Name = "btn_reserve_section";
            this.btn_reserve_section.Size = new System.Drawing.Size(121, 23);
            this.btn_reserve_section.TabIndex = 5;
            this.btn_reserve_section.Text = "Reserve Sec ID";
            this.btn_reserve_section.UseVisualStyleBackColor = true;
            this.btn_reserve_section.Visible = false;
            this.btn_reserve_section.Click += new System.EventHandler(this.btn_reserve_section_Click);
            // 
            // btn_set_vh
            // 
            this.btn_set_vh.Location = new System.Drawing.Point(11, 224);
            this.btn_set_vh.Name = "btn_set_vh";
            this.btn_set_vh.Size = new System.Drawing.Size(121, 23);
            this.btn_set_vh.TabIndex = 4;
            this.btn_set_vh.Text = "Set";
            this.btn_set_vh.UseVisualStyleBackColor = true;
            this.btn_set_vh.Visible = false;
            this.btn_set_vh.Click += new System.EventHandler(this.btn_set_vh_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 105);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "ADR ID";
            this.label2.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "VH ID";
            // 
            // cmb_adr_id
            // 
            this.cmb_adr_id.FormattingEnabled = true;
            this.cmb_adr_id.Location = new System.Drawing.Point(13, 120);
            this.cmb_adr_id.Name = "cmb_adr_id";
            this.cmb_adr_id.Size = new System.Drawing.Size(121, 20);
            this.cmb_adr_id.TabIndex = 1;
            this.cmb_adr_id.Visible = false;
            // 
            // cmb_vh_ids
            // 
            this.cmb_vh_ids.FormattingEnabled = true;
            this.cmb_vh_ids.Location = new System.Drawing.Point(11, 54);
            this.cmb_vh_ids.Name = "cmb_vh_ids";
            this.cmb_vh_ids.Size = new System.Drawing.Size(121, 20);
            this.cmb_vh_ids.TabIndex = 0;
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.Location = new System.Drawing.Point(3, 3);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(1094, 552);
            this.elementHost1.TabIndex = 1;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.uctlReserveSectionView1;
            // 
            // ReserveSectionInfoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1249, 558);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ReserveSectionInfoForm";
            this.Text = "ReserveSectionInfo";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ReserveSectionInfoForm_FormClosed);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_vh_angle)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private Components.WPFComponents.uctlReserveSectionView uctlReserveSectionView1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmb_adr_id;
        private System.Windows.Forms.ComboBox cmb_vh_ids;
        private System.Windows.Forms.Button btn_set_vh;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cmb_reserve_section;
        private System.Windows.Forms.Button btn_reserve_section;
        private System.Windows.Forms.Button btn_reset_reserve_all;
        private System.Windows.Forms.ComboBox cmb_sensor_dir;
        private System.Windows.Forms.ComboBox cmb_fork_dir;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cmb_vh_fork_dir;
        private System.Windows.Forms.ComboBox cmb_vh_sensor_dir;
        private System.Windows.Forms.TextBox txt_y;
        private System.Windows.Forms.TextBox txt_x;
        private System.Windows.Forms.Button btn_set_vh_by_axis;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btn_resetReservedSectionByVh;
        private System.Windows.Forms.NumericUpDown num_vh_angle;
        private System.Windows.Forms.Label label10;
    }
}