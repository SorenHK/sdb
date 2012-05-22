namespace SDB.Viewer
{
    partial class MainForm
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
            this.tree = new Aga.Controls.Tree.TreeViewAdv();
            this.Id = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.Value = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.Identifier = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.SuspendLayout();
            // 
            // tree
            // 
            this.tree.BackColor = System.Drawing.SystemColors.Window;
            this.tree.DefaultToolTipProvider = null;
            this.tree.DragDropMarkColor = System.Drawing.Color.Black;
            this.tree.LineColor = System.Drawing.SystemColors.ControlDark;
            this.tree.LoadOnDemand = true;
            this.tree.Location = new System.Drawing.Point(13, 13);
            this.tree.Model = null;
            this.tree.Name = "tree";
            this.tree.NodeControls.Add(this.Id);
            this.tree.NodeControls.Add(this.Value);
            this.tree.NodeControls.Add(this.Identifier);
            this.tree.SelectedNode = null;
            this.tree.Size = new System.Drawing.Size(724, 524);
            this.tree.TabIndex = 0;
            this.tree.Text = "treeViewAdv1";
            // 
            // Id
            // 
            this.Id.DataPropertyName = "Id";
            this.Id.IncrementalSearchEnabled = true;
            this.Id.LeftMargin = 3;
            this.Id.ParentColumn = null;
            // 
            // Value
            // 
            this.Value.DataPropertyName = "Value";
            this.Value.IncrementalSearchEnabled = true;
            this.Value.LeftMargin = 3;
            this.Value.ParentColumn = null;
            // 
            // Identifier
            // 
            this.Identifier.DataPropertyName = "Identifier";
            this.Identifier.IncrementalSearchEnabled = true;
            this.Identifier.LeftMargin = 3;
            this.Identifier.ParentColumn = null;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(749, 549);
            this.Controls.Add(this.tree);
            this.Name = "MainForm";
            this.Text = "SDB Viewer";
            this.ResumeLayout(false);

        }

        #endregion

        private Aga.Controls.Tree.TreeViewAdv tree;
        private Aga.Controls.Tree.NodeControls.NodeTextBox Id;
        private Aga.Controls.Tree.NodeControls.NodeTextBox Value;
        private Aga.Controls.Tree.NodeControls.NodeTextBox Identifier;
    }
}

