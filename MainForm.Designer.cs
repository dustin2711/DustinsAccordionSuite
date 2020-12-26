namespace CreateSheetsFromVideo
{
    partial class MainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.buttonPlay = new System.Windows.Forms.Button();
            this.buttonNextFrame = new System.Windows.Forms.Button();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.textBoxTonesActiveLeft = new System.Windows.Forms.TextBox();
            this.textBoxTonesPastLeft = new System.Windows.Forms.TextBox();
            this.buttonInit = new System.Windows.Forms.Button();
            this.buttonClearNotesActive = new System.Windows.Forms.Button();
            this.buttonClearNotesPast = new System.Windows.Forms.Button();
            this.checkBoxRealtime = new System.Windows.Forms.CheckBox();
            this.textBoxTonesRightActive = new System.Windows.Forms.TextBox();
            this.textBoxTonesRight = new System.Windows.Forms.TextBox();
            this.checkBoxShowVisuals = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonPreviousFrame = new System.Windows.Forms.Button();
            this.textBoxJumpTo = new System.Windows.Forms.TextBox();
            this.labelJumpTo = new System.Windows.Forms.Label();
            this.textBoxResetTime = new System.Windows.Forms.TextBox();
            this.textBoxBeatTimes = new System.Windows.Forms.TextBox();
            this.buttonClearBeatTimes = new System.Windows.Forms.Button();
            this.bindingNavigator1 = new System.Windows.Forms.BindingNavigator(this.components);
            this.bindingNavigatorAddNewItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorCountItem = new System.Windows.Forms.ToolStripLabel();
            this.bindingNavigatorDeleteItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorMoveFirstItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorMovePreviousItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.bindingNavigatorPositionItem = new System.Windows.Forms.ToolStripTextBox();
            this.bindingNavigatorSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.bindingNavigatorMoveNextItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorMoveLastItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.mediaPlayer = new AxWMPLib.AxWindowsMediaPlayer();
            this.pictureBoxNotes = new System.Windows.Forms.PictureBox();
            this.labelActiveTones = new System.Windows.Forms.Label();
            this.labelPastTones = new System.Windows.Forms.Label();
            this.labelLog = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingNavigator1)).BeginInit();
            this.bindingNavigator1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mediaPlayer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxNotes)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.Location = new System.Drawing.Point(1282, 28);
            this.pictureBox.MaximumSize = new System.Drawing.Size(960, 562);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(640, 360);
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            // 
            // buttonPlay
            // 
            this.buttonPlay.Location = new System.Drawing.Point(195, 25);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(87, 32);
            this.buttonPlay.TabIndex = 1;
            this.buttonPlay.Text = "Play";
            this.buttonPlay.UseVisualStyleBackColor = true;
            // 
            // buttonNextFrame
            // 
            this.buttonNextFrame.Location = new System.Drawing.Point(288, 24);
            this.buttonNextFrame.Name = "buttonNextFrame";
            this.buttonNextFrame.Size = new System.Drawing.Size(88, 32);
            this.buttonNextFrame.TabIndex = 2;
            this.buttonNextFrame.Text = "Next frame";
            this.buttonNextFrame.UseVisualStyleBackColor = true;
            // 
            // textBoxLog
            // 
            this.textBoxLog.Location = new System.Drawing.Point(964, 356);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.Size = new System.Drawing.Size(301, 177);
            this.textBoxLog.TabIndex = 3;
            // 
            // textBoxTonesActiveLeft
            // 
            this.textBoxTonesActiveLeft.Location = new System.Drawing.Point(964, 27);
            this.textBoxTonesActiveLeft.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxTonesActiveLeft.Multiline = true;
            this.textBoxTonesActiveLeft.Name = "textBoxTonesActiveLeft";
            this.textBoxTonesActiveLeft.Size = new System.Drawing.Size(145, 91);
            this.textBoxTonesActiveLeft.TabIndex = 6;
            // 
            // textBoxTonesPastLeft
            // 
            this.textBoxTonesPastLeft.Location = new System.Drawing.Point(964, 144);
            this.textBoxTonesPastLeft.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxTonesPastLeft.Multiline = true;
            this.textBoxTonesPastLeft.Name = "textBoxTonesPastLeft";
            this.textBoxTonesPastLeft.Size = new System.Drawing.Size(145, 191);
            this.textBoxTonesPastLeft.TabIndex = 7;
            // 
            // buttonInit
            // 
            this.buttonInit.Location = new System.Drawing.Point(8, 25);
            this.buttonInit.Name = "buttonInit";
            this.buttonInit.Size = new System.Drawing.Size(87, 32);
            this.buttonInit.TabIndex = 8;
            this.buttonInit.Text = "Reset";
            this.buttonInit.UseVisualStyleBackColor = true;
            // 
            // buttonClearNotesActive
            // 
            this.buttonClearNotesActive.Location = new System.Drawing.Point(1074, 3);
            this.buttonClearNotesActive.Name = "buttonClearNotesActive";
            this.buttonClearNotesActive.Size = new System.Drawing.Size(82, 22);
            this.buttonClearNotesActive.TabIndex = 9;
            this.buttonClearNotesActive.Text = "Clear";
            this.buttonClearNotesActive.UseVisualStyleBackColor = true;
            // 
            // buttonClearNotesPast
            // 
            this.buttonClearNotesPast.Location = new System.Drawing.Point(964, 539);
            this.buttonClearNotesPast.Name = "buttonClearNotesPast";
            this.buttonClearNotesPast.Size = new System.Drawing.Size(301, 23);
            this.buttonClearNotesPast.TabIndex = 10;
            this.buttonClearNotesPast.Text = "Clear";
            this.buttonClearNotesPast.UseVisualStyleBackColor = true;
            // 
            // checkBoxRealtime
            // 
            this.checkBoxRealtime.AutoSize = true;
            this.checkBoxRealtime.Location = new System.Drawing.Point(380, 26);
            this.checkBoxRealtime.Margin = new System.Windows.Forms.Padding(2);
            this.checkBoxRealtime.Name = "checkBoxRealtime";
            this.checkBoxRealtime.Size = new System.Drawing.Size(71, 17);
            this.checkBoxRealtime.TabIndex = 11;
            this.checkBoxRealtime.Text = "RealTime";
            this.checkBoxRealtime.UseVisualStyleBackColor = true;
            // 
            // textBoxTonesRightActive
            // 
            this.textBoxTonesRightActive.Location = new System.Drawing.Point(1122, 27);
            this.textBoxTonesRightActive.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxTonesRightActive.Multiline = true;
            this.textBoxTonesRightActive.Name = "textBoxTonesRightActive";
            this.textBoxTonesRightActive.Size = new System.Drawing.Size(145, 91);
            this.textBoxTonesRightActive.TabIndex = 12;
            // 
            // textBoxTonesRight
            // 
            this.textBoxTonesRight.Location = new System.Drawing.Point(1122, 144);
            this.textBoxTonesRight.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxTonesRight.Multiline = true;
            this.textBoxTonesRight.Name = "textBoxTonesRight";
            this.textBoxTonesRight.Size = new System.Drawing.Size(145, 191);
            this.textBoxTonesRight.TabIndex = 13;
            // 
            // checkBoxShowVisuals
            // 
            this.checkBoxShowVisuals.AutoSize = true;
            this.checkBoxShowVisuals.Location = new System.Drawing.Point(380, 44);
            this.checkBoxShowVisuals.Margin = new System.Windows.Forms.Padding(2);
            this.checkBoxShowVisuals.Name = "checkBoxShowVisuals";
            this.checkBoxShowVisuals.Size = new System.Drawing.Size(88, 17);
            this.checkBoxShowVisuals.TabIndex = 14;
            this.checkBoxShowVisuals.Text = "Show visuals";
            this.checkBoxShowVisuals.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1025, 9);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(25, 13);
            this.label1.TabIndex = 15;
            this.label1.Text = "Left";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(1178, 7);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 16;
            this.label2.Text = "Right";
            // 
            // buttonPreviousFrame
            // 
            this.buttonPreviousFrame.Location = new System.Drawing.Point(101, 25);
            this.buttonPreviousFrame.Name = "buttonPreviousFrame";
            this.buttonPreviousFrame.Size = new System.Drawing.Size(88, 32);
            this.buttonPreviousFrame.TabIndex = 17;
            this.buttonPreviousFrame.Text = "Previous frame";
            this.buttonPreviousFrame.UseVisualStyleBackColor = true;
            // 
            // textBoxJumpTo
            // 
            this.textBoxJumpTo.Location = new System.Drawing.Point(487, 40);
            this.textBoxJumpTo.Name = "textBoxJumpTo";
            this.textBoxJumpTo.Size = new System.Drawing.Size(100, 20);
            this.textBoxJumpTo.TabIndex = 18;
            // 
            // labelJumpTo
            // 
            this.labelJumpTo.AutoSize = true;
            this.labelJumpTo.Location = new System.Drawing.Point(484, 24);
            this.labelJumpTo.Name = "labelJumpTo";
            this.labelJumpTo.Size = new System.Drawing.Size(44, 13);
            this.labelJumpTo.TabIndex = 19;
            this.labelJumpTo.Text = "Jump to";
            // 
            // textBoxResetTime
            // 
            this.textBoxResetTime.Location = new System.Drawing.Point(8, 63);
            this.textBoxResetTime.Name = "textBoxResetTime";
            this.textBoxResetTime.Size = new System.Drawing.Size(87, 20);
            this.textBoxResetTime.TabIndex = 20;
            // 
            // textBoxBeatTimes
            // 
            this.textBoxBeatTimes.Location = new System.Drawing.Point(787, 48);
            this.textBoxBeatTimes.Multiline = true;
            this.textBoxBeatTimes.Name = "textBoxBeatTimes";
            this.textBoxBeatTimes.Size = new System.Drawing.Size(153, 194);
            this.textBoxBeatTimes.TabIndex = 21;
            // 
            // buttonClearBeatTimes
            // 
            this.buttonClearBeatTimes.Location = new System.Drawing.Point(787, 24);
            this.buttonClearBeatTimes.Name = "buttonClearBeatTimes";
            this.buttonClearBeatTimes.Size = new System.Drawing.Size(153, 20);
            this.buttonClearBeatTimes.TabIndex = 22;
            this.buttonClearBeatTimes.Text = "Clear";
            this.buttonClearBeatTimes.UseVisualStyleBackColor = true;
            // 
            // bindingNavigator1
            // 
            this.bindingNavigator1.AddNewItem = this.bindingNavigatorAddNewItem;
            this.bindingNavigator1.CountItem = this.bindingNavigatorCountItem;
            this.bindingNavigator1.DeleteItem = this.bindingNavigatorDeleteItem;
            this.bindingNavigator1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bindingNavigatorMoveFirstItem,
            this.bindingNavigatorMovePreviousItem,
            this.bindingNavigatorSeparator,
            this.bindingNavigatorPositionItem,
            this.bindingNavigatorCountItem,
            this.bindingNavigatorSeparator1,
            this.bindingNavigatorMoveNextItem,
            this.bindingNavigatorMoveLastItem,
            this.bindingNavigatorSeparator2,
            this.bindingNavigatorAddNewItem,
            this.bindingNavigatorDeleteItem});
            this.bindingNavigator1.Location = new System.Drawing.Point(0, 0);
            this.bindingNavigator1.MoveFirstItem = this.bindingNavigatorMoveFirstItem;
            this.bindingNavigator1.MoveLastItem = this.bindingNavigatorMoveLastItem;
            this.bindingNavigator1.MoveNextItem = this.bindingNavigatorMoveNextItem;
            this.bindingNavigator1.MovePreviousItem = this.bindingNavigatorMovePreviousItem;
            this.bindingNavigator1.Name = "bindingNavigator1";
            this.bindingNavigator1.PositionItem = this.bindingNavigatorPositionItem;
            this.bindingNavigator1.Size = new System.Drawing.Size(2037, 25);
            this.bindingNavigator1.TabIndex = 23;
            this.bindingNavigator1.Text = "bindingNavigator1";
            // 
            // bindingNavigatorAddNewItem
            // 
            this.bindingNavigatorAddNewItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorAddNewItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorAddNewItem.Image")));
            this.bindingNavigatorAddNewItem.Name = "bindingNavigatorAddNewItem";
            this.bindingNavigatorAddNewItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorAddNewItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorAddNewItem.Text = "Add new";
            // 
            // bindingNavigatorCountItem
            // 
            this.bindingNavigatorCountItem.Name = "bindingNavigatorCountItem";
            this.bindingNavigatorCountItem.Size = new System.Drawing.Size(35, 22);
            this.bindingNavigatorCountItem.Text = "of {0}";
            this.bindingNavigatorCountItem.ToolTipText = "Total number of items";
            // 
            // bindingNavigatorDeleteItem
            // 
            this.bindingNavigatorDeleteItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorDeleteItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorDeleteItem.Image")));
            this.bindingNavigatorDeleteItem.Name = "bindingNavigatorDeleteItem";
            this.bindingNavigatorDeleteItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorDeleteItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorDeleteItem.Text = "Delete";
            // 
            // bindingNavigatorMoveFirstItem
            // 
            this.bindingNavigatorMoveFirstItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveFirstItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveFirstItem.Image")));
            this.bindingNavigatorMoveFirstItem.Name = "bindingNavigatorMoveFirstItem";
            this.bindingNavigatorMoveFirstItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveFirstItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMoveFirstItem.Text = "Move first";
            // 
            // bindingNavigatorMovePreviousItem
            // 
            this.bindingNavigatorMovePreviousItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMovePreviousItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMovePreviousItem.Image")));
            this.bindingNavigatorMovePreviousItem.Name = "bindingNavigatorMovePreviousItem";
            this.bindingNavigatorMovePreviousItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMovePreviousItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMovePreviousItem.Text = "Move previous";
            // 
            // bindingNavigatorSeparator
            // 
            this.bindingNavigatorSeparator.Name = "bindingNavigatorSeparator";
            this.bindingNavigatorSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // bindingNavigatorPositionItem
            // 
            this.bindingNavigatorPositionItem.AccessibleName = "Position";
            this.bindingNavigatorPositionItem.AutoSize = false;
            this.bindingNavigatorPositionItem.Name = "bindingNavigatorPositionItem";
            this.bindingNavigatorPositionItem.Size = new System.Drawing.Size(50, 23);
            this.bindingNavigatorPositionItem.Text = "0";
            this.bindingNavigatorPositionItem.ToolTipText = "Current position";
            // 
            // bindingNavigatorSeparator1
            // 
            this.bindingNavigatorSeparator1.Name = "bindingNavigatorSeparator1";
            this.bindingNavigatorSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // bindingNavigatorMoveNextItem
            // 
            this.bindingNavigatorMoveNextItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveNextItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveNextItem.Image")));
            this.bindingNavigatorMoveNextItem.Name = "bindingNavigatorMoveNextItem";
            this.bindingNavigatorMoveNextItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveNextItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMoveNextItem.Text = "Move next";
            // 
            // bindingNavigatorMoveLastItem
            // 
            this.bindingNavigatorMoveLastItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveLastItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveLastItem.Image")));
            this.bindingNavigatorMoveLastItem.Name = "bindingNavigatorMoveLastItem";
            this.bindingNavigatorMoveLastItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveLastItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMoveLastItem.Text = "Move last";
            // 
            // bindingNavigatorSeparator2
            // 
            this.bindingNavigatorSeparator2.Name = "bindingNavigatorSeparator2";
            this.bindingNavigatorSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // mediaPlayer
            // 
            this.mediaPlayer.Enabled = true;
            this.mediaPlayer.Location = new System.Drawing.Point(0, 25);
            this.mediaPlayer.Name = "mediaPlayer";
            this.mediaPlayer.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("mediaPlayer.OcxState")));
            this.mediaPlayer.Size = new System.Drawing.Size(951, 537);
            this.mediaPlayer.TabIndex = 24;
            // 
            // pictureBoxNotes
            // 
            this.pictureBoxNotes.Location = new System.Drawing.Point(0, 558);
            this.pictureBoxNotes.Name = "pictureBoxNotes";
            this.pictureBoxNotes.Size = new System.Drawing.Size(1920, 201);
            this.pictureBoxNotes.TabIndex = 25;
            this.pictureBoxNotes.TabStop = false;
            // 
            // labelActiveTones
            // 
            this.labelActiveTones.AutoSize = true;
            this.labelActiveTones.Location = new System.Drawing.Point(1081, 7);
            this.labelActiveTones.Name = "labelActiveTones";
            this.labelActiveTones.Size = new System.Drawing.Size(66, 13);
            this.labelActiveTones.TabIndex = 26;
            this.labelActiveTones.Text = "Active tones";
            // 
            // labelPastTones
            // 
            this.labelPastTones.AutoSize = true;
            this.labelPastTones.Location = new System.Drawing.Point(1090, 129);
            this.labelPastTones.Name = "labelPastTones";
            this.labelPastTones.Size = new System.Drawing.Size(57, 13);
            this.labelPastTones.TabIndex = 27;
            this.labelPastTones.Text = "Past tones";
            // 
            // labelLog
            // 
            this.labelLog.AutoSize = true;
            this.labelLog.Location = new System.Drawing.Point(1104, 340);
            this.labelLog.Name = "labelLog";
            this.labelLog.Size = new System.Drawing.Size(25, 13);
            this.labelLog.TabIndex = 28;
            this.labelLog.Text = "Log";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2037, 804);
            this.Controls.Add(this.labelLog);
            this.Controls.Add(this.labelPastTones);
            this.Controls.Add(this.labelActiveTones);
            this.Controls.Add(this.pictureBoxNotes);
            this.Controls.Add(this.bindingNavigator1);
            this.Controls.Add(this.buttonClearBeatTimes);
            this.Controls.Add(this.textBoxBeatTimes);
            this.Controls.Add(this.textBoxResetTime);
            this.Controls.Add(this.labelJumpTo);
            this.Controls.Add(this.textBoxJumpTo);
            this.Controls.Add(this.buttonPreviousFrame);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkBoxShowVisuals);
            this.Controls.Add(this.textBoxTonesRight);
            this.Controls.Add(this.textBoxTonesRightActive);
            this.Controls.Add(this.checkBoxRealtime);
            this.Controls.Add(this.buttonClearNotesPast);
            this.Controls.Add(this.buttonClearNotesActive);
            this.Controls.Add(this.buttonInit);
            this.Controls.Add(this.textBoxTonesPastLeft);
            this.Controls.Add(this.textBoxTonesActiveLeft);
            this.Controls.Add(this.textBoxLog);
            this.Controls.Add(this.buttonNextFrame);
            this.Controls.Add(this.buttonPlay);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.mediaPlayer);
            this.Name = "MainForm";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingNavigator1)).EndInit();
            this.bindingNavigator1.ResumeLayout(false);
            this.bindingNavigator1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mediaPlayer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxNotes)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Button buttonPlay;
        private System.Windows.Forms.Button buttonNextFrame;
        private System.Windows.Forms.TextBox textBoxLog;
        private System.Windows.Forms.TextBox textBoxTonesActiveLeft;
        private System.Windows.Forms.TextBox textBoxTonesPastLeft;
        private System.Windows.Forms.Button buttonInit;
        private System.Windows.Forms.Button buttonClearNotesActive;
        private System.Windows.Forms.Button buttonClearNotesPast;
        private System.Windows.Forms.CheckBox checkBoxRealtime;
        private System.Windows.Forms.TextBox textBoxTonesRightActive;
        private System.Windows.Forms.TextBox textBoxTonesRight;
        private System.Windows.Forms.CheckBox checkBoxShowVisuals;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonPreviousFrame;
        private System.Windows.Forms.TextBox textBoxJumpTo;
        private System.Windows.Forms.Label labelJumpTo;
        private System.Windows.Forms.TextBox textBoxResetTime;
        private System.Windows.Forms.TextBox textBoxBeatTimes;
        private System.Windows.Forms.Button buttonClearBeatTimes;
        private System.Windows.Forms.BindingNavigator bindingNavigator1;
        private System.Windows.Forms.ToolStripButton bindingNavigatorAddNewItem;
        private System.Windows.Forms.ToolStripLabel bindingNavigatorCountItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorDeleteItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMoveFirstItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMovePreviousItem;
        private System.Windows.Forms.ToolStripSeparator bindingNavigatorSeparator;
        private System.Windows.Forms.ToolStripTextBox bindingNavigatorPositionItem;
        private System.Windows.Forms.ToolStripSeparator bindingNavigatorSeparator1;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMoveNextItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMoveLastItem;
        private System.Windows.Forms.ToolStripSeparator bindingNavigatorSeparator2;
        private AxWMPLib.AxWindowsMediaPlayer mediaPlayer;
        private System.Windows.Forms.PictureBox pictureBoxNotes;
        private System.Windows.Forms.Label labelActiveTones;
        private System.Windows.Forms.Label labelPastTones;
        private System.Windows.Forms.Label labelLog;
    }
}

