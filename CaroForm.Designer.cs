namespace Caro.NET
{
    partial class CaroForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CaroForm));
            radPCvsPC = new RadioButton();
            radHumanPC = new RadioButton();
            radPCvsHuman = new RadioButton();
            radHuman = new RadioButton();
            gridCaro = new Label();
            lblMouse = new Label();
            picX = new PictureBox();
            picO = new PictureBox();
            label1 = new Label();
            label2 = new Label();
            btnSave = new Button();
            btnLoad = new Button();
            btnFirst = new Button();
            btnPrev = new Button();
            btnLast = new Button();
            btnNext = new Button();
            btnRestart = new Button();
            btnSuggest = new Button();
            btnStartStop = new Button();
            btnStop = new Button();
            radioButton1 = new RadioButton();
            radioButton2 = new RadioButton();
            radioButton3 = new RadioButton();
            radioButton4 = new RadioButton();
            panel1 = new Panel();
            panel2 = new Panel();
            ((System.ComponentModel.ISupportInitialize)picX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picO).BeginInit();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // radPCvsPC
            // 
            radPCvsPC.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            radPCvsPC.AutoSize = true;
            radPCvsPC.Location = new Point(13, 13);
            radPCvsPC.Name = "radPCvsPC";
            radPCvsPC.Size = new Size(108, 24);
            radPCvsPC.TabIndex = 1;
            radPCvsPC.Text = "PC (X) vs PC";
            radPCvsPC.UseVisualStyleBackColor = true;
            radPCvsPC.CheckedChanged += radPCvsPC_CheckedChanged;
            // 
            // radHumanPC
            // 
            radHumanPC.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            radHumanPC.AutoSize = true;
            radHumanPC.Checked = true;
            radHumanPC.Location = new Point(13, 47);
            radHumanPC.Name = "radHumanPC";
            radHumanPC.Size = new Size(139, 24);
            radHumanPC.TabIndex = 2;
            radHumanPC.TabStop = true;
            radHumanPC.Text = "Human (X) vs PC";
            radHumanPC.UseVisualStyleBackColor = true;
            radHumanPC.CheckedChanged += radHumanPC_CheckedChanged;
            // 
            // radPCvsHuman
            // 
            radPCvsHuman.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            radPCvsHuman.AutoSize = true;
            radPCvsHuman.Location = new Point(13, 81);
            radPCvsHuman.Name = "radPCvsHuman";
            radPCvsHuman.Size = new Size(139, 24);
            radPCvsHuman.TabIndex = 3;
            radPCvsHuman.Text = "PC (X) vs Human";
            radPCvsHuman.UseVisualStyleBackColor = true;
            radPCvsHuman.CheckedChanged += radPCvsHuman_CheckedChanged;
            // 
            // radHuman
            // 
            radHuman.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            radHuman.AutoSize = true;
            radHuman.Location = new Point(13, 115);
            radHuman.Name = "radHuman";
            radHuman.Size = new Size(170, 24);
            radHuman.TabIndex = 4;
            radHuman.Text = "Human (X) vs Human";
            radHuman.UseVisualStyleBackColor = true;
            radHuman.CheckedChanged += radHuman_CheckedChanged;
            // 
            // gridCaro
            // 
            gridCaro.BorderStyle = BorderStyle.FixedSingle;
            gridCaro.Location = new Point(12, 9);
            gridCaro.Name = "gridCaro";
            gridCaro.Size = new Size(1622, 915);
            gridCaro.TabIndex = 6;
            gridCaro.Paint += gridCaro_Paint;
            gridCaro.MouseClick += gridCaro_MouseClick;
            // 
            // lblMouse
            // 
            lblMouse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblMouse.AutoSize = true;
            lblMouse.Location = new Point(1653, 761);
            lblMouse.Name = "lblMouse";
            lblMouse.Size = new Size(110, 20);
            lblMouse.TabIndex = 7;
            lblMouse.Text = "Mouse. X:0 , Y:0";
            // 
            // picX
            // 
            picX.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            picX.Image = (Image)resources.GetObject("picX.Image");
            picX.Location = new Point(1653, 871);
            picX.Name = "picX";
            picX.Size = new Size(50, 50);
            picX.SizeMode = PictureBoxSizeMode.AutoSize;
            picX.TabIndex = 8;
            picX.TabStop = false;
            picX.UseWaitCursor = true;
            // 
            // picO
            // 
            picO.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            picO.Image = (Image)resources.GetObject("picO.Image");
            picO.Location = new Point(1738, 871);
            picO.Name = "picO";
            picO.Size = new Size(50, 50);
            picO.SizeMode = PictureBoxSizeMode.AutoSize;
            picO.TabIndex = 9;
            picO.TabStop = false;
            picO.UseWaitCursor = true;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Location = new Point(1653, 797);
            label1.Name = "label1";
            label1.Size = new Size(110, 20);
            label1.TabIndex = 10;
            label1.Text = "Mouse. X:0 , Y:0";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Location = new Point(1653, 833);
            label2.Name = "label2";
            label2.Size = new Size(135, 20);
            label2.TabIndex = 11;
            label2.Text = "Cell. COL:0 , ROW:0";
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSave.Image = Properties.Resources.save24x24;
            btnSave.ImageAlign = ContentAlignment.MiddleLeft;
            btnSave.Location = new Point(1662, 406);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(94, 32);
            btnSave.TabIndex = 12;
            btnSave.Text = "     Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnLoad
            // 
            btnLoad.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLoad.Image = Properties.Resources.load24x241;
            btnLoad.ImageAlign = ContentAlignment.MiddleLeft;
            btnLoad.Location = new Point(1782, 406);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(94, 32);
            btnLoad.TabIndex = 13;
            btnLoad.Text = "     Load";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // btnFirst
            // 
            btnFirst.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnFirst.Image = Properties.Resources.first24x24;
            btnFirst.Location = new Point(1662, 352);
            btnFirst.Name = "btnFirst";
            btnFirst.Size = new Size(38, 32);
            btnFirst.TabIndex = 14;
            btnFirst.UseVisualStyleBackColor = true;
            btnFirst.Click += btnFirst_Click;
            // 
            // btnPrev
            // 
            btnPrev.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPrev.Image = Properties.Resources.previous24x24;
            btnPrev.Location = new Point(1720, 352);
            btnPrev.Name = "btnPrev";
            btnPrev.Size = new Size(38, 32);
            btnPrev.TabIndex = 15;
            btnPrev.UseVisualStyleBackColor = true;
            btnPrev.Click += btnPrev_Click;
            // 
            // btnLast
            // 
            btnLast.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLast.Image = Properties.Resources.last24x24;
            btnLast.Location = new Point(1836, 352);
            btnLast.Name = "btnLast";
            btnLast.Size = new Size(38, 32);
            btnLast.TabIndex = 17;
            btnLast.UseVisualStyleBackColor = true;
            btnLast.Click += btnLast_Click;
            // 
            // btnNext
            // 
            btnNext.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNext.Image = Properties.Resources.next24x24;
            btnNext.Location = new Point(1778, 352);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(38, 32);
            btnNext.TabIndex = 16;
            btnNext.UseVisualStyleBackColor = true;
            btnNext.Click += btnNext_Click;
            // 
            // btnRestart
            // 
            btnRestart.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRestart.Image = Properties.Resources.update24x24;
            btnRestart.ImageAlign = ContentAlignment.MiddleLeft;
            btnRestart.Location = new Point(1662, 238);
            btnRestart.Name = "btnRestart";
            btnRestart.Size = new Size(94, 32);
            btnRestart.TabIndex = 0;
            btnRestart.Text = "Restart ";
            btnRestart.TextAlign = ContentAlignment.MiddleRight;
            btnRestart.UseVisualStyleBackColor = true;
            btnRestart.Click += btnRestart_Click;
            // 
            // btnSuggest
            // 
            btnSuggest.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSuggest.Image = Properties.Resources.play24x24;
            btnSuggest.ImageAlign = ContentAlignment.MiddleLeft;
            btnSuggest.Location = new Point(1662, 292);
            btnSuggest.Name = "btnSuggest";
            btnSuggest.Size = new Size(94, 32);
            btnSuggest.TabIndex = 18;
            btnSuggest.Text = "Suggest ";
            btnSuggest.TextAlign = ContentAlignment.MiddleRight;
            btnSuggest.UseVisualStyleBackColor = true;
            btnSuggest.Click += btnSuggest_Click;
            // 
            // btnStartStop
            // 
            btnStartStop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnStartStop.Image = Properties.Resources.computer;
            btnStartStop.ImageAlign = ContentAlignment.MiddleLeft;
            btnStartStop.Location = new Point(13, 20);
            btnStartStop.Name = "btnStartStop";
            btnStartStop.Size = new Size(94, 32);
            btnStartStop.TabIndex = 19;
            btnStartStop.Text = "   Start";
            btnStartStop.UseVisualStyleBackColor = true;
            btnStartStop.Click += btnStartStop_Click;
            // 
            // btnStop
            // 
            btnStop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnStop.Image = Properties.Resources.close24x24;
            btnStop.ImageAlign = ContentAlignment.MiddleLeft;
            btnStop.Location = new Point(1782, 238);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(94, 32);
            btnStop.TabIndex = 20;
            btnStop.Text = "   Stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // radioButton1
            // 
            radioButton1.AutoSize = true;
            radioButton1.Checked = true;
            radioButton1.Location = new Point(13, 62);
            radioButton1.Name = "radioButton1";
            radioButton1.Size = new Size(143, 24);
            radioButton1.TabIndex = 21;
            radioButton1.TabStop = true;
            radioButton1.Text = "PC (X) vs Remote";
            radioButton1.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            radioButton2.AutoSize = true;
            radioButton2.Location = new Point(13, 95);
            radioButton2.Name = "radioButton2";
            radioButton2.Size = new Size(143, 24);
            radioButton2.TabIndex = 22;
            radioButton2.Text = "Remote (X) vs PC";
            radioButton2.UseVisualStyleBackColor = true;
            // 
            // radioButton3
            // 
            radioButton3.AutoSize = true;
            radioButton3.Location = new Point(13, 128);
            radioButton3.Name = "radioButton3";
            radioButton3.Size = new Size(174, 24);
            radioButton3.TabIndex = 23;
            radioButton3.Text = "Human (X) vs Remote";
            radioButton3.UseVisualStyleBackColor = true;
            // 
            // radioButton4
            // 
            radioButton4.AutoSize = true;
            radioButton4.Location = new Point(13, 161);
            radioButton4.Name = "radioButton4";
            radioButton4.Size = new Size(174, 24);
            radioButton4.TabIndex = 24;
            radioButton4.Text = "Remote (X) vs Human";
            radioButton4.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panel1.Controls.Add(radPCvsPC);
            panel1.Controls.Add(radHumanPC);
            panel1.Controls.Add(radPCvsHuman);
            panel1.Controls.Add(radHuman);
            panel1.Location = new Point(1649, 42);
            panel1.Name = "panel1";
            panel1.Size = new Size(227, 158);
            panel1.TabIndex = 25;
            // 
            // panel2
            // 
            panel2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panel2.Controls.Add(radioButton4);
            panel2.Controls.Add(btnStartStop);
            panel2.Controls.Add(radioButton1);
            panel2.Controls.Add(radioButton3);
            panel2.Controls.Add(radioButton2);
            panel2.Location = new Point(1649, 460);
            panel2.Name = "panel2";
            panel2.Size = new Size(227, 203);
            panel2.TabIndex = 26;
            // 
            // CaroForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1892, 949);
            Controls.Add(btnStop);
            Controls.Add(btnSuggest);
            Controls.Add(btnLast);
            Controls.Add(btnNext);
            Controls.Add(btnPrev);
            Controls.Add(btnFirst);
            Controls.Add(btnLoad);
            Controls.Add(btnSave);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(picO);
            Controls.Add(picX);
            Controls.Add(lblMouse);
            Controls.Add(gridCaro);
            Controls.Add(btnRestart);
            Controls.Add(panel1);
            Controls.Add(panel2);
            Name = "CaroForm";
            Text = "Vietnamese Gomoku (aka Caro)";
            WindowState = FormWindowState.Maximized;
            FormClosing += CaroForm_FormClosing;
            Paint += CaroForm_Paint;
            ((System.ComponentModel.ISupportInitialize)picX).EndInit();
            ((System.ComponentModel.ISupportInitialize)picO).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private RadioButton radPCvsPC;
        private RadioButton radHumanPC;
        private RadioButton radPCvsHuman;
        private RadioButton radHuman;
        private Label gridCaro;
        private Label lblMouse;
        private PictureBox picX;
        private PictureBox picO;
        private Label label1;
        private Label label2;
        private Button btnSave;
        private Button btnLoad;
        private Button btnFirst;
        private Button btnPrev;
        private Button btnLast;
        private Button btnNext;
        private Button btnRestart;
        private Button btnSuggest;
        private Button btnStartStop;
        private Button btnStop;
        private RadioButton radioButton1;
        private RadioButton radioButton2;
        private RadioButton radioButton3;
        private RadioButton radioButton4;
        private Panel panel1;
        private Panel panel2;
    }
}
